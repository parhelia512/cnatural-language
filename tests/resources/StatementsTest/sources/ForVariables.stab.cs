public class ForVariables {
	public static bool test() {
		var v1 = 0;
		var v2 = 0;
		var v3 = 0;
		for (int j = 0, i = 0; i < j + 1; j = 1, i++) {
			v1 = j;
			v2 += 1;
		}
		v3 += 1;
		return v1 == 1 && v2 == 2 && v3 == 1;
	}
}