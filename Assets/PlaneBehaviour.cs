using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PlaneBehaviour : MonoBehaviour {

	public Plane Plane;
	public PointCloud PointCloud;

	public void ColorPoints(float maxDistance) {
		int hits = 0;
		for (int i = 0; i < this.PointCloud.CenteredPoints.Length; i++) {
			float distance = this.Plane.GetDistanceToPoint(this.PointCloud.CenteredPoints[i]);
			if (Mathf.Abs(distance) > maxDistance) {
				this.PointCloud.Colors[i] = Color.blue;
			} else {
				this.PointCloud.Colors[i] = Color.Lerp(Color.green, Color.red, Mathf.Abs(distance) / maxDistance);
				hits++;
			}			
		}
		this.PointCloud.Show();
		Debug.Log(hits + " / " + this.PointCloud.Points.Length);
	}

	public void UpdateTransform() {
		this.transform.localPosition = -this.Plane.normal * this.Plane.distance;
		this.transform.rotation = Quaternion.LookRotation(this.Plane.normal);
	}

	public void UpdateColor() {
		float inclination = Vector3.Angle(this.Plane.normal, Vector3.up);
		float direction = Vector3.Angle(this.Plane.normal, Vector3.forward) * (this.Plane.normal.x < 0 ? -1 : 1) + 180.0f;
		Color color = Color.HSVToRGB(direction / 360.0f, inclination / 60.0f, 1.0f);
		
		foreach (var meshRenderer in this.transform.GetComponentsInChildren<MeshRenderer>()) {
			var tempMaterial = new Material(meshRenderer.sharedMaterial);
			tempMaterial.color = color;
			meshRenderer.sharedMaterial = tempMaterial;
		}
	}

	public void Start() {
		this.gameObject.tag = "Quad";
		
		{
			var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
			quad.transform.parent = this.transform;
			quad.transform.localScale = Vector3.one * 10.0f;
			quad.transform.localPosition = Vector3.zero;
		}
		{
			var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
			quad.transform.parent = this.transform;
			quad.transform.localScale = Vector3.one * 10.0f;
			quad.transform.localPosition = Vector3.zero;
			quad.transform.rotation = Quaternion.Euler(Vector3.right * 180.0f);
		}
		this.UpdateTransform();
		this.UpdateColor();
	}
}