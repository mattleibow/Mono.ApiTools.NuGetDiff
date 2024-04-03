//
// The main differences with mono-api-diff are:
// * this tool directly produce HTML similar to gdiff.sh used for Xamarin.iOS
// * this tool reports changes in an "evolutionary" way, not in a breaking way,
//   i.e. it does not assume the source assembly is right (but simply older)
// * the diff .xml output was not easy to convert back into the HTML format
//   that gdiff.sh produced
// 
// Authors
//    Sebastien Pouliot  <sebastien.pouliot@microsoft.com>
//
// Copyright 2013-2014 Xamarin Inc. http://www.xamarin.com
// Copyright 2018 Microsoft Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Mono.ApiTools.Comparers;

namespace Mono.ApiTools;

public static class ApiDiffFormatted
{
	public static void Generate(Stream firstInfo, Stream secondInfo, TextWriter outStream, ApiDiffFormattedConfig config = null)
	{
		var state = CreateState(config);
		Generate(firstInfo, secondInfo, outStream, state);
	}

	public static void Generate(string firstInfo, string secondInfo, TextWriter outStream, ApiDiffFormattedConfig config = null)
	{
		var state = CreateState(config);
		Generate(firstInfo, secondInfo, outStream, state);
	}

	internal static void Generate(string firstInfo, string secondInfo, TextWriter outStream, State state)
	{
		var ac = new AssemblyComparer(firstInfo, secondInfo, state);
		Generate(ac, outStream, state);
	}

	internal static void Generate(Stream firstInfo, Stream secondInfo, TextWriter outStream, State state)
	{
		var ac = new AssemblyComparer(firstInfo, secondInfo, state);
		Generate(ac, outStream, state);
	}

	static void Generate(AssemblyComparer ac, TextWriter outStream, State state)
	{
		var diffHtml = String.Empty;
		using (var writer = new StringWriter())
		{
			state.Output = writer;
			ac.Compare();
			diffHtml = state.Output.ToString();
		}

		if (diffHtml.Length > 0)
		{
			var title = $"{ac.SourceAssembly}.dll";
			if (ac.SourceAssembly != ac.TargetAssembly)
				title += $" vs {ac.TargetAssembly}.dll";

			state.Formatter.BeginDocument(outStream, $"API diff: {title}");
			state.Formatter.BeginAssembly(outStream);
			outStream.Write(diffHtml);
			state.Formatter.EndAssembly(outStream);
			state.Formatter.EndDocument(outStream);
		}
	}

	static State CreateState(ApiDiffFormattedConfig config)
	{
		if (config == null)
			config = new ApiDiffFormattedConfig();

		var state = new State
		{
			Colorize = config.Colorize,
			Formatter = null,
			IgnoreAddedPropertySetters = config.IgnoreAddedPropertySetters,
			IgnoreVirtualChanges = config.IgnoreVirtualChanges,
			IgnoreNonbreaking = config.IgnoreNonbreaking,
			IgnoreParameterNameChanges = config.IgnoreParameterNameChanges,
			Lax = config.IgnoreDuplicateXml,

			Verbosity = config.Verbosity
		};

		state.IgnoreAdded.AddRange(config.IgnoreAdded);
		state.IgnoreNew.AddRange(config.IgnoreNew);
		state.IgnoreRemoved.AddRange(config.IgnoreRemoved);

		switch (config.Formatter)
		{
			case ApiDiffFormatter.Html:
				state.Formatter = new HtmlFormatter(state);
				break;
			case ApiDiffFormatter.Markdown:
				state.Formatter = new MarkdownFormatter(state);
				break;
			default:
				throw new ArgumentException("Invlid formatter specified.");
		}

		// unless specified default to HTML
		if (state.Formatter == null)
			state.Formatter = new HtmlFormatter(state);

		if (state.IgnoreNonbreaking)
		{
			state.IgnoreAddedPropertySetters = true;
			state.IgnoreVirtualChanges = true;
			state.IgnoreNew.Add(new Regex(".*"));
			state.IgnoreAdded.Add(new Regex(".*"));
		}

		return state;
	}
}
