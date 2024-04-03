//
// mono-api-info.cs - Dumps public assembly information to an xml file.
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// Copyright (C) 2003-2008 Novell, Inc (http://www.novell.com)
//

using System.Collections;

using Mono.Cecil;

namespace Mono.ApiTools;

class MemberReferenceComparer : IComparer
{
	public static MemberReferenceComparer Default = new MemberReferenceComparer();

	public int Compare(object a, object b)
	{
		MemberReference ma = (MemberReference)a;
		MemberReference mb = (MemberReference)b;
		return String.Compare(ma.Name, mb.Name, StringComparison.Ordinal);
	}
}
