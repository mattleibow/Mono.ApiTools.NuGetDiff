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

abstract class XMLNameGroup : XMLData
{
	protected XmlNode group;
	protected Hashtable keys;

	public override void LoadData (XmlNode node)
	{
		if (node == null)
			throw new ArgumentNullException ("node");

		if (node.Name != GroupName)
			throw new FormatException (String.Format ("Expecting <{0}>", GroupName));

		keys = new Hashtable ();
		foreach (XmlNode n in node.ChildNodes) {
			string name = n.Attributes ["name"].Value;
			if (CheckIfAdd (name, n)) {
				string key = GetNodeKey (name, n);
				//keys.Add (key, name);
				keys [key] = name;
				LoadExtraData (key, n);
			}
		}
	}

	protected virtual bool CheckIfAdd (string value, XmlNode node)
	{
		return true;
	}

	protected virtual void LoadExtraData (string name, XmlNode node)
	{
	}

	public override void CompareTo (XmlDocument doc, XmlNode parent, object other)
	{
		this.document = doc;
		if (group == null)
			group = doc.CreateElement (GroupName, null);

		Hashtable okeys = null;
		if (other != null && ((XMLNameGroup) other).keys != null) {
			okeys = ((XMLNameGroup) other).keys;
		}

		XmlNode node = null;
		bool onull = (okeys == null);
		if (keys != null) {
			foreach (DictionaryEntry entry in keys) {
				node = doc.CreateElement (Name, null);
				group.AppendChild (node);
				string key = (string) entry.Key;
				string name = (string) entry.Value;
				AddAttribute (node, "name", name);

				if (!onull && HasKey (key, okeys)) {
					CompareToInner (key, node, (XMLNameGroup) other);
					okeys.Remove (key);
					counters.Present++;
				} else {
					AddAttribute (node, "presence", "missing");
					counters.Missing++;
				}
			}
		}

		if (!onull && okeys.Count != 0) {
			foreach (string value in okeys.Values) {
				node = doc.CreateElement (Name, null);
				AddAttribute (node, "name", (string) value);
				AddAttribute (node, "presence", "extra");
				group.AppendChild (node);
				counters.Extra++;
			}
		}

		if (group.HasChildNodes)
			parent.AppendChild (group);
	}

	protected virtual void CompareToInner (string name, XmlNode node, XMLNameGroup other)
	{
	}

	public virtual string GetNodeKey (string name, XmlNode node)
	{
		return name;
	}

	public virtual bool HasKey (string key, Hashtable other)
	{
		return other.ContainsKey (key);
	}

	public abstract string GroupName { get; }
	public abstract string Name { get; }
}
