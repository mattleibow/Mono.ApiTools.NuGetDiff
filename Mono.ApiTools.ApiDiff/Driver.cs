////
//// mono-api-diff.cs - Compares 2 xml files produced by mono-api-info and
////		      produces a file suitable to build class status pages.
////
//// Authors:
////	Gonzalo Paniagua Javier (gonzalo@ximian.com)
////	Marek Safar		(marek.safar@gmail.com)
////
//// Maintainer:
////	C.J. Adams-Collier	(cjac@colliertech.org)
////
//// (C) 2003 Novell, Inc (http://www.novell.com)
//// (C) 2009,2010 Collier Technologies (http://www.colliertech.org)

//namespace Mono.ApiTools;

//class Driver
//{
//	static int Main(string[] args)
//	{
//		bool showHelp = false;
//		string output = null;
//		List<string> extra = null;

//		var options = new Mono.Options.OptionSet {
//				{ "h|?|help", "Show this help", v => showHelp = true },
//				{ "o|out|output=", "XML diff file output (omit for stdout)", v => output = v },
//			};

//		try
//		{
//			extra = options.Parse(args);
//		}
//		catch (Mono.Options.OptionException e)
//		{
//			Console.WriteLine("Option error: {0}", e.Message);
//			extra = null;
//		}

//		if (showHelp || extra == null || extra.Count != 2)
//		{
//			Console.WriteLine(@"Usage: mono-api-diff.exe [options] <assembly 1 xml> <assembly 2 xml>");
//			Console.WriteLine();
//			Console.WriteLine("Available options:");
//			options.WriteOptionDescriptions(Console.Out);
//			Console.WriteLine();
//			return showHelp ? 0 : 1;
//		}

//		TextWriter outputStream = null;
//		try
//		{
//			if (!string.IsNullOrEmpty(output))
//				outputStream = new StreamWriter(output);

//			ApiDiff.Generate(extra[0], extra[1], outputStream ?? Console.Out);
//		}
//		catch (Exception e)
//		{
//			Console.WriteLine(e);
//			return 1;
//		}
//		finally
//		{
//			outputStream?.Dispose();
//		}
//		return 0;
//	}
//}