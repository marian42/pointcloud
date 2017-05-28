using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System;


[CustomEditor(typeof(PointCloud))]
public class PointCloudEditor : Editor {
	private static PlaneClassifier.Type classifierType = PlaneClassifier.Type.Ransac;

	public override void OnInspectorGUI() {
		base.OnInspectorGUI();

		var pointCloud = this.target as PointCloud;

		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Load...")) {
			string selected = EditorUtility.OpenFilePanel("Load file", Application.dataPath, "xyz");
			if (selected.Any() && File.Exists(selected)) {
				pointCloud.Load(selected);
				pointCloud.Show();
			}
		}

		if (GUILayout.Button("Load folder...")) {
			var folder = new DirectoryInfo(PointCloud.GetDataPath());
			foreach (var xyzFile in folder.GetFiles()) {
				if (xyzFile.Extension.ToLower() != ".xyz") {
					continue;
				}
				GameObject go = new GameObject();
				go.name = xyzFile.Name.Substring(0, xyzFile.Name.Length - xyzFile.Extension.Length);
				var newPointCloud = go.AddComponent<PointCloud>();
				newPointCloud.Load(xyzFile.FullName);
				newPointCloud.Show();
			}
		}

		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Estimate normals")) {
			pointCloud.EstimateNormals();
		}

		if (GUILayout.Button("Show normals")) {
			pointCloud.DisplayNormals();
		}

		GUILayout.EndHorizontal();

		if (GUILayout.Button("Reset colors")) {
			pointCloud.ResetColors(Color.red);
			pointCloud.Show();
		}

		if (GUILayout.Button("Classify by ridge")) {
			var roofClassifier = new RidgeFirstClassifier(pointCloud);
			roofClassifier.Classify();
			pointCloud.Show();
		}

		GUILayout.BeginHorizontal();

		classifierType = (PlaneClassifier.Type)(EditorGUILayout.EnumPopup(classifierType));

		if (GUILayout.Button("Find Planes")) {
			this.findPlanes(pointCloud, classifierType);
		}

		if (GUILayout.Button("Find all")) {
			DateTime start = DateTime.Now;
			foreach (var otherPointCloud in GameObject.FindObjectsOfType<PointCloud>()) {
				this.findPlanes(otherPointCloud, classifierType);
			}
			var time = DateTime.Now - start;
			Debug.Log("Classified all planes in " + (int)System.Math.Floor(time.TotalMinutes) + ":" + time.Seconds.ToString().PadLeft(2, '0'));
		}

		GUILayout.EndHorizontal();

		if (GUILayout.Button("Create mesh")) {
			PointCloudEditor.DeleteMeshesIn(pointCloud.transform);
			var meshCreator = new MeshCreator(pointCloud);
			meshCreator.CreateMesh();
			meshCreator.DisplayMesh();
		}
	}

	private void hideSelectionHighlight() {
		foreach (var renderer in (this.target as PointCloud).transform.GetComponentsInChildren<Renderer>()) {
			EditorUtility.SetSelectedRenderState(renderer, EditorSelectedRenderState.Hidden);
		}
	}

	public void OnSceneGUI() {
		hideSelectionHighlight();
	}

	private void findPlanes(PointCloud pointCloud, PlaneClassifier.Type type) {
		PlaneBehaviour.DeletePlanesIn(pointCloud.transform);
		PointCloudEditor.DeleteMeshesIn(pointCloud.transform);
		var planeClassifier = PlaneClassifier.Instantiate(type, pointCloud);
		planeClassifier.Classify();
		planeClassifier.RemoveGroundPlanes();
		planeClassifier.DisplayPlanes(6);
		pointCloud.Planes = planeClassifier.PlanesWithScore.OrderByDescending(t => t.Value2).Take(10).Select(t => t.Value1).ToList();
	}

	public static void DeleteMeshesIn(Transform transform) {
		var existingMeshes = new List<GameObject>();
		foreach (var child in transform) {
			if ((child as Transform).tag == "RoofMesh") {
				existingMeshes.Add((child as Transform).gameObject);
			}
		}
		foreach (var existingMesh in existingMeshes) {
			GameObject.DestroyImmediate(existingMesh);
		}
	}
}
