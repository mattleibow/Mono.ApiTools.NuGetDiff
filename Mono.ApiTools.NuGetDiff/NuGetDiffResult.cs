﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;

namespace Mono.ApiTools
{
	public class NuGetDiffResult
	{
		// versions

		public PackageIdentity OldIdentity { get; set; }

		public PackageIdentity NewIdentity { get; set; }

		// frameworks diff

		public NuGetFramework[] AddedFrameworks { get; set; }

		public NuGetFramework[] RemovedFrameworks { get; set; }

		public NuGetFramework[] UnchangedFrameworks { get; set; }

		public Dictionary<NuGetFramework, NuGetFramework> SimilarFrameworks { get; set; }

		// assembly diff

		public Dictionary<NuGetFramework, string[]> AddedAssemblies { get; set; }

		public Dictionary<NuGetFramework, string[]> RemovedAssemblies { get; set; }

		public Dictionary<NuGetFramework, (string newPath, string oldPath)[]> UnchangedAssemblies { get; set; }

		public Dictionary<NuGetFramework, (string newPath, string oldPath)[]> SimilarAssemblies { get; set; }

		// file diff

		public List<string> AddedFiles { get; } = new List<string>();

		public List<string> RemovedFiles { get; } = new List<string>();

		public List<NuGetSpecDiff.ElementDiff> MetadataDiff { get; } = new List<NuGetSpecDiff.ElementDiff>();

		public NuGetFramework[] GetAllFrameworks()
		{
			return
				AddedFrameworks
				.Union(RemovedFrameworks)
				.Union(UnchangedFrameworks)
				.OrderBy(fw => fw.GetShortFolderName())
				.ToArray();
		}

		public string[] GetAllAddedAssemblies()
		{
			return AddedAssemblies.Values.SelectMany(a => a).ToArray();
		}

		public string[] GetAllRemovedAssemblies()
		{
			return RemovedAssemblies.Values.SelectMany(a => a).ToArray();
		}

		public (string newPath, string oldPath)[] GetAllUnchangedAssemblies()
		{
			return UnchangedAssemblies.Values.SelectMany(a => a).ToArray();
		}

		internal void Write(TextWriter writer)
		{
			writer.WriteLine("Added Target Frameworks:");
			foreach (var fw in AddedFrameworks)
			{
				writer.WriteLine(" - " + fw.GetFrameworkString());
			}
			writer.WriteLine();
			writer.WriteLine("Removed Target Frameworks:");
			foreach (var fw in RemovedFrameworks)
			{
				writer.WriteLine(" - " + fw.GetFrameworkString());
			}
			writer.WriteLine();
			writer.WriteLine("Unchanged Target Frameworks:");
			foreach (var fw in UnchangedFrameworks)
			{
				writer.WriteLine(" - " + fw.GetFrameworkString());
			}
			writer.WriteLine();
			writer.WriteLine("Added Assemblies:");
			foreach (var pair in AddedAssemblies)
			{
				writer.WriteLine(" - " + pair.Key.GetFrameworkString());
				foreach (var ass in pair.Value)
				{
					writer.WriteLine("    - " + ass);
				}
			}
			writer.WriteLine();
			writer.WriteLine("Removed Assemblies:");
			foreach (var pair in RemovedAssemblies)
			{
				writer.WriteLine(" - " + pair.Key.GetFrameworkString());
				foreach (var ass in pair.Value)
				{
					writer.WriteLine("    - " + ass);
				}
			}
			writer.WriteLine();
			writer.WriteLine("Unchanged Assemblies:");
			foreach (var pair in UnchangedAssemblies)
			{
				writer.WriteLine(" - " + pair.Key.GetFrameworkString());
				foreach (var ass in pair.Value)
				{
					writer.WriteLine("    - " + ass);
				}
			}
			writer.WriteLine();
		}
	}
}
