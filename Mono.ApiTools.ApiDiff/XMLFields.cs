//
// mono-api-diff.cs - Compares 2 xml files produced by mono-api-info and
//		      produces a file suitable to build class status pages.
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Marek Safar		(marek.safar@gmail.com)
//
// Maintainer:
//	C.J. Adams-Collier	(cjac@colliertech.org)
//
// (C) 2003 Novell, Inc (http://www.novell.com)
// (C) 2009,2010 Collier Technologies (http://www.colliertech.org)

using System.Collections;
using System.Reflection;
using System.Xml;

namespace Mono.ApiTools;

class XMLFields : XMLMember
{
	Hashtable fieldTypes;
	Hashtable fieldValues;

	protected override void LoadExtraData (string name, XmlNode node)
	{
		XmlAttribute xatt = node.Attributes ["fieldtype"];
		if (xatt != null) {
			if (fieldTypes == null)
				fieldTypes = new Hashtable ();

			fieldTypes [name] = xatt.Value;
		}

		xatt = node.Attributes ["value"];
		if (xatt != null) {
			if (fieldValues == null)
				fieldValues = new Hashtable ();

			fieldValues[name] = xatt.Value;
		}

		base.LoadExtraData (name, node);
	}

	protected override void CompareToInner (string name, XmlNode parent, XMLNameGroup other)
	{
		base.CompareToInner (name, parent, other);
		XMLFields fields = (XMLFields) other;
		if (fieldTypes != null) {
			string ftype = fieldTypes [name] as string;
			string oftype = null;
			if (fields.fieldTypes != null)
				oftype = fields.fieldTypes [name] as string;

			if (ftype != oftype)
				AddWarning (parent, "Field type is {0} and should be {1}", oftype, ftype);
		}
		if (fieldValues != null) {
			string fvalue = fieldValues [name] as string;
			string ofvalue = null;
			if (fields.fieldValues != null)
				ofvalue = fields.fieldValues [name] as string;

			if (fvalue != ofvalue)
				AddWarning (parent, "Field value is {0} and should be {1}", ofvalue, fvalue);
		}
	}

	protected override string ConvertToString (int att)
	{
		FieldAttributes fa = (FieldAttributes) att;
		return fa.ToString ();
	}

	public override string GroupName {
		get { return "fields"; }
	}

	public override string Name {
		get { return "field"; }
	}
}
