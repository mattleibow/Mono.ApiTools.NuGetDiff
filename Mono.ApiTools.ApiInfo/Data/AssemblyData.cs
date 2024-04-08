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

class AssemblyData : BaseData
{
	AssemblyDefinition ass;

	public AssemblyData(XmlWriter writer, AssemblyDefinition ass, State state)
		: base(writer, state)
	{
		this.ass = ass;
	}

	public override void DoOutput()
	{
		if (writer == null)
			throw new InvalidOperationException("Document not set");

		writer.WriteStartElement("assembly");
		AssemblyNameDefinition aname = ass.Name;
		AddAttribute("name", aname.Name);
		AddAttribute("version", aname.Version.ToString());

		AttributeData.OutputAttributes(writer, state, ass);

		var types = new List<TypeDefinition>();
		if (ass.MainModule.Types != null)
		{
			types.AddRange(ass.MainModule.Types);
		}

		if (state.FollowForwarders && ass.MainModule.ExportedTypes != null)
		{
			foreach (var t in ass.MainModule.ExportedTypes)
			{
				var forwarded = t.Resolve();
				if (forwarded == null)
				{
					throw new Exception("Could not resolve forwarded type " + t.FullName + " in " + ass.Name);
				}
				types.Add(forwarded);
			}
		}

		if (types.Count == 0)
		{
			writer.WriteEndElement(); // assembly
			return;
		}

		types.Sort(TypeReferenceComparer.Default);

		writer.WriteStartElement("namespaces");

		string current_namespace = "$%&$&";
		bool in_namespace = false;
		foreach (TypeDefinition t in types)
		{
			if (string.IsNullOrEmpty(t.Namespace))
				continue;

			if (!state.AbiMode && ((t.Attributes & TypeAttributes.VisibilityMask) != TypeAttributes.Public))
				continue;

			if (t.DeclaringType != null)
				continue; // enforce !nested

			if (t.Namespace != current_namespace)
			{
				current_namespace = t.Namespace;
				if (in_namespace)
				{
					writer.WriteEndElement(); // classes
					writer.WriteEndElement(); // namespace
				}
				else
				{
					in_namespace = true;
				}
				writer.WriteStartElement("namespace");
				AddAttribute("name", current_namespace);
				writer.WriteStartElement("classes");
			}

			TypeData bd = new TypeData(writer, t, state);
			bd.DoOutput();

		}

		if (in_namespace)
		{
			writer.WriteEndElement(); // classes
			writer.WriteEndElement(); // namespace
		}

		writer.WriteEndElement(); // namespaces

		writer.WriteEndElement(); // assembly
	}
}
