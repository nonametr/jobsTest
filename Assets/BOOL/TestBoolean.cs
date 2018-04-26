using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using System.Timers;

public class RemapContext
{
	public NativeHashMap<int, int> vertRemap = new NativeHashMap<int, int>();
	public NativeList<Vector3> vertices = new NativeList<Vector3>();
	public NativeList<Vector3> normals = new NativeList<Vector3>();
	public NativeList<Color> colors = new NativeList<Color>();
	public NativeList<int> triangles = new NativeList<int>();
}

[ExecuteInEditMode]
public class TestBoolean : MonoBehaviour
{
	[ComputeJobOptimization]
	struct SplitAndRemapJob : IJob
	{
		[ReadOnly] public NativeArray<int> sourceTriangles;
		[ReadOnly] public NativeArray<Vector3> sourceVertices;
		[ReadOnly] public NativeArray<Color> sourceVColors;
		[ReadOnly] public NativeArray<Vector3> sourceNormals;
		
		public NativeHashMap<int, int> dmgSurfVertRemap;
		public NativeList<Vector3> dmgSurfVertices;
		public NativeList<Vector3> dmgSurfNormals;
		public NativeList<Color> dmgSurfColors;
		public NativeList<int> dmgSurfTriangles;
		
		public NativeHashMap<int, int> damagedObjVertRemap;
		public NativeList<Vector3> damagedObjVertices;
		public NativeList<Vector3> damagedObjNormals;
		public NativeList<Color> damagedObjColors;
		public NativeList<int> damagedObjTriangles;
		
		int getDamagedObjVert(int oldVertIndx)
		{
			int v0newIndx;
			if (!damagedObjVertRemap.TryGetValue(oldVertIndx, out v0newIndx))
			{
				damagedObjVertices.Add(sourceVertices[oldVertIndx]);
				damagedObjNormals.Add(sourceNormals[oldVertIndx]);
				damagedObjColors.Add(Color.cyan);
				v0newIndx = damagedObjVertices.Length - 1;
				damagedObjVertRemap.TryAdd(oldVertIndx, v0newIndx);
			}

			return v0newIndx;
		}
		
		int getDmgSurfVert(int oldVertIndx)
		{
			int v0newIndx;
			if (!dmgSurfVertRemap.TryGetValue(oldVertIndx, out v0newIndx))
			{
				dmgSurfVertices.Add(sourceVertices[oldVertIndx]);
				dmgSurfNormals.Add(Vector3.Normalize(dmgSurfVertices[0] - sourceVertices[oldVertIndx]));
				dmgSurfColors.Add(Color.cyan);
				v0newIndx = dmgSurfVertices.Length - 1;
				dmgSurfVertRemap.TryAdd(oldVertIndx, v0newIndx);
			}

			return v0newIndx;
		}
		
		public void Execute()
		{
			for (int i = 0; i < sourceTriangles.Length / 3; ++i)
			{
				int v0Indx = sourceTriangles[i * 3 + 0];
				int v1Indx = sourceTriangles[i * 3 + 1];
				int v2Indx = sourceTriangles[i * 3 + 2];

				if (sourceVColors[v0Indx] != Color.red && 
				    sourceVColors[v1Indx] != Color.red &&
				    sourceVColors[v2Indx] != Color.red)
				{
					int v0newIndx = getDamagedObjVert(v0Indx);
					int v1newIndx = getDamagedObjVert(v1Indx);
					int v2newIndx = getDamagedObjVert(v2Indx);
					
					damagedObjTriangles.Add(v0newIndx);
					damagedObjTriangles.Add(v1newIndx);
					damagedObjTriangles.Add(v2newIndx);
				}
				
				int blueCount = 0;
				blueCount += sourceVColors[v0Indx] == Color.blue ? 1 : 0;
				blueCount += sourceVColors[v1Indx] == Color.blue ? 1 : 0;
				blueCount += sourceVColors[v2Indx] == Color.blue ? 1 : 0;
				
				if (blueCount >= 2)
				{
					if (sourceVColors[v2Indx] == Color.blue)
					{
						dmgSurfTriangles.Add(getDmgSurfVert(v2Indx));
					}
					else
					{
						dmgSurfTriangles.Add(0);
					}
					if (sourceVColors[v1Indx] == Color.blue)
					{
						dmgSurfTriangles.Add(getDmgSurfVert(v1Indx));
					}
					else
					{
						dmgSurfTriangles.Add(0);
					}
					if (sourceVColors[v0Indx] == Color.blue)
					{
						dmgSurfTriangles.Add(getDmgSurfVert(v0Indx));
					}
					else
					{
						dmgSurfTriangles.Add(0);
					}
				}
			}
		}
	}
	
