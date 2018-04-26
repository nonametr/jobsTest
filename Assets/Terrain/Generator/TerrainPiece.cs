using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class TerrainPiece : MonoBehaviour
{
	public GPU_TerrainMeshGenerator terrain;
	public int x;
	public int y;
	public Texture2D nhm_texture;
	public Texture2D diffuse_texture;
	public Texture2D normal_texture;
	
	public void initialize()
	{
		nhm_texture = new Texture2D(terrain.HMRT.width, terrain.HMRT.height, TextureFormat.ARGB32, false);
		diffuse_texture = new Texture2D(terrain.SplatMapDiffuseRT.width, terrain.SplatMapDiffuseRT.height, TextureFormat.ARGB32, false);
		normal_texture = new Texture2D(terrain.SplatMapNormalRT.width, terrain.SplatMapNormalRT.height, TextureFormat.ARGB32, false);
		
		nhm_texture.filterMode = FilterMode.Point;
		Material material = GetComponent<MeshRenderer>().material;
		
		material.SetTexture("_HeightMap", nhm_texture);
		material.SetTexture("_MainTex", diffuse_texture);
		material.SetTexture("_NormalTex", normal_texture);

		OnEnable();
	}

	private void OnEnable()
	{
		if (nhm_texture == null)
		{
			return;
		}
		
		float step = 0.999f / terrain.arrayResolution;
		float yStart = (float)x / terrain.arrayResolution;
		float xStart = (float)y / terrain.arrayResolution;
		
		TaskManager.inst.taskQueue.Enqueue(new TerrainPieceUpdateTask(terrain, this, xStart, yStart, xStart + step, yStart + step));
	}

	private void OnDisable()
	{
		
	}
}
