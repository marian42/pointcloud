using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

[SelectionBase]
public class PointCloud : MonoBehaviour {
	private const int pointsPerMesh = 60000;
	[SerializeField, HideInInspector]
	public Vector3[] Points;
	[SerializeField, HideInInspector]
	public Color[] Colors;
	
	public void Load(Vector3[] points) {
		this.Points = points;
		this.moveToCenter();
		this.ResetColors(Color.red);
	}

	public void ResetColors(Color color) {
		this.Colors = new Color[this.Points.Length];
		for (int i = 0; i < this.Points.Length; i++) {
			this.Colors[i] = color;
		}
	}

	public void Show() {
		this.transform.DestroyAllChildren();
		for (int start = 0; start < Points.Length; start += pointsPerMesh) {
			this.createMeshObject(start, Math.Min(start + pointsPerMesh, Points.Length - 1));
		}
	}

	private void moveToCenter() {
		float minX = Points[0].x, minY = Points[0].y, minZ = Points[0].z, maxX = Points[0].x, maxY = Points[0].y, maxZ = Points[0].z;

		foreach (var point in this.Points) {
			if (point.x < minX) minX = point.x;
			if (point.y < minY) minY = point.y;
			if (point.x < minZ) minZ = point.z;
			if (point.x > maxX) maxX = point.x;
			if (point.y > maxY) maxY = point.y;
			if (point.x > maxZ) maxZ = point.z;
		}
		this.transform.position = new Vector3(Mathf.Lerp(minX, maxX, 0.5f), Mathf.Lerp(minY, maxY, 0.5f), Mathf.Lerp(minZ, maxZ, 0.5f));
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
			meshPoints[i - fromIndex] = this.Points[i];
			meshColors[i - fromIndex] = this.Colors[i];
		}

		mesh.vertices = meshPoints;
		mesh.colors = meshColors;
		mesh.SetIndices(indecies, MeshTopology.Points, 0);		
	}
}
