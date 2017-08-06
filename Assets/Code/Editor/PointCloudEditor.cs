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

		if (GUILayout.Button("Show normals")) {
			pointCloudBehaviour.DisplayNormals();
		}

		PointCloudEditor.showPlanes = EditorGUILayout.Toggle("Display planes", PointCloudEditor.showPlanes);

		GUILayout.BeginHorizontal();

		planeClassifierType = (AbstractPlaneFinder.Type)(EditorGUILayout.EnumPopup(planeClassifierType));

		if (GUILayout.Button("Find planes")) {
			pointCloudBehaviour.FindPlanes(planeClassifierType, showPlanes);
		}

		if (GUILayout.Button("Find all")) {
			DateTime start = DateTime.Now;
			foreach (var otherPointCloud in BuildingLoader.Instance.GetLoadedPointClouds()) {
				otherPointCloud.FindPlanes(planeClassifierType, showPlanes);
			}
			var time = DateTime.Now - start;
			Debug.Log("Classified all planes in " + (int)System.Math.Floor(time.TotalMinutes) + ":" + time.Seconds.ToString().PadLeft(2, '0'));
		}

		GUILayout.EndHorizontal();

		PointCloudEditor.cleanMesh = EditorGUILayout.Toggle("Clean mesh", PointCloudEditor.cleanMesh);

		GUILayout.BeginHorizontal();

		meshCreatorType = (AbstractMeshCreator.Type)(EditorGUILayout.EnumPopup(meshCreatorType));

		if (GUILayout.Button("Create mesh")) {
			if (pointCloudBehaviour.PointCloud.Planes == null) {
				pointCloudBehaviour.FindPlanes(planeClassifierType, showPlanes);
			}
			pointCloudBehaviour.CreateMesh(meshCreatorType, cleanMesh);
		}

		if (GUILayout.Button("Create all")) {
			DateTime start = DateTime.Now;
			foreach (var otherPointCloud in BuildingLoader.Instance.GetLoadedPointClouds()) {
				if (otherPointCloud.PointCloud.Planes == null) {
					otherPointCloud.FindPlanes(planeClassifierType, showPlanes);
				}
				otherPointCloud.CreateMesh(meshCreatorType, cleanMesh);
			}
			var time = DateTime.Now - start;
			Debug.Log("Created all meshes in " + (int)System.Math.Floor(time.TotalMinutes) + ":" + time.Seconds.ToString().PadLeft(2, '0'));
		}

		GUILayout.EndHorizontal();

		if (GUILayout.Button("Isolate")) {
			BuildingLoader.Instance.Isolate(pointCloud);
		}
	}

	private void hideSelectionHighlight() {
		foreach (var renderer in (this.target as PointCloudBehaviour).transform.GetComponentsInChildren<Renderer>()) {
			EditorUtility.SetSelectedRenderState(renderer, EditorSelectedRenderState.Hidden);
		}
	}

	public void OnSceneGUI() {
		hideSelectionHighlight();
	}
}
