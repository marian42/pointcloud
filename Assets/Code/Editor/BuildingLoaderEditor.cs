using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System;


[CustomEditor(typeof(BuildingLoader))]
public class BuildingLoaderEditor : Editor {
	
	public override void OnInspectorGUI() {
		base.OnInspectorGUI();

		var loader = this.target as BuildingLoader;

		if (GUILayout.Button("Update")) {
			loader.UpdateBuildings();
		}
	}
}
