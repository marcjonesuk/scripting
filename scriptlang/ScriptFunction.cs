using System;
using System.Threading.Tasks;

namespace scriptlang
{
	public class ScriptFunction
	{
		public string SymbolName { get; set; }
		public Func<object> Invoke { get; private set; }
		public Func<Task<object>> InvokeAsync { get; }
		public bool AsyncFunction { get; }

		public void Chain(object[] args)
		{
			var f = Invoke;
			Invoke = () => {
				var result = f();
				var s = result as CustomFunction;
				return s.Invoke(args);
			};
		}

		public ScriptFunction(Func<object> func)
		{
			Invoke = func;
			AsyncFunction = false;
		}

		public ScriptFunction(Func<Task<object>> func)
		{
			InvokeAsync = func;
			AsyncFunction = true;
		}
	}
}
