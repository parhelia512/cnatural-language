using java.lang;
using java.util;
using stab.query;

public class Any {
	public static bool test() {
		var list = new ArrayList<string> { "a", "bb", "ccc" };
		return list.any(p => p.length() > 2);
	}
}
