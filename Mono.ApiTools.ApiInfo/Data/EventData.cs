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
using System.Globalization;

namespace Mono.ApiTools;

class EventData : MemberData
{
	public EventData(XmlWriter writer, EventDefinition[] members, State state)
		: base(writer, members, state)
	{
	}

	protected override string GetName(MemberReference memberDefenition)
	{
		EventDefinition evt = (EventDefinition)memberDefenition;
		return evt.Name;
	}

	protected override string GetMemberAttributes(MemberReference memberDefenition)
	{
		EventDefinition evt = (EventDefinition)memberDefenition;
		return ((int)evt.Attributes).ToString(CultureInfo.InvariantCulture);
	}

	protected override void AddExtraAttributes(MemberReference memberDefinition)
	{
		base.AddExtraAttributes(memberDefinition);

		EventDefinition evt = (EventDefinition)memberDefinition;
		AddAttribute("eventtype", Utils.CleanupTypeName(evt.EventType));
	}

	public override string ParentTag
	{
		get { return "events"; }
	}

	public override string Tag
	{
		get { return "event"; }
	}
}
