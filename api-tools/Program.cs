using Mono.Options;

namespace Mono.ApiTools
{
	public class Program
	{
		public const string Name = "api-tools";

		public static bool Verbose { get; private set; }

		static int Main(string[] args)
		{
			var commands = new CommandSet(Name)
			{
				$"usage: {Name} COMMAND [OPTIONS]",
				"",
				"A set of tools to help with .NET API development and and NuGet diff-ing.",
				"",
				"Global options:",
				{ "v|verbose", "Use a more verbose output", _ => Verbose = true },
				"",
				"Assembly commands:",
				new CommandSet("assembly")
				{
					new ApiInfoCommand("info"),
					new ApiCompatCommand(),
					new DiffCommand(),
					new MergeCommand(),
				},
				"",
				"NuGet commands:",
				new CommandSet("nuget")
				{
					new NuGetDiffCommand("diff"),
					new NuGetDownloadCommand()
				},
				"",
				"Obsolete commands:",
				new ApiInfoCommand(),
				new ApiCompatCommand(),
				new DiffCommand(),
				new MergeCommand(),
				new NuGetDiffCommand(),
			};
			return commands.Run(args);
		}
	}
}
