//
// mono-api-info.cs - Dumps public assembly information to an xml file.
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// Copyright (C) 2003-2008 Novell, Inc (http://www.novell.com)
//

using System.Text;

using Mono.Cecil;

namespace Mono.ApiTools;

class Utils
{
	static char[] CharsToCleanup = new char[] { '<', '>', '/' };

	public static string CleanupTypeName(TypeReference type)
	{
		return CleanupTypeName(type.FullName);
	}

	public static string CleanupTypeName(string t)
	{
		if (t.IndexOfAny(CharsToCleanup) == -1)
			return t;

		var sb = new StringBuilder(t.Length);
		for (int i = 0; i < t.Length; i++)
		{
			var ch = t[i];
			switch (ch)
			{
				case '<':
					sb.Append('[');
					break;
				case '>':
					sb.Append(']');
					break;
				case '/':
					sb.Append('+');
					break;
				default:
					sb.Append(ch);
					break;
			}
		}
		return sb.ToString();
	}
}
