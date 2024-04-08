namespace ClassLibrary;

public class ClassV1AndV2
{
	public void MethodV1AndV2()
	{
	}

	public void MethodV1Only()
	{
	}

	public bool ByRefParamMethod(int input, out StructV1AndV2 output)
	{
		output = default;
		return true;
	}

	public unsafe void PointerMethod(StructV1AndV2* pointer)
	{
	}

	// classes

	public class NestedClassV1AndV2
	{
		public void MethodV1AndV2()
		{
		}

		public void MethodV1Only()
		{
		}
	}

	public class NestedClassV1Only
	{
		public void Method()
		{
		}
	}

	public class UnusedNestedClassV1Only
	{
	}

	public struct NestedStructV1AndV2
	{
		public void MethodV1AndV2()
		{
		}

		public void MethodV1Only()
		{
		}
	}

	public struct NestedStructV1Only
	{
		public void Method()
		{
		}
	}
}

public class ClassV1Only
{
	public void Method()
	{
	}
}

public class TypeV1Only
{
}

public struct StructV1AndV2
{
	public void MethodV1AndV2()
	{
	}

	public void MethodV1Only()
	{
	}
}

public struct StructV1Only
{
	public void Method()
	{
	}
}
