using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnitySlippyMap.Map;

[RequireComponent(typeof(MapBehaviour))]
public class Bookmarks : MonoBehaviour {
	[System.Serializable]
	public class Bookmark {
		public string Name;
		public double Latitude;
		public double Longitude;

		public double[] Coordinates {
			get {
				return new double[] {
					this.Latitude, this.Longitude
				};
			}
		}

		public override string ToString() {
			return this.Name;
		}
	}

	public Bookmark[] Items;
}
