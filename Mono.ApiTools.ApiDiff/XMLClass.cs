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

class XMLClass : XMLData
{
	string name;
	string type;
	string baseName;
	bool isSealed;
	bool isSerializable;
	bool isAbstract;
	string charSet;
	string layout;
	XMLAttributes attributes;
	XMLInterfaces interfaces;
	XMLGenericTypeConstraints genericConstraints;
	XMLFields fields;
	XMLConstructors constructors;
	XMLProperties properties;
	XMLEvents events;
	XMLMethods methods;
	XMLClass [] nested;

	public override void LoadData (XmlNode node)
	{
		if (node == null)
			throw new ArgumentNullException ("node");

		name = node.Attributes ["name"].Value;
		type = node.Attributes  ["type"].Value;
		XmlAttribute xatt = node.Attributes ["base"];
		if (xatt != null)
			baseName = xatt.Value;

		xatt = node.Attributes ["sealed"];
		isSealed = (xatt != null && xatt.Value == "true");

		xatt = node.Attributes ["abstract"];
		isAbstract = (xatt != null && xatt.Value == "true");

		xatt = node.Attributes["serializable"];
		isSerializable = (xatt != null && xatt.Value == "true");

		xatt = node.Attributes["charset"];
		if (xatt != null)
			charSet = xatt.Value;

		xatt = node.Attributes["layout"];
		if (xatt != null)
			layout = xatt.Value;

		XmlNode child = node.FirstChild;
		if (child == null) {
			return;
		}

		if (child.Name == "attributes") {
			attributes = new XMLAttributes ();
			attributes.LoadData (child);
			child = child.NextSibling;
		}

		if (child != null && child.Name == "interfaces") {
			interfaces = new XMLInterfaces ();
			interfaces.LoadData (child);
			child = child.NextSibling;
		}

		if (child != null && child.Name == "generic-type-constraints") {
			genericConstraints = new XMLGenericTypeConstraints ();
			genericConstraints.LoadData (child);
			child = child.NextSibling;
		}

		if (child != null && child.Name == "fields") {
			fields = new XMLFields ();
			fields.LoadData (child);
			child = child.NextSibling;
		}

		if (child != null && child.Name == "constructors") {
			constructors = new XMLConstructors ();
			constructors.LoadData (child);
			child = child.NextSibling;
		}

		if (child != null && child.Name == "properties") {
			properties = new XMLProperties ();
			properties.LoadData (child);
			child = child.NextSibling;
		}

		if (child != null && child.Name == "events") {
			events = new XMLEvents ();
			events.LoadData (child);
			child = child.NextSibling;
		}

		if (child != null && child.Name == "methods") {
			methods = new XMLMethods ();
			methods.LoadData (child);
			child = child.NextSibling;
		}

		if (child != null && child.Name == "generic-parameters") {
			// HACK: ignore this tag as it doesn't seem to
			// add any value when checking for differences
			return;
		}

		if (child == null)
			return;

		if (child.Name != "classes") {
			throw new FormatException ($"Expecting <classes>. Got <{child.Name}> ({type} {name}).");
		}

		nested = (XMLClass []) LoadRecursive (child.ChildNodes, typeof (XMLClass));
	}

