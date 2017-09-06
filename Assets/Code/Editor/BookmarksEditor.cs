using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System;

using UnitySlippyMap;
using UnitySlippyMap.Map;


[CustomEditor(typeof(Bookmarks))]
public class BookmarksEditor : Editor {

	public override void OnInspectorGUI() {
		base.OnInspectorGUI();

		if (!Application.isPlaying) {
			return;
		}

		var bookmarks = this.target as Bookmarks;

		foreach (var bookmark in bookmarks.Items.Skip(1)) {
			if (GUILayout.Button(bookmark.Name)) {
				MapBehaviour.Instance.CenterWGS84 = bookmark.Coordinates;
			}
		}
	}
}
