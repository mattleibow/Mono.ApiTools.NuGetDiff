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
using System.Globalization;

namespace Mono.ApiTools;

class PropertyData : MemberData
{
	public PropertyData(XmlWriter writer, PropertyDefinition[] members, State state)
		: base(writer, members, state)
	{
	}

	protected override string GetName(MemberReference memberDefenition)
	{
		PropertyDefinition prop = (PropertyDefinition)memberDefenition;
		return prop.Name;
	}

	MethodDefinition[] GetMethods(PropertyDefinition prop, out bool haveParameters)
	{
		MethodDefinition _get = prop.GetMethod;
		MethodDefinition _set = prop.SetMethod;
		bool haveGet = (_get != null && TypeData.MustDocumentMethod(_get));
		bool haveSet = (_set != null && TypeData.MustDocumentMethod(_set));
		haveParameters = haveGet || (haveSet && _set.Parameters.Count > 1);
		MethodDefinition[] methods;

		if (haveGet && haveSet)
		{
			methods = new MethodDefinition[] { _get, _set };
		}
		else if (haveGet)
		{
			methods = new MethodDefinition[] { _get };
		}
		else if (haveSet)
		{
			methods = new MethodDefinition[] { _set };
		}
		else
		{
			//odd
			return null;
		}

		return methods;
	}

	protected override void AddExtraAttributes(MemberReference memberDefinition)
	{
		base.AddExtraAttributes(memberDefinition);

		PropertyDefinition prop = (PropertyDefinition)memberDefinition;
		AddAttribute("ptype", Utils.CleanupTypeName(prop.PropertyType));

		bool haveParameters;
		MethodDefinition[] methods = GetMethods((PropertyDefinition)memberDefinition, out haveParameters);

		if (methods != null && haveParameters)
		{
			string parms = Parameters.GetSignature(methods[0].Parameters);
			if (!string.IsNullOrEmpty(parms))
				AddAttribute("params", parms);
		}

	}

	protected override void AddExtraData(MemberReference memberDefenition)
	{
		base.AddExtraData(memberDefenition);

		bool haveParameters;
		MethodDefinition[] methods = GetMethods((PropertyDefinition)memberDefenition, out haveParameters);

		if (methods == null)
			return;

		MethodData data = new MethodData(writer, methods, state);
		//data.NoMemberAttributes = true;
		data.DoOutput();
	}

	protected override string GetMemberAttributes(MemberReference memberDefenition)
	{
		PropertyDefinition prop = (PropertyDefinition)memberDefenition;
		return ((int)prop.Attributes).ToString(CultureInfo.InvariantCulture);
	}

	public override string ParentTag
	{
		get { return "properties"; }
	}

	public override string Tag
	{
		get { return "property"; }
	}
}
