using System;
using System.Threading.Tasks;

namespace scriptlang
{
	public enum FunctionType
	{
		Unknown,
		Assignment,
		Lambda,
		Lazy,
		StringConst,
		NumberConst,
		ListConst,
		Indexer,
		Getter,
		InvocationWithArgs
	}

	public class Function
	{
		public string SymbolName { get; set; }
		public Func<State, object[], object> Invoke { get; private set; }
		public FunctionType FunctionType { get; set; }
		public Func<State, object[], Task<object>> InvokeAsync { get; }
		public bool AsyncFunction { get; }

		public void Chain(object[] args)
		{
			// var f = Invoke;
			// Invoke = _ =>
			// {
			// 	var result = f(_);
			// 	var s = result as Function;
			// 	return s.Invoke(args);
			// };
		}

		public Function(Func<State, object[], object> func, FunctionType name = FunctionType.Unknown)
		{
			Invoke = func;
			FunctionType = name;
			AsyncFunction = false;
		}

		public Function(Func<State, object[], Task<object>> asyncFunc, FunctionType name = FunctionType.Unknown)
		{
			InvokeAsync = asyncFunc;
			FunctionType = name;			
			AsyncFunction = true;
		}
	}
}
