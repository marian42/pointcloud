using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BuildingMetadata : ISerializationCallbackReceiver {
	public string address;
	public string schwerp_x;
	public string schwerp_y;
	public string filename;

	[System.NonSerialized]
	public double[] Coordinates;

	public void OnBeforeSerialize() { }

	public void OnAfterDeserialize() {
		this.Coordinates = new double[] {
			double.Parse(this.schwerp_x.Replace(',', '.')),
			double.Parse(this.schwerp_y.Replace(',', '.'))
		};
	}
}