	[ComputeJobOptimization]
	struct MarkCutoutJob : IJob
	{
		[ReadOnly] public NativeArray<Vector3> sourceVertices;
		[ReadOnly] public NativeArray<Vector3> sourceNormals;
		
		public NativeArray<Color> sourceVColors;
		public NativeQueue<int> cutoutVIndx;
		public NativeList<Vector3> dmgSurfVertices;
		public NativeList<Vector3> dmgSurfNormals;
		
		public void Execute()
		{
			int contourVertCount = 0;
			int cutoutVindxSize = cutoutVIndx.Count;
			for (int i = 0; i < cutoutVindxSize; ++i)
			{
				int vIndx = cutoutVIndx.Dequeue();
				if (sourceVColors[vIndx] == Color.red || sourceVColors[vIndx] == Color.blue)
				{
					sourceVColors[vIndx] = Color.blue;
				}
				else
				{
					if (sourceVColors[vIndx] != Color.green)
					{
						++contourVertCount;
						dmgSurfVertices[0] += sourceVertices[vIndx];
						dmgSurfNormals[0] += sourceNormals[vIndx];
						sourceVColors[vIndx] = Color.green;
					}
				}
			}

			dmgSurfVertices[0] /= contourVertCount;//center mass point calculation
			dmgSurfNormals[0] /= contourVertCount;
		}
	}

	[ComputeJobOptimization]
	struct CutoutTestJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeArray<int> sourceTriangles;
		[ReadOnly]
		public NativeArray<Color> sourceVColors;
		
		public NativeQueue<int>.Concurrent cutoutVIndx;
		public NativeQueue<int>.Concurrent cutoutTriIndx;
		
