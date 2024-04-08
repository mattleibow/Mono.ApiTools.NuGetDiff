//
// mono-api-info.cs - Dumps public assembly information to an xml file.
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// Copyright (C) 2003-2008 Novell, Inc (http://www.novell.com)
//

using System.Runtime.CompilerServices;
using System.Xml;

using Mono.Cecil;

namespace Mono.ApiTools;

class TypeForwardedToData : BaseData
{
	AssemblyDefinition ass;

	public TypeForwardedToData(XmlWriter writer, AssemblyDefinition ass, State state)
		: base(writer, state)
	{
		this.ass = ass;
	}

	public override void DoOutput()
	{
		foreach (ExportedType type in ass.MainModule.ExportedTypes)
		{

			if (((uint)type.Attributes & 0x200000u) == 0)
				continue;

			writer.WriteStartElement("attribute");
			AddAttribute("name", typeof(TypeForwardedToAttribute).FullName);
			writer.WriteStartElement("properties");
			writer.WriteStartElement("property");
			AddAttribute("name", "Destination");
			AddAttribute("value", Utils.CleanupTypeName(type.FullName));
			writer.WriteEndElement(); // properties
			writer.WriteEndElement(); // properties
			writer.WriteEndElement(); // attribute
		}
	}

	public static void OutputForwarders(XmlWriter writer, AssemblyDefinition ass, State state)
	{
		TypeForwardedToData tftd = new TypeForwardedToData(writer, ass, state);
		tftd.DoOutput();
	}
}
