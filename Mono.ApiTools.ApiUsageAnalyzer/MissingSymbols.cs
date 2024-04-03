using System.IO;
using System.Xml.Linq;

namespace ApiUsageAnalyzer;

public class MissingSymbols
{
	public MissingSymbols(IReadOnlyCollection<string> types, IReadOnlyCollection<string> members)
	{
		Types = types;
		Members = members;
	}

	public IReadOnlyCollection<string> Types { get; }

	public IReadOnlyCollection<string> Members { get; }

	private XElement CreateList(string name, IEnumerable<string> items)
	{
		var xItems = new List<XElement>();
		foreach (var item in items)
		{
			xItems.Add(new XElement(name, 
				new XAttribute("fullname", item)));
		}
		return new XElement(name + "s", xItems);
	}

	public void Save(TextWriter writer)
	{
		var xdoc = new XDocument(
			new XElement("assemblies",
				new XElement("assembly",
					CreateList("type", Types),
					CreateList("member", Members))));

		xdoc.Save(writer, SaveOptions.None);
	}
}
