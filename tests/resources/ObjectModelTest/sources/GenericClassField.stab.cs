namespace p {
	using java.lang;

	public class GenericClassField {
		private GenericClassFieldAux<string> field = new GenericClassFieldAux<string>();
	
		public int method() {
			return field.field;		
		}
		
		public static int test() {
			var obj = new GenericClassField();
			return obj.method();
		}
	}
	
	public class GenericClassFieldAux<T> {
		public int field = 2;
	}

}