	public override void CompareTo (XmlDocument doc, XmlNode parent, object other)
	{
		this.document = doc;
		XMLClass oclass = (XMLClass) other;

		if (attributes != null || oclass.attributes != null) {
			if (attributes == null)
				attributes = new XMLAttributes ();

			attributes.CompareTo (doc, parent, oclass.attributes);
			counters.AddPartialToPartial (attributes.Counters);
			if (oclass.attributes != null && oclass.attributes.IsTodo) {
				counters.Todo++;
				counters.TodoTotal++;
				counters.ErrorTotal++;
				AddAttribute (parent, "error", "todo");
				if (oclass.attributes.Comment != null)
					AddAttribute (parent, "comment", oclass.attributes.Comment);
			}
		}

		if (type != oclass.type)
			AddWarning (parent, "Class type is wrong: {0} != {1}", type, oclass.type);

		if (baseName != oclass.baseName)
			AddWarning (parent, "Base class is wrong: {0} != {1}", baseName, oclass.baseName);

		if (isAbstract != oclass.isAbstract || isSealed != oclass.isSealed) {
			if ((isAbstract && isSealed) || (oclass.isAbstract && oclass.isSealed))
				AddWarning (parent, "Should {0}be static", (isAbstract && isSealed) ? "" : "not ");
			else if (isAbstract != oclass.isAbstract)
				AddWarning (parent, "Should {0}be abstract", isAbstract ? "" : "not ");
			else if (isSealed != oclass.isSealed)
				AddWarning (parent, "Should {0}be sealed", isSealed ? "" : "not ");
		}

		if (isSerializable != oclass.isSerializable)
			AddWarning (parent, "Should {0}be serializable", isSerializable ? "" : "not ");

		if (charSet != oclass.charSet)
			AddWarning (parent, "CharSet is wrong: {0} != {1}", charSet, oclass.charSet);

		if (layout != oclass.layout)
			AddWarning (parent, "Layout is wrong: {0} != {1}", layout, oclass.layout);

		if (interfaces != null || oclass.interfaces != null) {
			if (interfaces == null)
				interfaces = new XMLInterfaces ();

			interfaces.CompareTo (doc, parent, oclass.interfaces);
			counters.AddPartialToPartial (interfaces.Counters);
		}

		if (genericConstraints != null || oclass.genericConstraints != null) {
			if (genericConstraints == null)
				genericConstraints = new XMLGenericTypeConstraints ();

			genericConstraints.CompareTo (doc, parent, oclass.genericConstraints);
			counters.AddPartialToPartial (genericConstraints.Counters);
		}

		if (fields != null || oclass.fields != null) {
			if (fields == null)
				fields = new XMLFields ();

			fields.CompareTo (doc, parent, oclass.fields);
			counters.AddPartialToPartial (fields.Counters);
		}

		if (constructors != null || oclass.constructors != null) {
			if (constructors == null)
				constructors = new XMLConstructors ();

			constructors.CompareTo (doc, parent, oclass.constructors);
			counters.AddPartialToPartial (constructors.Counters);
		}

		if (properties != null || oclass.properties != null) {
			if (properties == null)
				properties = new XMLProperties ();

			properties.CompareTo (doc, parent, oclass.properties);
			counters.AddPartialToPartial (properties.Counters);
		}

		if (events != null || oclass.events != null) {
			if (events == null)
				events = new XMLEvents ();

			events.CompareTo (doc, parent, oclass.events);
			counters.AddPartialToPartial (events.Counters);
		}

		if (methods != null || oclass.methods != null) {
			if (methods == null)
				methods = new XMLMethods ();

			methods.CompareTo (doc, parent, oclass.methods);
			counters.AddPartialToPartial (methods.Counters);
		}

		if (nested != null || oclass.nested != null) {
			XmlNode n = doc.CreateElement ("classes", null);
			parent.AppendChild (n);
			CompareTypes (n, oclass.nested);
		}

		AddCountersAttributes (parent);
	}

	void CompareTypes (XmlNode parent, XMLClass [] other)
	{
		ArrayList newNodes = new ArrayList ();
		Hashtable oh = CreateHash (other);
		XmlNode node = null;
		int count = (nested == null) ? 0 : nested.Length;
		for (int i = 0; i < count; i++) {
			XMLClass xclass = nested [i];

			node = document.CreateElement ("class", null);
			newNodes.Add (node);
			AddAttribute (node, "name", xclass.Name);
			AddAttribute (node, "type", xclass.Type);

			if (oh.ContainsKey (xclass.Name)) {
				int idx = (int) oh [xclass.Name];
				xclass.CompareTo (document, node, other [idx]);
				other [idx] = null;
				counters.AddPartialToPartial (xclass.Counters);
			} else {
				// TODO: Should I count here?
				AddAttribute (node, "presence", "missing");
				counters.Missing++;
				counters.MissingTotal++;
			}
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

	public string Type {
		get { return type; }
	}
}
