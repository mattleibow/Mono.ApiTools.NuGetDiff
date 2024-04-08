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

class XMLProperties : XMLMember
{
	Hashtable nameToMethod = new Hashtable ();

	protected override void CompareToInner (string name, XmlNode parent, XMLNameGroup other)
	{
		Counters copy = counters;
		counters = new Counters();

		XMLProperties oprop = other as XMLProperties;
		if (oprop != null) {
			XMLMethods m = nameToMethod [name] as XMLMethods;
			XMLMethods om = oprop.nameToMethod [name] as XMLMethods;
			if (m != null || om != null) {
				if (m == null)
					m = new XMLMethods ();

				m.CompareTo(document, parent, om);
				counters.AddPartialToPartial(m.Counters);
			}
		}

		base.CompareToInner (name, parent, other);
		AddCountersAttributes(parent);

		copy.AddPartialToPartial(counters);
		counters = copy;
	}

	protected override void LoadExtraData (string name, XmlNode node)
	{
		XmlNode orig = node;
		node = node.FirstChild;
		while (node != null) {
			if (node != null && node.Name == "methods") {
				XMLMethods m = new XMLMethods ();
				XmlNode parent = node.ParentNode;
				string key = GetNodeKey (name, parent);
				m.LoadData (node);
				nameToMethod [key] = m;
				break;
			}
			node = node.NextSibling;
		}

		base.LoadExtraData (name, orig);
	}

	public override string GetNodeKey (string name, XmlNode node)
	{
		XmlAttributeCollection atts = node.Attributes;
		return String.Format ("{0}:{1}:{2}",
				      (atts["name"]   != null ? atts["name"].Value   : ""),
				      (atts["ptype"]  != null ? atts["ptype"].Value  : ""),
				      (atts["params"] != null ? atts["params"].Value : "")
				      );
	}

	public override string GroupName {
		get { return "properties"; }
	}

	public override string Name {
		get { return "property"; }
	}
}
