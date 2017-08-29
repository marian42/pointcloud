using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class Timekeeping {
	private class Task {
		public readonly string Name;
		public readonly TimeSpan TimeSpan;

		public Task(string name, TimeSpan timeSpan) {
			this.Name = name;
			this.TimeSpan = timeSpan;
		}

		public override string ToString() {
			return this.Name + ": " + this.GetDurationString() +"s";
		}

		public string GetDurationString() {
			return this.TimeSpan.TotalSeconds.ToString("0.00");
		}
	}

	private static List<Task> tasks;
	private static DateTime startTime;


	public static void Reset() {
		Timekeeping.tasks = new List<Task>();
		Timekeeping.start();
	}

	public static void CompleteTask(string name) {
		Timekeeping.tasks.Add(new Task(name, DateTime.Now - Timekeeping.startTime));
		Timekeeping.start();
	}

	public static string GetStatus() {
		return Timekeeping.tasks.Aggregate("", (s, t) => s + t.ToString() + ", ");
	}

	private static void start() {
		Timekeeping.startTime = DateTime.Now;
	}

	public static string CreateTableLine(string name, int rowCount) {
		return name + String.Concat(tasks.Select(t => " & " + t.GetDurationString()).Concat(Enumerable.Repeat(" &", rowCount - tasks.Count - 2)).ToArray())
			+ " & " + tasks.Select(t => t.TimeSpan).Aggregate(TimeSpan.FromTicks(0), (t1, t2) => t1.Add(t2)).TotalSeconds.ToString("0.00")
			+ " \\\\\n";
	}

	public static string GetDataLine(int rowCount) {
		return String.Concat(tasks.Select(t => t.GetDurationString() + ";").Concat(Enumerable.Repeat("0;", rowCount - tasks.Count - 1)).ToArray())
			+ tasks.Select(t => t.TimeSpan).Aggregate(TimeSpan.FromTicks(0), (t1, t2) => t1.Add(t2)).TotalSeconds.ToString("0.00") + ";";
	}
}
