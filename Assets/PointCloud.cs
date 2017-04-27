using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

[SelectionBase]
public class PointCloud : MonoBehaviour {
	private const int pointsPerMesh = 60000;
	[SerializeField, HideInInspector]
	private Vector3[] points;
	private Color[] colors;
	Vector3 center;

	public RoofClassifier RoofClassifier;
	
	public void Load(Vector3[] points) {
		this.points = points;
		this.moveToCenter();
		

		Debug.Log("Loaded " + this.points.Length + " points.");
	}

	public void ClassifyAndShow() {
		this.transform.DestroyAllChildren();
		this.runClassifier();

		for (int start = 0; start < points.Length; start += pointsPerMesh) {
			this.createMeshObject(start, Math.Min(start + pointsPerMesh, points.Length - 1));
		}
	}

	private void runClassifier() {
		this.RoofClassifier = new RoofClassifier(this.points);
		RoofClassifier.Classify();

		this.colors = new Color[this.points.Length];
		for (int i = 0; i < this.points.Length; i++) {
			this.colors[i] = RoofClassifier.PointColoring[i];
		}
	}

	private void moveToCenter() {
		float minX = points[0].x, minY = points[0].y, minZ = points[0].z, maxX = points[0].x, maxY = points[0].y, maxZ = points[0].z;

		foreach (var point in this.points) {
			if (point.x < minX) minX = point.x;
			if (point.y < minY) minY = point.y;
			if (point.x < minZ) minZ = point.z;
			if (point.x > maxX) maxX = point.x;
			if (point.y > maxY) maxY = point.y;
			if (point.x > maxZ) maxZ = point.z;
		}
		this.center = new Vector3(Mathf.Lerp(minX, maxX, 0.5f), minY, Mathf.Lerp(minZ, maxZ, 0.5f));
		for (int i = 0; i < this.points.Length; i++) {
			this.points[i] = this.points[i] - this.center;
		}
	}

	private void createMeshObject(int fromIndex, int toIndex) {
		var prefab = Resources.Load("PointMesh") as GameObject;
		var gameObject = GameObject.Instantiate(prefab) as GameObject;
		gameObject.transform.parent = this.transform;
		var mesh = new Mesh();
		gameObject.GetComponent<MeshFilter>().mesh = mesh;

		int[] indecies = new int[toIndex - fromIndex];
		Vector3[] meshPoints = new Vector3[toIndex - fromIndex];
		Color[] meshColors = new Color[toIndex - fromIndex];
		for (int i = fromIndex; i < toIndex; ++i) {
			indecies[i - fromIndex] = i - fromIndex;
			meshPoints[i - fromIndex] = this.points[i];
			meshColors[i - fromIndex] = this.colors[i];
		}

		mesh.vertices = meshPoints;
		mesh.colors = meshColors;
		mesh.SetIndices(indecies, MeshTopology.Points, 0);		
	}
}
