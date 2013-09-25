using java.lang;
using java.util;
using stab.query;
using stab.tree;

public class ExpressionTreeObjectCreation4 {
    public static string test() {
        ExpressionTree<FunctionIntT<Object>> expr = p => new ArrayList<string> { "s1" };
        return ((NewObjectExpression)((ExpressionStatement)expr.Body).Expression).Initializers.first().Member.getName();
    }
}
