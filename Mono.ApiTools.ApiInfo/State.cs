//
// mono-api-info.cs - Dumps public assembly information to an xml file.
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// Copyright (C) 2003-2008 Novell, Inc (http://www.novell.com)
//

namespace Mono.ApiTools;

class State
{
	public bool AbiMode { get; set; } = false;

	public bool FollowForwarders { get; set; } = false;

	public bool FullApiSet { get; set; } = false;

	public bool IgnoreResolutionErrors { get; set; } = false;

	public bool IgnoreInheritedInterfaces { get; set; } = false;

	public List<string> SearchDirectories { get; } = new List<string>();

	public List<string> ResolveFiles { get; } = new List<string>();

	public List<Stream> ResolveStreams { get; } = new List<Stream>();

	public TypeHelper TypeHelper { get; private set; }

	public void ResolveTypes()
	{
		TypeHelper = new TypeHelper(IgnoreResolutionErrors, IgnoreInheritedInterfaces);

		if (SearchDirectories != null)
		{
			foreach (var v in SearchDirectories)
				TypeHelper.Resolver.AddSearchDirectory(v);
		}
		if (ResolveFiles != null)
		{
			foreach (var v in ResolveFiles)
				TypeHelper.Resolver.ResolveFile(v);
		}
		if (ResolveStreams != null)
		{
			foreach (var v in ResolveStreams)
				TypeHelper.Resolver.ResolveStream(v);
		}
	}
}
