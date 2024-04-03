//
// mono-api-info.cs - Dumps public assembly information to an xml file.
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// Copyright (C) 2003-2008 Novell, Inc (http://www.novell.com)
//

using System.Xml;

using Mono.Cecil;

namespace Mono.ApiTools;

class ConstructorData : MethodData
{
	public ConstructorData(XmlWriter writer, MethodDefinition[] members, State state)
		: base(writer, members, state)
	{
	}

	public override string ParentTag
	{
		get { return "constructors"; }
	}

	public override string Tag
	{
		get { return "constructor"; }
	}
}
