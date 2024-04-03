//
// mono-api-info.cs - Dumps public assembly information to an xml file.
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// Copyright (C) 2003-2008 Novell, Inc (http://www.novell.com)
//

using Mono.Cecil;

namespace Mono.ApiTools;

class TypeReferenceComparer : IComparer<TypeReference>
{
	public static TypeReferenceComparer Default = new TypeReferenceComparer();

	public int Compare(TypeReference a, TypeReference b)
	{
		int result = String.Compare(a.Namespace, b.Namespace, StringComparison.Ordinal);
		if (result != 0)
			return result;

		return String.Compare(a.Name, b.Name, StringComparison.Ordinal);
	}
}
