using java.lang;

public class InheritedMethodCall {
	public static string test() {
		var sb = new StringBuilder();
		sb.append("ABC");
		sb.setLength(2);
		return sb.toString();
	}
}
