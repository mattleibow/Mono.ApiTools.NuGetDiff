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
using System.Runtime.CompilerServices;
using System.Text;
using Mono.Cecil.Cil;

namespace Mono.ApiTools;

class AttributeData
{
	State state;

	public AttributeData(State state)
	{
		this.state = state;
	}

	public void DoOutput(XmlWriter writer, IList<ICustomAttributeProvider> providers)
	{
		if (writer == null)
			throw new InvalidOperationException("Document not set");

		if (providers == null || providers.Count == 0)
			return;

		if (!providers.Any((provider) => provider != null && provider.HasCustomAttributes))
			return;

		writer.WriteStartElement("attributes");

		foreach (var provider in providers)
		{
			if (provider == null)
				continue;

			if (!provider.HasCustomAttributes)
				continue;


			var ass = provider as AssemblyDefinition;
			if (ass != null && !state.FollowForwarders)
				TypeForwardedToData.OutputForwarders(writer, ass, state);

			var attributes = provider.CustomAttributes.
				Where((att) => !SkipAttribute(att)).
				OrderBy((a) => a.Constructor.DeclaringType.FullName, StringComparer.Ordinal);

			foreach (var att in attributes)
			{
				string attName = Utils.CleanupTypeName(att.Constructor.DeclaringType);

				writer.WriteStartElement("attribute");
				writer.WriteAttributeString("name", attName);

				var attribute_mapping = CreateAttributeMapping(att);

				if (attribute_mapping != null)
				{
					var mapping = attribute_mapping.Where((attr) => attr.Key != "TypeId");
					if (mapping.Any())
					{
						writer.WriteStartElement("properties");
						foreach (var kvp in mapping)
						{
							string name = kvp.Key;
							object o = kvp.Value;

							writer.WriteStartElement("property");
							writer.WriteAttributeString("name", name);

							if (o == null)
							{
								writer.WriteAttributeString("value", "null");
							}
							else
							{
								string value = o.ToString();
								if (attName.EndsWith("GuidAttribute", StringComparison.Ordinal))
									value = value.ToUpper();
								writer.WriteAttributeString("value", value);
							}

							writer.WriteEndElement(); // property
						}
						writer.WriteEndElement(); // properties
					}
				}
				writer.WriteEndElement(); // attribute
			}
		}

		writer.WriteEndElement(); // attributes
	}

	Dictionary<string, object> CreateAttributeMapping(CustomAttribute attribute)
	{
		Dictionary<string, object> mapping = null;

		if (!state.TypeHelper.TryResolve(attribute))
			return mapping;

		PopulateMapping(ref mapping, attribute);

		var constructor = state.TypeHelper.GetMethod(attribute.Constructor);
		if (constructor == null || !constructor.HasParameters)
			return mapping;

		PopulateMapping(ref mapping, constructor, attribute);

		return mapping;
	}

	static void PopulateMapping(ref Dictionary<string, object> mapping, CustomAttribute attribute)
	{
		if (!attribute.HasProperties)
			return;

		foreach (var named_argument in attribute.Properties)
		{
			var name = named_argument.Name;
			var arg = named_argument.Argument;

			if (arg.Value is CustomAttributeArgument)
				arg = (CustomAttributeArgument)arg.Value;

			if (mapping == null)
				mapping = new Dictionary<string, object>(StringComparer.Ordinal);
			mapping.Add(name, GetArgumentValue(arg.Type, arg.Value));
		}
	}

	static Dictionary<FieldReference, int> CreateArgumentFieldMapping(MethodDefinition constructor)
	{
		Dictionary<FieldReference, int> field_mapping = null;

		int? argument = null;

		foreach (Instruction instruction in constructor.Body.Instructions)
		{
			switch (instruction.OpCode.Code)
			{
				case Code.Ldarg_1:
					argument = 1;
					break;
				case Code.Ldarg_2:
					argument = 2;
					break;
				case Code.Ldarg_3:
					argument = 3;
					break;
				case Code.Ldarg:
				case Code.Ldarg_S:
					argument = ((ParameterDefinition)instruction.Operand).Index + 1;
					break;

				case Code.Stfld:
					FieldReference field = (FieldReference)instruction.Operand;
					if (field.DeclaringType.FullName != constructor.DeclaringType.FullName)
						continue;

					if (!argument.HasValue)
						break;

					if (field_mapping == null)
						field_mapping = new Dictionary<FieldReference, int>();

					if (!field_mapping.ContainsKey(field))
						field_mapping.Add(field, (int)argument - 1);

					argument = null;
					break;
			}
		}

		return field_mapping;
	}

	static Dictionary<PropertyDefinition, FieldReference> CreatePropertyFieldMapping(TypeDefinition type)
	{
		Dictionary<PropertyDefinition, FieldReference> property_mapping = null;

		foreach (PropertyDefinition property in type.Properties)
		{
			if (property.GetMethod == null)
				continue;
			if (!property.GetMethod.HasBody)
				continue;

			foreach (Instruction instruction in property.GetMethod.Body.Instructions)
			{
				if (instruction.OpCode.Code != Code.Ldfld)
					continue;

				FieldReference field = (FieldReference)instruction.Operand;
				if (field.DeclaringType.FullName != type.FullName)
					continue;

				if (property_mapping == null)
					property_mapping = new Dictionary<PropertyDefinition, FieldReference>();
				property_mapping.Add(property, field);
				break;
			}
		}

		return property_mapping;
	}

