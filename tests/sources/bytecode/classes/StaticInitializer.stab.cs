using java.lang;
using java.util;

namespace stab.bytecode.test.classes {
	public class StaticInitializer {
		static HashMap<String, String> map*;
		static StaticInitializer() {
			map = new HashMap<String, String>();
		}
	}
}
