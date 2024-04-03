using ApiUsageAnalyzer;
using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mono.ApiTools
{
	public class ApiCompatCommand : BaseCommand
	{
		public ApiCompatCommand()
			: base("api-compat", "ASSEMBLY1 ASSEMBLY2", "Determine how compatible assemblies are.")
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

			foreach (var ass in assemblies)
			{
				if (File.Exists(ass))
				{
					Assemblies.Add(ass);
				}
				else
				{
					Console.Error.WriteLine($"{Program.Name}: Assembly does not exist: `{ass}`.");
					hasError = true;
				}
			}

			if (Assemblies.Count != 2)
			{
				Console.Error.WriteLine($"{Program.Name}: Exactly two assemblies are required.");
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
			if (Program.Verbose)
				Console.WriteLine($"Running analysis on '{Assemblies[0]}' against '{Assemblies[1]}'...");

			var mainAssembly = new InputAssembly(Assemblies[0])
			{
				SearchPaths =
				{
					Path.GetDirectoryName(Assemblies[0])
				}
			};

			var dependencyAssembly = new InputAssembly(Assemblies[1])
			{
				SearchPaths =
				{
					Path.GetDirectoryName(Assemblies[1])
				}
			};

			ProcessAsync(mainAssembly, dependencyAssembly).Wait();

			return true;
		}

		private async Task ProcessAsync(InputAssembly mainAssembly, InputAssembly dependencyAssembly)
		{
			using var outputStream = GenerateOutput(mainAssembly, dependencyAssembly);

			if (!string.IsNullOrWhiteSpace(OutputPath))
			{
				// write the file
				using var file = File.Create(OutputPath);
				await outputStream.CopyToAsync(file);
			}
			else
			{
				// write to console out
				using var md = new StreamReader(outputStream);
				var contents = await md.ReadToEndAsync();
				await Console.Out.WriteLineAsync(contents);
			}

			// we are done
			if (Program.Verbose)
				Console.WriteLine($"Analysis complete of '{mainAssembly.FileName}'.");
		}

		private static MemoryStream GenerateOutput(InputAssembly mainAssembly, InputAssembly dependencyAssembly)
		{
			var analyzer = new ApiAnalyzer();

			// run the actual analysis
			var missing = analyzer.GetMissingSymbols(mainAssembly, dependencyAssembly);

			var outputStream = new MemoryStream();

			using (var writer = new StreamWriter(outputStream, UTF8NoBOM, DefaultSaveBufferSize, true))
			{
				missing.Save(writer);
			}

			outputStream.Position = 0;

			return outputStream;
		}
	}
}
