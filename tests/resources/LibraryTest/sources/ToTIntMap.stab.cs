using java.lang;
using java.util;
using stab.query;

public class ToTIntMap {
	public static bool test() {
		var map = Query.asIterable(new[] { 1, 2, 3 }).toMap(p => "K" + p);
		return map.containsValue(1) &&
				map.containsValue(2) &&
				map.containsValue(3) &&
				!map.containsValue(4) &&
				map.containsKey("K1") &&
				map.containsKey("K2") &&
				map.containsKey("K3") &&
				!map.containsKey("K4");
	}
}
