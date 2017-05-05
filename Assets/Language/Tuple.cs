public class Tuple<T1, T2> {
	public readonly T1 Value1;
	public readonly T2 Value2;

	public Tuple(T1 value1, T2 value2) {
		this.Value1 = value1;
		this.Value2 = value2;
	}

	public override bool Equals(object obj) {
		if (!(obj is Tuple<T1, T2>)) {
			return false;
		}
		var tuple = obj as Tuple<T1, T2>;
		return tuple.Value1.Equals(this.Value1) && tuple.Value2.Equals(this.Value2);
	}

	public override int GetHashCode() {
		return this.Value1.GetHashCode() + this.Value2.GetHashCode();
	}

	public override string ToString() {
		return "(" + this.Value1.ToString() + ", " + this.Value2.ToString() + ")";
	}
}

public class Tuple<T1, T2, T3> {
	public readonly T1 Value1;
	public readonly T2 Value2;
	public readonly T3 Value3;

	public Tuple(T1 value1, T2 value2, T3 value3) {
		this.Value1 = value1;
		this.Value2 = value2;
		this.Value3 = value3;
	}

	public override bool Equals(object obj) {
		if (!(obj is Tuple<T1, T2, T3>)) {
			return false;
		}
		var tuple = obj as Tuple<T1, T2, T3>;
		return tuple.Value1.Equals(this.Value1) && tuple.Value2.Equals(this.Value2) && tuple.Value3.Equals(this.Value3);
	}

	public override int GetHashCode() {
		return this.Value1.GetHashCode() + this.Value2.GetHashCode() + this.Value3.GetHashCode();
	}

	public override string ToString() {
		return "(" + this.Value1.ToString() + ", " + this.Value2.ToString() + ", " + this.Value3.ToString() + ")";
	}
}