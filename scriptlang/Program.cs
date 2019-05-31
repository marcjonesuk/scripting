using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace scriptlang
{
	class Program
	{
		static void Main(string[] args)
		{
			var code = File.ReadAllText("test.script");
			var tokenizer = new Tokenizer(new LizzieTokenizer());
			var func = Compiler.CompileAsync(tokenizer.Tokenize(code));
			var t = func().Result;
				
			var sw = Stopwatch.StartNew();

			double result = 0;
			for (var i = 0; i < 1000000; i++)
				t = func().Result;

			Console.WriteLine(sw.ElapsedMilliseconds);
			Console.WriteLine(result);
		}
	}
}