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

class MethodData : MemberData
{
	bool noAtts;

	public MethodData(XmlWriter writer, MethodDefinition[] members, State state)
		: base(writer, members, state)
	{
	}

	protected override string GetName(MemberReference memberDefenition)
	{
		MethodDefinition method = (MethodDefinition)memberDefenition;
		string name = method.Name;
		string parms = Parameters.GetSignature(method.Parameters);

		return string.Format("{0}({1})", name, parms);
	}

	protected override string GetMemberAttributes(MemberReference memberDefenition)
	{
		MethodDefinition method = (MethodDefinition)memberDefenition;
		return ((int)(method.Attributes)).ToString(CultureInfo.InvariantCulture);
	}

	protected override ICustomAttributeProvider GetAdditionalCustomAttributeProvider(MemberReference member)
	{
		var mbase = (MethodDefinition)member;
		return mbase.MethodReturnType;
	}

	protected override void AddExtraAttributes(MemberReference memberDefinition)
	{
		base.AddExtraAttributes(memberDefinition);

		if (!(memberDefinition is MethodDefinition))
			return;

		MethodDefinition mbase = (MethodDefinition)memberDefinition;

		if (mbase.IsAbstract)
			AddAttribute("abstract", "true");
		if (mbase.IsVirtual)
			AddAttribute("virtual", "true");
		if (mbase.IsFinal && mbase.IsVirtual && mbase.IsReuseSlot)
			AddAttribute("sealed", "true");
		if (mbase.IsStatic)
			AddAttribute("static", "true");
		var baseMethod = state.TypeHelper.GetBaseMethodInTypeHierarchy(mbase);
		if (baseMethod != null && baseMethod != mbase)
		{
			// This indicates whether this method is an override of another method.
			// This information is not necessarily available in the api info for any
			// particular assembly, because a method is only overriding another if
			// there is a base virtual function with the same signature, and that
			// base method can come from another assembly.
			AddAttribute("is-override", "true");
		}
		string rettype = Utils.CleanupTypeName(mbase.MethodReturnType.ReturnType);
		if (rettype != "System.Void" || !mbase.IsConstructor)
			AddAttribute("returntype", (rettype));
		//
		//			if (mbase.MethodReturnType.HasCustomAttributes)
		//				AttributeData.OutputAttributes (writer, mbase.MethodReturnType);
	}

	protected override void AddExtraData(MemberReference memberDefenition)
	{
		base.AddExtraData(memberDefenition);

		if (!(memberDefenition is MethodDefinition))
			return;

		MethodDefinition mbase = (MethodDefinition)memberDefenition;

		ParameterData parms = new ParameterData(writer, mbase.Parameters, state)
		{
			HasExtensionParameter = mbase.CustomAttributes.Any(l => l.AttributeType.FullName == "System.Runtime.CompilerServices.ExtensionAttribute")
		};

		parms.DoOutput();

		MemberData.OutputGenericParameters(writer, mbase, state);
	}

	public override bool NoMemberAttributes
	{
		get { return noAtts; }
		set { noAtts = value; }
	}

	public override string ParentTag
	{
		get { return "methods"; }
	}

	public override string Tag
	{
		get { return "method"; }
	}
}
