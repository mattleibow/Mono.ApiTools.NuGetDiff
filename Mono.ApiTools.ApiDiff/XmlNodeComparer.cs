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

class XmlNodeComparer : IComparer
{
	public static XmlNodeComparer Default = new XmlNodeComparer ();

	public int Compare (object a, object b)
	{
		XmlNode na = (XmlNode) a;
		XmlNode nb = (XmlNode) b;
		return String.Compare (na.Attributes ["name"].Value, nb.Attributes ["name"].Value);
	}
}