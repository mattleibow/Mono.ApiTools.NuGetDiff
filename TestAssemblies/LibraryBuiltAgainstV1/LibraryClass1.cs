using ClassLibrary;

namespace LibraryBuiltAgainstV1;

public class LibraryClass1
{
    public void LibraryMethod1()
    {
        var c = new ClassV1AndV2();

        c.MethodV1Only();
        c.MethodV1AndV2();

        var c2 = new ClassV1Only();
        c2.Method();
    }

    public Type Type = typeof(TypeV1Only);

    public Type NestedType = typeof(ClassV1AndV2.NestedClassV1Only);
}
