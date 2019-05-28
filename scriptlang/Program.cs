using System;
using System.IO;
using System.Threading.Tasks;

namespace scriptlang
{
	public class ScriptFunction
	{
		public string SymbolName {get;set;}
		public Func<object> Invoke { get; }
		public Func<Task<object>> InvokeAsync { get; }
		public bool AsyncFunction {get;}

		public ScriptFunction(Func<object> func)
		{
			Invoke = func;
			AsyncFunction  = false;
		}

		public ScriptFunction(Func<Task<object>> func)
		{
			InvokeAsync = func;
			AsyncFunction  = true;
		}
	}

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
			File.ReadAllText("test.script");
			var func = Compiler.Compile(tokenizer.Tokenize(code));
			Console.WriteLine(func.Invoke());
		}
	}
}
