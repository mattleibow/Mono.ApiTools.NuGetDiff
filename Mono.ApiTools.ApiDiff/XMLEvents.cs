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

class XMLEvents : XMLMember
{
	Hashtable eventTypes;
	Hashtable nameToMethod = new Hashtable ();

	protected override void LoadExtraData (string name, XmlNode node)
	{
		XmlAttribute xatt = node.Attributes ["eventtype"];
		if (xatt != null) {
			if (eventTypes == null)
				eventTypes = new Hashtable ();

			eventTypes [name] = xatt.Value;
		}

		XmlNode child = node.FirstChild;
		while (child != null) {
			if (child != null && child.Name == "methods") {
				XMLMethods m = new XMLMethods ();
				XmlNode parent = child.ParentNode;
				string key = GetNodeKey (name, parent);
				m.LoadData (child);
				nameToMethod [key] = m;
				break;
			}
			child = child.NextSibling;
		}

		base.LoadExtraData (name, node);
	}

	protected override void CompareToInner (string name, XmlNode parent, XMLNameGroup other)
	{
		Counters copy = counters;
		counters = new Counters ();

		try {
			base.CompareToInner (name, parent, other);
			AddCountersAttributes (parent);
			if (eventTypes == null)
				return;

			XMLEvents evt = (XMLEvents) other;
			string etype = eventTypes [name] as string;
			string oetype = null;
			if (evt.eventTypes != null)
				oetype = evt.eventTypes [name] as string;

			if (etype != oetype)
				AddWarning (parent, "Event type is {0} and should be {1}", oetype, etype);

			XMLMethods m = nameToMethod [name] as XMLMethods;
			XMLMethods om = evt.nameToMethod [name] as XMLMethods;
			if (m != null || om != null) {
				if (m == null)
					m = new XMLMethods ();

				m.CompareTo (document, parent, om);
				counters.AddPartialToPartial (m.Counters);
			}
		} finally {
			AddCountersAttributes (parent);
			copy.AddPartialToPartial (counters);
			counters = copy;
		}
	}

	protected override string ConvertToString (int att)
	{
		EventAttributes ea = (EventAttributes) att;
		return ea.ToString ();
	}

	public override string GroupName {
		get { return "events"; }
	}

	public override string Name {
		get { return "event"; }
	}
}
