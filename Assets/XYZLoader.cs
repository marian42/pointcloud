using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System;

public class XYZLoader {
	public static Vector3[] LoadFile(string fileName) {
		return File.ReadAllLines(fileName).Select(line => XYZLoader.parseLine(line)).ToArray();
	}

	private static Vector3 parseLine(string line) {
		try {
			var points = line.Split(' ').Where(s => s.Any()).Select(s => float.Parse(s)).ToArray();
			return new Vector3(points[0], points[2], points[1]);
		}
		catch (FormatException) {
			throw new Exception("Bad line: " + line);
		}		
	}
}