	static void PopulateMapping(ref Dictionary<string, object> mapping, MethodDefinition constructor, CustomAttribute attribute)
	{
		if (!constructor.HasBody)
			return;

		// Custom handling for attributes with arguments which cannot be easily extracted
		var ca = attribute.ConstructorArguments;
		switch (constructor.DeclaringType.FullName)
		{
			case "System.Runtime.CompilerServices.DecimalConstantAttribute":
				var dca = constructor.Parameters[2].ParameterType == constructor.Module.TypeSystem.Int32 ?
					new DecimalConstantAttribute((byte)ca[0].Value, (byte)ca[1].Value, (int)ca[2].Value, (int)ca[3].Value, (int)ca[4].Value) :
					new DecimalConstantAttribute((byte)ca[0].Value, (byte)ca[1].Value, (uint)ca[2].Value, (uint)ca[3].Value, (uint)ca[4].Value);

				if (mapping == null)
					mapping = new Dictionary<string, object>(StringComparer.Ordinal);
				mapping.Add("Value", dca.Value);
				return;
			case "System.ComponentModel.BindableAttribute":
				if (ca.Count != 1)
					break;

				if (mapping == null)
					mapping = new Dictionary<string, object>(StringComparer.Ordinal);

				if (constructor.Parameters[0].ParameterType == constructor.Module.TypeSystem.Boolean)
				{
					mapping.Add("Bindable", ca[0].Value);
				}
				else if (constructor.Parameters[0].ParameterType.FullName == "System.ComponentModel.BindableSupport")
				{
					if ((int)ca[0].Value == 0)
						mapping.Add("Bindable", false);
					else if ((int)ca[0].Value == 1)
						mapping.Add("Bindable", true);
					else
						throw new NotImplementedException();
				}
				else
				{
					throw new NotImplementedException();
				}

				return;
		}

		var field_mapping = CreateArgumentFieldMapping(constructor);
		if (field_mapping != null)
		{
			var property_mapping = CreatePropertyFieldMapping((TypeDefinition)constructor.DeclaringType);

			if (property_mapping != null)
			{
				foreach (var pair in property_mapping)
				{
					int argument;
					if (!field_mapping.TryGetValue(pair.Value, out argument))
						continue;

					var ca_arg = ca[argument];
					if (ca_arg.Value is CustomAttributeArgument)
						ca_arg = (CustomAttributeArgument)ca_arg.Value;

					if (mapping == null)
						mapping = new Dictionary<string, object>(StringComparer.Ordinal);
					mapping.Add(pair.Key.Name, GetArgumentValue(ca_arg.Type, ca_arg.Value));
				}
			}
		}
	}

	static object GetArgumentValue(TypeReference reference, object value)
	{
		var type = reference.Resolve();
		if (type == null)
			return value;

		if (type.IsEnum)
		{
			if (IsFlaggedEnum(type))
				return GetFlaggedEnumValue(type, value);

			return GetEnumValue(type, value);
		}

		return value;
	}

	static bool IsFlaggedEnum(TypeDefinition type)
	{
		if (!type.IsEnum)
			return false;

		if (!type.HasCustomAttributes)
			return false;

		foreach (CustomAttribute attribute in type.CustomAttributes)
			if (attribute.Constructor.DeclaringType.FullName == "System.FlagsAttribute")
				return true;

		return false;
	}

	static object GetFlaggedEnumValue(TypeDefinition type, object value)
	{
		if (value is ulong)
			return GetFlaggedEnumValue(type, (ulong)value);

		long flags = Convert.ToInt64(value);
		var signature = new StringBuilder();

		for (int i = type.Fields.Count - 1; i >= 0; i--)
		{
			FieldDefinition field = type.Fields[i];

			if (!field.HasConstant)
				continue;

			long flag = Convert.ToInt64(field.Constant);

			if (flag == 0)
				continue;

			if ((flags & flag) == flag)
			{
				if (signature.Length != 0)
					signature.Append(", ");

				signature.Append(field.Name);
				flags -= flag;
			}
		}

		return signature.ToString();
	}

	static object GetFlaggedEnumValue(TypeDefinition type, ulong flags)
	{
		var signature = new StringBuilder();

		for (int i = type.Fields.Count - 1; i >= 0; i--)
		{
			FieldDefinition field = type.Fields[i];

			if (!field.HasConstant)
				continue;

			ulong flag = Convert.ToUInt64(field.Constant);

			if (flag == 0)
				continue;

			if ((flags & flag) == flag)
			{
				if (signature.Length != 0)
					signature.Append(", ");

				signature.Append(field.Name);
				flags -= flag;
			}
		}

		return signature.ToString();
	}

	static object GetEnumValue(TypeDefinition type, object value)
	{
		foreach (FieldDefinition field in type.Fields)
		{
			if (!field.HasConstant)
				continue;

			if (Comparer.Default.Compare(field.Constant, value) == 0)
				return field.Name;
		}

		return value;
	}

	bool SkipAttribute(CustomAttribute attribute)
	{
		if (!state.TypeHelper.IsPublic(attribute))
			return true;

		return attribute.Constructor.DeclaringType.Name.EndsWith("TODOAttribute", StringComparison.Ordinal);
	}

	public static void OutputAttributes(XmlWriter writer, State state, params ICustomAttributeProvider[] providers)
	{
		var data = new AttributeData(state);
		data.DoOutput(writer, providers);
	}
}
