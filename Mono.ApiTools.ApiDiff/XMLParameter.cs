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

using System.Xml;

namespace Mono.ApiTools;

class XMLParameter : XMLData
{
	string name;
	string type;
	string attrib;
	string direction;
	bool isUnsafe;
	bool isOptional;
	string defaultValue;
	XMLAttributes attributes;

	public override void LoadData (XmlNode node)
	{
		if (node == null)
			throw new ArgumentNullException ("node");

		if (node.Name != "parameter")
			throw new ArgumentException ("Expecting <parameter>");

		name = node.Attributes["name"].Value;
		type = node.Attributes["type"].Value;
		attrib = node.Attributes["attrib"].Value;
		if (node.Attributes ["direction"] != null)
			direction = node.Attributes["direction"].Value;
		if (node.Attributes["unsafe"] != null)
			isUnsafe = bool.Parse (node.Attributes["unsafe"].Value);
		if (node.Attributes["optional"] != null)
			isOptional = bool.Parse (node.Attributes["optional"].Value);
		if (node.Attributes["defaultValue"] != null)
			defaultValue = node.Attributes["defaultValue"].Value;

		XmlNode child = node.FirstChild;
		if (child == null)
			return;

		if (child.Name == "attributes") {
			attributes = new XMLAttributes ();
			attributes.LoadData (child);
			child = child.NextSibling;
		}
	}

	public override void CompareTo (XmlDocument doc, XmlNode parent, object other)
	{
		this.document = doc;

		XMLParameter oparm = (XMLParameter) other;

		if (name != oparm.name)
			AddWarning (parent, "Parameter name is wrong: {0} != {1}", name, oparm.name);

		if (type != oparm.type)
			AddWarning (parent, "Parameter type is wrong: {0} != {1}", type, oparm.type);

		if (attrib != oparm.attrib)
			AddWarning (parent, "Parameter attributes wrong: {0} != {1}", attrib, oparm.attrib);

		if (direction != oparm.direction)
			AddWarning (parent, "Parameter direction wrong: {0} != {1}", direction, oparm.direction);

		if (isUnsafe != oparm.isUnsafe)
			AddWarning (parent, "Parameter unsafe wrong: {0} != {1}", isUnsafe, oparm.isUnsafe);

		if (isOptional != oparm.isOptional)
			AddWarning (parent, "Parameter optional wrong: {0} != {1}", isOptional, oparm.isOptional);

		if (defaultValue != oparm.defaultValue)
			AddWarning (parent, "Parameter default value wrong: {0} != {1}", (defaultValue == null) ? "(no default value)" : defaultValue, (oparm.defaultValue == null) ? "(no default value)" : oparm.defaultValue);

		if (attributes != null || oparm.attributes != null) {
			if (attributes == null)
				attributes = new XMLAttributes ();

			attributes.CompareTo (doc, parent, oparm.attributes);
			counters.AddPartialToPartial (attributes.Counters);
			if (oparm.attributes != null && oparm.attributes.IsTodo) {
				counters.Todo++;
				counters.TodoTotal++;
				counters.ErrorTotal++;
				AddAttribute (parent, "error", "todo");
				if (oparm.attributes.Comment != null)
					AddAttribute (parent, "comment", oparm.attributes.Comment);
			}
		}
	}

	public string Name {
		get { return name; }
	}
}
