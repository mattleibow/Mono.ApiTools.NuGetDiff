//
// mono-api-info.cs - Dumps public assembly information to an xml file.
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// Copyright (C) 2003-2008 Novell, Inc (http://www.novell.com)
//

namespace Mono.ApiTools;

public class ApiInfoConfig
{
	public bool AbiMode { get; set; } = false;

	public bool FollowForwarders { get; set; } = false;

	public bool FullApiSet { get; set; } = false;

	public bool IgnoreResolutionErrors { get; set; } = false;

	public bool IgnoreInheritedInterfaces { get; set; } = false;

	public List<string> SearchDirectories { get; set; } = new List<string>();

	public List<string> ResolveFiles { get; set; } = new List<string>();

	public List<Stream> ResolveStreams { get; set; } = new List<Stream>();
}
