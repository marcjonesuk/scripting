using System;
using System.Diagnostics;
using System.IO;
using scriptlang;

namespace scriptlang.benchmarking
{
	class Program
	{
		static void Main(string[] args)
		{
			var tokenizer = new Tokenizer(new LizzieTokenizer());
			var func = Compiler.CompileAsync(tokenizer.Tokenize("y = list.new(1, 2); list.length(y)"));
			var r = func().Result;
			var sw = Stopwatch.StartNew();
			//400ms sync 850ms with async
			for (var i = 0; i < 1000000; i++)
				r = func().Result;

			Console.WriteLine(sw.ElapsedMilliseconds);
			Console.WriteLine(r);
		}
	}
}
