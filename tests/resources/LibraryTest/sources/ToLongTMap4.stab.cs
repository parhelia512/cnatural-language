using java.lang;
using java.util;
using stab.query;

public class ToLongTMap4 {
	public static int test() {
		var map1 = new HashMap<Long, string> { { 1L, "V1" }, { 2L, "V2" }, { 3L, "V3" }};
		var map2 = Query.empty<string>().toMap(p => 0L);
		map2.putAll(map1);
		int i = 0;
		foreach (var v in map2.values()) {
			if (!map1.containsValue(v)) {
				return 1;
			}
			if (!map2.containsValue(v)) {
				return 2;
			}
			i++;
		}
		if (i != 3) {
			return 3;
		}
		return 0;
	}
}
