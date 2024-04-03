using Mono.Cecil;

namespace ApiUsageAnalyzer;

public class ApiAnalyzer
{
    public MissingSymbols GetMissingSymbols(InputAssembly inputAssembly, InputAssembly dependencyAssembly)
    {
        List<string> missingTypes = [];
        List<string> missingMembers = [];

        using var module = ReadModule(inputAssembly);

        using var dependency = ReadModule(dependencyAssembly);

        ProcessTypes(missingTypes, module, dependency);
        ProcessMembers(missingMembers, module, dependency);

        return new MissingSymbols(missingTypes, missingMembers);
    }

    public MissingSymbols GetMissingMembers(InputAssembly inputAssembly, InputAssembly dependencyAssembly)
    {
        List<string> missingMembers = [];

        using var module = ReadModule(inputAssembly);

        using var dependency = ReadModule(dependencyAssembly);

        ProcessMembers(missingMembers, module, dependency);

        return new MissingSymbols([], missingMembers);
    }

    public MissingSymbols GetMissingTypes(InputAssembly inputAssembly, InputAssembly dependencyAssembly)
    {
        List<string> missingTypes = [];

        using var module = ReadModule(inputAssembly);

        using var dependency = ReadModule(dependencyAssembly);

        ProcessTypes(missingTypes, module, dependency);

        return new MissingSymbols(missingTypes, []);
    }

    private ModuleDefinition ReadModule(InputAssembly inputAssembly)
    {
        var parameters = new ReaderParameters
        {
            AssemblyResolver = new Resolver(inputAssembly.SearchPaths),
        };

        return ModuleDefinition.ReadModule(inputAssembly.FileName, parameters);
    }

    private void ProcessTypes(List<string> missingTypes, ModuleDefinition module, ModuleDefinition dependency)
    {
        var typeRefs = module.GetTypeReferences();

        var dependencyAssemblyName = GetAssemblyName(dependency);

        foreach (var typeRef in typeRefs)
        {
            var name = GetSymbolAssemblyName(typeRef);
            if (name != dependencyAssemblyName)
                continue;

            var clonedDependency = CloneType(typeRef, dependency);
            var resolvedDependency = dependency.MetadataResolver.Resolve(clonedDependency);
            if (resolvedDependency is not null)
                continue;

            missingTypes.Add(typeRef.FullName);
        }
    }

    private void ProcessMembers(List<string> missingMembers, ModuleDefinition module, ModuleDefinition dependency)
    {
        var memberRefs = module.GetMemberReferences();

        var dependencyAssemblyName = GetAssemblyName(dependency);
        foreach (var memberRef in memberRefs)
        {
            var memberAssemblyName = GetSymbolAssemblyName(memberRef);
            if (memberAssemblyName != dependencyAssemblyName)
                continue;

            if (memberRef is MethodReference methodRef)
            {
                var resolved = dependency.MetadataResolver.Resolve(methodRef);
                if (resolved is null)
                {
                    missingMembers.Add(methodRef.FullName);
                }
            }
            else if (memberRef is FieldReference fieldRef)
            {
                var resolved = dependency.MetadataResolver.Resolve(fieldRef);
                if (resolved is null)
                {
                    missingMembers.Add(fieldRef.FullName);
                }
            }
        }
    }

    private static TypeReference CloneType(TypeReference type, ModuleDefinition module)
    {
        var newType = new TypeReference(type.Namespace, type.Name, module, module.Assembly.Name);
        if (type.DeclaringType is not null)
        {
            newType.DeclaringType = CloneType(type.DeclaringType, module);
        }
        return newType;
    }

    private static string GetSymbolAssemblyName(MemberReference reference) =>
        GetSymbolAssemblyName(reference.DeclaringType);

    private static string GetSymbolAssemblyName(TypeReference reference)
    {
        if (reference is TypeDefinition typeDefinition)
            return GetAssemblyName(typeDefinition.Module);

        var scope = reference.Scope;
        var name = scope.Name;

        return name;
    }

    private static string GetAssemblyName(ModuleDefinition module) =>
        module.Assembly.Name.Name;

    class Resolver : DefaultAssemblyResolver
    {
        public Resolver(IEnumerable<string>? searchPaths)
            : this()
        {
            if (searchPaths is not null)
            {
                foreach (var path in searchPaths)
                {
                    if (path is not null)
                        AddSearchDirectory(path);
                }
            }
        }

        public Resolver()
        {
            RemoveSearchDirectory(".");
            RemoveSearchDirectory("bin");
        }
    }
}
