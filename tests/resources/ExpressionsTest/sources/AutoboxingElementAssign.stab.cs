using java.lang;
using java.util;

public class AutoboxingElementAssign {
	public static int test() {
		int i = 1;
		var m = new HashMap<string, Integer>();
		m["test"] = i++;
		return m["test"];
	}
}
