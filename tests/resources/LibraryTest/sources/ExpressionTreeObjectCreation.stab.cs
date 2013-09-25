using java.lang;
using stab.query;
using stab.tree;

public class ExpressionTreeObjectCreation {
    public static string test() {
        ExpressionTree<FunctionIntT<Object>> expr = p => new Object();
        return ((NewObjectExpression)((ExpressionStatement)expr.Body).Expression).Constructor.getDeclaringClass().getName();
    }
}
