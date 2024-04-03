using System.Text;

namespace Mono.ApiTools;

class ApiChange
{
	public string Header;
	public StringBuilder Member = new StringBuilder();
	public bool Breaking;
	public bool AnyChange;
	public bool HasIgnoredChanges;
	public string SourceDescription;
	public State State;

	public ApiChange(string sourceDescription, State state)
	{
		SourceDescription = sourceDescription;
		State = state;
	}

	public ApiChange Append(string text)
	{
		Member.Append(text);
		return this;
	}

	public ApiChange AppendAdded(string text, bool breaking = false)
	{
		State.Formatter.DiffAddition(Member, text, breaking);
		Breaking |= breaking;
		AnyChange = true;
		return this;
	}

	public ApiChange AppendRemoved(string text, bool breaking = true)
	{
		State.Formatter.DiffRemoval(Member, text, breaking);
		Breaking |= breaking;
		AnyChange = true;
		return this;
	}

	public ApiChange AppendModified(string old, string @new, bool breaking = true)
	{
		State.Formatter.DiffModification(Member, old, @new, breaking);
		Breaking |= breaking;
		AnyChange = true;
		return this;
	}
}
