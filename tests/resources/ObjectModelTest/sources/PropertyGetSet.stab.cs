public class PropertyGetSet {
	public static int test() {
		var obj = new PropertyGetSet();
		obj.Value = 1;
		return obj.Value;
	}

	public int field*;
	
	public int Value {
		get {
			return field;
		}
		set {
			field = value;
		}
	}
}
