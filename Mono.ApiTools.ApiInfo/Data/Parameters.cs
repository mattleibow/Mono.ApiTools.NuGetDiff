//
// mono-api-info.cs - Dumps public assembly information to an xml file.
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// Copyright (C) 2003-2008 Novell, Inc (http://www.novell.com)
//

using Mono.Cecil;
using System.Text;

namespace Mono.ApiTools;

static class Parameters
{
	public static string GetSignature(IList<ParameterDefinition> infos)
	{
		if (infos == null || infos.Count == 0)
			return string.Empty;

		var signature = new StringBuilder();
		for (int i = 0; i < infos.Count; i++)
		{

			if (i > 0)
				signature.Append(", ");

			ParameterDefinition info = infos[i];

			string modifier = string.Empty;
			if (info.ParameterType.IsByReference)
			{
				if ((info.Attributes & ParameterAttributes.In) != 0)
					modifier = "in";
				else if ((info.Attributes & ParameterAttributes.Out) != 0)
					modifier = "out";
			}

			if (modifier.Length > 0)
			{
				signature.Append(modifier);
				signature.Append(" ");
			}

			signature.Append(Utils.CleanupTypeName(info.ParameterType));
		}

		return signature.ToString();
	}
}
