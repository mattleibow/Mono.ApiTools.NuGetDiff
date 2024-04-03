﻿//
// WellFormedXmlWriter.cs
//
// Author:
//	Atsushi Enomoto <atsushi@ximian.com>
//
// Copyright (C) 2006 Novell, Inc. http://www.novell.com
//
using System.Xml;

namespace Mono.ApiTools;

class DefaultXmlWriter : XmlWriter
{
	XmlWriter writer;

	public DefaultXmlWriter(XmlWriter writer)
	{
		this.writer = writer;
	}

	protected XmlWriter Writer
	{
		get { return writer; }
	}

	public override void Close()
	{
		writer.Close();
	}

	public override void Flush()
	{
		writer.Flush();
	}

	public override string LookupPrefix(string ns)
	{
		return writer.LookupPrefix(ns);
	}

	public override void WriteBase64(byte[] buffer, int index, int count)
	{
		writer.WriteBase64(buffer, index, count);
	}

	public override void WriteBinHex(byte[] buffer, int index, int count)
	{
		writer.WriteBinHex(buffer, index, count);
	}

	public override void WriteCData(string text)
	{
		writer.WriteCData(text);
	}

	public override void WriteCharEntity(char ch)
	{
		writer.WriteCharEntity(ch);
	}

	public override void WriteChars(char[] buffer, int index, int count)
	{
		writer.WriteChars(buffer, index, count);
	}

	public override void WriteComment(string text)
	{
		writer.WriteComment(text);
	}

	public override void WriteDocType(string name, string pubid, string sysid, string subset)
	{
		writer.WriteDocType(name, pubid, sysid, subset);
	}

	public override void WriteEndAttribute()
	{
		writer.WriteEndAttribute();
	}

	public override void WriteEndDocument()
	{
		writer.WriteEndDocument();
	}

	public override void WriteEndElement()
	{
		writer.WriteEndElement();
	}

	public override void WriteEntityRef(string name)
	{
		writer.WriteEntityRef(name);
	}

	public override void WriteFullEndElement()
	{
		writer.WriteFullEndElement();
	}

	public override void WriteName(string name)
	{
		writer.WriteName(name);
	}

	public override void WriteNmToken(string name)
	{
		writer.WriteNmToken(name);
	}

	public override void WriteNode(XmlReader reader, bool defattr)
	{
		writer.WriteNode(reader, defattr);
	}

	public override void WriteProcessingInstruction(string name, string text)
	{
		writer.WriteProcessingInstruction(name, text);
	}

	public override void WriteQualifiedName(string localName, string ns)
	{
		writer.WriteQualifiedName(localName, ns);
	}

	public override void WriteRaw(string data)
	{
		writer.WriteRaw(data);
	}

	public override void WriteRaw(char[] buffer, int index, int count)
	{
		writer.WriteRaw(buffer, index, count);
	}

	public override void WriteStartAttribute(string prefix, string localName, string ns)
	{
		writer.WriteStartAttribute(prefix, localName, ns);
	}

	public override void WriteStartDocument(bool standalone)
	{
		writer.WriteStartDocument(standalone);
	}

	public override void WriteStartDocument()
	{
		writer.WriteStartDocument();
	}

	public override void WriteStartElement(string prefix, string localName, string ns)
	{
		writer.WriteStartElement(prefix, localName, ns);
	}

	public override void WriteString(string text)
	{
		writer.WriteString(text);
	}

	public override void WriteSurrogateCharEntity(char lowChar, char highChar)
	{
		writer.WriteSurrogateCharEntity(lowChar, highChar);
	}

	public override void WriteWhitespace(string ws)
	{
		writer.WriteWhitespace(ws);
	}

	public override WriteState WriteState
	{
		get
		{
			return writer.WriteState;
		}
	}

	public override string XmlLang
	{
		get
		{
			return writer.XmlLang;
		}
	}

	public override XmlSpace XmlSpace
	{
		get
		{
			return writer.XmlSpace;
		}
	}
}