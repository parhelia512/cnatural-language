namespace p {

	using java.lang;
	using stab.lang;

	public class ForeachInt2 {
		public static string test() {
			var sb = new StringBuilder();
			var first = true;
			foreach (var i in even(range(0, 10))) {
				if (first) {
					first = false;
				} else {
					sb.append(", ");
				}
				sb.append(i);
			}		
			return sb.toString();
		}

		private static IntIterable range(int first, int count) {
			while (count-- > 0) {
				yield return first++;
			}
		}
	
		private static IntIterable even(IntIterable source) {
			foreach (var i in source) {
				if (i % 2 == 0) {
					yield return i;
				}
			}
		}
	}

}
