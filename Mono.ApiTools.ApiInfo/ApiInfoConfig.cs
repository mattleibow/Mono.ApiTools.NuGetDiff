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

public class ApiInfoConfig
{
	public bool AbiMode { get; set; } = false;

	public bool FollowForwarders { get; set; } = false;

	public bool FullApiSet { get; set; } = false;

	public bool IgnoreResolutionErrors { get; set; } = false;

	public bool IgnoreInheritedInterfaces { get; set; } = false;

	public IList<string> SearchDirectories { get; set; } = new List<string>();

	public IList<string> ResolveFiles { get; set; } = new List<string>();

	public IList<Stream> ResolveStreams { get; set; } = new List<Stream>();

	public XmlWriterSettings XmlWriterSettings { get; set; } = new XmlWriterSettings { Indent = true };
}
