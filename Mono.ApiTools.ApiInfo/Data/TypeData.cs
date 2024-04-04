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


using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Mono.ApiTools;

class TypeData : MemberData
{
	TypeDefinition type;

	public TypeData(XmlWriter writer, TypeDefinition type, State state)
		: base(writer, null, state)
	{
		this.type = type;
	}
	public override void DoOutput()
	{
		if (writer == null)
			throw new InvalidOperationException("Document not set");

		writer.WriteStartElement("class");
		AddAttribute("name", type.Name);
		string classType = GetClassType(type);
		AddAttribute("type", classType);

		if (type.BaseType != null)
			AddAttribute("base", Utils.CleanupTypeName(type.BaseType));

		if (type.IsSealed)
			AddAttribute("sealed", "true");

		if (type.IsAbstract)
			AddAttribute("abstract", "true");

		if ((type.Attributes & TypeAttributes.Serializable) != 0 || type.IsEnum)
			AddAttribute("serializable", "true");

		string charSet = GetCharSet(type);
		AddAttribute("charset", charSet);

		string layout = GetLayout(type);
		if (layout != null)
			AddAttribute("layout", layout);

		if (type.PackingSize >= 0)
		{
			AddAttribute("pack", type.PackingSize.ToString());
		}

		if (type.ClassSize >= 0)
		{
			AddAttribute("size", type.ClassSize.ToString());
		}

		if (type.IsEnum)
		{
			var value_type = GetEnumValueField(type);
			if (value_type == null)
				throw new NotSupportedException();

			AddAttribute("enumtype", Utils.CleanupTypeName(value_type.FieldType));
		}

		AttributeData.OutputAttributes(writer, state, type);

		var ifaces = state.TypeHelper.GetInterfaces(type).
			Where((iface) => state.TypeHelper.IsPublic(iface)). // we're only interested in public interfaces
			OrderBy(s => s.FullName, StringComparer.Ordinal);

		if (ifaces.Any())
		{
			writer.WriteStartElement("interfaces");
			foreach (TypeReference iface in ifaces)
			{
				writer.WriteStartElement("interface");
				AddAttribute("name", Utils.CleanupTypeName(iface));
				writer.WriteEndElement(); // interface
			}
			writer.WriteEndElement(); // interfaces
		}

		MemberData.OutputGenericParameters(writer, type, state);

		ArrayList members = new ArrayList();

		FieldDefinition[] fields = GetFields(type);
		if (fields.Length > 0)
		{
			Array.Sort(fields, MemberReferenceComparer.Default);
			FieldData fd = new FieldData(writer, fields, state);
			members.Add(fd);
		}

		if (!state.AbiMode)
		{

			MethodDefinition[] ctors = GetConstructors(type);
			if (ctors.Length > 0)
			{
				Array.Sort(ctors, MethodDefinitionComparer.Default);
				members.Add(new ConstructorData(writer, ctors, state));
			}

			PropertyDefinition[] properties = GetProperties(type, state.FullApiSet);
			if (properties.Length > 0)
			{
				Array.Sort(properties, PropertyDefinitionComparer.Default);
				members.Add(new PropertyData(writer, properties, state));
			}

			EventDefinition[] events = GetEvents(type);
			if (events.Length > 0)
			{
				Array.Sort(events, MemberReferenceComparer.Default);
				members.Add(new EventData(writer, events, state));
			}

			MethodDefinition[] methods = GetMethods(type, state.FullApiSet);
			if (methods.Length > 0)
			{
				Array.Sort(methods, MethodDefinitionComparer.Default);
				members.Add(new MethodData(writer, methods, state));
			}
		}

		foreach (MemberData md in members)
			md.DoOutput();

		var nested = type.NestedTypes;
		//remove non public(familiy) and nested in second degree
		for (int i = nested.Count - 1; i >= 0; i--)
		{
			TypeDefinition t = nested[i];
			if ((t.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPublic ||
				(t.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamily ||
				(t.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamORAssem)
			{
				// public
				if (t.DeclaringType == type)
					continue; // not nested of nested
			}

			nested.RemoveAt(i);
		}

		if (nested.Count > 0)
		{
			var nestedArray = nested.ToArray();
			Array.Sort(nestedArray, TypeReferenceComparer.Default);

			writer.WriteStartElement("classes");
			foreach (TypeDefinition t in nestedArray)
			{
				TypeData td = new TypeData(writer, t, state);
				td.DoOutput();
			}
			writer.WriteEndElement(); // classes
		}

		writer.WriteEndElement(); // class
	}

	static FieldReference GetEnumValueField(TypeDefinition type)
	{
		foreach (FieldDefinition field in type.Fields)
			if (field.IsSpecialName && field.Name == "value__")
				return field;

		return null;
	}

	protected override string GetMemberAttributes(MemberReference member)
	{
		if (member != type)
			throw new InvalidOperationException("odd");

		return ((int)type.Attributes).ToString(CultureInfo.InvariantCulture);
	}

	public static bool MustDocumentMethod(MethodDefinition method)
	{
		// All other methods
		MethodAttributes maskedAccess = method.Attributes & MethodAttributes.MemberAccessMask;
		return maskedAccess == MethodAttributes.Public
			|| maskedAccess == MethodAttributes.Family
			|| maskedAccess == MethodAttributes.FamORAssem;
	}

	string GetClassType(TypeDefinition t)
	{
		if (t.IsEnum)
			return "enum";

		if (t.IsValueType)
			return "struct";

		if (t.IsInterface)
			return "interface";

		if (state.TypeHelper.IsDelegate(t))
			return "delegate";

		if (t.IsPointer)
			return "pointer";

		return "class";
	}

	static string GetCharSet(TypeDefinition type)
	{
		TypeAttributes maskedStringFormat = type.Attributes & TypeAttributes.StringFormatMask;
		if (maskedStringFormat == TypeAttributes.AnsiClass)
			return CharSet.Ansi.ToString();

		if (maskedStringFormat == TypeAttributes.AutoClass)
			return CharSet.Auto.ToString();

		if (maskedStringFormat == TypeAttributes.UnicodeClass)
			return CharSet.Unicode.ToString();

		return CharSet.None.ToString();
	}

	static string GetLayout(TypeDefinition type)
	{
		TypeAttributes maskedLayout = type.Attributes & TypeAttributes.LayoutMask;
		if (maskedLayout == TypeAttributes.AutoLayout)
			return LayoutKind.Auto.ToString();

		if (maskedLayout == TypeAttributes.ExplicitLayout)
			return LayoutKind.Explicit.ToString();

		if (maskedLayout == TypeAttributes.SequentialLayout)
			return LayoutKind.Sequential.ToString();

		return null;
	}

	FieldDefinition[] GetFields(TypeDefinition type)
	{
		ArrayList list = new ArrayList();

		var fields = type.Fields;
		foreach (FieldDefinition field in fields)
		{
			if (field.IsSpecialName)
				continue;

			if (state.AbiMode && field.IsStatic)
				continue;

			// we're only interested in public or protected members
			FieldAttributes maskedVisibility = (field.Attributes & FieldAttributes.FieldAccessMask);
			if (state.AbiMode && !field.IsNotSerialized)
			{
				list.Add(field);
			}
			else
			{
				if (maskedVisibility == FieldAttributes.Public
					|| maskedVisibility == FieldAttributes.Family
					|| maskedVisibility == FieldAttributes.FamORAssem)
				{
					list.Add(field);
				}
			}
		}

		return (FieldDefinition[])list.ToArray(typeof(FieldDefinition));
	}


	internal PropertyDefinition[] GetProperties(TypeDefinition type, bool fullAPI)
	{
		var list = new List<PropertyDefinition>();

		var t = type;
		do
		{
			var properties = t.Properties;//type.GetProperties (flags);
			foreach (PropertyDefinition property in properties)
			{
				MethodDefinition getMethod = property.GetMethod;
				MethodDefinition setMethod = property.SetMethod;

				bool hasGetter = (getMethod != null) && MustDocumentMethod(getMethod);
				bool hasSetter = (setMethod != null) && MustDocumentMethod(setMethod);

				// if neither the getter or setter should be documented, then
				// skip the property
				if (hasGetter || hasSetter)
				{

					if (t != type && list.Any(l => l.Name == property.Name))
						continue;

					list.Add(property);
				}
			}

			if (!fullAPI)
				break;

			if (t.IsInterface || t.IsEnum)
				break;

			if (t.BaseType == null || t.BaseType.FullName == "System.Object")
				t = null;
			else
				t = state.TypeHelper.GetBaseType(t);

		} while (t != null);

		return list.ToArray();
	}

	private MethodDefinition[] GetMethods(TypeDefinition type, bool fullAPI)
	{
		var list = new List<MethodDefinition>();

		var t = type;
		do
		{
			var methods = t.Methods;//type.GetMethods (flags);
			foreach (MethodDefinition method in methods)
			{
				if (method.IsSpecialName && !method.Name.StartsWith("op_", StringComparison.Ordinal))
					continue;

				// we're only interested in public or protected members
				if (!MustDocumentMethod(method))
					continue;

				if (t == type && IsFinalizer(method))
				{
					string name = method.DeclaringType.Name;
					int arity = name.IndexOf('`');
					if (arity > 0)
						name = name.Substring(0, arity);

					method.Name = "~" + name;
				}

				if (t != type && list.Any(l => l.DeclaringType != method.DeclaringType && l.Name == method.Name && l.Parameters.Count == method.Parameters.Count &&
										   l.Parameters.SequenceEqual(method.Parameters, new ParameterComparer())))
					continue;

				list.Add(method);
			}

			if (!fullAPI)
				break;

			if (t.IsInterface || t.IsEnum)
				break;

			if (t.BaseType == null || t.BaseType.FullName == "System.Object")
				t = null;
			else
				t = state.TypeHelper.GetBaseType(t);

		} while (t != null);

		return list.ToArray();
	}

	sealed class ParameterComparer : IEqualityComparer<ParameterDefinition>
	{
		public bool Equals(ParameterDefinition x, ParameterDefinition y)
		{
			return x.ParameterType.Name == y.ParameterType.Name;
		}

		public int GetHashCode(ParameterDefinition obj)
		{
			return obj.ParameterType.Name.GetHashCode();
		}
	}

	static bool IsFinalizer(MethodDefinition method)
	{
		if (method.Name != "Finalize")
			return false;

		if (!method.IsVirtual)
			return false;

		if (method.Parameters.Count != 0)
			return false;

		return true;
	}

	private MethodDefinition[] GetConstructors(TypeDefinition type)
	{
		ArrayList list = new ArrayList();

		var ctors = type.Methods.Where(m => m.IsConstructor);//type.GetConstructors (flags);
		foreach (MethodDefinition constructor in ctors)
		{
			// we're only interested in public or protected members
			if (!MustDocumentMethod(constructor))
				continue;

			list.Add(constructor);
		}

		return (MethodDefinition[])list.ToArray(typeof(MethodDefinition));
	}

	private EventDefinition[] GetEvents(TypeDefinition type)
	{
		ArrayList list = new ArrayList();

		var events = type.Events;//type.GetEvents (flags);
		foreach (EventDefinition eventDef in events)
		{
			MethodDefinition addMethod = eventDef.AddMethod;//eventInfo.GetAddMethod (true);

			if (addMethod == null || !MustDocumentMethod(addMethod))
				continue;

			list.Add(eventDef);
		}

		return (EventDefinition[])list.ToArray(typeof(EventDefinition));
	}
}
