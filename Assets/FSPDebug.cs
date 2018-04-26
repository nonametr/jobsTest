using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FSPDebug : MonoBehaviour
{
	private int fps;
	private int frames;
	private float deltaTime;
	private Text textUI;
	// Use this for initialization
	void Start ()
	{
		textUI = GetComponent<Text>();
	}
	
	// Update is called once per frame
	void Update ()
	{
		++frames;
		deltaTime += Time.deltaTime;
		if (deltaTime > 1.0f)
		{
			fps = frames;
			frames = 0;
			deltaTime = 0;
		}
		textUI.text = fps.ToString() + " FPS";
	}
}
