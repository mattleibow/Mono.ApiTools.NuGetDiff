using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mono.ApiTools
{
	public class MergeCommand : BaseCommand
	{
		public MergeCommand()
			: base("merge", "ASSEMBLY | DIRECTORY", "Merge multiple .NET assemblies.")
		{
		}

		public List<string> Assemblies { get; } = new List<string>();

		public string OutputPath { get; set; }

		public string AttributeFullName { get; set; }

		public string AssemblyName { get; set; }

		public List<string> SearchDirectories { get; } = new List<string>();

		public bool InjectAssemblyNames { get; set; }

		protected override OptionSet OnCreateOptions() => new OptionSet
		{
			{ "o|output=", "The output path to use for the merged assembly", v => OutputPath = v },
			{ "s|search=", "One or more search directories", v => SearchDirectories.Add(v) },
			{ "inject-assembly-name", "Add the assembly names to the types", _ => InjectAssemblyNames = true },
			{ "attribute-type=", "The full name of the attribute", v => AttributeFullName = v },
			{ "n|assembly-name=", "The name of the merged assembly", v => AssemblyName = v },
			{ "inject-assemblyname", "[Obsolete] Use `--inject-assembly-name`", _ => InjectAssemblyNames = true },
		};

		protected override bool OnValidateArguments(IEnumerable<string> extras)
		{
			var hasError = false;

			AssemblyName ??= "Merged";

			if (string.IsNullOrWhiteSpace(OutputPath))
			{
				OutputPath = AssemblyName + ".dll";
			}
			else
			{
				if (OutputPath.EndsWith("/") || OutputPath.EndsWith("\\") || Directory.Exists(OutputPath))
					OutputPath = Path.Combine(OutputPath, AssemblyName + ".dll");

				var dir = Path.GetDirectoryName(OutputPath);
				if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
					Directory.CreateDirectory(dir);
			}

			var assemblies = extras.Where(p => !string.IsNullOrEmpty(p)).ToArray();
			foreach (var assemblyOrDir in assemblies.ToArray())
			{
				if (Directory.Exists(assemblyOrDir))
				{
					Assemblies.AddRange(Directory.GetFiles(assemblyOrDir, "*.dll"));
				}
				else if (File.Exists(assemblyOrDir))
				{
					Assemblies.Add(assemblyOrDir);
				}
				else
				{
					Console.Error.WriteLine($"{Program.Name}: File does not exist: `{assemblyOrDir}`.");
					hasError = true;
				}
			}

			if (Assemblies.Count == 0)
			{
				Console.Error.WriteLine($"{Program.Name}: At least one assembly is required `--assembly=PATH`.");
				hasError = true;
			}

			if (hasError)
				Console.Error.WriteLine($"{Program.Name}: Use `{Program.Name} help {Name}` for details.");

			return !hasError;
		}

		protected override bool OnInvoke(IEnumerable<string> extras)
		{
			var merger = new AssemblyMerger
			{
				SearchDirectories = SearchDirectories,
				InjectAssemblyNames = InjectAssemblyNames,
				Verbose = Program.Verbose,
			};

			if (!string.IsNullOrEmpty(AttributeFullName))
				merger.InjectedAttributeFullName = AttributeFullName;

			merger.Merge(Assemblies, OutputPath);

			return true;
		}
	}
}
