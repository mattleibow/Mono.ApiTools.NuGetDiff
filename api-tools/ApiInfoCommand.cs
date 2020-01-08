using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Mono.ApiTools
{
	public class ApiInfoCommand : BaseCommand
	{
		private const int DefaultSaveBufferSize = 1024;

		private static readonly Encoding UTF8NoBOM = new UTF8Encoding(false, true);

		public ApiInfoCommand()
			: base("api-info", "ASSEMBLY ...", "Generate API info XML for assemblies.")
		{
		}

		public List<string> Assemblies { get; set; } = new List<string>();

		public string OutputPath { get; set; }

		protected override OptionSet OnCreateOptions() => new OptionSet
		{
			{ "o|output=", "The output file path", v => OutputPath = v },
		};

		protected override bool OnValidateArguments(IEnumerable<string> extras)
		{
			var hasError = false;

			var assemblies = extras.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
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
				Console.Error.WriteLine($"{Program.Name}: At least one assembly is required.");
				hasError = true;
			}

			if (!string.IsNullOrWhiteSpace(OutputPath))
			{
				var dir = Path.GetDirectoryName(OutputPath);
				if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
					Directory.CreateDirectory(dir);
			}

			return !hasError;
		}

		protected override bool OnInvoke(IEnumerable<string> extras)
		{
			foreach (var assembly in Assemblies)
			{
				if (Program.Verbose)
					Console.WriteLine($"Generating API information for '{assembly}'...");

				using var stream = File.OpenRead(assembly);
				using var info = GenerateAssemblyApiInfo(stream);

				var path = OutputPath;
				if (string.IsNullOrWhiteSpace(path))
				{
					if (assembly.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
						path = Path.ChangeExtension(assembly, ".api-info.xml");
					else
						path = assembly + ".api-info.xml";
				}

				using var output = File.Create(path);
				info.CopyTo(output);
			}

			return true;
		}

		private Stream GenerateAssemblyApiInfo(Stream assemblyStream)
		{
			var config = new ApiInfoConfig
			{
				IgnoreResolutionErrors = true
			};

			var info = new MemoryStream();

			using (var writer = new StreamWriter(info, UTF8NoBOM, DefaultSaveBufferSize, true))
			{
				ApiInfo.Generate(assemblyStream, writer, config);
			}

			assemblyStream.Position = 0;
			info.Position = 0;

			return info;
		}
	}
}
