﻿////
//// The main differences with mono-api-diff are:
//// * this tool directly produce HTML similar to gdiff.sh used for Xamarin.iOS
//// * this tool reports changes in an "evolutionary" way, not in a breaking way,
////   i.e. it does not assume the source assembly is right (but simply older)
//// * the diff .xml output was not easy to convert back into the HTML format
////   that gdiff.sh produced
//// 
//// Authors
////    Sebastien Pouliot  <sebastien.pouliot@microsoft.com>
////
//// Copyright 2013-2014 Xamarin Inc. http://www.xamarin.com
//// Copyright 2018 Microsoft Inc.
//// 
//// Permission is hereby granted, free of charge, to any person obtaining
//// a copy of this software and associated documentation files (the
//// "Software"), to deal in the Software without restriction, including
//// without limitation the rights to use, copy, modify, merge, publish,
//// distribute, sublicense, and/or sell copies of the Software, and to
//// permit persons to whom the Software is furnished to do so, subject to
//// the following conditions:
////
//// The above copyright notice and this permission notice shall be
//// included in all copies or substantial portions of the Software.
////
//// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
//// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
//// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
////

//using System.Text.RegularExpressions;

//namespace Mono.ApiTools;

//class Program
//{

//	public static int Main(string[] args)
//	{
//		var showHelp = false;
//		string diff = null;
//		List<string> extra = null;
//		var config = new ApiDiffFormattedConfig();

//		var options = new Mono.Options.OptionSet {
//			{ "h|help", "Show this help", v => showHelp = true },
//			{ "d|o|out=|output=|diff=", "HTML diff file out output (omit for stdout)", v => diff = v },
//			{ "i|ignore=", "Ignore new, added, and removed members whose description matches a given C# regular expression (see below).",
//				v => {
//					var r = new Regex (v);
//					config.IgnoreAdded.Add (r);
//					config.IgnoreRemoved.Add (r);
//					config.IgnoreNew.Add (r);
//				}
//			},
//			{ "a|ignore-added=", "Ignore added members whose description matches a given C# regular expression (see below).",
//				v => config.IgnoreAdded.Add (new Regex (v))
//			},
//			{ "r|ignore-removed=", "Ignore removed members whose description matches a given C# regular expression (see below).",
//				v => config.IgnoreRemoved.Add (new Regex (v))
//			},
//			{ "n|ignore-new=", "Ignore new namespaces and types whose description matches a given C# regular expression (see below).",
//				v => config.IgnoreNew.Add (new Regex (v))
//			},
//			{ "ignore-changes-parameter-names", "Ignore changes to parameter names for identically prototyped methods.",
//				v => config.IgnoreParameterNameChanges   = v != null
//			},
//			{ "ignore-changes-property-setters", "Ignore adding setters to properties.",
//				v => config.IgnoreAddedPropertySetters = v != null
//			},
//			{ "ignore-changes-virtual", "Ignore changing non-`virtual` to `virtual` or adding `override`.",
//				v => config.IgnoreVirtualChanges = v != null
//			},
//			{ "c|colorize:", "Colorize HTML output", v => config.Colorize = string.IsNullOrEmpty (v) ? true : bool.Parse (v) },
//			{ "x|lax", "Ignore duplicate XML entries", v => config.IgnoreDuplicateXml = true },
//			{ "ignore-nonbreaking", "Ignore all nonbreaking changes", v => config.IgnoreNonbreaking = true },
//			{ "v|verbose:", "Verbosity level; when set, will print debug messages",
//			  (int? v) => config.Verbosity = v ?? (config.Verbosity + 1)},
//			{ "md|markdown", "Output markdown instead of HTML", v => config.Formatter = ApiDiffFormatter.Markdown },
//			new Mono.Options.ResponseFileSource (),
//		};

//		try
//		{
//			extra = options.Parse(args);
//		}
//		catch (Mono.Options.OptionException e)
//		{
//			Console.WriteLine("Option error: {0}", e.Message);
//			extra = null;
//		}

//		if (showHelp || extra == null || extra.Count < 2 || extra.Count > 3)
//		{
//			Console.WriteLine(@"Usage: mono-api-html [options] <reference.xml> <assembly.xml> [diff.html]");
//			Console.WriteLine();
//			Console.WriteLine("Available options:");
//			options.WriteOptionDescriptions(Console.Out);
//			Console.WriteLine();
//			Console.WriteLine("Ignoring Members:");
//			Console.WriteLine();
//			Console.WriteLine("  Members that were added can be filtered out of the diff by using the");
//			Console.WriteLine("  -i, --ignore-added option. The option takes a C# regular expression");
//			Console.WriteLine("  to match against member descriptions. For example, to ignore the");
//			Console.WriteLine("  introduction of the interfaces 'INSCopying' and 'INSCoding' on types");
//			Console.WriteLine("  pass the following to mono-api-html:");
//			Console.WriteLine();
//			Console.WriteLine("    mono-api-html ... -i 'INSCopying$' -i 'INSCoding$'");
//			Console.WriteLine();
//			Console.WriteLine("  The regular expressions will match any member description ending with");
//			Console.WriteLine("  'INSCopying' or 'INSCoding'.");
//			Console.WriteLine();
//			return showHelp ? 0 : 1;
//		}

//		var input = extra[0];
//		var output = extra[1];
//		if (extra.Count == 3 && diff == null)
//			diff = extra[2];

//		TextWriter outputStream = null;
//		try
//		{
//			if (!string.IsNullOrEmpty(diff))
//				outputStream = new StreamWriter(diff);

//			ApiDiffFormatted.Generate(input, output, outputStream ?? Console.Out, config);
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
