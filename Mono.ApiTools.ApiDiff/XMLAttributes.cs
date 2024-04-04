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
using System.Text;
using System.Xml;

namespace Mono.ApiTools;

class XMLAttributes : XMLNameGroup
{
	Hashtable properties = new Hashtable ();

	bool isTodo;
	string comment;

	protected override bool CheckIfAdd (string value, XmlNode node)
	{
		if (IsMonoTODOAttribute (value)) {
			isTodo = true;

			XmlNode pNode = node.SelectSingleNode ("properties");
			if (pNode != null && pNode.ChildNodes.Count > 0 && pNode.ChildNodes [0].Attributes ["value"] != null) {
				comment = pNode.ChildNodes [0].Attributes ["value"].Value;
			}
			return false;
		}

		return !IsMeaninglessAttribute (value);
	}

	protected override void CompareToInner (string name, XmlNode node, XMLNameGroup other)
	{
		XMLAttributeProperties other_prop = ((XMLAttributes)other).properties [name] as XMLAttributeProperties;
		XMLAttributeProperties this_prop = properties [name] as XMLAttributeProperties;
		if (other_prop == null || this_prop == null)
			return;

		this_prop.CompareTo (document, node, other_prop);
		counters.AddPartialToPartial (this_prop.Counters);
	}

	public override string GetNodeKey (string name, XmlNode node)
	{
		string key = null;

		// if multiple attributes with the same name (type) exist, then we
		// cannot be sure which attributes correspond, so we must use the
		// name of the attribute (type) and the name/value of its properties
		// as key

		XmlNodeList attributes = node.ParentNode.SelectNodes("attribute[@name='" + name + "']");
		if (attributes.Count > 1) {
			ArrayList keyParts = new ArrayList ();

			XmlNodeList properties = node.SelectNodes ("properties/property");
			foreach (XmlNode property in properties) {
				XmlAttributeCollection attrs = property.Attributes;
				if (attrs["value"] != null) {
					keyParts.Add (attrs["name"].Value + "=" + attrs["value"].Value);
				} else {
					keyParts.Add (attrs["name"].Value + "=");
				}
			}

			// sort properties by name, as order of properties in XML is
			// undefined
			keyParts.Sort ();

			// insert name (type) of attribute
			keyParts.Insert (0, name);

			StringBuilder sb = new StringBuilder ();
			foreach (string value in keyParts) {
				sb.Append (value);
				sb.Append (';');
			}
			key = sb.ToString ();
		} else {
			key = name;
		}

		return key;
	}

	protected override void LoadExtraData(string name, XmlNode node)
	{
		XmlNode pNode = node.SelectSingleNode ("properties");

		if (IsMonoTODOAttribute (name)) {
			isTodo = true;
			if (pNode.ChildNodes [0].Attributes ["value"] != null) {
				comment = pNode.ChildNodes [0].Attributes ["value"].Value;
			}
			return;
		}

		if (pNode != null) {
			XMLAttributeProperties p = new XMLAttributeProperties (name);
			p.LoadData (pNode);

			properties[name] = p;
		}
	}

	public override string GroupName {
		get { return "attributes"; }
	}

	public override string Name {
		get { return "attribute"; }
	}

	public bool IsTodo {
		get { return isTodo; }
	}

	public string Comment {
		get { return comment; }
	}
}
