using java.lang;

public class GenericOverload {
	public static int method<T>(T t) {
		return 1;
	}
	
	public static int method(string s) {
		return 2;
	}
	
	public static int test() {
		return method<string>("STR");
	}
}
