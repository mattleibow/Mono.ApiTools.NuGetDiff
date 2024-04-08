////
//// mono-api-info.cs - Dumps public assembly information to an xml file.
////
//// Authors:
////	Gonzalo Paniagua Javier (gonzalo@ximian.com)
////
//// Copyright (C) 2003-2008 Novell, Inc (http://www.novell.com)
////

//namespace Mono.ApiTools;

//class Driver
//{
//	public static int Main(string[] args)
//	{
//		bool showHelp = false;
//		string output = null;
//		List<string> asms = null;
//		ApiInfoConfig config = new ApiInfoConfig();

//		var options = new Mono.Options.OptionSet {
//				{ "abi",
//					"Generate ABI, not API; contains only classes with instance fields which are not [NonSerialized].",
//					v => config.AbiMode = v != null },
//				{ "f|follow-forwarders",
//					"Follow type forwarders.",
//					v => config.FollowForwarders = v != null },
//				{ "ignore-inherited-interfaces",
//					"Ignore interfaces on the base type.",
//					v => config.IgnoreInheritedInterfaces = v != null },
//				{ "ignore-resolution-errors",
//					"Ignore any assemblies that cannot be found.",
//					v => config.IgnoreResolutionErrors = v != null },
//				{ "d|L|lib|search-directory=",
//					"Check for assembly references in {DIRECTORY}.",
//					v => config.SearchDirectories.Add (v) },
//				{ "r=",
//					"Read and register the file {ASSEMBLY}, and add the directory containing ASSEMBLY to the search path.",
//					v => config.ResolveFiles.Add (v) },
//				{ "o|out|output=",
//					"The output file. If not specified the output will be written to stdout.",
//					v => output = v },
//				{ "h|?|help",
//					"Show this message and exit.",
//					v => showHelp = v != null },
//				{ "contract-api",
//					"Produces contract API with all members at each level of inheritance hierarchy",
//					v => config.FullApiSet = v != null },
//			};

//		try
//		{
//			asms = options.Parse(args);
//		}
//		catch (Mono.Options.OptionException e)
//		{
//			Console.WriteLine("Option error: {0}", e.Message);
//			asms = null;
//		}

//		if (showHelp || asms == null || asms.Count == 0)
//		{
//			Console.WriteLine("usage: mono-api-info [OPTIONS+] ASSEMBLY+");
//			Console.WriteLine();
//			Console.WriteLine("Expose IL structure of CLR assemblies as XML.");
//			Console.WriteLine();
//			Console.WriteLine("Available Options:");
//			Console.WriteLine();
//			options.WriteOptionDescriptions(Console.Out);
//			Console.WriteLine();
//			return showHelp ? 0 : 1;
//		}

//		TextWriter outputStream = null;
//		try
//		{
//			if (!string.IsNullOrEmpty(output))
//				outputStream = new StreamWriter(output);

//			ApiInfo.Generate(asms, null, outputStream ?? Console.Out, config);
//		}
//		catch (Exception e)
//		{
//			Console.Error.WriteLine(e);
//			return 1;
//		}
//		finally
//		{
//			outputStream?.Dispose();
//		}
//		return 0;
//	}
//}
