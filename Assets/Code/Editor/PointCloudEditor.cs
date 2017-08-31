using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System;


[CustomEditor(typeof(PointCloudBehaviour))]
public class PointCloudEditor : Editor {
	private static bool showPlanes = false;

	public override void OnInspectorGUI() {
		base.OnInspectorGUI();

		var pointCloudBehaviour = this.target as PointCloudBehaviour;
		var pointCloud = pointCloudBehaviour.PointCloud;

		if (GUILayout.Button("Show normals")) {
			pointCloudBehaviour.DisplayNormals();
		}

		PointCloudEditor.showPlanes = EditorGUILayout.Toggle("Display planes", PointCloudEditor.showPlanes);

		GUILayout.BeginHorizontal();

		AbstractPlaneFinder.CurrentType = (AbstractPlaneFinder.Type)(EditorGUILayout.EnumPopup(AbstractPlaneFinder.CurrentType));

		if (GUILayout.Button("Find planes")) {
			pointCloudBehaviour.FindPlanes(AbstractPlaneFinder.CurrentType, showPlanes);
		}

		if (GUILayout.Button("Find all")) {
			DateTime start = DateTime.Now;
			foreach (var otherPointCloud in BuildingLoader.Instance.GetLoadedPointClouds()) {
				otherPointCloud.FindPlanes(AbstractPlaneFinder.CurrentType, showPlanes);
			}
			var time = DateTime.Now - start;
			Debug.Log("Classified all planes in " + (int)System.Math.Floor(time.TotalMinutes) + ":" + time.Seconds.ToString().PadLeft(2, '0'));
		}

		GUILayout.EndHorizontal();

		ShapeMeshCreator.CleanMeshDefault = EditorGUILayout.Toggle("Clean mesh", ShapeMeshCreator.CleanMeshDefault);

		GUILayout.BeginHorizontal();

		AbstractMeshCreator.CurrentType = (AbstractMeshCreator.Type)(EditorGUILayout.EnumPopup(AbstractMeshCreator.CurrentType));

		if (GUILayout.Button("Create mesh")) {
			pointCloudBehaviour.CreateMesh(AbstractMeshCreator.CurrentType, ShapeMeshCreator.CleanMeshDefault);
		}

		if (GUILayout.Button("Create all")) {
			DateTime start = DateTime.Now;
			foreach (var otherPointCloud in BuildingLoader.Instance.GetLoadedPointClouds()) {
				otherPointCloud.CreateMesh(AbstractMeshCreator.CurrentType, ShapeMeshCreator.CleanMeshDefault);
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
