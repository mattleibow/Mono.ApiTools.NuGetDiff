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

namespace Mono.ApiTools;

class Counters
{
	public int Present;
	public int PresentTotal;
	public int Missing;
	public int MissingTotal;
	public int Todo;
	public int TodoTotal;

	public int Extra;
	public int ExtraTotal;
	public int Warning;
	public int WarningTotal;
	public int ErrorTotal;

	public Counters ()
	{
	}

	public void AddPartialToPartial (Counters other)
	{
		Present += other.Present;
		Extra += other.Extra;
		Missing += other.Missing;

		Todo += other.Todo;
		Warning += other.Warning;
		AddPartialToTotal (other);
	}

	public void AddPartialToTotal (Counters other)
	{
		PresentTotal += other.Present;
		ExtraTotal += other.Extra;
		MissingTotal += other.Missing;

		TodoTotal += other.Todo;
		WarningTotal += other.Warning;
	}

	public void AddTotalToPartial (Counters other)
	{
		Present += other.PresentTotal;
		Extra += other.ExtraTotal;
		Missing += other.MissingTotal;

		Todo += other.TodoTotal;
		Warning += other.WarningTotal;
		AddTotalToTotal (other);
	}

	public void AddTotalToTotal (Counters other)
	{
		PresentTotal += other.PresentTotal;
		ExtraTotal += other.ExtraTotal;
		MissingTotal += other.MissingTotal;

		TodoTotal += other.TodoTotal;
		WarningTotal += other.WarningTotal;
		ErrorTotal += other.ErrorTotal;
	}

	public int Total {
		get { return Present + Missing; }
	}

	public int AbsTotal {
		get { return PresentTotal + MissingTotal; }
	}

	public int Ok {
		get { return Present - Todo; }
	}

	public int OkTotal {
		get { return PresentTotal - TodoTotal - ErrorTotal; }
	}

	public override string ToString ()
	{
		StringWriter sw = new StringWriter ();
		sw.WriteLine ("Present: {0}", Present);
		sw.WriteLine ("PresentTotal: {0}", PresentTotal);
		sw.WriteLine ("Missing: {0}", Missing);
		sw.WriteLine ("MissingTotal: {0}", MissingTotal);
		sw.WriteLine ("Todo: {0}", Todo);
		sw.WriteLine ("TodoTotal: {0}", TodoTotal);
		sw.WriteLine ("Extra: {0}", Extra);
		sw.WriteLine ("ExtraTotal: {0}", ExtraTotal);
		sw.WriteLine ("Warning: {0}", Warning);
		sw.WriteLine ("WarningTotal: {0}", WarningTotal);
		sw.WriteLine ("ErrorTotal: {0}", ErrorTotal);
		sw.WriteLine ("--");
		return sw.GetStringBuilder ().ToString ();
	}
}
