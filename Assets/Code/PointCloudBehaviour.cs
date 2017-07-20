using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using System.Linq;
using System.IO;

[SelectionBase]
public class PointCloudBehaviour : MonoBehaviour {
	private const int pointsPerMesh = 60000;

	public PointCloud PointCloud;

	public void Initialize(PointCloud pointCloud) {
		this.PointCloud = pointCloud;
		this.gameObject.name = pointCloud.Metadata.address;
		this.transform.position = Vector3.down * this.PointCloud.GroundPoint.y;
		this.Show();
	}

	public void Show() {
		this.deleteMeshes();
		for (int start = 0; start < this.PointCloud.Points.Length; start += pointsPerMesh) {
			this.createMeshObject(start, Math.Min(start + pointsPerMesh, this.PointCloud.Points.Length - 1));
		}
	}

	private void createMeshObject(int fromIndex, int toIndex) {
		var prefab = Resources.Load("Prefabs/PointMesh") as GameObject;
		var gameObject = GameObject.Instantiate(prefab) as GameObject;
		gameObject.layer = 8;
		gameObject.transform.parent = this.transform;
		gameObject.transform.localPosition = Vector3.zero;
		var mesh = new Mesh();
		gameObject.GetComponent<MeshFilter>().mesh = mesh;

		int[] indecies = new int[toIndex - fromIndex];
		Vector3[] meshPoints = new Vector3[toIndex - fromIndex];
		Color[] meshColors = new Color[toIndex - fromIndex];
		for (int i = fromIndex; i < toIndex; ++i) {
			indecies[i - fromIndex] = i - fromIndex;
			meshPoints[i - fromIndex] = this.PointCloud.Points[i];
			meshColors[i - fromIndex] = this.PointCloud.Colors[i];
		}

		mesh.vertices = meshPoints;
		mesh.colors = meshColors;
		mesh.SetIndices(indecies, MeshTopology.Points, 0);
	}

	private void deleteMeshes() {
		var existingMeshes = new List<GameObject>();
		foreach (var child in this.transform) {
			if ((child as Transform).tag == "PointMesh") {
				existingMeshes.Add((child as Transform).gameObject);
			}
		}
		foreach (var existingQuad in existingMeshes) {
			GameObject.DestroyImmediate(existingQuad);
		}
	}

	public void DisplayNormals() {
		if (this.PointCloud.Normals == null) {
			Debug.LogError("No normals found.");
			return;
		}
		for (int i = 0; i < this.PointCloud.Normals.Length; i++) {
			var start = this.PointCloud.Points[i];
			Debug.DrawLine(start, start + this.PointCloud.Normals[i], Color.blue, 10.0f);
		}
	}

	[MenuItem("File/Load pointcloud...")]
	public static void LoadSingle() {
		string selected = EditorUtility.OpenFilePanel("Load file", Application.dataPath + "/data/buildings/", null);
		if (selected.Any() && File.Exists(selected)) {
			GameObject gameObject = new GameObject();
			var pointCloudBehaviour = gameObject.AddComponent<PointCloudBehaviour>();
			pointCloudBehaviour.Initialize(new PointCloud(selected));
			Selection.activeTransform = pointCloudBehaviour.transform;
			SceneView.lastActiveSceneView.FrameSelected();
		}
	}

	[MenuItem("File/Load all pointclouds")]
	public static void LoadFolder() {
		var folder = new DirectoryInfo(Application.dataPath + "/data/buildings/");
		foreach (var xyzFile in folder.GetFiles()) {
			if (xyzFile.Extension.ToLower() != ".xyz" && xyzFile.Extension.ToLower() != ".points") {
				continue;
			}
			GameObject gameObject = new GameObject();
			var pointCloudBehaviour = gameObject.AddComponent<PointCloudBehaviour>();
			pointCloudBehaviour.Initialize(new PointCloud(xyzFile.FullName));
		}
	}

	public void DisplayMesh(Mesh mesh) {
		var material = Resources.Load("Materials/MeshMaterial", typeof(Material)) as Material;
		var gameObject = new GameObject();
		gameObject.transform.parent = this.transform;
		gameObject.tag = "RoofMesh";
		gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
		gameObject.AddComponent<MeshRenderer>().material = material;
		gameObject.transform.localPosition = Vector3.zero;
		gameObject.name = "Shape";
		gameObject.layer = 10;
	}
}
