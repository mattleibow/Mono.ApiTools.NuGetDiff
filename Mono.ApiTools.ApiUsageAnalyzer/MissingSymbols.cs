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
}
