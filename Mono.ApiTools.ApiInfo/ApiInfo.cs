//
// mono-api-info.cs - Dumps public assembly information to an xml file.
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// Copyright (C) 2003-2008 Novell, Inc (http://www.novell.com)
//

using System.Xml;

namespace Mono.ApiTools;

public static class ApiInfo
{
	public static void Generate(string assemblyPath, TextWriter outStream, ApiInfoConfig config = null)
	{
		if (assemblyPath == null)
			throw new ArgumentNullException(nameof(assemblyPath));

		Generate(new[] { assemblyPath }, null, outStream, config);
	}

	public static void Generate(Stream assemblyStream, TextWriter outStream, ApiInfoConfig config = null)
	{
		if (assemblyStream == null)
			throw new ArgumentNullException(nameof(assemblyStream));

		Generate(null, new[] { assemblyStream }, outStream, config);
	}

	public static void Generate(IEnumerable<string> assemblyPaths, TextWriter outStream, ApiInfoConfig config = null)
	{
		Generate(assemblyPaths, null, outStream, config);
	}

	public static void Generate(IEnumerable<Stream> assemblyStreams, TextWriter outStream, ApiInfoConfig config = null)
	{
		Generate(null, assemblyStreams, outStream, config);
	}

	public static void Generate(IEnumerable<string> assemblyPaths, IEnumerable<Stream> assemblyStreams, TextWriter outStream, ApiInfoConfig config = null)
	{
		if (outStream == null)
			throw new ArgumentNullException(nameof(outStream));

		if (config == null)
			config = new ApiInfoConfig();

		var state = new State
		{
			AbiMode = config.AbiMode,
			FollowForwarders = config.FollowForwarders,
			FullApiSet = config.FullApiSet,
			IgnoreResolutionErrors = config.IgnoreResolutionErrors,
			IgnoreInheritedInterfaces = config.IgnoreInheritedInterfaces,
		};
		state.SearchDirectories.AddRange(config.SearchDirectories);
		state.ResolveFiles.AddRange(config.ResolveFiles);
		state.ResolveStreams.AddRange(config.ResolveStreams);

		Generate(assemblyPaths, assemblyStreams, outStream, state);
	}

	internal static void Generate(IEnumerable<string> assemblyFiles, IEnumerable<Stream> assemblyStreams, TextWriter outStream, State state = null)
	{
		if (outStream == null)
			throw new ArgumentNullException(nameof(outStream));

		if (state == null)
			state = new State();

		state.ResolveTypes();

		string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
		string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
		state.TypeHelper.Resolver.AddSearchDirectory(Path.Combine(windir, @"assembly\GAC\MSDATASRC\7.0.3300.0__b03f5f7f11d50a3a"));

		var acoll = new AssemblyCollection(state);
		if (assemblyFiles != null)
		{
			foreach (string arg in assemblyFiles)
			{
				acoll.Add(arg);

				if (arg.Contains("v3.0"))
				{
					state.TypeHelper.Resolver.AddSearchDirectory(Path.Combine(windir, @"Microsoft.NET\Framework\v2.0.50727"));
				}
				else if (arg.Contains("v3.5"))
				{
					state.TypeHelper.Resolver.AddSearchDirectory(Path.Combine(windir, @"Microsoft.NET\Framework\v2.0.50727"));
					state.TypeHelper.Resolver.AddSearchDirectory(Path.Combine(windir, @"Microsoft.NET\Framework\v3.0\Windows Communication Foundation"));
				}
				else if (arg.Contains("v4.0"))
				{
					if (arg.Contains("Silverlight"))
					{
						state.TypeHelper.Resolver.AddSearchDirectory(Path.Combine(pf, @"Microsoft Silverlight\4.0.51204.0"));
					}
					else
					{
						state.TypeHelper.Resolver.AddSearchDirectory(Path.Combine(windir, @"Microsoft.NET\Framework\v4.0.30319"));
						state.TypeHelper.Resolver.AddSearchDirectory(Path.Combine(windir, @"Microsoft.NET\Framework\v4.0.30319\WPF"));
					}
				}
				else
				{
					state.TypeHelper.Resolver.AddSearchDirectory(Path.GetDirectoryName(arg));
				}
			}
		}
		if (assemblyStreams != null)
		{
			foreach (var arg in assemblyStreams)
			{
				acoll.Add(arg);
			}
		}

		var settings = new XmlWriterSettings
		{
			Indent = true,
		};
		using (var textWriter = XmlWriter.Create(outStream, settings))
		{
			var writer = new WellFormedXmlWriter(textWriter);
			writer.WriteStartDocument();
			acoll.Writer = writer;
			acoll.DoOutput();
			writer.WriteEndDocument();
			writer.Flush();
		}
	}
}
