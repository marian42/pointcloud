using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[ExecuteInEditMode, SelectionBase]
public class PlaneBehaviour : MonoBehaviour {
	private readonly Color orange = new Color(1f, 0.5f, 0f);

	public Plane Plane;
	public PointCloud PointCloud;

	public void ColorPoints() {
		int hits = 0;
		for (int i = 0; i < this.PointCloud.CenteredPoints.Length; i++) {
			float score = HoughPlaneFinder.GetScore(this.Plane, this.PointCloud.CenteredPoints[i]);
			if (score == 0) {
				this.PointCloud.Colors[i] = Color.red;
			} else {
				this.PointCloud.Colors[i] = Color.Lerp(orange, Color.green, score);
				hits++;
			}
		}
		this.PointCloud.Show();
	}

	public void UpdateTransform() {
		this.transform.localPosition = -this.Plane.normal * this.Plane.distance;
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
		}
		{
			var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
			quad.transform.parent = this.transform;
			quad.transform.localScale = Vector3.one * 4.0f;
			quad.transform.localPosition = Vector3.zero;
			quad.transform.rotation = Quaternion.Euler(Vector3.right * 180.0f);
		}
		this.Display();
	}

	public void ReadFromTransform() {
		this.Plane = new Plane(this.transform.rotation * Vector3.forward, this.transform.localPosition);
		this.Display();
	}

	public void UpdateName() {
		float score = this.PointCloud.CenteredPoints.Sum(p => HoughPlaneFinder.GetScore(this.Plane, p));
		this.gameObject.name = "Plane, score: " + string.Format("{0:0.0}", score) + ", n: " + (this.Plane.normal / this.Plane.normal.y) + ", d: " + this.Plane.distance;
	}

	public void Display() {
		this.UpdateTransform();
		this.UpdateColor();
		this.UpdateName();
	}

	public static void DisplayPlane(Plane plane, PointCloud pointCloud) {
		GameObject planeGameObject = new GameObject();
		var planeBehaviour = planeGameObject.AddComponent<PlaneBehaviour>();
		planeBehaviour.Plane = plane;
		planeBehaviour.PointCloud = pointCloud;
		planeGameObject.transform.parent = pointCloud.transform;
		planeBehaviour.Initialize();
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