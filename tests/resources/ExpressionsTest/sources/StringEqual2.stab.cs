using java.lang;

public class StringEqual2 {
	public static bool test() {
		return StringEqual2Aux.STR == "s" + "t" + "r";
	}
}

public class StringEqual2Aux {
	public static final string STR = "str";
}