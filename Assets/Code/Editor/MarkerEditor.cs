using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System;
using UnitySlippyMap.Markers;


[CustomEditor(typeof(LocationMarkerBehaviour))]
public class MarkerEditor : Editor {

	public override void OnInspectorGUI() {
		base.OnInspectorGUI();

		var marker = this.target as LocationMarkerBehaviour;

		if (GUILayout.Button("Target")) {
			marker.Map.CenterEPSG900913 = marker.CoordinatesEPSG900913;
		}
	}
}
