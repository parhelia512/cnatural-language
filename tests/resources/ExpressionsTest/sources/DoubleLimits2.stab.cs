
using java.lang;

public class DoubleLimits2 {
	public static bool test() {
		return 1f/0d == Double.POSITIVE_INFINITY && -1/0d == Double.NEGATIVE_INFINITY;
	}
}