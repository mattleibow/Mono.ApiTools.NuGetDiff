//
// WellFormedXmlWriter.cs
//
// Author:
//	Atsushi Enomoto <atsushi@ximian.com>
//
// Copyright (C) 2006 Novell, Inc. http://www.novell.com
//

using System.Globalization;
using System.Xml;

namespace Mono.ApiTools;

class WellFormedXmlWriter : DefaultXmlWriter
{
	public static bool IsInvalid(int ch)
	{
		switch (ch)
		{
			case 9:
			case 10:
			case 13:
				return false;
		}
		if (ch < 32)
			return true;
		if (ch < 0xD800)
			return false;
		if (ch < 0xE000)
			return true;
		if (ch < 0xFFFE)
			return false;
		if (ch < 0x10000)
			return true;
		if (ch < 0x110000)
			return false;
		else
			return true;
	}

	public static int IndexOfInvalid(string s, bool allowSurrogate)
	{
		for (int i = 0; i < s.Length; i++)
			if (IsInvalid(s[i]))
			{
				if (!allowSurrogate ||
					i + 1 == s.Length ||
					s[i] < '\uD800' ||
					s[i] >= '\uDC00' ||
					s[i + 1] < '\uDC00' ||
					s[i + 1] >= '\uE000')
					return i;
				i++;
			}
		return -1;
	}

	public static int IndexOfInvalid(char[] s, int start, int length, bool allowSurrogate)
	{
		int end = start + length;
		if (s.Length < end)
			throw new ArgumentOutOfRangeException("length");
		for (int i = start; i < end; i++)
			if (IsInvalid(s[i]))
			{
				if (!allowSurrogate ||
					i + 1 == end ||
					s[i] < '\uD800' ||
					s[i] >= '\uDC00' ||
					s[i + 1] < '\uDC00' ||
					s[i + 1] >= '\uE000')
					return i;
				i++;
			}
		return -1;
	}

	public WellFormedXmlWriter(XmlWriter writer)
		: base(writer)
	{
	}

	public override void WriteString(string text)
	{
		int i = IndexOfInvalid(text, true);
		if (i >= 0)
		{
			char[] arr = text.ToCharArray();
			Writer.WriteChars(arr, 0, i);
			WriteChars(arr, i, arr.Length - i);
		}
		else
		{
			// no invalid character.
			Writer.WriteString(text);
		}
	}

	public override void WriteChars(char[] text, int idx, int length)
	{
		int start = idx;
		int end = idx + length;
		while ((idx = IndexOfInvalid(text, start, length, true)) >= 0)
		{
			if (start < idx)
				Writer.WriteChars(text, start, idx - start);
			Writer.WriteString(String.Format(CultureInfo.InvariantCulture,
				text[idx] < 0x80 ? "\\x{0:X02}" : "\\u{0:X04}",
				(int)text[idx]));
			length -= idx - start + 1;
			start = idx + 1;
		}
		if (start < end)
			Writer.WriteChars(text, start, end - start);
	}

}
