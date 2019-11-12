﻿using ILRepacking;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mono.ApiTools
{
	public class AssemblyMerger
	{
		public List<string> SearchDirectories { get; set; } = new List<string>();

		public bool InjectAssemblyNames { get; set; }

		public string InjectedAttributeFullName { get; set; } = "Mono.ApiTools.Internal.InjectedAssemblyNameAttribute";

		public bool Verbose { get; set; }

		public void Merge(IEnumerable<string> assemblies, string outputPath)
		{
			var assembliesToMerge = assemblies?.ToList() ?? throw new ArgumentNullException(nameof(assemblies));

			var assemblyResolver = new DefaultAssemblyResolver();
			if (SearchDirectories != null)
			{
				foreach (var dir in SearchDirectories)
				{
					assemblyResolver.AddSearchDirectory(dir);
				}
			}

			if (Verbose)
			{
				Console.WriteLine("Merging:");
				foreach (var include in assembliesToMerge)
					Console.WriteLine($" - {include}");
			}

			var tempRoot = Path.Combine(Path.GetTempPath(), "Mono.ApiTools", "AssemblyMerger", Guid.NewGuid().ToString());

			if (InjectAssemblyNames)
			{
				if (!string.IsNullOrWhiteSpace(tempRoot) && !Directory.Exists(tempRoot))
					Directory.CreateDirectory(tempRoot);

				assembliesToMerge = assembliesToMerge.ToList();

				for (int i = 0; i < assembliesToMerge.Count; i++)
				{
					var ass = assembliesToMerge[i];
					var temp = Path.Combine(tempRoot, Guid.NewGuid().ToString() + ".dll");
					InjectAssemblyName(assemblyResolver, ass, temp);
					assembliesToMerge[i] = temp;
				}

				if (Verbose)
				{
					Console.WriteLine("Temporary assemblies:");
					foreach (var include in assembliesToMerge)
						Console.WriteLine($" - {include}");
				}
			}

			var options = new RepackOptions
			{
				InputAssemblies = assembliesToMerge.ToArray(),
				OutputFile = outputPath,
				SearchDirectories = SearchDirectories.ToArray(),
				CopyAttributes = true,
				AllowMultipleAssemblyLevelAttributes = true,
				LogVerbose = Verbose
			};
			options.AllowedDuplicateTypes.Add(InjectedAttributeFullName, InjectedAttributeFullName);

			var repacker = new ILRepack(options);
			repacker.Repack();

			if (InjectAssemblyNames)
			{
				if (Directory.Exists(tempRoot))
				{
					try
					{
						Directory.Delete(tempRoot, true);
					}
					catch
					{
					}
				}
			}
		}

		private void InjectAssemblyName(IAssemblyResolver assemblyResolver, string assemblyPath, string outputPath)
		{
			var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);

			using var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
			var module = assembly.MainModule;

			var attributeType = ResolveSystemAttribute(module);
			var baseCtor = attributeType.Methods.FirstOrDefault(m => m.Name == ".ctor");

			var ns = InjectedAttributeFullName.Substring(0, InjectedAttributeFullName.LastIndexOf("."));
			var tn = InjectedAttributeFullName.Substring(InjectedAttributeFullName.LastIndexOf(".") + 1);
			var iana = new TypeDefinition(ns, tn, TypeAttributes.Class);
			iana.BaseType = module.ImportReference(attributeType);

			var field = new FieldDefinition("assemblyName", FieldAttributes.Private | FieldAttributes.InitOnly, module.TypeSystem.String);

			var getterAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName;
			var getter = new MethodDefinition("get_AssemblyName", getterAttributes, module.TypeSystem.String);
			getter.DeclaringType = iana;
			getter.HasThis = true;
			getter.IsGetter = true;
			getter.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
			getter.Body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, field));
			getter.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

			var property = new PropertyDefinition("AssemblyName", PropertyAttributes.None, module.TypeSystem.String);
			property.HasThis = true;
			property.GetMethod = getter;

			var methodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
			var ctor = new MethodDefinition(".ctor", methodAttributes, module.TypeSystem.Void);
			var param = new ParameterDefinition("assemblyName", ParameterAttributes.None, module.TypeSystem.String);
			ctor.Parameters.Add(param);

			ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
			ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, module.ImportReference(baseCtor)));
			ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
			ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
			ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
			ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
			ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Stfld, field));
			ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

			iana.Fields.Add(field);
			iana.Properties.Add(property);
			iana.Methods.Add(ctor);
			iana.Methods.Add(getter);
			module.Types.Add(iana);

			foreach (var type in module.Types)
			{
				if (type.FullName == InjectedAttributeFullName)
					continue;

				type.CustomAttributes.Add(new CustomAttribute(ctor)
				{
					ConstructorArguments = { new CustomAttributeArgument(module.TypeSystem.String, assemblyName) }
				});
			}

			assembly.Write(outputPath);

			TypeDefinition ResolveSystemAttribute(ModuleDefinition module)
			{
				foreach (var reference in module.AssemblyReferences)
				{
					var resolved = assemblyResolver.Resolve(reference);
					var attributeType = resolved.MainModule.GetType("System.Attribute");
					if (attributeType != null)
						return attributeType;
				}
				throw new Exception("Unable to locate System.Attribute in any of the referenced assemblies.");
			}
		}
	}
}
