using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Options : MonoBehaviour {
	public static Options Instance { get; private set; }

	public string PointDataFolder;
	public string MetadataFolder;
	public string MeshOutputFolder;
	public bool SaveMeshes;

	private static string dataPath;

	public void Start() {
		Options.Instance = this;
		Options.dataPath = Application.dataPath;

		if (!this.PointDataFolder.EndsWith("/") && !this.PointDataFolder.EndsWith("\\")) {
			this.PointDataFolder += "/";
		}
		if (!this.MetadataFolder.EndsWith("/") && !this.MetadataFolder.EndsWith("\\")) {
			this.MetadataFolder += "/";
		}
		if (!this.MeshOutputFolder.EndsWith("/") && !this.MeshOutputFolder.EndsWith("\\")) {
			this.MeshOutputFolder += "/";
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
