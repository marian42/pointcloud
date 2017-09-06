using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Options : MonoBehaviour {
	public static Options Instance { get; private set; }

	public string PointDataFolder;
	public string MetadataFile;
	
	public void Start() {
		Options.Instance = this;

		if (!this.PointDataFolder.EndsWith("/") && !this.PointDataFolder.EndsWith("\\")) {
			this.PointDataFolder += "/";
		}
	}

	public static string CleanPath(string path) {
		if (path.Contains(":")) {
			return path;
		} else {
			return System.IO.Path.Combine(Application.dataPath, path);
		}
	}
}
