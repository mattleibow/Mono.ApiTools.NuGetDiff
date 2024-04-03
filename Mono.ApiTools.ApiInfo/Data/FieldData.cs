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

class FieldData : MemberData
{
	public FieldData(XmlWriter writer, FieldDefinition[] members, State state)
		: base(writer, members, state)
	{
	}

	protected override string GetName(MemberReference memberDefenition)
	{
		FieldDefinition field = (FieldDefinition)memberDefenition;
		return field.Name;
	}

	protected override string GetMemberAttributes(MemberReference memberDefenition)
	{
		FieldDefinition field = (FieldDefinition)memberDefenition;
		return ((int)field.Attributes).ToString(CultureInfo.InvariantCulture);
	}

	protected override void AddExtraAttributes(MemberReference memberDefinition)
	{
		base.AddExtraAttributes(memberDefinition);

		FieldDefinition field = (FieldDefinition)memberDefinition;
		AddAttribute("fieldtype", Utils.CleanupTypeName(field.FieldType));

		if (field.IsLiteral)
		{
			object value = field.Constant;//object value = field.GetValue (null);
			string stringValue = null;
			//if (value is Enum) {
			//    // FIXME: when Mono bug #60090 has been
			//    // fixed, we should just be able to use
			//    // Convert.ToString
			//    stringValue = ((Enum) value).ToString ("D", CultureInfo.InvariantCulture);
			//}
			//else {
			stringValue = Convert.ToString(value, CultureInfo.InvariantCulture);
			//}

			if (stringValue != null)
				AddAttribute("value", stringValue);
		}
	}

	public override string ParentTag
	{
		get { return "fields"; }
	}

	public override string Tag
	{
		get { return "field"; }
	}
}
