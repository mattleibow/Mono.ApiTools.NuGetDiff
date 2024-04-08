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

class XMLNamespace : XMLData
{
	string name;
	XMLClass [] types;

	public override void LoadData (XmlNode node)
	{
		if (node == null)
			throw new ArgumentNullException ("node");

		if (node.Name != "namespace")
			throw new FormatException ("Expecting <namespace>");

		name = node.Attributes  ["name"].Value;
		XmlNode classes = node.FirstChild;
		if (classes == null) {
//#if !EXCLUDE_DRIVER
//			Console.Error.WriteLine ($"Warning: no classes for {name}");
//#endif
			return;
		}

		if (classes.Name != "classes")
			throw new FormatException ($"Expecting <classes>. Got <{classes.Name}> (namespace {name}).");

		types = (XMLClass []) LoadRecursive (classes.ChildNodes, typeof (XMLClass));
	}

	public override void CompareTo (XmlDocument doc, XmlNode parent, object other)
	{
		this.document = doc;
		XMLNamespace nspace = (XMLNamespace) other;

		XmlNode childA = doc.CreateElement ("classes", null);
		parent.AppendChild (childA);

		CompareTypes (childA, nspace != null ? nspace.types : new XMLClass [0]);
	}

	void CompareTypes (XmlNode parent, XMLClass [] other)
	{
		ArrayList newNodes = new ArrayList ();
		Hashtable oh = CreateHash (other);
		XmlNode node = null;
		int count = (types == null) ? 0 : types.Length;
		for (int i = 0; i < count; i++) {
			XMLClass xclass = types [i];

			node = document.CreateElement ("class", null);
			newNodes.Add (node);
			AddAttribute (node, "name", xclass.Name);
			AddAttribute (node, "type", xclass.Type);

			int idx = -1;
			if (oh.ContainsKey (xclass.Name))
				idx = (int) oh [xclass.Name];
			xclass.CompareTo (document, node, idx >= 0 ? other [idx] : new XMLClass ());
			if (idx >= 0)
				other [idx] = null;
			counters.AddPartialToPartial (xclass.Counters);
		}

		if (other != null) {
			count = other.Length;
			for (int i = 0; i < count; i++) {
				XMLClass c = other [i];
				if (c == null || IsMonoTODOAttribute (c.Name))
					continue;

				node = document.CreateElement ("class", null);
				newNodes.Add (node);
				AddAttribute (node, "name", c.Name);
				AddAttribute (node, "type", c.Type);
				AddExtra (node);
				counters.Extra++;
				counters.ExtraTotal++;
			}
		}

		XmlNode [] nodes = (XmlNode []) newNodes.ToArray (typeof (XmlNode));
		Array.Sort (nodes, XmlNodeComparer.Default);
		foreach (XmlNode nn in nodes)
			parent.AppendChild (nn);
	}

	static Hashtable CreateHash (XMLClass [] other)
	{
		Hashtable result = new Hashtable ();
		if (other != null) {
			int i = 0;
			foreach (XMLClass c in other) {
				result [c.Name] = i++;
			}
		}

		return result;
	}

	public string Name {
		get { return name; }
	}
}
