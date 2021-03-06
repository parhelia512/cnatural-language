using java.lang;

public class Wildcard {
	public static bool test() {
		var w1 = new WildcardAux<Object>(new Object());
		var w2 = new WildcardAux<string>("STR");
		var o1 = test(w1);
		var o2 = test(w2);
		return o1 instanceof Object && o2 instanceof string;
	}
	
	public static Object test(WildcardAux<?> w) {
		return w.value;	
	}
}

public class WildcardAux<T> {
	public WildcardAux(T value) {
		this.value = value;	
	}
	public T value*;
}
