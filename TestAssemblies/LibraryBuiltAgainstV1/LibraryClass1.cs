using ClassLibrary;

namespace LibraryBuiltAgainstV1;

public class LibraryClass1
{
	public unsafe void LibraryMethod1()
	{
		var c = new ClassV1AndV2();
		c.MethodV1Only();
		c.MethodV1AndV2();
		c.ByRefParamMethod(10, out var output);
		c.PointerMethod(&output);

		var c2 = new ClassV1Only();
		c2.Method();

		var s = new StructV1AndV2();
		s.MethodV1AndV2();

		var s2 = new StructV1Only();
		s2.Method();
	}

	public Type Type = typeof(TypeV1Only);

	public Type NestedType = typeof(ClassV1AndV2.NestedClassV1Only);
}
