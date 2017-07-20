using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[ExecuteInEditMode, SelectionBase]
public class PlaneBehaviour : MonoBehaviour {
	private readonly Color orange = new Color(1f, 0.5f, 0f);

	public Plane Plane;
	public PointCloudBehaviour PointCloudBehaviour;

	public void ColorPoints() {
		var pointCloud = this.PointCloudBehaviour.PointCloud;
		for (int i = 0; i < pointCloud.Points.Length; i++) {
			float score = pointCloud.GetScore(i, this.Plane);
			if (score == 0) {
				pointCloud.Colors[i] = Color.red;
			} else {
				pointCloud.Colors[i] = Color.Lerp(orange, Color.green, score);
			}
		}
		this.PointCloudBehaviour.Show();
	}

	public void UpdateTransform() {
		this.transform.localPosition = Math3d.ProjectFromGroundToPlane(Vector2.zero, this.Plane);
		this.transform.rotation = Quaternion.LookRotation(this.Plane.normal);
	}

	public void UpdateColor() {
		float inclination = Vector3.Angle(this.Plane.normal, Vector3.up);
		float direction = Vector3.Angle(this.Plane.normal, Vector3.forward) * (this.Plane.normal.x < 0 ? -1 : 1) + 180.0f;
		Color color = Color.HSVToRGB(direction / 360.0f, 0.5f + 0.5f * inclination / 60.0f, 1.0f);
		
		foreach (var meshRenderer in this.transform.GetComponentsInChildren<MeshRenderer>()) {
			var tempMaterial = new Material(meshRenderer.sharedMaterial);
			tempMaterial.color = color;
			meshRenderer.sharedMaterial = tempMaterial;
		}
	}

	public void Initialize() {
		this.gameObject.tag = "Quad";
		
		{
			var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
			quad.transform.parent = this.transform;
			quad.transform.localScale = Vector3.one * 4.0f;
			quad.transform.localPosition = Vector3.zero;
			quad.gameObject.layer = 9;
		}
		{
			var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
			quad.transform.parent = this.transform;
			quad.transform.localScale = Vector3.one * 4.0f;
			quad.transform.localPosition = Vector3.zero;
			quad.transform.rotation = Quaternion.Euler(Vector3.right * 180.0f);
			quad.gameObject.layer = 9;
		}
		this.Display();
	}

	public void ReadFromTransform() {
		this.Plane = new Plane(this.transform.rotation * Vector3.forward, this.transform.localPosition);
		this.Display();
	}

	public void UpdateName() {
		float score = this.PointCloudBehaviour.PointCloud.GetScore(this.Plane);
		this.gameObject.name = "Plane, score: " + string.Format("{0:0.0}", score) + ", n: " + (this.Plane.normal / this.Plane.normal.y) + ", d: " + this.Plane.distance;
	}

	public void Display() {
		this.UpdateTransform();
		this.UpdateColor();
		this.UpdateName();
	}

	public static void DisplayPlane(Plane plane, PointCloudBehaviour pointCloudBehaviour) {
		GameObject planeGameObject = new GameObject();
		var planeBehaviour = planeGameObject.AddComponent<PlaneBehaviour>();
		planeBehaviour.Plane = plane;
		planeBehaviour.PointCloudBehaviour = pointCloudBehaviour;
		planeGameObject.transform.parent = pointCloudBehaviour.transform;
		planeBehaviour.Initialize();
		planeBehaviour.gameObject.layer = 9;
	}

	public static void DeletePlanesIn(Transform transform) {
		var existingQuads = new List<GameObject>();
		foreach (var child in transform) {
			if ((child as Transform).tag == "Quad") {
				existingQuads.Add((child as Transform).gameObject);
			}
		}
		foreach (var existingQuad in existingQuads) {
			GameObject.DestroyImmediate(existingQuad);
		}
	}
}