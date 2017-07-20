using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System;


[CustomEditor(typeof(PointCloudBehaviour))]
public class PointCloudEditor : Editor {
	private static AbstractPlaneFinder.Type planeClassifierType = AbstractPlaneFinder.Type.Ransac;
	private static AbstractMeshCreator.Type meshCreatorType = AbstractMeshCreator.Type.CutoffWithAttachments;
	private static bool showPlanes = false;
	private static bool cleanMesh = true;

	public override void OnInspectorGUI() {
		base.OnInspectorGUI();

		var pointCloudBehaviour = this.target as PointCloudBehaviour;
		var pointCloud = pointCloudBehaviour.PointCloud;

		PointCloudEditor.showPlanes = EditorGUILayout.Toggle("Display planes", PointCloudEditor.showPlanes);

		GUILayout.BeginHorizontal();

		planeClassifierType = (AbstractPlaneFinder.Type)(EditorGUILayout.EnumPopup(planeClassifierType));

		if (GUILayout.Button("Find planes")) {
			this.findPlanes(pointCloudBehaviour, planeClassifierType);
		}

		if (GUILayout.Button("Find all")) {
			DateTime start = DateTime.Now;
			foreach (var otherPointCloud in BuildingLoader.Instance.GetLoadedPointClouds()) {
				this.findPlanes(otherPointCloud, planeClassifierType);
			}
			var time = DateTime.Now - start;
			Debug.Log("Classified all planes in " + (int)System.Math.Floor(time.TotalMinutes) + ":" + time.Seconds.ToString().PadLeft(2, '0'));
		}

		GUILayout.EndHorizontal();

		PointCloudEditor.cleanMesh = EditorGUILayout.Toggle("Clean mesh", PointCloudEditor.cleanMesh);

		GUILayout.BeginHorizontal();

		meshCreatorType = (AbstractMeshCreator.Type)(EditorGUILayout.EnumPopup(meshCreatorType));

		if (GUILayout.Button("Create mesh")) {
			this.createMesh(pointCloudBehaviour, meshCreatorType);
		}

		if (GUILayout.Button("Create all")) {
			DateTime start = DateTime.Now;
			foreach (var otherPointCloud in BuildingLoader.Instance.GetLoadedPointClouds()) {
				this.createMesh(pointCloudBehaviour, meshCreatorType);
			}
			var time = DateTime.Now - start;
			Debug.Log("Created all meshes in " + (int)System.Math.Floor(time.TotalMinutes) + ":" + time.Seconds.ToString().PadLeft(2, '0'));
		}

		GUILayout.EndHorizontal();
	}

	private void hideSelectionHighlight() {
		foreach (var renderer in (this.target as PointCloudBehaviour).transform.GetComponentsInChildren<Renderer>()) {
			EditorUtility.SetSelectedRenderState(renderer, EditorSelectedRenderState.Hidden);
		}
	}

	public void OnSceneGUI() {
		hideSelectionHighlight();
	}

	private void findPlanes(PointCloudBehaviour pointCloudBehaviour, AbstractPlaneFinder.Type type) {
		PlaneBehaviour.DeletePlanesIn(pointCloudBehaviour.transform);
		PointCloudEditor.DeleteMeshesIn(pointCloudBehaviour.transform);
		var planeClassifier = AbstractPlaneFinder.Instantiate(type, pointCloudBehaviour.PointCloud);
		planeClassifier.Classify();
		planeClassifier.RemoveGroundPlanesAndVerticalPlanes();
		if (PointCloudEditor.showPlanes) {
			foreach (var tuple in planeClassifier.PlanesWithScore.OrderByDescending(t => t.Value2).Take(6)) {
				var plane = tuple.Value1;
				PlaneBehaviour.DisplayPlane(plane, pointCloudBehaviour);
			}
		}

		pointCloudBehaviour.PointCloud.Planes = planeClassifier.PlanesWithScore.OrderByDescending(t => t.Value2).Take(10).Select(t => t.Value1).ToList();
		Debug.Log(Timekeeping.GetStatus() + " -> " + planeClassifier.PlanesWithScore.Count() + " planes out of " + pointCloudBehaviour.PointCloud.Points.Length + " points.");
	}

	private void createMesh(PointCloudBehaviour pointCloudBehaviour, AbstractMeshCreator.Type type) {
		if (pointCloudBehaviour.PointCloud.Planes == null) {
			this.findPlanes(pointCloudBehaviour, planeClassifierType);
		}

		PointCloudEditor.DeleteMeshesIn(pointCloudBehaviour.transform);
		var meshCreator = AbstractMeshCreator.CreateMesh(pointCloudBehaviour.PointCloud, type, PointCloudEditor.cleanMesh);
		pointCloudBehaviour.DisplayMesh(meshCreator.GetMesh());
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
