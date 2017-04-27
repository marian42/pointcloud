using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;

public static class TransformExtension {
	public static void DestroyAllChildren(this Transform transform) {
		var children = new List<GameObject>();
		foreach (Transform child in transform) children.Add(child.gameObject);
		children.ForEach(child => Transform.DestroyImmediate(child));
	}

	public static T TakeRandom<T>(this IEnumerable<T> list) {
		if (!list.Any()) {
			return default(T);
		}
		return list.ElementAt(Random.Range(0, list.Count()));
	}
}