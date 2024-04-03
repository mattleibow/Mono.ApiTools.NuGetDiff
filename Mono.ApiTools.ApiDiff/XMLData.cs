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

abstract class XMLData
{
	protected XmlDocument document;
	protected Counters counters;
	bool haveWarnings;

	public XMLData ()
	{
		counters = new Counters ();
	}

	public virtual void LoadData (XmlNode node)
	{
	}

	protected object [] LoadRecursive (XmlNodeList nodeList, Type type)
	{
		ArrayList list = new ArrayList ();
		foreach (XmlNode node in nodeList) {
			XMLData data = (XMLData) Activator.CreateInstance (type);
			data.LoadData (node);
			list.Add (data);
		}

		return (object []) list.ToArray (type);
	}

	public static bool IsMeaninglessAttribute (string s)
	{
		if (s == null)
			return false;
		if (s == "System.Runtime.CompilerServices.CompilerGeneratedAttribute")
			return true;
		return false;
	}

	public static bool IsMonoTODOAttribute (string s)
	{
		if (s == null)
			return false;
		if (//s.EndsWith ("MonoTODOAttribute") ||
		    s.EndsWith ("MonoDocumentationNoteAttribute") ||
		    s.EndsWith ("MonoExtensionAttribute") ||
//			    s.EndsWith ("MonoInternalNoteAttribute") ||
		    s.EndsWith ("MonoLimitationAttribute") ||
		    s.EndsWith ("MonoNotSupportedAttribute"))
			return true;
		return s.EndsWith ("TODOAttribute");
	}

	protected void AddAttribute (XmlNode node, string name, string value)
	{
		XmlAttribute attr = document.CreateAttribute (name);
		attr.Value = value;
		node.Attributes.Append (attr);
	}

	protected void AddExtra (XmlNode node)
	{
		//TODO: count all the subnodes?
		AddAttribute (node, "presence", "extra");
		AddAttribute (node, "ok", "1");
		AddAttribute (node, "ok_total", "1");
		AddAttribute (node, "extra", "1");
		AddAttribute (node, "extra_total", "1");
	}

	public void AddCountersAttributes (XmlNode node)
	{
  			if (counters.Missing > 0)
			AddAttribute (node, "missing", counters.Missing.ToString ());

  			if (counters.Present > 0)
			AddAttribute (node, "present", counters.Present.ToString ());

  			if (counters.Extra > 0)
			AddAttribute (node, "extra", counters.Extra.ToString ());

  			if (counters.Ok > 0)
			AddAttribute (node, "ok", counters.Ok.ToString ());

  			if (counters.Total > 0) {
			int percent = (100 * counters.Ok / counters.Total);
			AddAttribute (node, "complete", percent.ToString ());
		}

  			if (counters.Todo > 0)
			AddAttribute (node, "todo", counters.Todo.ToString ());

  			if (counters.Warning > 0)
			AddAttribute (node, "warning", counters.Warning.ToString ());

  			if (counters.MissingTotal > 0)
			AddAttribute (node, "missing_total", counters.MissingTotal.ToString ());

  			if (counters.PresentTotal > 0)
			AddAttribute (node, "present_total", counters.PresentTotal.ToString ());

  			if (counters.ExtraTotal > 0)
			AddAttribute (node, "extra_total", counters.ExtraTotal.ToString ());

  			if (counters.OkTotal > 0)
			AddAttribute (node, "ok_total", counters.OkTotal.ToString ());

  			if (counters.AbsTotal > 0) {
			int percent = (100 * counters.OkTotal / counters.AbsTotal);
			AddAttribute (node, "complete_total", percent.ToString ());
		}

  			if (counters.TodoTotal > 0) {
			AddAttribute (node, "todo_total", counters.TodoTotal.ToString ());
			//TODO: should be different on error. check error cases in corcompare.
			AddAttribute (node, "error_total", counters.Todo.ToString ());
		}

  			if (counters.WarningTotal > 0)
			AddAttribute (node, "warning_total", counters.WarningTotal.ToString ());

	}

	protected void AddWarning (XmlNode parent, string fmt, params object [] args)
	{
		counters.Warning++;
		haveWarnings = true;
		XmlNode warnings = parent.SelectSingleNode ("warnings");
		if (warnings == null) {
			warnings = document.CreateElement ("warnings", null);
			parent.AppendChild (warnings);
		}

		AddAttribute (parent, "error", "warning");
		XmlNode warning = document.CreateElement ("warning", null);
		AddAttribute (warning, "text", String.Format (fmt, args));
		warnings.AppendChild (warning);
	}

	public bool HaveWarnings {
		get { return haveWarnings; }
	}

	public Counters Counters {
		get { return counters; }
	}

	public abstract void CompareTo (XmlDocument doc, XmlNode parent, object other);
}
