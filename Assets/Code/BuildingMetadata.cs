using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BuildingMetadata {
	public string address;
	public string schwerp_x;
	public string schwerp_y;

	public double CenterX {
		get {
			return double.Parse(this.schwerp_x.Replace(',', '.'));
		}
	}

	public double CenterZ {
		get {
			return double.Parse(this.schwerp_y.Replace(',', '.'));
		}
	}
}
