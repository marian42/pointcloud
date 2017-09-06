using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Options : MonoBehaviour {
	public static Options Instance { get; private set; }

	public string PointDataFolder;
	public string MetadataFile;
	public string MeshOutputFolder;
	public bool SaveMeshes;

	private static string dataPath;

	public void Start() {
		Options.Instance = this;
		Options.dataPath = Application.dataPath;

		if (!this.PointDataFolder.EndsWith("/") && !this.PointDataFolder.EndsWith("\\")) {
			this.PointDataFolder += "/";
		}
	}

	public static string CleanPath(string path) {
		if (path.Contains(":")) {
			return path;
		} else {
			return System.IO.Path.Combine(dataPath, path);
		}
	}
}
