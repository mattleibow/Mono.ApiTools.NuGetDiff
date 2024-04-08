namespace ClassLibrary;

public class ClassV1AndV2
{
	public void MethodV1AndV2()
	{
	}

	public void MethodV2Only()
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

		public void MethodV2Only()
		{
		}
	}

	public class NestedClassV2Only
	{
		public void Method()
		{
		}
	}

	public class UnusedNestedClassV2Only
	{
	}

	public struct NestedStructV1AndV2
	{
		public void MethodV1AndV2()
		{
		}

		public void MethodV2Only()
		{
		}
	}

	public struct NestedStructV2Only
	{
		public void Method()
		{
		}
	}
}

public class ClassV2Only
{
	public void Method()
	{
	}
}

public class TypeV2Only
{
}

public struct StructV1AndV2
{
	public void MethodV1AndV2()
	{
	}

	public void MethodV2Only()
	{
	}
}

public struct StructV2Only
{
	public void Method()
	{
	}
}
