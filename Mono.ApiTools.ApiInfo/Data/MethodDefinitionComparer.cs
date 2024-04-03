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

class MethodDefinitionComparer : IComparer
{
	public static MethodDefinitionComparer Default = new MethodDefinitionComparer();

	public int Compare(object a, object b)
	{
		MethodDefinition ma = (MethodDefinition)a;
		MethodDefinition mb = (MethodDefinition)b;
		int res = String.Compare(ma.Name, mb.Name, StringComparison.Ordinal);
		if (res != 0)
			return res;

		if (!ma.HasParameters && !mb.HasParameters)
			return 0;

		if (!ma.HasParameters)
			return -1;

		if (!mb.HasParameters)
			return 1;

		res = Compare(ma.Parameters, mb.Parameters);
		if (res != 0)
			return res;

		if (ma.HasGenericParameters != mb.HasGenericParameters)
			return ma.HasGenericParameters ? -1 : 1;

		if (ma.HasGenericParameters && mb.HasGenericParameters)
		{
			res = ma.GenericParameters.Count - mb.GenericParameters.Count;
			if (res != 0)
				return res;
		}

		// operators can differ by only return type
		return string.CompareOrdinal(ma.ReturnType.FullName, mb.ReturnType.FullName);
	}

	public static int Compare(IList<ParameterDefinition> pia, IList<ParameterDefinition> pib)
	{
		var res = pia.Count - pib.Count;
		if (res != 0)
			return res;

		string siga = Parameters.GetSignature(pia);
		string sigb = Parameters.GetSignature(pib);
		return String.Compare(siga, sigb, StringComparison.Ordinal);
	}
}