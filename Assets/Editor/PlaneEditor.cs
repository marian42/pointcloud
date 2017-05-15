using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System;

[CustomEditor(typeof(PlaneBehaviour))]
public class PlaneEditor : Editor {
	public override void OnInspectorGUI() {
		base.OnInspectorGUI();

		var planeBehaviour = this.target as PlaneBehaviour;

		if (GUILayout.Button("Show")) {
			planeBehaviour.ColorPoints();
		}
		if (GUILayout.Button("From Transform")) {
			planeBehaviour.ReadFromTransform();
		}
	}
}
