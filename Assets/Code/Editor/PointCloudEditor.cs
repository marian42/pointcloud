using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System;


[CustomEditor(typeof(PointCloud))]
public class PointCloudEditor : Editor {
	private static AbstractPlaneFinder.Type classifierType = AbstractPlaneFinder.Type.Ransac;
	private static AbstractMeshCreator.Type meshCreatorType = AbstractMeshCreator.Type.CutoffWithAttachments;
	private static bool showPlanes = false;
	private static bool cleanMesh = true;

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

		classifierType = (AbstractPlaneFinder.Type)(EditorGUILayout.EnumPopup(classifierType));

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

		PointCloudEditor.cleanMesh = EditorGUILayout.Toggle("Clean mesh", PointCloudEditor.cleanMesh);

		GUILayout.BeginHorizontal();

		meshCreatorType = (AbstractMeshCreator.Type)(EditorGUILayout.EnumPopup(meshCreatorType));

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

	private void findPlanes(PointCloud pointCloud, AbstractPlaneFinder.Type type) {
		PlaneBehaviour.DeletePlanesIn(pointCloud.transform);
		PointCloudEditor.DeleteMeshesIn(pointCloud.transform);
		var planeClassifier = AbstractPlaneFinder.Instantiate(type, pointCloud);
		planeClassifier.Classify();
		planeClassifier.RemoveGroundPlanesAndVerticalPlanes();
		if (PointCloudEditor.showPlanes) {
			planeClassifier.DisplayPlanes(6);
		}
		pointCloud.Planes = planeClassifier.PlanesWithScore.OrderByDescending(t => t.Value2).Take(10).Select(t => t.Value1).ToList();
		Debug.Log(Timekeeping.GetStatus() + " -> " + planeClassifier.PlanesWithScore.Count() + " planes out of " + pointCloud.Points.Length + " points.");
	}

	private void createMesh(PointCloud pointCloud, AbstractMeshCreator.Type type) {
		PointCloudEditor.DeleteMeshesIn(pointCloud.transform);
		var meshCreator = AbstractMeshCreator.CreateMesh(pointCloud, type, PointCloudEditor.cleanMesh);
		meshCreator.DisplayMesh();
		meshCreator.SaveMesh();
		Debug.Log(Timekeeping.GetStatus());
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
