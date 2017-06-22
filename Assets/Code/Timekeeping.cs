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
			return this.Name + ": " + this.TimeSpan.TotalSeconds.ToString("0.00") +"s";
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
}
