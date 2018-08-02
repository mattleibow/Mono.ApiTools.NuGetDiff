using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;

namespace NuGetComparer
{
	public class PackageDiff
	{
		// versions

		public PackageIdentity OldIdentity { get; set; }

		public PackageIdentity NewIdentity { get; set; }

		// frameworks diff

		public NuGetFramework[] AddedFrameworks { get; set; }

		public NuGetFramework[] RemovedFrameworks { get; set; }

		public NuGetFramework[] UnchangedFrameworks { get; set; }

		// assembly diff

		public Dictionary<NuGetFramework, string[]> AddedAssemblies { get; set; }

		public Dictionary<NuGetFramework, string[]> RemovedAssemblies { get; set; }

		public Dictionary<NuGetFramework, string[]> UnchangedAssemblies { get; set; }

		public NuGetFramework[] GetAllFrameworks()
		{
			return
				AddedFrameworks
				.Union(RemovedFrameworks)
				.Union(UnchangedFrameworks)
				.OrderBy(fw => fw.GetShortFolderName())
				.ToArray();
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
