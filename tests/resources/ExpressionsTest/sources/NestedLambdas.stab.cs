using java.lang;
using java.util;
using stab.query;

public class NestedLambdas {
	public static bool test() {
		var l = new ArrayList<string> { "aa", "bb", "cc" };
		var l2 = new ArrayList<string> { "a", "b", "c" };
		return l.where(str => l2.where(p => str.startsWith(p)).any()).count() == 3;
	}
}
