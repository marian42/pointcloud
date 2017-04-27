using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

	private Vector3 center;
	float distance = 40.0f;
	float rotH = 0.0f;
	float rotV = 45.0f;

	private Vector3 mouseStart;
	private bool moving = false;

	private Vector3 getMousePosition() {
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
		float rayDistance;
		if (groundPlane.Raycast(ray, out rayDistance)) {
			return ray.GetPoint(rayDistance);
		} else {
			return Vector3.zero;
		}
	}

	private void reset() {
		this.center = Vector3.zero;
		this.distance = 20.0f;
		this.rotH = 0.0f;
		this.rotV = 45.0f;
	}

	void Start () {
		this.reset();
	}
	
	void Update () {
		float scroll = Input.GetAxis("Mouse ScrollWheel");
		if (scroll != 0) {
			this.distance *= Mathf.Pow(1.3f, -Mathf.Sign(scroll));
		}		
		if (Input.GetMouseButton(1)) {
			this.rotH -= Input.GetAxis("Mouse X");
			this.rotV -= Input.GetAxis("Mouse Y");
		
		}
		if (Input.GetMouseButtonDown(0)) {
			this.mouseStart = this.getMousePosition();
			this.moving = true;
		}

		if (Input.GetMouseButtonUp(0)) {
			this.moving = false;
		}
		if (this.moving) {				
			this.center -= this.getMousePosition() - this.mouseStart;
		}
		if (Input.GetKeyDown(KeyCode.R)) {
			this.reset();
		}

		this.transform.position = this.center + Quaternion.Euler(new Vector3(90.0f - rotV, 180.0f - rotH, 0)) * Vector3.up * distance;
		this.transform.rotation = Quaternion.Euler(new Vector3(rotV, -rotH, 0));
		Debug.DrawLine(this.center, this.transform.position);
	}
}
