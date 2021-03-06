using java.lang;
using java.util;
using stab.query;

public class AllAny {
	public static bool test() {
		return new AllAny(new ArrayList<Class<?>> { typeof(Integer), typeof(Double) }).method(new ArrayList<Object> { new Integer(0), new Double(0.5)  });
	}
	
	Iterable<Class<?>> types;
	
	AllAny(Iterable<Class<?>> types) {
		this.types = types;
	}
	
	bool method(Iterable<Object> objects) {
		return objects.all(o => types.any(t => t.isInstance(o)));
	}
}
