using java.lang;
using java.util;
using stab.query;

public class ToLongTMap5 {
	public static bool test() {
		var map1 = new HashMap<Long, string> { { 1L, "V1" }, { 2L, "V2" }, { 3L, "V3" }};
		var list = new ArrayList<string> { "V1", "V2", "V3" };
		var key = 1L;
		var map2 = list.toMap(p => key++);
		int i = 0;
		foreach (var k in map2.keySet()) {
			if (!map1[k].equals(map2.get(k))) {
				return false;
			}
			i++;
		}
		return map2.size() == 3 && i == 3;
	}
}
