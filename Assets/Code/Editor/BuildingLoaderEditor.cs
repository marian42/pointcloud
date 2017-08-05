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

		var loader = this.target as BuildingLoader;

		if (GUILayout.Button("Update")) {
			loader.UpdateBuildings();
		}

		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Unload all")) {
			loader.UnloadBuildings(0.0f);
		}

		if (GUILayout.Button("Unload others")) {
			loader.UnloadBuildings(2.0f);
		}

		GUILayout.EndHorizontal();

		var map = UnitySlippyMap.Map.MapBehaviour.Instance;

		EditorGUILayout.LabelField("Bookmarks", EditorStyles.boldLabel);

		if (GUILayout.Button("TU Dortmund")) {
			map.CenterWGS84 = new double[] { 7.4141978, 51.4921254 };
		}

		if (GUILayout.Button("Mitte")) {
			map.CenterWGS84 = new double[] { 7.4649531, 51.5139996 };
		}

		if (GUILayout.Button("Testdaten")) {
			map.CenterWGS84 = new double[] { 7.4402747, 51.5638601 };
		}

		GUILayout.TextField(map.CenterWGS84[0] + ", " + map.CenterWGS84[1]);
	}
}
