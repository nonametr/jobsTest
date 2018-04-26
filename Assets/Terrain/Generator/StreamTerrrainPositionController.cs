using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class StreamTerrrainPositionController : MonoBehaviour
{
	public float worldSize = 500;
	public StreamTerrainGenerator terrain;

	
	private float visibleWorldSize;
	private float uvStep; 
	
	private void OnEnable()
	{
		terrain = GetComponent<StreamTerrainGenerator>();
		visibleWorldSize = terrain.cellSize * terrain.cellResolution;
		uvStep = visibleWorldSize / worldSize;
	}

	// Update is called once per frame
	void Update ()
	{
		transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z + 0.05f);
		if (transform.position.x > worldSize - visibleWorldSize)
		{
			transform.position = new Vector3(worldSize - visibleWorldSize, transform.position.y, transform.position.z);
		}
		else if (transform.position.z > worldSize - visibleWorldSize)
		{
			transform.position = new Vector3(transform.position.x, transform.position.y, worldSize - visibleWorldSize);
		}
		else if (transform.position.x < 0.0f)
		{
			transform.position = new Vector3(0, transform.position.y, transform.position.z);
		}
		else if (transform.position.z < 0.0f)
		{
			transform.position = new Vector3(transform.position.x, transform.position.y, 0);
		}
		float uvXStart = transform.position.x / worldSize;
		float uvYStart = transform.position.z / worldSize;
		terrain.HMRT.material.SetVector("_RenderArea", new Vector4(uvXStart, uvYStart, uvXStart + uvStep, uvYStart + uvStep));
		
		//--SPLATMAP START--
		terrain.SplatMapControlRT.material.SetInt("active_splat_count", terrain.splatMapEntries.Count);
		terrain.SplatMapControlRT.material.SetFloatArray("start_transition_lvl", terrain.splatMapEntries.Select(entry => entry.start_trans_impact).ToList());
		terrain.SplatMapControlRT.material.SetFloatArray("end_transition_lvl", terrain.splatMapEntries.Select(entry => entry.end_trans_impact).ToList());
		terrain.SplatMapControlRT.material.SetFloatArray("channel_lvl", terrain.splatMapEntries.Select(entry => (float)entry.channel).ToList());
		terrain.SplatMapControlRT.material.SetFloatArray("start_lvl", terrain.splatMapEntries.Select(entry => entry.start).ToList());
		terrain.SplatMapControlRT.material.SetFloatArray("end_lvl", terrain.splatMapEntries.Select(entry => entry.end).ToList());

		for (int i = 0; i < terrain.splatMapEntries.Count; ++i)
		{
			string texture_name = "_Texture" + i.ToString();
			terrain.SplatMapDiffuseRT.material.SetTexture(texture_name, terrain.splatMapEntries[i].diffuse_tex);
			terrain.SplatMapDiffuseRT.material.SetTextureScale(texture_name, new Vector2(terrain.splatMapEntries[i].scale, terrain.splatMapEntries[i].scale));
			terrain.SplatMapNormalRT.material.SetTexture(texture_name, terrain.splatMapEntries[i].normal_tex);
			terrain.SplatMapNormalRT.material.SetTextureScale(texture_name, new Vector2(terrain.splatMapEntries[i].scale, terrain.splatMapEntries[i].scale));
		}
		terrain.SplatMapControlRT.material.SetVector("_RenderArea", new Vector4(uvXStart, uvYStart, uvXStart + uvStep, uvYStart + uvStep));
		//--SPLATMAP END--
		
		terrain.SplatMapDiffuseRT.material.SetVector("_TilingOffset", new Vector4(1, 1, transform.position.x / visibleWorldSize, transform.position.z / visibleWorldSize));
		terrain.SplatMapNormalRT.material.SetVector("_TilingOffset", new Vector4(1, 1, transform.position.x / visibleWorldSize, transform.position.z / visibleWorldSize));
		terrain.HMRT.Update();
		terrain.SplatMapControlRT.Update();
		terrain.SplatMapDiffuseRT.Update();
		terrain.SplatMapNormalRT.Update();
	}
}
