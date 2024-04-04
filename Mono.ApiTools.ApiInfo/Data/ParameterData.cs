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

class ParameterData : BaseData
{
	private IList<ParameterDefinition> parameters;

	public ParameterData(XmlWriter writer, IList<ParameterDefinition> parameters, State state)
		: base(writer, state)
	{
		this.parameters = parameters;
	}

	public bool HasExtensionParameter { get; set; }

	public override void DoOutput()
	{
		bool first = true;
		writer.WriteStartElement("parameters");
		foreach (ParameterDefinition parameter in parameters)
		{
			writer.WriteStartElement("parameter");
			AddAttribute("name", parameter.Name);
			AddAttribute("position", parameter.Method.Parameters.IndexOf(parameter).ToString(CultureInfo.InvariantCulture));
			AddAttribute("attrib", ((int)parameter.Attributes).ToString());

			string direction = first && HasExtensionParameter ? "this" : "in";
			first = false;

			var pt = parameter.ParameterType;
			var brt = pt as ByReferenceType;
			if (brt != null)
			{
				direction = parameter.IsOut ? "out" : "ref";
				pt = brt.ElementType;
			}

			AddAttribute("type", Utils.CleanupTypeName(pt));

			if (parameter.IsOptional)
			{
				AddAttribute("optional", "true");
				if (parameter.HasConstant)
					AddAttribute("defaultValue", parameter.Constant == null ? "NULL" : parameter.Constant.ToString());
			}

			if (direction != "in")
				AddAttribute("direction", direction);

			AttributeData.OutputAttributes(writer, state, parameter);
			writer.WriteEndElement(); // parameter
		}
		writer.WriteEndElement(); // parameters
	}
}
