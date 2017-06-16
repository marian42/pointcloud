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
	private static MeshCreator.Type meshCreatorType = MeshCreator.Type.CutoffWithAttachments;
	private static bool showPlanes = false;

	public override void OnInspectorGUI() {
		base.OnInspectorGUI();

		var pointCloud = this.target as PointCloud;

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

		PointCloudEditor.showPlanes = EditorGUILayout.Toggle("Display planes", PointCloudEditor.showPlanes);

		GUILayout.BeginHorizontal();

		classifierType = (PlaneClassifier.Type)(EditorGUILayout.EnumPopup(classifierType));

		if (GUILayout.Button("Find planes")) {
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

		GUILayout.BeginHorizontal();

		meshCreatorType = (MeshCreator.Type)(EditorGUILayout.EnumPopup(meshCreatorType));

		if (GUILayout.Button("Create mesh")) {
			this.createMesh(pointCloud, meshCreatorType);
		}

		if (GUILayout.Button("Create all")) {
			DateTime start = DateTime.Now;
			foreach (var otherPointCloud in GameObject.FindObjectsOfType<PointCloud>()) {
				this.createMesh(otherPointCloud, meshCreatorType);
			}
			var time = DateTime.Now - start;
			Debug.Log("Created all meshes in " + (int)System.Math.Floor(time.TotalMinutes) + ":" + time.Seconds.ToString().PadLeft(2, '0'));
		}

		GUILayout.EndHorizontal();
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
		planeClassifier.RemoveGroundPlanesAndVerticalPlanes();
		if (PointCloudEditor.showPlanes) {
			planeClassifier.DisplayPlanes(6);
		}
		pointCloud.Planes = planeClassifier.PlanesWithScore.OrderByDescending(t => t.Value2).Take(10).Select(t => t.Value1).ToList();
		Debug.Log(Timekeeping.GetStatus() + " -> " + planeClassifier.PlanesWithScore.Count() + " planes out of " + pointCloud.Points.Length + " points.");
	}

	private void createMesh(PointCloud pointCloud, MeshCreator.Type type) {
		PointCloudEditor.DeleteMeshesIn(pointCloud.transform);
		var meshCreator = new MeshCreator(pointCloud);
		meshCreator.CreateMesh(type);
		meshCreator.DisplayMesh();
		meshCreator.SaveMesh();
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
