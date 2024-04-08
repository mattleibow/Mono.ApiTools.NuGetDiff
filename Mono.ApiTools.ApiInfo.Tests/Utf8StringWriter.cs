using System.Text;

namespace Mono.ApiTools.Tests;

public sealed class Utf8StringWriter : StringWriter
{
	public override Encoding Encoding => Encoding.UTF8;
}
