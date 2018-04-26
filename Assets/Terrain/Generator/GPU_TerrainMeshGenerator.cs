using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using System.Timers;

public static class CustomRenderTextureExtension
{
	public static void toTexture2D(this CustomRenderTexture tex, Texture2D target)
	{
		CustomRenderTexture.active = tex;
		target.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
		target.Apply();
	}
}

[ExecuteInEditMode]
public class GPU_TerrainMeshGenerator : MonoBehaviour
{
	public int cellResolution = 255;
	public float cellSize = 0.5f;
	public int arrayResolution = 10;
	public CustomRenderTexture HMRT;
	public CustomRenderTexture HMRT_FINAL;
	public CustomRenderTexture SplatMapControlRT;
	public CustomRenderTexture SplatMapDiffuseRT;
	public CustomRenderTexture SplatMapNormalRT;

	[HideInInspector] public float HMRT_NormalStrongness;
	[HideInInspector] public float HMRT_SobelDelta;
	[HideInInspector] public Vector4 HMRT_RenderArea;
	[HideInInspector] public float HMRT_FINAL_BlurAmount;
	[HideInInspector] public Texture2D splatMapPreview;
	[HideInInspector] public bool showControlMapPreview;
	[HideInInspector] public int splatMapPreviewResolution = 0;//0 - low, 1 - high
	[HideInInspector] public List<SplatMapEntry> splatMapEntries = new List<SplatMapEntry>();
	
	private NativeArray<Vector3> vertices;
	private NativeArray<int> triangles;
	private NativeArray<Vector2> uvs;

#if UNITY_EDITOR
	public class SplatmapPreviewWindow : EditorWindow
	{
		public static GPU_TerrainMeshGenerator terrainGenerator;
		[MenuItem("MMO/Splatmap preview")]
		public static void ShowWindow()
		{
			GetWindow<SplatmapPreviewWindow>(false, "Preview", true);
		}

		void OnGUI()
		{
			if (terrainGenerator)
			{
				GUILayout.Label(terrainGenerator.splatMapPreview);
			
				if (terrainGenerator.splatMapPreviewResolution != 2 || !terrainGenerator.showControlMapPreview)
				{
					this.Close();
				}
			}
		}

		private void OnDestroy()
		{
			if (terrainGenerator)
			{
				if (terrainGenerator.splatMapPreviewResolution == 2 && terrainGenerator.showControlMapPreview)
				{
					terrainGenerator.showControlMapPreview = false;
				}
			}
		}
	}
#endif
	
	[System.Serializable]
	public class SplatMapEntry
	{
		public enum TextureChannel
		{
			R = 0,
			G,
			B,
			A
		}

		public enum Transition
		{
			Linear = 0,
			LinearWithPerlinNoise,
		}
	
		public bool showPreview;
		public float start;
		public float end;
		public float end_trans_impact;
		public float start_trans_impact;
		public int scale;
		public Texture2D diffuse_tex;
		public Texture2D normal_tex;
		public Transition transition = Transition.Linear;
		public TextureChannel channel = TextureChannel.R;
	}
	
	[ComputeJobOptimization]
	struct GenerateVerticesJob : IJobParallelFor
	{
		public int dim;
		public float cellSize;

		public NativeArray<Vector3> vertices;
		public NativeArray<Vector2> uvs;
		
		public void Execute(int vid)
		{
			int row = vid / dim;
			int col = vid % dim;

			vertices[vid] = new Vector3(col * cellSize, 0.0f, row * cellSize);
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
		splatMapPreview = new Texture2D(SplatMapControlRT.width, SplatMapControlRT.height, TextureFormat.RGB24, false);
		
		vertices = new NativeArray<Vector3>(cellResolution * cellResolution, Allocator.Persistent);
		uvs = new NativeArray<Vector2>(cellResolution * cellResolution, Allocator.Persistent);
		triangles = new NativeArray<int>(3 * 2 * cellResolution * cellResolution, Allocator.Persistent);
	}

	void cleanup()
	{
		vertices.Dispose();
		uvs.Dispose();
		triangles.Dispose();
	}

