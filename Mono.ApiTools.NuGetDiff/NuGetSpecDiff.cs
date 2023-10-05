using NuGet.Packaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Mono.ApiTools
{
	public class NuGetSpecDiff
	{
		static readonly Regex tag = new Regex("artifact_versioned=(?<GroupId>.+)?:(?<ArtifactId>.+?):(?<Version>.+)\\s?", RegexOptions.Compiled);

		public static IEnumerable<ElementDiff> Generate(PackageArchiveReader oldReader, PackageArchiveReader newReader, bool skipVersionMetadata)
		{
			// make a copy so we aren't modifying the *real* nuspec
			var oldMetadata = new XDocument(oldReader?.NuspecReader?.Xml ?? XDocument.Parse("<package><metadata /></package>")).StripAllNamespaces();
			var newMetadata = new XDocument(newReader?.NuspecReader?.Xml ?? XDocument.Parse("<package><metadata /></package>")).StripAllNamespaces();

			if (oldMetadata.Element("package")?.Element("metadata") is not XElement oldXml || newMetadata.Element("package")?.Element("metadata") is not XElement newXml)
				throw new ArgumentException("Malformed Nuspec xml");

			if (skipVersionMetadata)
			{
				StripVersionMetadata(oldMetadata);
				StripVersionMetadata(newMetadata);
			}

			var allElements = oldXml.Elements().Concat(newXml.Elements()).Select(x => x.Name.LocalName).Distinct();
			var allDiffs = new List<ElementDiff>();

			foreach (var element in allElements)
			{
				var diff = new ElementDiff(oldXml.Element(element), newXml.Element(element));

				if (diff.Type != DiffType.None)
					allDiffs.Add(diff);
			}


			return allDiffs;
		}

		static void StripVersionMetadata(XDocument xml)
		{
			if (xml.Element("package")?.Element("metadata") is not XElement metadata)
				return;

			// strip metadata that changes on every release, like version and git branch/commit
			metadata.Element("version")?.Remove();

			if (metadata.Element("repository") is XElement repo)
			{
				repo.Attribute("branch")?.Remove();
				repo.Attribute("commit")?.Remove();
			}

			// strip a custom tag that AndroidX/GPS uses that contains a version number
			// ex: artifact_versioned=com.google.code.gson:gson:2.10.1
			if (metadata.Element("tags") is XElement tags)
			{
				var match = tag.Match(tags.Value);

				if (match.Success)
					tags.Value = tags.Value.Replace(match.Groups["Version"].Value, "[Version]");
			}
		}

		public class ElementDiff
		{
			public XElement OldElement { get; }
			public XElement NewElement { get; }

			public ElementDiff(XElement oldElement, XElement newElement)
			{
				OldElement = oldElement;
				NewElement = newElement;
			}

			public string Name => OldElement?.Name?.LocalName ?? NewElement?.Name?.LocalName;

			public DiffType Type
			{
				get
				{
					if (OldElement is null)
						return DiffType.Added;
					if (NewElement is null)
						return DiffType.Removed;
					if (OldElement.Value == NewElement.Value)
						return DiffType.None;

					return DiffType.Modified;
				}
			}

			public string ToFormattedString()
			{
				switch (Type)
				{
					case DiffType.Added:
						return NewElement.GetPrefixedString("+");
					case DiffType.Removed:
						return OldElement.GetPrefixedString("-");
					case DiffType.Modified:
						return $"{OldElement.GetPrefixedString("-")}{Environment.NewLine}{NewElement.GetPrefixedString("+")}";
				}

				return string.Empty;
			}
		}

		public enum DiffType
		{
			None,
			Added,
			Removed,
			Modified
		}
	}
}
