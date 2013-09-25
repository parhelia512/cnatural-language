using java.lang;
using java.util;
using stab.query;

public class ToDoubleTMap {
	public static bool test() {
		var list = new ArrayList<string> { "V1", "V2", "V3" };
		var k = 0d;
		var map = list.toMap(p => k++);
		return map.containsValue("V1") &&
				map.containsValue("V2") &&
				map.containsValue("V3") &&
				!map.containsValue("V4") &&
				map.containsKey(0d) &&
				map.containsKey(1d) &&
				map.containsKey(2d) &&
				!map.containsKey(3d);
	}
}
