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

	public static IEnumerable<T> Yield<T>(this T item) {
		yield return item;
	}

	public static IEnumerable<T> NonNull<T>(this IEnumerable<T> collection) {
		return collection.Where(item => item != null);
	}

	public static IEnumerable<IEnumerable<T>> Subsets<T>(this IEnumerable<T> source) {
		List<T> list = source.ToList();
		int length = list.Count;
		int max = (int)System.Math.Pow(2, list.Count);

		for (int count = 0; count < max; count++) {
			List<T> subset = new List<T>();
			uint rs = 0;
			while (rs < length) {
				if ((count & (1u << (int)rs)) > 0) {
					subset.Add(list[(int)rs]);
				}
				rs++;
			}
			yield return subset;
		}
	}
}