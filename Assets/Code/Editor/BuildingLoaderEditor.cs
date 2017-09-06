using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System;

using UnitySlippyMap;


[CustomEditor(typeof(BuildingLoader))]
public class BuildingLoaderEditor : Editor {
	
	public override void OnInspectorGUI() {
		base.OnInspectorGUI();

		if (!Application.isPlaying) {
			return;
		}

		var loader = this.target as BuildingLoader;

		if (GUILayout.Button("Update")) {
			loader.UpdateBuildings();
		}

		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Unload all")) {
			loader.UnloadBuildings(0.0f);
		}

		GUILayout.EndHorizontal();
	}
}
