using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TerrainSwitch : MonoBehaviour
{

	public GameObject t1;
	public GameObject t2;
	public GameObject t3;

	public bool enableT1;
	public bool enableT2;
	public bool enableT3;
	
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		t1.SetActive(enableT1);
		t2.SetActive(enableT2);
		t3.SetActive(enableT3);
	}
}
