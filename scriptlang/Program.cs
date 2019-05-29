using System;
using System.IO;
using System.Threading.Tasks;

namespace scriptlang
{
	public class CustomFunction
	{
		public Func<object[], object> Invoke { get; }

		public CustomFunction(Func<object[], object> func)
		{
			Invoke = func;
		}
	}

	public class Token
	{
		private readonly string rawValue;

		public Token(string rawValue)
		{
			this.rawValue = rawValue;
		}

		public override string ToString()
		{
			return rawValue;
		}

		public static implicit operator string(Token t)
		{
			return t.ToString();
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			var code = File.ReadAllText("test.script");
			var tokenizer = new Tokenizer(new LizzieTokenizer());
			var func = Compiler.Compile(tokenizer.Tokenize(code));
			Console.WriteLine(func.Invoke());
		}
	}
}
