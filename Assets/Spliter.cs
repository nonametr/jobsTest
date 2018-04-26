using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEditor;
using Unity.Jobs;
using Unity.Collections;

[ExecuteInEditMode]
public class Spliter : MonoBehaviour
{
	public enum PlaneSide
	{
		LEFT, RIGHT, MID
	}
	
	public struct SplitJob : IJobParallelFor
	{
		public Plane plane;
		public Matrix4x4 localToWorldMatrix;
		[ReadOnly]
		public NativeArray<Vector3> source_vertices;
		
		public NativeArray<byte> info_vertices;

		public void Execute(int i)
		{
			if (plane.GetDistanceToPoint(localToWorldMatrix * source_vertices[i]) > 0)
			{
				info_vertices[i] = (int)PlaneSide.RIGHT;
			}
			else
			{
				info_vertices[i] = (int)PlaneSide.LEFT;
			}
		}
	}

	public GameObject planeObj;
	public GameObject targetObj;
	
	private bool _jobPending = false;
	private SplitJob _splitJob;
	private JobHandle _splitJobHandle;
	
	void Update () {
		if (_jobPending)
		{
			Debug.Log("DATA READY!!!!!!!!!!");
			_jobPending = false;

			_splitJob.source_vertices.Dispose();
			_splitJob.info_vertices.Dispose();
		}
	}
	
	public void split()
	{
		Mesh mesh = targetObj.GetComponent<MeshFilter>().mesh;
		NativeArray<Vector3> source_vertices = new NativeArray<Vector3>(mesh.vertices, Allocator.Persistent);
		NativeArray<byte> info_vertices = new NativeArray<byte>(mesh.vertices.Length, Allocator.Persistent);
		
		Mesh planeMesh = planeObj.GetComponent<MeshFilter>().sharedMesh;
		Plane plane = new Plane(planeObj.transform.localToWorldMatrix * planeMesh.vertices[0], planeObj.transform.localToWorldMatrix * planeMesh.vertices[5], planeObj.transform.localToWorldMatrix * planeMesh.vertices[planeMesh.vertexCount-1]);

		_splitJob = new SplitJob()
		{
			plane = plane,
			localToWorldMatrix = targetObj.transform.localToWorldMatrix,
			source_vertices = source_vertices,
			info_vertices = info_vertices,
		};

		_splitJobHandle = _splitJob.Schedule(mesh.vertices.Length, 8192);
		_splitJobHandle.Complete();
		
		Color[] colors = new Color[targetObj.GetComponent<MeshFilter>().sharedMesh.vertexCount];
		for (int i = 0; i < info_vertices.Length; ++i)
		{
			if (info_vertices[i] == (byte)PlaneSide.LEFT)
			{
				colors[i] = Color.red;
			}
			else
			{
				colors[i] = Color.green;
			}
		}
		targetObj.GetComponent<MeshFilter>().sharedMesh.colors = colors;
		
		Dictionary<int, int> indexReMap = new Dictionary<int, int>();//<old,new>
		List<Vector3> rightVertices = new List<Vector3>();
		List<Vector3> rightNormals = new List<Vector3>();
		List<int> rightTriangles = new List<int>();
		List<Vector3> leftVertices = new List<Vector3>();
		List<Vector3> leftNormals = new List<Vector3>();
		List<int> leftTriangles = new List<int>();
		List<Vector3> midVertices = new List<Vector3>();
		List<Vector3> midNormals = new List<Vector3>();
		List<int> midTriangles = new List<int>();
		
		Func<int, PlaneSide, int> getIndx = (oldIndx, side) => { 
			int newIndx;
			if (!indexReMap.TryGetValue(oldIndx, out newIndx))
			{
				switch (side)
				{
					case PlaneSide.LEFT:
						leftVertices.Add(source_vertices[oldIndx]);
						leftNormals.Add(mesh.normals[oldIndx]);
						newIndx = leftVertices.Count - 1;
						break;
					case PlaneSide.RIGHT:
						rightVertices.Add(source_vertices[oldIndx]);
						rightNormals.Add(mesh.normals[oldIndx]);
						newIndx = rightVertices.Count - 1;
						break;
					case PlaneSide.MID:
						midVertices.Add(source_vertices[oldIndx]);
						midNormals.Add(mesh.normals[oldIndx]);
						newIndx = midVertices.Count - 1;
						break;
				}
				indexReMap.Add(oldIndx, newIndx);
			}

			return newIndx;
		};
		
		for (int i = 0; i < mesh.triangles.Length; i += 3)
		{
			int v0Indx = mesh.triangles[i + 0];
			int v1Indx = mesh.triangles[i + 1];
			int v2Indx = mesh.triangles[i + 2];

			PlaneSide v0Side = (PlaneSide)info_vertices[v0Indx];
			PlaneSide v1Side = (PlaneSide)info_vertices[v1Indx];
			PlaneSide v2Side = (PlaneSide)info_vertices[v2Indx];

			if (v0Side == v1Side && v0Side == v2Side)
			{
				if (v0Side == PlaneSide.RIGHT)
				{
					int newV0Indx = getIndx(v0Indx, PlaneSide.RIGHT);
					int newV1Indx = getIndx(v1Indx, PlaneSide.RIGHT);
					int newV2Indx = getIndx(v2Indx, PlaneSide.RIGHT);
					
					rightTriangles.Add(newV0Indx);
					rightTriangles.Add(newV1Indx);
					rightTriangles.Add(newV2Indx);
				}
				else
				{
					int newV0Indx = getIndx(v0Indx, PlaneSide.LEFT);
					int newV1Indx = getIndx(v1Indx, PlaneSide.LEFT);
					int newV2Indx = getIndx(v2Indx, PlaneSide.LEFT);
					
					leftTriangles.Add(newV0Indx);
					leftTriangles.Add(newV1Indx);
					leftTriangles.Add(newV2Indx);
				}
			}
			else
			{
				int newV0Indx = getIndx(v0Indx, PlaneSide.MID);
				int newV1Indx = getIndx(v1Indx, PlaneSide.MID);
				int newV2Indx = getIndx(v2Indx, PlaneSide.MID);
					
				midTriangles.Add(newV0Indx);
				midTriangles.Add(newV1Indx);
				midTriangles.Add(newV2Indx);
			}
		}

		GameObject rightGO = new GameObject();
		MeshFilter rightMF = rightGO.AddComponent<MeshFilter>();
		Mesh rightMesh = new Mesh();
		MeshRenderer mr = rightGO.AddComponent<MeshRenderer>();
		mr.material = new Material(Shader.Find("Standard"));
		rightMesh.vertices = midVertices.ToArray();
		rightMesh.normals = midNormals.ToArray();
		rightMesh.triangles = midTriangles.ToArray();
		
		rightMF.mesh = rightMesh;
		
		
		_splitJob.source_vertices.Dispose();
		_splitJob.info_vertices.Dispose();
		_jobPending = false;
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(Spliter))]
public class SpliterEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		Spliter spliter = (Spliter) target;
		if (GUILayout.Button("Split"))
		{
			spliter.split();
		}
	}
}
#endif