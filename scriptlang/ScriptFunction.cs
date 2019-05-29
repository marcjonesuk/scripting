using System;
using System.Threading.Tasks;

namespace scriptlang
{
	public class ScriptFunction
	{
		public string SymbolName { get; set; }
		public Func<object[], object> Invoke { get; private set; }
		public Func<Task<object>> InvokeAsync { get; }
		public bool AsyncFunction { get; }

		public void Chain(object[] args)
		{
			var f = Invoke;
			Invoke = _ => {
				var result = f(_);
				var s = result as CustomFunction;
				return s.Invoke(args);
			};
		}

		public ScriptFunction(Func<object[], object> func)
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
