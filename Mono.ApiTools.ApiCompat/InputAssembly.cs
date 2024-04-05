namespace Mono.ApiTools;

public abstract class InputAssembly
{
	public abstract Stream Open();

	public IList<string>? SearchPaths { get; set; } = [];
}

public class StreamInputAssembly : InputAssembly
{
	private readonly Func<Stream> streamFunc;

	public StreamInputAssembly(Stream stream)
	{
		this.streamFunc = () => stream;
	}

	public StreamInputAssembly(Func<Stream> streamFunc)
	{
		this.streamFunc = streamFunc;
	}

	public override Stream Open() => streamFunc();
}

public class FileInputAssembly : InputAssembly
{
	private readonly string fileName;

	public FileInputAssembly(string fileName)
	{
		this.fileName = fileName;
	}

	public override Stream Open() =>
		File.OpenRead(fileName);

	public string FileName => fileName;
}
