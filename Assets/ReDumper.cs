using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Jobs;
using Unity.Collections;

[ExecuteInEditMode]
public class ReDumper : MonoBehaviour
{
	struct CopyJob : IJobParallelFor
	{
		[ReadOnly]
		public float strength;
		[ReadOnly]
		public float sin_time; 
		[ReadOnly]
		public NativeArray<Vector3> source_vertices;
		[ReadOnly]
		public NativeArray<int> source_triangles;
		[ReadOnly]
		public NativeArray<Vector3> source_normals;
		[ReadOnly]
		public NativeArray<Vector2> source_uvs;
		
		public NativeArray<Vector3> target_vertices;
		public NativeArray<int> target_triangles;
		public NativeArray<Vector3> target_normals;
		public NativeArray<Vector2> target_uvs;

		public void Execute(int i)
		{
			if (i < source_vertices.Length)
			{
				var vertex = source_vertices[i];
				var perlin = Mathf.PerlinNoise(source_vertices[i].x * sin_time * 100, source_vertices[i].y * sin_time * 100) * strength;
				var noise = source_normals[i] * perlin * sin_time;

				vertex = vertex + noise;
				
				target_vertices[i] = vertex;
				target_normals[i] = source_normals[i];
				target_uvs[i] = source_uvs[i];
			}

			target_triangles[i] = source_triangles[i];
		}
	}

	private MeshFilter _mf;
	private Mesh _mesh;
	private bool _jobPending = false;
	private CopyJob testJob;
	private JobHandle testJobHandle;
	private NativeArray<Vector3> source_vertices;
	private NativeArray<int> source_triangles;
	private NativeArray<Vector3> source_normals;
	private NativeArray<Vector2> source_uvs;
	private NativeArray<Vector3> target_vertices;
	private NativeArray<int> target_triangles;
	private NativeArray<Vector3> target_normals;
	private NativeArray<Vector2> target_uvs;
	
	// Use this for initialization
	void Start ()
	{
		_mf = GetComponent<MeshFilter>();
		_mesh = _mf.mesh;
		
		source_vertices = new NativeArray<Vector3>(_mesh.vertices, Allocator.Persistent);
		source_triangles = new NativeArray<int>(_mesh.triangles, Allocator.Persistent);
		source_normals = new NativeArray<Vector3>(_mesh.normals, Allocator.Persistent);
		source_uvs = new NativeArray<Vector2>(_mesh.uv, Allocator.Persistent);
		target_vertices = new NativeArray<Vector3>(_mesh.vertices.Length, Allocator.Persistent);
		target_triangles = new NativeArray<int>(_mesh.triangles.Length, Allocator.Persistent);
		target_normals = new NativeArray<Vector3>(_mesh.normals.Length, Allocator.Persistent);
		target_uvs = new NativeArray<Vector2>(_mesh.uv.Length, Allocator.Persistent);
	}
	
	// Update is called once per frame
	void Update () {
		if (_jobPending && testJobHandle.IsCompleted)
		{
			_jobPending = false;
			testJobHandle.Complete();

			_mf.sharedMesh.vertices = testJob.target_vertices.ToArray();
			_mf.sharedMesh.triangles = testJob.target_triangles.ToArray();
			_mf.sharedMesh.normals = testJob.target_normals.ToArray();
			_mf.sharedMesh.uv = testJob.target_uvs.ToArray();
			
			dump();
		}
	}

	public void dump()
	{
		testJob = new CopyJob()
		{
			strength = 0.05f,
			sin_time = Mathf.Sin(Time.time),
			source_vertices = source_vertices,
			source_triangles = source_triangles,
			source_normals = source_normals,
			source_uvs = source_uvs,
			target_vertices = target_vertices,
			target_triangles = target_triangles,
			target_normals = target_normals,
			target_uvs = target_uvs,
		};
		
		testJobHandle = testJob.Schedule(_mesh.triangles.Length, 8192);
		_jobPending = true;
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(ReDumper))]
public class ReDumperEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		ReDumper dumper = (ReDumper) target;
		if (GUILayout.Button("Dump"))
		{
			dumper.dump();
		}
	}
}
#endif