		public void Execute(int sourceTriIndx)
		{
			int v0Indx = sourceTriangles[sourceTriIndx * 3 + 0];
			int v1Indx = sourceTriangles[sourceTriIndx * 3 + 1];
			int v2Indx = sourceTriangles[sourceTriIndx * 3 + 2];
			if (sourceVColors[v0Indx] != sourceVColors[v1Indx] || sourceVColors[v0Indx] != sourceVColors[v2Indx])
			{
				cutoutTriIndx.Enqueue(sourceTriIndx);
				cutoutVIndx.Enqueue(v0Indx);
				cutoutVIndx.Enqueue(v1Indx);
				cutoutVIndx.Enqueue(v2Indx);
			}
		}
	}
	[ComputeJobOptimization]
	struct IntersectionTestJob : IJobParallelFor
	{
		[ReadOnly]
		public Matrix4x4 sourceLocalToWorldMatrix;
		[ReadOnly]
		public Matrix4x4 targetLocalToWorldMatrix;
		[ReadOnly]
		public NativeArray<int> sourceTriangles;
		[ReadOnly]
		public NativeArray<Vector3> sourceVertices;
		[ReadOnly]
		public NativeArray<int> targetTriangles;
		[ReadOnly]
		public NativeArray<Vector3> targetVertices;
		[ReadOnly]
		public Vector3 rayDir;

		public NativeArray<Color> sourceVColors;
		
		public void Execute(int sourceVIndx)
		{
			int intersectCount = 0;

			Vector3 rayOrigin = sourceLocalToWorldMatrix * new Vector4(sourceVertices[sourceVIndx].x, sourceVertices[sourceVIndx].y, sourceVertices[sourceVIndx].z, 1.0f);
			for (int i = 0; i < targetTriangles.Length; i += 3)
			{
				int v0Indx = targetTriangles[i];
				int v1Indx = targetTriangles[i + 1];
				int v2Indx = targetTriangles[i + 2];
				Vector3 v0 = targetLocalToWorldMatrix * new Vector4(targetVertices[v0Indx].x, targetVertices[v0Indx].y, targetVertices[v0Indx].z, 1.0f);
				Vector3 v1 = targetLocalToWorldMatrix * new Vector4(targetVertices[v1Indx].x, targetVertices[v1Indx].y, targetVertices[v1Indx].z, 1.0f);
				Vector3 v2 = targetLocalToWorldMatrix * new Vector4(targetVertices[v2Indx].x, targetVertices[v2Indx].y, targetVertices[v2Indx].z, 1.0f);
				if (hasIntersection(v0, v1, v2, rayOrigin, rayDir))
				{
					++intersectCount;
				}
			}

			if (intersectCount == 1)
			{
				sourceVColors[sourceVIndx] = Color.red;
			}
			else
			{
				sourceVColors[sourceVIndx] = Color.gray;
			}
		}
		
		public static bool hasIntersection(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 rayOrigin, Vector3 rayDir)
		{
			// Vectors from p1 to p2/p3 (edges)
			Vector3 e1, e2;  
 
			Vector3 p, q, t;
			float det, invDet, u, v;
 
 
			//Find vectors for two edges sharing vertex/point p1
			e1 = p2 - p1;
			e2 = p3 - p1;
 
			// calculating determinant 
			p = Vector3.Cross(rayDir, e2);
 
			//Calculate determinat
			det = Vector3.Dot(e1, p);
 
			//if determinant is near zero, ray lies in plane of triangle otherwise not
			if (det > -Epsilon && det < Epsilon) { return false; }
			invDet = 1.0f / det;
 
			//calculate distance from p1 to ray origin
			t = rayOrigin - p1;
 
			//Calculate u parameter
			u = Vector3.Dot(t, p) * invDet;
 
			//Check for ray hit
			if (u < 0 || u > 1) { return false; }
 
			//Prepare to test v parameter
			q = Vector3.Cross(t, e1);
 
			//Calculate v parameter
			v = Vector3.Dot(rayDir, q) * invDet;
 
			//Check for ray hit
			if (v < 0 || u + v > 1) { return false; }
 
			if ((Vector3.Dot(e2, q) * invDet) > Epsilon)
			{ 
				//ray does intersect
				return true;
			}
 
			// No hit at all
			return false;
		}
	}
	
	private const float Epsilon = 0.001f;
	
	private Mesh mesh;
	private TestBoolean[] others;
	
	//backup
	private List<Vector3> vertices;
	private List<Vector3> normals;
	private List<int> triangles;
	private List<Color> colors;

	public bool showGizmo;
	public bool showCutoutPreview;
	
	// Use this for initialization
	void Start ()
	{
		MeshFilter mf = GetComponent<MeshFilter>();
		if (mf == null)
		{
			SkinnedMeshRenderer smr = GetComponent<SkinnedMeshRenderer>();
			mesh = smr.sharedMesh;
		}
		else
		{
			mesh = mf.mesh;
		}

		vertices = new List<Vector3>();
		normals = new List<Vector3>();
		triangles = new List<int>();
		colors = new List<Color>();
	
		vertices.AddRange(mesh.vertices);
		normals.AddRange(mesh.normals);
		triangles.AddRange(mesh.triangles);

		for (int i = 0; i < mesh.vertexCount; ++i)
		{
			colors.Add(Color.gray);
		}

		mesh.colors = colors.ToArray();
		
		others = FindObjectsOfType<TestBoolean>().Except(new[] { this }).ToArray();
	}

	public void reset()
	{
		mesh.vertices = vertices.ToArray();
		mesh.colors = colors.ToArray();
		mesh.triangles = triangles.ToArray();
		mesh.normals = normals.ToArray();
	}
	public void showOverlap()
	{
		Timer timer = new Timer();
		timer.Interval = 1;
		timer.Enabled = true;
		
		System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
		foreach (TestBoolean otherObj in others)
		{
			NativeArray<int> sourceTriangles = new NativeArray<int>(otherObj.mesh.triangles, Allocator.Persistent);
			NativeArray<Vector3> sourceVertices = new NativeArray<Vector3>(otherObj.mesh.vertices, Allocator.Persistent);
			NativeArray<Vector3> sourceNormals = new NativeArray<Vector3>(otherObj.mesh.normals, Allocator.Persistent);
			NativeArray<Color> sourceVColors = new NativeArray<Color>(otherObj.mesh.vertexCount, Allocator.Persistent);
			
			NativeArray<int> targetTriangles = new NativeArray<int>(mesh.triangles, Allocator.Persistent);
			NativeArray<Vector3> targetVertices = new NativeArray<Vector3>(mesh.vertices, Allocator.Persistent);
			
			NativeQueue<int> cutoutVIndx = new NativeQueue<int>(Allocator.Persistent);
			NativeQueue<int> cutoutTriIndx = new NativeQueue<int>(Allocator.Persistent);
			
			NativeHashMap<int, int> damagedObjVertRemap = new NativeHashMap<int, int>(otherObj.mesh.vertexCount, Allocator.Persistent);
			NativeList<Vector3> damagedObjVertices = new NativeList<Vector3>(Allocator.Persistent);
			NativeList<Vector3> damagedObjNormals = new NativeList<Vector3>(Allocator.Persistent);
			NativeList<Color> damagedObjColors = new NativeList<Color>(Allocator.Persistent);
			NativeList<int> damagedObjTriangles = new NativeList<int>(Allocator.Persistent);
						
			NativeHashMap<int, int> dmgSurfVertRemap = new NativeHashMap<int, int>(otherObj.mesh.vertexCount, Allocator.Persistent);
			NativeList<Vector3> dmgSurfVertices = new NativeList<Vector3>(Allocator.Persistent);
			NativeList<Vector3> dmgSurfNormals = new NativeList<Vector3>(Allocator.Persistent);
			NativeList<Color> dmgSurfColors = new NativeList<Color>(Allocator.Persistent);
			NativeList<int> dmgSurfTriangles = new NativeList<int>(Allocator.Persistent);
			
			//add damage surface center mass point
			dmgSurfColors.Add(Color.yellow);
			dmgSurfNormals.Add(Vector3.zero);
			dmgSurfVertices.Add(Vector3.zero);
			
			IntersectionTestJob intersectionTestJob = new IntersectionTestJob()
			{
				sourceLocalToWorldMatrix = otherObj.transform.localToWorldMatrix,
				targetLocalToWorldMatrix = transform.localToWorldMatrix,
				
				sourceTriangles = sourceTriangles,
				sourceVertices = sourceVertices,
				targetTriangles = targetTriangles,
				targetVertices = targetVertices,
				sourceVColors = sourceVColors,
				rayDir = new Vector3(0, 1, 0)
			};

			CutoutTestJob cutoutTestJob = new CutoutTestJob()
			{
				sourceVColors = sourceVColors,
				sourceTriangles = sourceTriangles,
				cutoutVIndx = cutoutVIndx,
				cutoutTriIndx = cutoutTriIndx
			};

			MarkCutoutJob markCutoutJob = new MarkCutoutJob()
			{
				sourceVColors = sourceVColors,
				sourceNormals = sourceNormals,
				cutoutVIndx = cutoutVIndx,
				sourceVertices = sourceVertices,
				dmgSurfVertices = dmgSurfVertices,
				dmgSurfNormals = dmgSurfNormals
			};
	
			SplitAndRemapJob splitAndRemapJob = new SplitAndRemapJob()
			{
				sourceTriangles = sourceTriangles,
				sourceVColors = sourceVColors,
				sourceVertices = sourceVertices,
				sourceNormals = sourceNormals,
				
				dmgSurfVertRemap = dmgSurfVertRemap,
				dmgSurfVertices = dmgSurfVertices,
				dmgSurfNormals = dmgSurfNormals,
				dmgSurfColors = dmgSurfColors,
				dmgSurfTriangles = dmgSurfTriangles,
				
				damagedObjVertRemap = damagedObjVertRemap,
				damagedObjVertices = damagedObjVertices,
				damagedObjNormals = damagedObjNormals,
				damagedObjColors = damagedObjColors,
				damagedObjTriangles = damagedObjTriangles
			};
			
			var intersectionTestJobHandle = intersectionTestJob.Schedule(otherObj.mesh.vertexCount, 1);
			var cutoutTestJobHandle = cutoutTestJob.Schedule(sourceTriangles.Length / 3, 1, intersectionTestJobHandle);
			var markCutoutJobHandle = markCutoutJob.Schedule(cutoutTestJobHandle);
			var splitAndRemapJobHandle = splitAndRemapJob.Schedule(markCutoutJobHandle);
			
			intersectionTestJobHandle.Complete();
			cutoutTestJobHandle.Complete();
			markCutoutJobHandle.Complete();
			splitAndRemapJobHandle.Complete();

			if (showCutoutPreview)
			{
				otherObj.mesh.colors = sourceVColors.ToArray();
			}
			else
			{

				GameObject go1 = new GameObject();
				go1.name = "damageSurface";
				MeshFilter mf1 = go1.AddComponent<MeshFilter>();
				MeshRenderer mr = go1.AddComponent<MeshRenderer>();
				mr.material = new Material(Shader.Find("Standard"));
				go1.transform.position = otherObj.transform.position;
				Mesh mesh1 = mf1.mesh;
				mesh1.vertices = dmgSurfVertices.ToArray();
				mesh1.colors = dmgSurfColors.ToArray();
				mesh1.normals = dmgSurfNormals.ToArray();
				mesh1.triangles = dmgSurfTriangles.ToArray();
				go1.AddComponent<TestBoolean>();

				Mesh newMesh = otherObj.GetComponent<MeshFilter>().mesh;
				newMesh.Clear();
				newMesh.vertices = damagedObjVertices.ToArray();
				newMesh.triangles = damagedObjTriangles.ToArray();
				newMesh.normals = damagedObjNormals.ToArray();
				newMesh.colors = damagedObjColors.ToArray();
				newMesh.RecalculateBounds();
			}
			
			sourceTriangles.Dispose();
			sourceVColors.Dispose();
			sourceVertices.Dispose();
			sourceNormals.Dispose();
			targetTriangles.Dispose();
			targetVertices.Dispose();
			cutoutVIndx.Dispose();
			cutoutTriIndx.Dispose();
			
			dmgSurfVertRemap.Dispose();
			dmgSurfVertices.Dispose();
			dmgSurfNormals.Dispose();
			dmgSurfColors.Dispose();
			dmgSurfTriangles.Dispose();
				
			damagedObjVertRemap.Dispose();
			damagedObjVertices.Dispose();
			damagedObjNormals.Dispose();
			damagedObjColors.Dispose();
			damagedObjTriangles.Dispose();
		}

		Debug.Log("Execution time => " + sw.ElapsedMilliseconds.ToString() + "ms");
	}
	
	void OnDrawGizmos()
	{
		if (mesh == null || showGizmo == false)
			return;
		
		for(int i = 0; i < mesh.vertexCount; ++i)
		{
			if (mesh.colors.Length > i)
			{
				Gizmos.color = mesh.colors[i];
			}
			else
			{
				Gizmos.color = Color.black;;
			}

			Vector3 vpos = transform.localToWorldMatrix * new Vector4(mesh.vertices[i].x, mesh.vertices[i].y, mesh.vertices[i].z, 1.0f);
			Gizmos.DrawSphere(vpos, 0.01f);
			Gizmos.DrawRay(vpos, mesh.normals[i] *0.05f);
		}
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(TestBoolean))]
public class TestBooleanEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		TestBoolean obj = (TestBoolean) target;
		if (GUILayout.Button("ShowOverlap"))
		{
			obj.showOverlap();
		}
		if (GUILayout.Button("Reset"))
		{
			obj.reset();
		}
	}
}
#endif