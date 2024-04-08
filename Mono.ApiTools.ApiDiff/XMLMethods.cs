//
// mono-api-diff.cs - Compares 2 xml files produced by mono-api-info and
//		      produces a file suitable to build class status pages.
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Marek Safar		(marek.safar@gmail.com)
//
// Maintainer:
//	C.J. Adams-Collier	(cjac@colliertech.org)
//
// (C) 2003 Novell, Inc (http://www.novell.com)
// (C) 2009,2010 Collier Technologies (http://www.colliertech.org)

using System.Collections;
using System.Reflection;
using System.Xml;

namespace Mono.ApiTools;

class XMLMethods : XMLMember
{
	Hashtable returnTypes;
	Hashtable parameters;
	Hashtable genericConstraints;
	Hashtable signatureFlags;

	[Flags]
	enum SignatureFlags
	{
		None = 0,
		Abstract = 1,
		Virtual = 2,
		Static = 4,
		Final = 8,
	}

	protected override void LoadExtraData (string name, XmlNode node)
	{
		XmlAttribute xatt = node.Attributes ["returntype"];
		if (xatt != null) {
			if (returnTypes == null)
				returnTypes = new Hashtable ();

			returnTypes [name] = xatt.Value;
		}

		SignatureFlags flags = SignatureFlags.None;
		if (((XmlElement) node).GetAttribute ("abstract") == "true")
			flags |= SignatureFlags.Abstract;
		if (((XmlElement) node).GetAttribute ("static") == "true")
			flags |= SignatureFlags.Static;
		if (((XmlElement) node).GetAttribute ("virtual") == "true")
			flags |= SignatureFlags.Virtual;
		if (((XmlElement) node).GetAttribute ("final") == "true")
			flags |= SignatureFlags.Final;
		if (flags != SignatureFlags.None) {
			if (signatureFlags == null)
				signatureFlags = new Hashtable ();
			signatureFlags [name] = flags;
		}

		XmlNode parametersNode = node.SelectSingleNode ("parameters");
		if (parametersNode != null) {
			if (parameters == null)
				parameters = new Hashtable ();

			XMLParameters parms = new XMLParameters ();
			parms.LoadData (parametersNode);

			parameters[name] = parms;
		}

		XmlNode genericNode = node.SelectSingleNode ("generic-method-constraints");
		if (genericNode != null) {
			if (genericConstraints == null)
				genericConstraints = new Hashtable ();
			XMLGenericMethodConstraints csts = new XMLGenericMethodConstraints ();
			csts.LoadData (genericNode);
			genericConstraints [name] = csts;
		}

		base.LoadExtraData (name, node);
	}

	public override string GetNodeKey (string name, XmlNode node)
	{
		// for explicit/implicit operators we need to include the return
		// type in the key to allow matching; as a side-effect, differences
		// in return types will be reported as extra/missing methods
		//
		// for regular methods we do not need to take into account the
		// return type for matching methods; differences in return types
		// will be reported as a warning on the method
		if (name.StartsWith ("op_")) {
			XmlAttribute xatt = node.Attributes ["returntype"];
			string returnType = xatt != null ? xatt.Value + " " : string.Empty;
			return returnType + name;
		}
		return name;
	}

	protected override void CompareToInner (string name, XmlNode parent, XMLNameGroup other)
	{
		// create backup of actual counters
		Counters copy = counters;
		// initialize counters for current method
		counters = new Counters();

		try {
			base.CompareToInner(name, parent, other);
			XMLMethods methods = (XMLMethods) other;

			SignatureFlags flags = signatureFlags != null &&
				signatureFlags.ContainsKey (name) ?
				(SignatureFlags) signatureFlags [name] :
				SignatureFlags.None;
			SignatureFlags oflags = methods.signatureFlags != null &&
				methods.signatureFlags.ContainsKey (name) ?
				(SignatureFlags) methods.signatureFlags [name] :
				SignatureFlags.None;

			if (flags!= oflags) {
				if (flags == SignatureFlags.None)
					AddWarning (parent, String.Format ("should not be {0}", oflags));
				else if (oflags == SignatureFlags.None)
					AddWarning (parent, String.Format ("should be {0}", flags));
				else
					AddWarning (parent, String.Format ("{0} and should be {1}", oflags, flags));
			}

			if (returnTypes != null) {
				string rtype = returnTypes[name] as string;
				string ortype = null;
				if (methods.returnTypes != null)
					ortype = methods.returnTypes[name] as string;

				if (rtype != ortype)
					AddWarning (parent, "Return type is {0} and should be {1}", ortype, rtype);
			}

			if (parameters != null) {
				XMLParameters parms = parameters[name] as XMLParameters;
				parms.CompareTo (document, parent, methods.parameters[name]);
				counters.AddPartialToPartial (parms.Counters);
			}
		} finally {
			// output counter attributes in result document
			AddCountersAttributes(parent);

			// add temporary counters to actual counters
			copy.AddPartialToPartial(counters);
			// restore backup of actual counters
			counters = copy;
		}
	}

	protected override string ConvertToString (int att)
	{
		MethodAttributes ma = (MethodAttributes) att;
		// ignore ReservedMasks
		ma &= ~ MethodAttributes.ReservedMask;
		ma &= ~ MethodAttributes.VtableLayoutMask;
		if ((ma & MethodAttributes.FamORAssem) != 0)
			ma = (ma & ~ MethodAttributes.FamORAssem) | MethodAttributes.Family;

		// ignore the HasSecurity attribute for now
		if ((ma & MethodAttributes.HasSecurity) != 0)
			ma = (MethodAttributes) (att - (int) MethodAttributes.HasSecurity);

		// ignore the RequireSecObject attribute for now
		if ((ma & MethodAttributes.RequireSecObject) != 0)
			ma = (MethodAttributes) (att - (int) MethodAttributes.RequireSecObject);

		// we don't care if the implementation is forwarded through PInvoke
		if ((ma & MethodAttributes.PinvokeImpl) != 0)
			ma = (MethodAttributes) (att - (int) MethodAttributes.PinvokeImpl);

		return ma.ToString ();
	}

	public override string GroupName {
		get { return "methods"; }
	}

	public override string Name {
		get { return "method"; }
	}
}
