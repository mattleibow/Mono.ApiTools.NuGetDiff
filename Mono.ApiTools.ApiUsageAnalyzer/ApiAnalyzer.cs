using Mono.Cecil;

namespace ApiUsageAnalyzer;

public static class ApiAnalyzer
{
	class State
	{
		public string? DependencyAssemblyName { get; set; }

		public List<string> MissingTypes { get; set; } = [];

		public List<string> MissingMembers { get; set; } = [];
	}

	public static MissingSymbols GetMissingSymbols(InputAssembly inputAssembly, InputAssembly dependencyAssembly)
	{
		using var module = ReadModule(inputAssembly);
		using var dependency = ReadModule(dependencyAssembly);

		var state = new State
		{
			DependencyAssemblyName = dependency.GetAssemblyName()
		};

		ProcessTypes(module, dependency, state);
		ProcessMembers(module, dependency, state);

		return new MissingSymbols(state.MissingTypes, state.MissingMembers);
	}

	private static ModuleDefinition ReadModule(InputAssembly inputAssembly)
	{
		var resolver = new AssemblyResolver();
		if (inputAssembly.SearchPaths is not null)
		{
			foreach (var path in inputAssembly.SearchPaths)
			{
				resolver.AddSearchDirectory(path);
			}
		}

		var assembly = resolver.ResolveStream(inputAssembly.Open());

		return assembly.MainModule;
	}

	private static void ProcessTypes(ModuleDefinition module, ModuleDefinition dependency, State state)
	{
		var typeRefs = module.GetTypeReferences();

		foreach (var typeRef in typeRefs)
		{
			// check to make sure the types we are looking at are the ones that are in the dependency
			var name = typeRef.GetAssemblyName();
			if (name != state.DependencyAssemblyName)
				continue;

			// check to see if the type is in the dependency
			var clonedDependency = typeRef.Clone(dependency);
			var resolvedDependency = dependency.MetadataResolver.Resolve(clonedDependency);
			if (resolvedDependency is not null)
				continue;

			// if the type is not in the dependency, add it to the list of missing types
			state.MissingTypes.Add(typeRef.FullName);
		}
	}

	private static void ProcessMembers(ModuleDefinition module, ModuleDefinition dependency, State state)
	{
		var memberRefs = module.GetMemberReferences();

		foreach (var memberRef in memberRefs)
		{
			// check to make sure the members we are looking at are the ones that are in the dependency
			var memberAssemblyName = memberRef.GetAssemblyName();
			if (memberAssemblyName != state.DependencyAssemblyName)
				continue;

			if (memberRef is MethodReference methodRef)
			{
				// if the member is a method, check to see if the method is in the dependency
				var clonedRef = methodRef.Clone(dependency);
				var resolved = dependency.MetadataResolver.Resolve(clonedRef);
				if (resolved is null)
				{
					// if the method is not in the dependency, add it to the list of missing members
					state.MissingMembers.Add(methodRef.FullName);
				}
			}
			else if (memberRef is FieldReference fieldRef)
			{
				// if the member is a field, check to see if the field is in the dependency
				var clonedRef = fieldRef.Clone(dependency);
				var resolved = dependency.MetadataResolver.Resolve(clonedRef);
				if (resolved is null)
				{
					// if the field is not in the dependency, add it to the list of missing members
					state.MissingMembers.Add(fieldRef.FullName);
				}
			}
		}
	}
}
