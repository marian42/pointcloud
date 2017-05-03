using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System;


[CustomEditor(typeof(PointCloud))]
public class PointCloudEditor : Editor {
	public override void OnInspectorGUI() {
		base.OnInspectorGUI();

		var pointCloud = this.target as PointCloud;

		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Load...")) {
			string selected = EditorUtility.OpenFilePanel("Load file", Application.dataPath, "xyz");
			if (selected.Any() && File.Exists(selected)) {
				pointCloud.Load(XYZLoader.LoadFile(selected));
				pointCloud.Show();
			}
		}

		GUILayout.EndHorizontal();

		if (GUILayout.Button("Classify by ridge")) {
			var roofClassifier = new RoofClassifier(pointCloud);
			roofClassifier.Classify();
			pointCloud.Show();
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
}
