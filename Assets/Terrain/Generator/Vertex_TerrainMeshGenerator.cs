using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using System.Timers;

[ExecuteInEditMode]
public class Vertex_TerrainMeshGenerator : MonoBehaviour
{
	public int dim = 250;
	public float cellSize = 0.1f;
	public int maxHeight = 50;
	public Texture2D heightMap;
	public Texture2D normalMap;
	
	public NativeArray<Vector3> vertices;
	public NativeArray<Vector3> normals;
	public NativeArray<int> triangles;
	public NativeArray<Vector2> uvs;

	[ComputeJobOptimization]
	struct NormalToColorJob : IJobParallelFor
	{
		[ReadOnly] public NativeArray<Vector3> normals;

		public NativeArray<Color> colors;
		public void Execute(int vid)
		{
			float r = normals[vid].x * 0.5f + 0.5f;
			float g = normals[vid].y;
			float b = normals[vid].z * 0.5f + 0.5f;
			colors[vid] = new Color(r, g, b);
		}
	}

	[ComputeJobOptimization]
	struct GenerateVerticesJob : IJobParallelFor
	{
		[ReadOnly] public NativeArray<Color> pixels;
		
		public int dim;
		public int maxHeight;
		public float cellSize;

		public int hmWidth;
		public int hmHeight;
		public NativeArray<Vector3> vertices;
		public NativeArray<Vector3> normals;
		public NativeArray<Vector2> uvs;
		
		public void Execute(int vid)
		{
			int row = vid / dim;
			int col = vid % dim;

			int hmRow = (int)(((float) row / dim) * hmHeight);
			int hmCol = (int)(((float) col / dim) * hmWidth);
			vertices[vid] = new Vector3(col * cellSize, pixels[hmRow * hmWidth + hmCol].r * maxHeight, row * cellSize);
			normals[vid] = new Vector3(0, 1.0f, 0);
			uvs[vid] = new Vector2((float)col / dim, (float)row / dim);
		}
	}

	[ComputeJobOptimization]
	struct GenerateTrianglesJob : IJobParallelFor
	{
		public int dim;
		[NativeDisableParallelForRestriction] public NativeArray<int> triangles;
		
		public void Execute(int row)
		{
			int vstart = row * dim;
			int tristart = 3 * 2 * row * (dim - 1);

			for (int i = 0; i < dim - 1; ++i)
			{
				triangles[tristart + i * 6 + 2] = vstart + i;
				triangles[tristart + i * 6 + 1] = vstart + i + 1;
				triangles[tristart + i * 6 + 0] = dim + i + vstart;
				
				triangles[tristart + i * 6 + 5] = vstart + i + 1;
				triangles[tristart + i * 6 + 4] = dim + i + vstart + 1;
				triangles[tristart + i * 6 + 3] = dim + i + vstart;
			}
		}
	}
	
	private void OnEnable()
	{
		init();
	}

	private void OnDisable()
	{
		cleanup();
	}

	void init()
	{
		vertices = new NativeArray<Vector3>(dim * dim, Allocator.Persistent);
		normals = new NativeArray<Vector3>(dim * dim, Allocator.Persistent);
		uvs = new NativeArray<Vector2>(dim * dim, Allocator.Persistent);
		triangles = new NativeArray<int>(3 * 2 * dim * dim, Allocator.Persistent);
	}

	void cleanup()
	{
		vertices.Dispose();
		normals.Dispose();
		uvs.Dispose();
		triangles.Dispose();
	}
	

	public void generate()
	{
		if (heightMap == null || normalMap == null)
		{
			Debug.LogWarning("Assing required fields first");
			return;
		}

		if (dim * dim != vertices.Length)
		{
			cleanup();
			init();
		}
		
		Timer timer = new Timer();
		timer.Interval = 1;
		timer.Enabled = true;
		
		System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

		NativeArray<Color> pixels = new NativeArray<Color>(heightMap.GetPixels(), Allocator.TempJob);
		GenerateVerticesJob genVerticesJob = new GenerateVerticesJob()
		{
			hmHeight = heightMap.height,
			hmWidth = heightMap.width,
			pixels = pixels,
			cellSize = cellSize,
			dim = dim,
			maxHeight = maxHeight,
			normals = normals,
			vertices = vertices,
			uvs = uvs
		};

		GenerateTrianglesJob genTrianglesJob = new GenerateTrianglesJob()
		{
			dim = dim,
			triangles = triangles
		};
		
		var genVerticesJobHandle = genVerticesJob.Schedule(dim * dim, dim);
		var genTrianglesJobHandle = genTrianglesJob.Schedule(dim - 1, 1);
		
		genVerticesJobHandle.Complete();
		genTrianglesJobHandle.Complete();

		MeshFilter mf = GetComponent<MeshFilter>();
		if (mf.mesh == null)
		{
			mf.mesh = new Mesh();
		}
		else
		{
			mf.mesh.Clear();
		}
		
		mf.mesh.vertices = vertices.ToArray();
		mf.mesh.normals = normals.ToArray();
		mf.mesh.uv = uvs.ToArray();
		mf.mesh.triangles = triangles.ToArray();
		mf.mesh.RecalculateBounds();
		mf.mesh.RecalculateNormals();
		mf.mesh.RecalculateTangents();

		NativeArray<Vector3> tmpNormals = new NativeArray<Vector3>(mf.mesh.normals, Allocator.TempJob);
		NativeArray<Color> colors = new NativeArray<Color>(mf.mesh.normals.Length, Allocator.TempJob);
		NormalToColorJob normalToColorJob = new NormalToColorJob()
		{
			normals = tmpNormals,
			colors = colors
		};
		var v3ToColorJobHandle = normalToColorJob.Schedule(normals.Length, 128);
		v3ToColorJobHandle.Complete();

		//normalMap = new Texture2D(dim, dim, TextureFormat.RGB24, false);
		normalMap.Resize(dim, dim);
		normalMap.SetPixels(colors.ToArray());
		normalMap.Apply();

		//byte[] bytes = normalMap.EncodeToPNG();
		//File.WriteAllBytes(Application.dataPath + "/world_normals.png", bytes);

		tmpNormals.Dispose();
		colors.Dispose();
		pixels.Dispose();

		Debug.Log("Execution time => " + sw.ElapsedMilliseconds.ToString() + "ms");
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(Vertex_TerrainMeshGenerator))]
public class TerrainMeshGeneratorEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		Vertex_TerrainMeshGenerator obj = (Vertex_TerrainMeshGenerator) target;
		if (GUILayout.Button("Generate"))
		{
			obj.generate();
		}
	}
}
#endif