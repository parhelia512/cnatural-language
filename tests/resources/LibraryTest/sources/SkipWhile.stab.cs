using java.lang;
using java.util;
using stab.query;

public class SkipWhile {
	public static bool test() {
		var list = new ArrayList<string> { "a1", "a2", "b1", "b2" };
		return list.skipWhile(p => p.startsWith("a")).sequenceEqual(Query.pair("b1", "b2"));
	}
}
