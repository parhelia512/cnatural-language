using java.lang;
using java.util;
using stab.query;

public class SequenceEqual2 {
	public static bool test() {
		return Query.range(0, 20).where(p => p % 3 == 0).sequenceEqual(Query.range(0, 7).select(p => p * 3));
	}
}
