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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Mono.ApiTools;

class XMLParameters : XMLNameGroup
{
	public override void LoadData (XmlNode node)
	{
		if (node == null)
			throw new ArgumentNullException ("node");

		if (node.Name != GroupName)
			throw new FormatException (String.Format ("Expecting <{0}>", GroupName));

		keys = new Hashtable ();
		foreach (XmlNode n in node.ChildNodes) {
			string name = n.Attributes["name"].Value;
			string key = GetNodeKey (name, n);
			XMLParameter parm = new XMLParameter ();
			parm.LoadData (n);
			keys.Add (key, parm);
			LoadExtraData (key, n);
		}
	}

	public override string GroupName {
		get {
			return "parameters";
		}
	}

	public override string Name {
		get {
			return "parameter";
		}
	}

	public override string GetNodeKey (string name, XmlNode node)
	{
		return node.Attributes["position"].Value;
	}

	public override void CompareTo (XmlDocument doc, XmlNode parent, object other)
	{
		this.document = doc;
		if (group == null)
			group = doc.CreateElement (GroupName, null);

		Hashtable okeys = null;
		if (other != null && ((XMLParameters) other).keys != null) {
			okeys = ((XMLParameters) other).keys;
		}

		XmlNode node = null;
		bool onull = (okeys == null);
		if (keys != null) {
			foreach (DictionaryEntry entry in keys) {
				node = doc.CreateElement (Name, null);
				group.AppendChild (node);
				string key = (string) entry.Key;
				XMLParameter parm = (XMLParameter) entry.Value;
				AddAttribute (node, "name", parm.Name);

				if (!onull && HasKey (key, okeys)) {
					parm.CompareTo (document, node, okeys[key]);
					counters.AddPartialToPartial (parm.Counters);
					okeys.Remove (key);
					counters.Present++;
				} else {
					AddAttribute (node, "presence", "missing");
					counters.Missing++;
				}
			}
		}

		if (!onull && okeys.Count != 0) {
			foreach (XMLParameter value in okeys.Values) {
				node = doc.CreateElement (Name, null);
				AddAttribute (node, "name", value.Name);
				AddAttribute (node, "presence", "extra");
				group.AppendChild (node);
				counters.Extra++;
			}
		}

		if (group.HasChildNodes)
			parent.AppendChild (group);
	}
}
