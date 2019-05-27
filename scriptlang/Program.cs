using System;
using System.Threading.Tasks;

namespace scriptlang
{
	public delegate object Function<TContext>(object[] arguments);

	public class ScriptFunction
	{
		public string SymbolName {get;set;}
		public Func<object> Invoke { get; }
		public Func<Task<object>> InvokeAsync { get; }

		public ScriptFunction(Func<object> func)
		{
			Invoke = func;
		}

		public ScriptFunction(Func<Task<object>> func)
		{
			InvokeAsync = func;
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
			//var code = @"write('hello')";
			var code = "10";
			var tokenizer = new Tokenizer(new LizzieTokenizer());
			var func = Compiler.Compile(tokenizer.Tokenize(code));
			Console.WriteLine(func.Invoke());
		}
	}
}
