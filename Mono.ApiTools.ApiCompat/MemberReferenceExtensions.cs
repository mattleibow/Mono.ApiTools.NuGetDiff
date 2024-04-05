using Mono.Cecil;

namespace Mono.ApiTools;

internal static class MemberReferenceExtensions
{
	public static TypeReference Clone(this TypeReference type, ModuleDefinition module)
	{
		if (GetAssemblyName(type) != GetAssemblyName(module))
			return type;

		var newType = new TypeReference(type.Namespace, type.Name, module, module.Assembly.Name);
		newType.IsValueType = type.IsValueType;

		if (type.DeclaringType is not null)
			newType.DeclaringType = type.DeclaringType.Clone(module);

		return newType;
	}

	public static FieldReference Clone(this FieldReference field, ModuleDefinition dependency)
	{
		var newType = field.FieldType.Clone(dependency);
		var newDeclaring = field.DeclaringType.Clone(dependency);

		var newField = new FieldReference(field.Name, newType, newDeclaring);

		return newField;
	}

	public static MethodReference Clone(this MethodReference method, ModuleDefinition module)
	{
		var newReturn = method.ReturnType.Clone(module);
		var newDeclaring = method.DeclaringType.Clone(module);

		var newMethod = new MethodReference(method.Name, newReturn, newDeclaring)
		{
			HasThis = method.HasThis,
			CallingConvention = method.CallingConvention,
			ExplicitThis = method.ExplicitThis,
		};

		CloneParameters(method, module, newMethod);

		return newMethod;
	}

	private static void CloneParameters(MethodReference method, ModuleDefinition module, MethodReference newMethod)
	{
		if (method.HasGenericParameters)
		{
			foreach (var gp in method.GenericParameters)
			{
				var newGeneric = new GenericParameter(gp.Name, newMethod);
				newMethod.GenericParameters.Add(newGeneric);
			}
		}

		if (method.HasParameters)
		{
			foreach (var param in method.Parameters)
			{
				var newParam = new ParameterDefinition(
					param.Name,
					param.Attributes,
					CloneParameterType(param.ParameterType, module, method));
				newMethod.Parameters.Add(newParam);
			}
		}
	}

	private static TypeReference CloneParameterType(TypeReference type, ModuleDefinition module, MethodReference? method)
	{
		if (GetAssemblyName(type) != GetAssemblyName(module))
			return type;

		if (type.IsGenericParameter)
		{
			var newType = new GenericParameter(type.Name, method);
			return newType;
		}

		if (type.IsGenericInstance)
		{
			var git = (GenericInstanceType)type;
			var newType = new GenericInstanceType(
				git.ElementType.Clone(module));
			foreach (var ga in git.GenericArguments)
			{
				var newArg = ga.Clone(module);
				newType.GenericArguments.Add(newArg);
			}
			return newType;
		}

		if (type.IsOptionalModifier)
		{
			var mod = (OptionalModifierType)type;
			var newType = new OptionalModifierType(
				mod.ModifierType.Clone(module),
				mod.ElementType.Clone(module));
			return newType;
		}

		if (type.IsRequiredModifier)
		{
			var mod = (RequiredModifierType)type;
			var newType = new RequiredModifierType(
				mod.ModifierType.Clone(module),
				mod.ElementType.Clone(module));
			return newType;
		}

		if (type.IsArray)
		{
			var array = (ArrayType)type;
			var newType = new ArrayType(
				array.ElementType.Clone(module),
				array.Rank);
			return newType;
		}

		if (type.IsFunctionPointer)
		{
			var fp = (FunctionPointerType)type;
			var newType = new FunctionPointerType()
			{
				HasThis = fp.HasThis,
				CallingConvention = fp.CallingConvention,
				ReturnType = fp.ReturnType.Clone(module),
			};

			if (fp.HasGenericParameters)
			{
				foreach (var gp in fp.GenericParameters)
				{
					var newGeneric = new GenericParameter(gp.Name, newType);
					newType.GenericParameters.Add(newGeneric);
				}
			}

			if (fp.HasParameters)
			{
				foreach (var param in fp.Parameters)
				{
					var newParam = new ParameterDefinition(
						param.Name,
						param.Attributes,
						CloneParameterType(param.ParameterType, module, null));
					newType.Parameters.Add(newParam);
				}
			}

			return newType;
		}

		return type.Clone(module);
	}

	public static string GetAssemblyName(this MemberReference reference) =>
		GetAssemblyName(reference.DeclaringType);

	public static string GetAssemblyName(this TypeReference reference)
	{
		if (reference is TypeDefinition typeDefinition)
			return GetAssemblyName(typeDefinition.Module);

		var scope = reference.Scope;
		var name = scope.Name;

		return name;
	}

	public static string GetAssemblyName(this ModuleDefinition module) =>
		module.Assembly.Name.Name;
}
