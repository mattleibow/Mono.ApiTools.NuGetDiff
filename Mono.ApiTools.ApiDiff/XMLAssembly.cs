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

class XMLAssembly : XMLData
{
	XMLAttributes attributes;
	XMLNamespace [] namespaces;
	string name;
	string version;

	public override void LoadData (XmlNode node)
	{
		if (node == null)
			throw new ArgumentNullException ("node");

		name = node.Attributes ["name"].Value;
		version = node.Attributes  ["version"].Value;
		XmlNode atts = node.FirstChild;
		attributes = new XMLAttributes ();
		if (atts.Name == "attributes") {
			attributes.LoadData (atts);
			atts = atts.NextSibling;
		}

		if (atts == null || atts.Name != "namespaces") {
//#if !EXCLUDE_DRIVER
//			Console.Error.WriteLine (@"Warning: no namespaces found for {name}");
//#endif
			return;
		}

		namespaces = (XMLNamespace []) LoadRecursive (atts.ChildNodes, typeof (XMLNamespace));
	}

	public override void CompareTo (XmlDocument doc, XmlNode parent, object other)
	{
		XMLAssembly assembly = (XMLAssembly) other;

		XmlNode childA = doc.CreateElement ("assembly", null);
		AddAttribute (childA, "name", name);
		AddAttribute (childA, "version", version);
		if (name != assembly.name)
			AddWarning (childA, "Assembly names not equal: {0}, {1}", name, assembly.name);

		if (version != assembly.version)
			AddWarning (childA, "Assembly version not equal: {0}, {1}", version, assembly.version);

		parent.AppendChild (childA);

		attributes.CompareTo (doc, childA, assembly.attributes);
		counters.AddPartialToPartial (attributes.Counters);

		CompareNamespaces (childA, assembly.namespaces);
		if (assembly.attributes != null && assembly.attributes.IsTodo) {
			counters.Todo++;
			counters.TodoTotal++;
			counters.ErrorTotal++;
			AddAttribute (childA, "error", "todo");
			if (assembly.attributes.Comment != null)
				AddAttribute (childA, "comment", assembly.attributes.Comment);
		}

		AddCountersAttributes (childA);
	}

	void CompareNamespaces (XmlNode parent, XMLNamespace [] other)
	{
		ArrayList newNS = new ArrayList ();
		XmlNode group = document.CreateElement ("namespaces", null);
		parent.AppendChild (group);

		Hashtable oh = CreateHash (other);
		XmlNode node = null;
		int count = (namespaces == null) ? 0 : namespaces.Length;
		for (int i = 0; i < count; i++) {
			XMLNamespace xns = namespaces [i];

			node = document.CreateElement ("namespace", null);
			newNS.Add (node);
			AddAttribute (node, "name", xns.Name);

			int idx = -1;
			if (oh.ContainsKey (xns.Name))
				idx = (int) oh [xns.Name];
			XMLNamespace ons = idx >= 0 ? (XMLNamespace) other [idx] : null;
			xns.CompareTo (document, node, ons);
			if (idx >= 0)
				other [idx] = null;
			xns.AddCountersAttributes (node);
			counters.Present++;
			counters.PresentTotal++;
			counters.AddPartialToTotal (xns.Counters);
		}

		if (other != null) {
			count = other.Length;
			for (int i = 0; i < count; i++) {
				XMLNamespace n = other [i];
				if (n == null)
					continue;

				node = document.CreateElement ("namespace", null);
				newNS.Add (node);
				AddAttribute (node, "name", n.Name);
				AddExtra (node);
				counters.ExtraTotal++;
			}
		}

		XmlNode [] nodes = (XmlNode []) newNS.ToArray (typeof (XmlNode));
		Array.Sort (nodes, XmlNodeComparer.Default);
		foreach (XmlNode nn in nodes)
			group.AppendChild (nn);
	}

	static Hashtable CreateHash (XMLNamespace [] other)
	{
		Hashtable result = new Hashtable ();
		if (other != null) {
			int i = 0;
			foreach (XMLNamespace n in other) {
				result [n.Name] = i++;
			}
		}

		return result;
	}

	public XmlDocument CompareAndGetDocument (XMLAssembly other)
	{
		XmlDocument doc = new XmlDocument ();
		this.document = doc;
		XmlNode parent = doc.CreateElement ("assemblies", null);
		doc.AppendChild (parent);

		CompareTo (doc, parent, other);

		XmlNode decl = doc.CreateXmlDeclaration ("1.0", null, null);
		doc.InsertBefore (decl, doc.DocumentElement);

		return doc;
	}
}
