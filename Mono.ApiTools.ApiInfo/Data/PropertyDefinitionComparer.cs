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

class PropertyDefinitionComparer : IComparer<PropertyDefinition>
{
	public static PropertyDefinitionComparer Default = new PropertyDefinitionComparer();

	public int Compare(PropertyDefinition ma, PropertyDefinition mb)
	{
		int res = String.Compare(ma.Name, mb.Name, StringComparison.Ordinal);
		if (res != 0)
			return res;

		if (!ma.HasParameters && !mb.HasParameters)
			return 0;

		if (!ma.HasParameters)
			return -1;

		if (!mb.HasParameters)
			return 1;

		return MethodDefinitionComparer.Compare(ma.Parameters, mb.Parameters);
	}
}
