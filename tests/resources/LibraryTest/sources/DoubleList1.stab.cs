using stab.query;

public class DoubleList1 {
	public static bool test() {
		var it = Query.asIterable(new[] { 1d, 2d, 3d });
		var list = it.toList();
		return list.sequenceEqual(it);
	}
}
