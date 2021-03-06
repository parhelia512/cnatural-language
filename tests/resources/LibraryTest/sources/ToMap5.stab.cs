using java.lang;
using java.util;
using stab.query;

public class ToMap5 {
	public static bool test() {
		var list = new ArrayList<string> { "V1", "V2", "V3" };
		var map = list.toMap(p => "K" + p.substring(1));
		return map.containsValue("V1") &&
				map.containsValue("V2") &&
				map.containsValue("V3") &&
				!map.containsValue("V4") &&
				map.containsKey("K1") &&
				map.containsKey("K2") &&
				map.containsKey("K3") &&
				!map.containsKey("K4");
	}
}
