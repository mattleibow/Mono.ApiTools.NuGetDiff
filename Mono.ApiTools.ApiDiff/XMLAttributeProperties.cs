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

class XMLAttributeProperties: XMLNameGroup
{
	static Hashtable ignored_properties;
	static XMLAttributeProperties ()
	{
		ignored_properties = new Hashtable ();
		ignored_properties.Add ("System.Reflection.AssemblyKeyFileAttribute", "KeyFile");
		ignored_properties.Add ("System.Reflection.AssemblyCompanyAttribute", "Company");
		ignored_properties.Add ("System.Reflection.AssemblyConfigurationAttribute", "Configuration");
		ignored_properties.Add ("System.Reflection.AssemblyCopyrightAttribute", "Copyright");
		ignored_properties.Add ("System.Reflection.AssemblyProductAttribute", "Product");
		ignored_properties.Add ("System.Reflection.AssemblyTrademarkAttribute", "Trademark");
		ignored_properties.Add ("System.Reflection.AssemblyInformationalVersionAttribute", "InformationalVersion");

		ignored_properties.Add ("System.ObsoleteAttribute", "Message");
		ignored_properties.Add ("System.IO.IODescriptionAttribute", "Description");
		ignored_properties.Add ("System.Diagnostics.MonitoringDescriptionAttribute", "Description");
	}

	Hashtable properties = new Hashtable ();
	string attribute;

	public XMLAttributeProperties (string attribute)
	{
		this.attribute = attribute;
	}

	public override void LoadData(XmlNode node)
	{
		if (node == null)
			throw new ArgumentNullException ("node");

		if (node.ChildNodes == null)
			return;

		string ignored = ignored_properties [attribute] as string;

		foreach (XmlNode n in node.ChildNodes) {
			string name = n.Attributes ["name"].Value;
			if (ignored == name)
				continue;

			if (n.Attributes ["null"] != null) {
				properties.Add (name, null);
				continue;
			}
			string value = n.Attributes ["value"].Value;
			properties.Add (name, value);
		}
	}

	public override void CompareTo (XmlDocument doc, XmlNode parent, object other)
	{
		this.document = doc;

		Hashtable other_properties = ((XMLAttributeProperties)other).properties;
		foreach (DictionaryEntry de in other_properties) {
			object other_value = properties [de.Key];

			if (de.Value == null) {
				if (other_value != null)
					AddWarning (parent, "Property '{0}' is 'null' and should be '{1}'", de.Key, other_value);
				continue;
			}

			if (de.Value.Equals (other_value))
				continue;

			AddWarning (parent, "Property '{0}' is '{1}' and should be '{2}'",
				de.Key, de.Value, other_value == null ? "null" : other_value);
		}
	}

	public override string GroupName {
		get {
			return "properties";
		}
	}

	public override string Name {
		get {
			return "";
		}
	}
}
