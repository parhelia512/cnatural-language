using java.lang;
using java.util;
using stab.query;

public class ToMap2 {
	public static bool test() {
		var map1 = new HashMap<string, string> { { "K1", "V1" }, { "K2", "V2" }, { "K3", "V3" }};
		var list = new ArrayList<string> { "V1", "V2", "V3" };
		var map2 = list.toMap(p => "K" + p.substring(1));
		int i = 0;
		foreach (var k in map2.keySet()) {
			if (!map1[k].equals(map2[k])) {
				return false;
			}
			i++;
		}
		return map2.size() == 3 && i == 3;
	}
}
