using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

    public float zoom_speed = 20.0f;
    public float min_zoom_base = 5.0f;
    public float max_zoom_base = 35.0f;

    private float min_zoom;
    private float max_zoom;
    private Vector3 displacement = new Vector3(0, 80, 0);
	
	// Update is called once per frame
	void Update () {
        min_zoom = min_zoom_base + transform.localScale.y;
        max_zoom = max_zoom_base + transform.localScale.y;
        Camera.main.transform.position = transform.position + displacement;

        displacement = Camera.main.transform.position - transform.position;
        displacement.y -= Input.GetAxis("Mouse ScrollWheel") * zoom_speed;
        displacement.y = displacement.y < min_zoom ? min_zoom : displacement.y;
        displacement.y = displacement.y > max_zoom ? max_zoom : displacement.y;
    }
}
