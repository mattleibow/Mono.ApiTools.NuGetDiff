﻿using System;
using System.Collections.Generic;
using System.Text;
using Mono.Options;

namespace Mono.ApiTools
{
	public abstract class BaseCommand : Command
	{
		protected const int DefaultSaveBufferSize = 1024;

		protected static readonly Encoding UTF8NoBOM = new UTF8Encoding(false, true);

		protected BaseCommand(string name, string help)
			: this(name, null, help)
		{
		}

		protected BaseCommand(string name, string extras, string help)
			: base(name, help)
		{
			var actualOptions = OnCreateOptions();

			if (string.IsNullOrWhiteSpace(extras))
				extras = "[OPTIONS]";
			else
				extras += " [OPTIONS]";

			Options = new OptionSet
			{
				$"usage: {Program.Name} {Name} {extras}",
				"",
				Help,
				"",
				"Options:",
			};

			foreach (var o in actualOptions)
				Options.Add(o);

			Options.Add("?|h|help", "Show this message and exit", _ => ShowHelp = true);
		}

		public bool ShowHelp { get; private set; }

		public override int Invoke(IEnumerable<string> args)
		{
			try
			{
				var extras = Options.Parse(args);

				if (ShowHelp)
				{
					Options.WriteOptionDescriptions(CommandSet.Out);
					return 0;
				}

				if (!OnValidateArguments(extras))
				{
					Console.Error.WriteLine($"{Program.Name}: Use `{Program.Name} help {Name}` for details.");
					return 1;
				}

				return OnInvoke(extras) ? 0 : 1;
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"{Program.Name}: An error occurred: `{ex.Message}`.");
				if (Program.Verbose)
					Console.Error.WriteLine(ex);
				return 1;
			}
		}

		protected abstract OptionSet OnCreateOptions();

		protected virtual bool OnValidateArguments(IEnumerable<string> extras) => true;

		protected abstract bool OnInvoke(IEnumerable<string> extras);
	}
}
