// 
//  Tile.cs
//  
//  Author:
//       Jonathan Derrough <jonathan.derrough@gmail.com>
//  
//  Copyright (c) 2012 Jonathan Derrough
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using UnityEngine;

namespace UnitySlippyMap.Map {
	public class TileBehaviour : MonoBehaviour {
		public int TextureId;

		public bool Showing {
			get;
			private set;
		}

		private Material material;

		private float apparitionDuration = 0.2f;

		private float apparitionStartTime = 0.0f;


		private void Update() {
			if (this.Showing) {
				float delta = Time.time - apparitionStartTime;
				float a = 1.0f;
				if (delta <= apparitionDuration) {
					a = delta / apparitionDuration;
				} else {
					this.Showing = false;
					MapBehaviour.Instance.IsDirty = true;
				}
				Color color = material.color;
				color.a = a;
				material.color = color;
			}
		}

		public void Show() {
			this.Showing = true;
			Color color = material.color;
			color.a = 0.0f;
			material.color = color;
			apparitionStartTime = Time.time;
		}

		public static TileBehaviour CreateTileTemplate() {
			return CreateTileTemplate("[Tile Template]");
		}

		public static TileBehaviour CreateTileTemplate(string tileName) {
			GameObject tileTemplate = new GameObject(tileName);
			TileBehaviour tile = tileTemplate.AddComponent<TileBehaviour>();
			MeshFilter meshFilter = tileTemplate.AddComponent<MeshFilter>();
			MeshRenderer meshRenderer = tileTemplate.AddComponent<MeshRenderer>();
			BoxCollider boxCollider = tileTemplate.AddComponent<BoxCollider>();

			// add the geometry
			Mesh mesh = meshFilter.mesh;
			mesh.vertices = new Vector3[] {
				new Vector3 (0.5f, 0.0f, 0.5f),
				new Vector3 (0.5f, 0.0f, -0.5f),
				new Vector3 (-0.5f, 0.0f, -0.5f),
				new Vector3 (-0.5f, 0.0f, 0.5f)
			};

			mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };

			// add normals
			mesh.normals = new Vector3[] {
				Vector3.up,
				Vector3.up,
				Vector3.up,
				Vector3.up
			};
			// add uv coordinates
			mesh.uv = new Vector2[] {
				new Vector2 (1.0f, 1.0f),
				new Vector2 (1.0f, 0.0f),
				new Vector2 (0.0f, 0.0f),
				new Vector2 (0.0f, 1.0f)
			};

			// add a material
			string shaderName = "Larku/UnlitTransparent";
			Shader shader = Shader.Find(shaderName);

			tile.material = meshRenderer.material = new Material(shader);

			// setup the collider
			boxCollider.size = new Vector3(1.0f, 0.0f, 1.0f);

			return tile;
		}

		public void SetTexture(Texture2D texture) {
			material = this.gameObject.GetComponent<Renderer>().material;
			material.mainTexture = texture;
			material.mainTexture.wrapMode = TextureWrapMode.Clamp;
			material.mainTexture.filterMode = FilterMode.Trilinear;
			this.GetComponent<Renderer>().enabled = true;
			this.Show();
		}

		public static string GetTileKey(int roundedZoom, int tileX, int tileY) {
			return roundedZoom + "_" + tileX + "_" + tileY;
		}
	}

}