using java.lang;
using stab.query;

public class C {
    public static void m(Iterable<string> s1, Iterable<string> s2, Iterable<string> s3) {
        var query = s1.selectMany(e1 => s2, (e1, e2) => new { e1, e2 }).selectMany(query$id0 => s3, (query$id0, e3) => query$id0.e1 + query$id0.e2 + e3);
    }
}
