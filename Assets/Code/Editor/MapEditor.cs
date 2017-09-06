using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System;

using UnitySlippyMap;
using UnitySlippyMap.Map;


[CustomEditor(typeof(MapBehaviour))]
public class MapEditor : Editor {

	public override void OnInspectorGUI() {
		base.OnInspectorGUI();

		if (!Application.isPlaying) {
			return;
		}

		var map = MapBehaviour.Instance;

		GUILayout.Label("WGS84");
		GUILayout.BeginHorizontal();
		GUILayout.Label("Latitude:");
		GUILayout.TextField(map.CenterWGS84[0].ToString());
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("Longitude:");
		GUILayout.TextField(map.CenterWGS84[1].ToString());
		GUILayout.EndHorizontal();

		GUILayout.Label("EPSG900913");
		GUILayout.BeginHorizontal();
		GUILayout.Label("X:");
		GUILayout.TextField(map.CenterEPSG900913[0].ToString());
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("Y:");
		GUILayout.TextField(map.CenterEPSG900913[1].ToString());
		GUILayout.EndHorizontal();

		var meters = BuildingLoader.latLonToMeters(map.CenterWGS84);
		GUILayout.Label("WGS84 UTM Zone 32");
		GUILayout.BeginHorizontal();
		GUILayout.Label("X:");
		GUILayout.TextField(meters[0].ToString());
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("Y:");
		GUILayout.TextField(meters[1].ToString());
		GUILayout.EndHorizontal();
	}
}
