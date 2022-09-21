﻿using System;
using Qrakhen.Sqr.Core;
using System.IO;

namespace Qrakhen.Sqr.shell
{
    class Program
    {
		static void Main(string[] args) {
			Console.ForegroundColor = ConsoleColor.White;
			if (args.Length == 0) {
				SqrDI.Dependor.get<Runtime>().run();
			} else {
				var content = File.ReadAllText(args[0] + (args[0].EndsWith(".sq") ? "" : ".sq"));
				SqrDI.Dependor.get<Runtime>().run(content);
			}
		}
	}
}
