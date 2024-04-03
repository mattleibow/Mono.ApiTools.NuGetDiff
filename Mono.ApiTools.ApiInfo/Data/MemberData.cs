//
// mono-api-info.cs - Dumps public assembly information to an xml file.
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// Copyright (C) 2003-2008 Novell, Inc (http://www.novell.com)
//

using Mono.Cecil;
using System.Xml;

namespace Mono.ApiTools;

abstract class MemberData : BaseData
{
	MemberReference[] members;

	public MemberData(XmlWriter writer, MemberReference[] members, State state)
		: base(writer, state)
	{
		this.members = members;
	}

	protected virtual ICustomAttributeProvider GetAdditionalCustomAttributeProvider(MemberReference member)
	{
		return null;
	}

	public override void DoOutput()
	{
		writer.WriteStartElement(ParentTag);

		foreach (MemberReference member in members)
		{
			writer.WriteStartElement(Tag);
			AddAttribute("name", GetName(member));
			if (!NoMemberAttributes)
				AddAttribute("attrib", GetMemberAttributes(member));
			AddExtraAttributes(member);

			AttributeData.OutputAttributes(writer, state, (ICustomAttributeProvider)member, GetAdditionalCustomAttributeProvider(member));

			AddExtraData(member);
			writer.WriteEndElement(); // Tag
		}

		writer.WriteEndElement(); // ParentTag
	}

	protected virtual void AddExtraData(MemberReference memberDefenition)
	{
	}

	protected virtual void AddExtraAttributes(MemberReference memberDefinition)
	{
	}

	protected virtual string GetName(MemberReference memberDefenition)
	{
		return "NoNAME";
	}

	protected virtual string GetMemberAttributes(MemberReference memberDefenition)
	{
		return null;
	}

	public virtual bool NoMemberAttributes
	{
		get { return false; }
		set { }
	}

	public virtual string ParentTag
	{
		get { return "NoPARENTTAG"; }
	}

	public virtual string Tag
	{
		get { return "NoTAG"; }
	}

	public static void OutputGenericParameters(XmlWriter writer, IGenericParameterProvider provider, State state)
	{
		if (provider.GenericParameters.Count == 0)
			return;

		var gparameters = provider.GenericParameters;

		writer.WriteStartElement("generic-parameters");

		foreach (GenericParameter gp in gparameters)
		{
			writer.WriteStartElement("generic-parameter");
			writer.WriteAttributeString("name", gp.Name);
			writer.WriteAttributeString("attributes", ((int)gp.Attributes).ToString());

			AttributeData.OutputAttributes(writer, state, gp);

			var constraints = gp.Constraints;
			if (constraints.Count == 0)
			{
				writer.WriteEndElement(); // generic-parameter
				continue;
			}

			writer.WriteStartElement("generic-parameter-constraints");

			foreach (TypeReference constraint in constraints)
			{
				writer.WriteStartElement("generic-parameter-constraint");
				writer.WriteAttributeString("name", Utils.CleanupTypeName(constraint));
				writer.WriteEndElement(); // generic-parameter-constraint
			}

			writer.WriteEndElement(); // generic-parameter-constraints

			writer.WriteEndElement(); // generic-parameter
		}

		writer.WriteEndElement(); // generic-parameters
	}
}
