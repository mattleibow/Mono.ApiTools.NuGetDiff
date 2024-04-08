using System;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace Mono.ApiTools
{
	internal static class XElementExtensions
	{
		internal static XDocument StripAllNamespaces(this XDocument document)
		{
			foreach (var node in document.Root.DescendantsAndSelf())
				node.Name = node.Name.LocalName;

			return document;
		}

		internal static string GetPrefixedString(this XElement element, string prefix)
			=> element.ToString().PrefixLines(prefix);

		internal static string PrefixLines(this string str, string prefix)
		{
			var sb = new StringBuilder();

			using var sr = new StringReader(str);

			while (sr.ReadLine() is string line)
				sb.AppendLine($"{prefix} {line}");

			return sb.ToString().Trim();
		}
	}
}
