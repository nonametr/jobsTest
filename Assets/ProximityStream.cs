using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProximityStream : MonoBehaviour
{
	public float triggerDistance = 10;
	public float triggerThreshold = 1;
	public GameObject target;

	private List<GameObject> terrainArray = new List<GameObject>();
	private List<Vector3> terrainArrayPos = new List<Vector3>();
	// Use this for initialization
	void Start () {
		InvokeRepeating("checkProximity", 0, 1);
		foreach (Transform child in transform)
		{
			terrainArray.Add(child.gameObject);
			terrainArrayPos.Add(child.gameObject.GetComponent<MeshFilter>().sharedMesh.bounds.center + child.gameObject.transform.position);
		}
	}
	
	void checkProximity ()
	{
		int terrainArraySize = terrainArray.Count;
		for (int i = 0; i < terrainArraySize; ++i)
		{
			if (Vector3.Distance(terrainArrayPos[i], target.transform.position) < triggerDistance)
			{
				terrainArray[i].SetActive(true);
			}
			else if(terrainArray[i].active)
			{
				terrainArray[i].SetActive(false);
			}
		}
	}
}
