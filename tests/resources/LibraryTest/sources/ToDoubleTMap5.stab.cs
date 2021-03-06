using java.lang;
using java.util;
using stab.query;

public class ToDoubleTMap5 {
	public static bool test() {
		var map1 = new HashMap<Double, string> { { 1d, "V1" }, { 2d, "V2" }, { 3d, "V3" }};
		var list = new ArrayList<string> { "V1", "V2", "V3" };
		var key = 1d;
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