	public void generate()
	{
		if (cellResolution * cellResolution != vertices.Length)
		{
			cleanup();
			init();
		}
		
		Timer timer = new Timer();
		timer.Interval = 1;
		timer.Enabled = true;
		
		System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

		GenerateVerticesJob genVerticesJob = new GenerateVerticesJob()
		{
			cellSize = cellSize,
			dim = cellResolution,
			vertices = vertices,
			uvs = uvs
		};

		GenerateTrianglesJob genTrianglesJob = new GenerateTrianglesJob()
		{
			dim = cellResolution,
			triangles = triangles
		};
	
		var genVerticesJobHandle = genVerticesJob.Schedule(cellResolution * cellResolution, cellResolution);
		var genTrianglesJobHandle = genTrianglesJob.Schedule(cellResolution - 1, 1);
		
		genVerticesJobHandle.Complete();
		genTrianglesJobHandle.Complete();

		foreach (Transform child in transform)
		{
			DestroyImmediate(child.gameObject);
		}
		
		float arrayElmtSize = (cellResolution - 1) * cellSize;
		for (int y = 0; y < arrayResolution; ++y)
		{
			for (int x = 0; x < arrayResolution; ++x)
			{
				GameObject newGO = new GameObject();
				newGO.transform.parent = transform;
				newGO.name = x.ToString() + ":" + y.ToString();
				newGO.transform.position = new Vector3(y * arrayElmtSize, 0, x * arrayElmtSize);
				
				MeshRenderer newMR = newGO.AddComponent<MeshRenderer>();
				newMR.material = new Material(Shader.Find("MMO/TerrainGPU"));
				newMR.material.SetFloat("_MaxHeight", 300.0f);
				newMR.castShadows = false;
				newMR.receiveShadows = false;
				
				TerrainPiece newPiece = newGO.AddComponent<TerrainPiece>();
				newPiece.x = x;
				newPiece.y = y;
				newPiece.terrain = this;
				newPiece.initialize();
				
				MeshFilter newMF = newGO.AddComponent<MeshFilter>();
				newMF.mesh = new Mesh();
				newMF.mesh.vertices = vertices.ToArray();
				newMF.mesh.uv = uvs.ToArray();
				newMF.mesh.triangles = triangles.ToArray();
				newMF.mesh.RecalculateBounds();
			}
		}
		
		Debug.Log("Execution time => " + sw.ElapsedMilliseconds.ToString() + "ms");
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(GPU_TerrainMeshGenerator))]
public class GPU_TerrainMeshGeneratorEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		GPU_TerrainMeshGenerator obj = (GPU_TerrainMeshGenerator) target;


		EditorGUILayout.Separator();
		
		if (GUILayout.Button("ReMesh"))
		{
			obj.generate();
		}

		EditorGUILayout.Separator();
		EditorGUILayout.Separator();
		EditorGUILayout.LabelField("Heightmap settings:", EditorStyles.boldLabel);
		
		EditorGUI.BeginChangeCheck();
		EditorGUILayout.BeginVertical();
		obj.HMRT_NormalStrongness = EditorGUILayout.Slider("NormalStrongness", obj.HMRT_NormalStrongness, 0.5f, 0.01f);
		obj.HMRT_SobelDelta = EditorGUILayout.Slider("SobelDelta", obj.HMRT_SobelDelta, 0.05f, 0.0005f);
		obj.HMRT_RenderArea = EditorGUILayout.Vector4Field("RenderArea", obj.HMRT_RenderArea);
		obj.HMRT_FINAL_BlurAmount = EditorGUILayout.Slider("BlurAmount", obj.HMRT_FINAL_BlurAmount, 0.01f, 0.0f);
		EditorGUILayout.EndVertical();
		if (EditorGUI.EndChangeCheck())
		{
			obj.HMRT.material.SetFloat("_NormalStrongness", obj.HMRT_NormalStrongness);
			obj.HMRT.material.SetFloat("_SobelDelta", obj.HMRT_SobelDelta);
			obj.HMRT.material.SetVector("_RenderArea", obj.HMRT_RenderArea);
			obj.HMRT_FINAL.material.SetFloat("_BlurAmount", obj.HMRT_FINAL_BlurAmount);
		}
		
		//-----SPLATMAP----
		
		EditorGUILayout.Separator();
		EditorGUILayout.LabelField("Splatmap settings:", EditorStyles.boldLabel);
		
