using java.lang;
using java.util;
using stab.query;

public class ToDoubleTMap2 {
	public static bool test() {
		var list = new ArrayList<string> { "A", "BB", "CCC" };
		var map = list.toMap(p => (double)(p.length() - 1));
		return map.get(1d).equals("BB");
	}
}
