// http://wiki.unity3d.com/index.php?title=Triangulator

using UnityEngine;
using System.Collections.Generic;

public class Triangulator {
	private List<Vector2> m_points = new List<Vector2>();

	public Triangulator(Vector2[] points) {
		m_points = new List<Vector2>(points);
	}

	public int[] Triangulate() {
		List<int> indices = new List<int>();

		int n = m_points.Count;
		if (n < 3) {
			return indices.ToArray();
		}

		int[] V = new int[n];
		if (Area() > 0) {
			for (int i = 0; i < n; i++) {
				V[i] = i;
			}
		} else {
			for (int i = 0; i < n; i++)
				V[i] = (n - 1) - i;
		}

		int verticesRemaining = n;
		int count = 2 * verticesRemaining;
		int v = verticesRemaining - 1;
		while (verticesRemaining > 2) {
			if ((count--) == 0) {
				return indices.ToArray();
			}

			int u = v;
			if (u >= verticesRemaining) {
				u = 0;
			}
			v = u + 1;
			if (v >= verticesRemaining) {
				v = 0;
			}
			int w = v + 1;
			if (w >= verticesRemaining) {
				w = 0;
			}

			if (Snip(u, v, w, verticesRemaining, V)) {
				indices.Add(V[u]);
				indices.Add(V[v]);
				indices.Add(V[w]);

				for (int i = v; i + 1 < verticesRemaining; i++) {
					V[i] = V[i + 1];
				}

				verticesRemaining--;
				count = 2 * verticesRemaining;
			}
		}

		indices.Reverse();
		return indices.ToArray();
	}

	private float Area() {
		int n = m_points.Count;
		float A = 0.0f;
		for (int p = n - 1, q = 0; q < n; p = q++) {
			Vector2 pval = m_points[p];
			Vector2 qval = m_points[q];
			A += pval.x * qval.y - qval.x * pval.y;
		}
		return (A * 0.5f);
	}

	private bool Snip(int u, int v, int w, int n, int[] V) {
		int p;
		Vector2 A = m_points[V[u]];
		Vector2 B = m_points[V[v]];
		Vector2 C = m_points[V[w]];
		if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
			return false;
		for (p = 0; p < n; p++) {
			if ((p == u) || (p == v) || (p == w))
				continue;
			Vector2 P = m_points[V[p]];
			if (InsideTriangle(A, B, C, P))
				return false;
		}
		return true;
	}

	private bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P) {
		float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
		float cCROSSap, bCROSScp, aCROSSbp;

		ax = C.x - B.x; ay = C.y - B.y;
		bx = A.x - C.x; by = A.y - C.y;
		cx = B.x - A.x; cy = B.y - A.y;
		apx = P.x - A.x; apy = P.y - A.y;
		bpx = P.x - B.x; bpy = P.y - B.y;
		cpx = P.x - C.x; cpy = P.y - C.y;

		aCROSSbp = ax * bpy - ay * bpx;
		cCROSSap = cx * apy - cy * apx;
		bCROSScp = bx * cpy - by * cpx;

		return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
	}
}