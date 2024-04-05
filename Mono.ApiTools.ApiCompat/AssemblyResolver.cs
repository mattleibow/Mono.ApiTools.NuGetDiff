using Mono.Cecil;

namespace Mono.ApiTools;

class AssemblyResolver : DefaultAssemblyResolver
{
	public AssemblyDefinition ResolveFile(string file)
	{
		AddSearchDirectory(Path.GetDirectoryName(file));
		var assembly = AssemblyDefinition.ReadAssembly(file, new ReaderParameters { AssemblyResolver = this, InMemory = true });
		RegisterAssembly(assembly);

		return assembly;
	}

	public AssemblyDefinition ResolveStream(Stream stream)
	{
		var assembly = AssemblyDefinition.ReadAssembly(stream, new ReaderParameters { AssemblyResolver = this, InMemory = true });
		RegisterAssembly(assembly);

		return assembly;
	}
}
