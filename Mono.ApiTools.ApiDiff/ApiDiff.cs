//
// mono-api-diff.cs - Compares 2 xml files produced by mono-api-info and
//		      produces a file suitable to build class status pages.
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Marek Safar		(marek.safar@gmail.com)
//
// Maintainer:
//	C.J. Adams-Collier	(cjac@colliertech.org)
//
// (C) 2003 Novell, Inc (http://www.novell.com)
// (C) 2009,2010 Collier Technologies (http://www.colliertech.org)

using System.Xml;

namespace Mono.ApiTools;

public static class ApiDiff
{
	public static void Generate (string firstInfo, string secondInfo, TextWriter outStream)
	{
		if (firstInfo == null)
			throw new ArgumentNullException (nameof (firstInfo));
		if (secondInfo == null)
			throw new ArgumentNullException (nameof (secondInfo));

		XMLAssembly ms = CreateXMLAssembly (firstInfo);
		XMLAssembly mono = CreateXMLAssembly (secondInfo);

		Generate (ms, mono, outStream);
	}

	public static void Generate (Stream firstInfo, Stream secondInfo, TextWriter outStream)
	{
		if (firstInfo == null)
			throw new ArgumentNullException (nameof (firstInfo));
		if (secondInfo == null)
			throw new ArgumentNullException (nameof (secondInfo));

		XMLAssembly ms = CreateXMLAssembly (firstInfo);
		XMLAssembly mono = CreateXMLAssembly (secondInfo);

		Generate (ms, mono, outStream);
	}

	static void Generate (XMLAssembly first, XMLAssembly second, TextWriter outStream)
	{
		if (first == null)
			throw new ArgumentNullException (nameof (first));
		if (second == null)
			throw new ArgumentNullException (nameof (second));
		if (outStream == null)
			throw new ArgumentNullException (nameof (outStream));

		XmlDocument doc = first.CompareAndGetDocument (second);

		using (XmlTextWriter writer = new XmlTextWriter (outStream)) {
			writer.Formatting = Formatting.Indented;
			doc.WriteTo (writer);
		}
	}

	static XMLAssembly CreateXMLAssembly (string file)
	{
		using (var stream = File.OpenRead(file)) {
			return CreateXMLAssembly (stream);
		}
	}

	static XMLAssembly CreateXMLAssembly (Stream stream)
	{
		XmlDocument doc = new XmlDocument ();
		doc.Load (stream);

		XmlNode node = doc.SelectSingleNode ("/assemblies/assembly");
		XMLAssembly result = new XMLAssembly ();
		result.LoadData (node);

		return result;
	}
}
