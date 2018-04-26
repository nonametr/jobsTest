using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class Test : MonoBehaviour
{
	[HideInInspector]
	public bool twoSided;
	
	public GameObject start;
	public GameObject end;

	public float holeSize = 0.01f;
	public bool animate;

	public float animationSpeed = 0.002f;
	// Use this for initialization
	void Awake () {
		Debug.Log("Executing...");
	}
	
	// Update is called once per frame
	void Update () {
		GetComponent<MeshRenderer>().sharedMaterial.SetVector("start", start.transform.position);
		GetComponent<MeshRenderer>().sharedMaterial.SetVector("end", end.transform.position);

		if (animate)
		{
			holeSize += animationSpeed;
			if (holeSize > 0.18)
			{
				holeSize = 0.0f;	
			}
			GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_DmgHoleSize", holeSize);
		}
	}

	public void togleTwoSided()
	{
		GetComponent<MeshRenderer>().sharedMaterial.SetInt("_CullVar", twoSided ? (int) UnityEngine.Rendering.CullMode.Off : (int) UnityEngine.Rendering.CullMode.Back);
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(Test))]
public class TestEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		Test test = (Test) target;
		bool twoSided = test.twoSided;
		test.twoSided = EditorGUILayout.Toggle("twoSided", twoSided);
		if (test.twoSided != twoSided)
		{
			test.togleTwoSided();
		}
	}
}
#endif