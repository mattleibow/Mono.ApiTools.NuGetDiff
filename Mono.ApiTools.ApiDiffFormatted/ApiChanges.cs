using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Mono.ApiTools;

class ApiChanges : Dictionary<string, List<ApiChange>>
{
	public State State;

	public ApiChanges(State state)
	{
		State = state;
	}

	public void Add(XElement source, XElement target, ApiChange change)
	{
		if (!change.AnyChange)
		{
			// This is most likely because the rendering doesn't take into account something that's different (solution: fix rendering).
			if (!change.HasIgnoredChanges)
			{
				var isField = source.Name.LocalName == "field";
				if (isField)
				{
					State.LogDebugMessage($"Comparison resulting in no changes (src: {source.GetFieldAttributes()} dst: {target.GetFieldAttributes()}) :{Environment.NewLine}{source}{Environment.NewLine}{target}{Environment.NewLine}{Environment.NewLine}");
				}
				else
				{
					State.LogDebugMessage($"Comparison resulting in no changes (src: {source.GetMethodAttributes()} dst: {target.GetMethodAttributes()}) :{Environment.NewLine}{source}{Environment.NewLine}{target}{Environment.NewLine}{Environment.NewLine}");
				}
			}
			return;
		}

		var changeDescription = $"{State.Namespace}.{State.Type}: {change.Header}: {change.SourceDescription}";
		State.LogDebugMessage($"Possible -r value: {changeDescription}");
		if (State.IgnoreRemoved.Any(re => re.IsMatch(changeDescription)))
			return;

		List<ApiChange> list;
		if (!TryGetValue(change.Header, out list))
		{
			list = new List<ApiChange>();
			base.Add(change.Header, list);
		}
		list.Add(change);
	}
}