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

abstract class XMLGenericGroup : XMLNameGroup
{
	string attributes;

	protected override void LoadExtraData (string name, XmlNode node)
	{
		attributes = ((XmlElement) node).GetAttribute ("generic-attribute");
	}

	protected override void CompareToInner (string name, XmlNode parent, XMLNameGroup other)
	{
		base.CompareToInner (name, parent, other);

		XMLGenericGroup g = (XMLGenericGroup) other;
		if (attributes != g.attributes)
			AddWarning (parent, "Incorrect generic attributes: '{0}' != '{1}'", attributes, g.attributes);
	}
}
