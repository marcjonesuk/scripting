using System;
using System.Threading.Tasks;

namespace scriptlang
{
	public class Function
	{
		public string SymbolName { get; set; }
		public Func<object[], object> Invoke { get; private set; }
		public string Name { get; set; }
		public Func<Task<object>> InvokeAsync { get; }
		public bool AsyncFunction { get; }

		public void Chain(object[] args)
		{
			var f = Invoke;
			Invoke = _ =>
			{
				var result = f(_);
				var s = result as Function;
				return s.Invoke(args);
			};
		}

		public Function(Func<object[], object> func, string name = null)
		{
			if (name == null) name = string.Empty;
			Invoke = func;
			Name = name;
			AsyncFunction = false;
		}

		public Function(Func<Task<object>> func)
		{
			InvokeAsync = func;
			AsyncFunction = true;
		}
	}
}
