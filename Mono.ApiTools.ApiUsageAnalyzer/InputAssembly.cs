namespace ApiUsageAnalyzer;

public class InputAssembly
{
	public InputAssembly(string fileName)
	{
		FileName = fileName;
	}

	public string? FileName { get; }

	public IList<string>? SearchPaths { get; set; } = [];
}
