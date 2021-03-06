using java.lang;
using stab.query;
using stab.tree;

public class ExpressionTreeAdd {
    public static string test() {
        ExpressionTree<FunctionIntInt> expr = p => p + 1;
        return ((BinaryExpression)((ExpressionStatement)expr.Body).Expression).Operator.toString();
    }
}
