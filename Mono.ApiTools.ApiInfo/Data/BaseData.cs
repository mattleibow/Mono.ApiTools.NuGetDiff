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

abstract class BaseData
{
	protected XmlWriter writer;
	protected State state;

	protected BaseData(XmlWriter writer, State state)
	{
		this.writer = writer;
		this.state = state;
	}

	public abstract void DoOutput();

	protected void AddAttribute(string name, string value)
	{
		writer.WriteAttributeString(name, value);
	}
}