		EditorGUI.BeginChangeCheck();
		EditorGUILayout.BeginVertical();
		using (new GUILayout.VerticalScope(GUI.skin.box))
		{
			EditorGUILayout.BeginVertical();
			EditorGUILayout.BeginHorizontal();
			obj.showControlMapPreview = EditorGUILayout.Toggle("Control map preview", obj.showControlMapPreview);
			string[] options = new string[]
			{
				"256x256", "512x512", "Detach"
			};
			obj.splatMapPreviewResolution = EditorGUILayout.Popup(" ", obj.splatMapPreviewResolution, options);
			EditorGUILayout.EndHorizontal();
			if(obj.showControlMapPreview)
			{
				if (obj.splatMapPreviewResolution == 2)
				{
					GPU_TerrainMeshGenerator.SplatmapPreviewWindow.terrainGenerator = obj;
					GPU_TerrainMeshGenerator.SplatmapPreviewWindow.ShowWindow();
				}
				else
				{
					GUILayout.Label(obj.splatMapPreview, GUILayout.Width((obj.splatMapPreviewResolution + 1) * 256), GUILayout.Height((obj.splatMapPreviewResolution + 1) * 256));
				}
			}
			EditorGUILayout.EndVertical();
			
			int delete_entry = -1;
			for (int i = 0; i < obj.splatMapEntries.Count; ++i)
			{
				using (new GUILayout.VerticalScope(GUI.skin.box))
				{
					EditorGUILayout.BeginVertical();

					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField(i.ToString(), GUILayout.Width(30));
					obj.splatMapEntries[i].showPreview = EditorGUILayout.Toggle(obj.splatMapEntries[i].showPreview);
					delete_entry = GUILayout.Button("Delete Entry") ? i : -1;
					EditorGUILayout.EndHorizontal();

					if (!obj.splatMapEntries[i].showPreview)
					{
						EditorGUILayout.EndVertical();
						continue;
					}

					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField(new GUIContent("Channel:"), GUILayout.Width(64));
					obj.splatMapEntries[i].channel =
						(GPU_TerrainMeshGenerator.SplatMapEntry.TextureChannel) EditorGUILayout.EnumPopup(obj.splatMapEntries[i].channel, GUILayout.Width(64));
					EditorGUILayout.LabelField(new GUIContent("Transition:"), GUILayout.Width(64));
					obj.splatMapEntries[i].transition =
						(GPU_TerrainMeshGenerator.SplatMapEntry.Transition) EditorGUILayout.EnumPopup(obj.splatMapEntries[i].transition, GUILayout.Width(64));
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginVertical();
					EditorGUILayout.BeginHorizontal();
					obj.splatMapEntries[i].diffuse_tex = (Texture2D) EditorGUILayout.ObjectField(obj.splatMapEntries[i].diffuse_tex, typeof(Texture2D),
						false, GUILayout.Width(64), GUILayout.Height(64));
					obj.splatMapEntries[i].normal_tex = (Texture2D) EditorGUILayout.ObjectField(obj.splatMapEntries[i].normal_tex, typeof(Texture2D),
						false, GUILayout.Width(64), GUILayout.Height(64));
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.LabelField(new GUIContent("Start Transition impact:"), GUILayout.Width(136));
					obj.splatMapEntries[i].start_trans_impact = EditorGUILayout.Slider(obj.splatMapEntries[i].start_trans_impact, 0, 1);
					EditorGUILayout.LabelField(new GUIContent("End Transition impact:"), GUILayout.Width(128));
					obj.splatMapEntries[i].end_trans_impact = EditorGUILayout.Slider(obj.splatMapEntries[i].end_trans_impact, 0, 1);
					EditorGUILayout.LabelField(new GUIContent("Start:"), GUILayout.Width(64));
					obj.splatMapEntries[i].start = EditorGUILayout.Slider(obj.splatMapEntries[i].start, 0, 1);
					EditorGUILayout.LabelField(new GUIContent("End:"), GUILayout.Width(64));
					obj.splatMapEntries[i].end = EditorGUILayout.Slider(obj.splatMapEntries[i].end, 0, 1);
					EditorGUILayout.LabelField(new GUIContent("Scale:"), GUILayout.Width(64));
					obj.splatMapEntries[i].scale = EditorGUILayout.IntSlider(obj.splatMapEntries[i].scale, 1, 64);
					EditorGUILayout.EndVertical();

					EditorGUILayout.EndVertical();
				}

				if (delete_entry != -1)
				{
					obj.splatMapEntries.RemoveAt(delete_entry);
				}
			}

			if (GUILayout.Button("Add entry"))
			{
				GPU_TerrainMeshGenerator.SplatMapEntry entry = new GPU_TerrainMeshGenerator.SplatMapEntry();
				obj.splatMapEntries.Add(entry);
			}
		}

		EditorGUILayout.EndVertical();
		if (EditorGUI.EndChangeCheck())
		{
			if (obj.splatMapEntries.Count > 0)
			{
				obj.SplatMapControlRT.material.SetInt("active_splat_count", obj.splatMapEntries.Count);
				obj.SplatMapControlRT.material.SetFloatArray("start_transition_lvl", obj.splatMapEntries.Select(entry => entry.start_trans_impact).ToList());
				obj.SplatMapControlRT.material.SetFloatArray("end_transition_lvl", obj.splatMapEntries.Select(entry => entry.end_trans_impact).ToList());
				obj.SplatMapControlRT.material.SetFloatArray("channel_lvl", obj.splatMapEntries.Select(entry => (float)entry.channel).ToList());
				obj.SplatMapControlRT.material.SetFloatArray("start_lvl", obj.splatMapEntries.Select(entry => entry.start).ToList());
				obj.SplatMapControlRT.material.SetFloatArray("end_lvl", obj.splatMapEntries.Select(entry => entry.end).ToList());

				for (int i = 0; i < obj.splatMapEntries.Count; ++i)
				{
					string texture_name = "_Texture" + i.ToString();
					obj.SplatMapDiffuseRT.material.SetTexture(texture_name, obj.splatMapEntries[i].diffuse_tex);
					obj.SplatMapDiffuseRT.material.SetTextureScale(texture_name, new Vector2(obj.splatMapEntries[i].scale, obj.splatMapEntries[i].scale));
					obj.SplatMapNormalRT.material.SetTexture(texture_name, obj.splatMapEntries[i].normal_tex);
					obj.SplatMapNormalRT.material.SetTextureScale(texture_name, new Vector2(obj.splatMapEntries[i].scale, obj.splatMapEntries[i].scale));
				}
				
				obj.SplatMapControlRT.Update();
				obj.SplatMapDiffuseRT.Update();
				
				
			
				obj.SplatMapControlRT.toTexture2D(obj.splatMapPreview);
			}
		}
	}
}
#endif