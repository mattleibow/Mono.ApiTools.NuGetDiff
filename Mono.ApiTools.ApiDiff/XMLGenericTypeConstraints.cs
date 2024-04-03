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

namespace Mono.ApiTools;

class XMLGenericTypeConstraints : XMLGenericGroup
{
	public override string GroupName {
		get { return "generic-type-constraints"; }
	}

	public override string Name {
		get { return "generic-type-constraint"; }
	}
}
