using java.lang;
// string must be fully qualified since consecutive using clauses do not have effects on others
using List = java.util.ArrayList<java.lang.String>;

public class UsingAliasGenerics {
	public static string test(string s1, string s2) {
		List l = new List();
		l.add(s1);
		l.add(s2);
		return l.get(1);
	}
}