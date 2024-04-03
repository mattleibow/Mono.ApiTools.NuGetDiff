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
using System.Xml;

namespace Mono.ApiTools;

abstract class XMLMember : XMLNameGroup
{
	Hashtable attributeMap;
	Hashtable access = new Hashtable ();

	protected override void LoadExtraData (string name, XmlNode node)
	{
		XmlAttribute xatt = node.Attributes ["attrib"];
		if (xatt != null)
			access [name] = xatt.Value;

		XmlNode orig = node;

		node = node.FirstChild;
		while (node != null) {
			if (node != null && node.Name == "attributes") {
				XMLAttributes a = new XMLAttributes ();
				a.LoadData (node);
				if (attributeMap == null)
					attributeMap = new Hashtable ();

				attributeMap [name] = a;
				break;
			}
			node = node.NextSibling;
		}

		base.LoadExtraData (name, orig);
	}

	protected override void CompareToInner (string name, XmlNode parent, XMLNameGroup other)
	{
		base.CompareToInner (name, parent, other);
		XMLMember mb = other as XMLMember;
		XMLAttributes att = null;
		XMLAttributes oatt = null;
		if (attributeMap != null)
			att = attributeMap [name] as XMLAttributes;

		if (mb != null && mb.attributeMap != null)
			oatt = mb.attributeMap [name] as XMLAttributes;

		if (att != null || oatt != null) {
			if (att == null)
				att = new XMLAttributes ();

			att.CompareTo (document, parent, oatt);
			counters.AddPartialToPartial(att.Counters);
			if (oatt != null && oatt.IsTodo) {
				counters.Todo++;
				counters.ErrorTotal++;
				AddAttribute (parent, "error", "todo");
				if (oatt.Comment != null)
					AddAttribute (parent, "comment", oatt.Comment);
			}
		}

		XMLMember member = (XMLMember) other;
		string acc = access [name] as string;
		if (acc == null)
			return;

		string oacc = null;
		if (member.access != null)
			oacc = member.access [name] as string;

		string accName = ConvertToString (Int32.Parse (acc));
		string oaccName = "";
		if (oacc != null)
			oaccName = ConvertToString (Int32.Parse (oacc));

		if (accName != oaccName)
			AddWarning (parent, "Incorrect attributes: '{0}' != '{1}'", accName, oaccName);
	}

	protected virtual string ConvertToString (int att)
	{
		return null;
	}
}
