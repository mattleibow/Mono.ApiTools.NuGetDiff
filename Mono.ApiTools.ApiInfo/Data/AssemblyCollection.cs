//
// mono-api-info.cs - Dumps public assembly information to an xml file.
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// Copyright (C) 2003-2008 Novell, Inc (http://www.novell.com)
//

using System.Xml;

using Mono.Cecil;

namespace Mono.ApiTools;

class AssemblyCollection
{
	XmlWriter writer;
	List<AssemblyDefinition> assemblies = new List<AssemblyDefinition>();
	State state;

	public AssemblyCollection(State state)
	{
		this.state = state;
	}

	public bool Add(string name)
	{
		AssemblyDefinition ass = LoadAssembly(name);
		assemblies.Add(ass);
		return true;
	}

	public bool Add(Stream stream)
	{
		AssemblyDefinition ass = LoadAssembly(stream);
		assemblies.Add(ass);
		return true;
	}

	public void DoOutput()
	{
		if (writer == null)
			throw new InvalidOperationException("Document not set");

		writer.WriteStartElement("assemblies");
		foreach (AssemblyDefinition a in assemblies)
		{
			AssemblyData data = new AssemblyData(writer, a, state);
			data.DoOutput();
		}
		writer.WriteEndElement();
	}

	public XmlWriter Writer
	{
		set { writer = value; }
	}

	AssemblyDefinition LoadAssembly(string assembly)
	{
		if (File.Exists(assembly))
			return state.TypeHelper.Resolver.ResolveFile(assembly);

		return state.TypeHelper.Resolver.Resolve(AssemblyNameReference.Parse(assembly), new ReaderParameters());
	}

	AssemblyDefinition LoadAssembly(Stream assembly)
	{
		return state.TypeHelper.Resolver.ResolveStream(assembly);
	}
}
