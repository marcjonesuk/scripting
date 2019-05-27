using System.Collections.Generic;

namespace scriptlang
{
	public class State
	{
		public static Dictionary<string, CustomFunction> Functions = new Dictionary<string, scriptlang.CustomFunction>();
		public static Dictionary<string, object> Variables = new Dictionary<string, object>();

		static void AssertArgCount(object[] args, int count, string functionName)
		{
			if (args.Length != count)
			{
				throw new RuntimeException("${functionName} should be invoked with {count} arg(s)");
			}
		}

		static State()
		{
			Functions["var"] = new CustomFunction(args =>
			{
				var func = args[0] as ScriptFunction;

				if (func == null)
					throw new RuntimeException();

				var variableName = func.SymbolName;

				if (args.Length > 1)
				{
					var valueFunc = args[1] as ScriptFunction;
					Variables[variableName] = valueFunc.Invoke();
				}
				else
				{
					Variables[variableName] = null;
				}

				// Functions[varName] = new CustomFunction(varArgs =>
				// {
				// 	if (varArgs.Length == 0)
				// 	{
				// 		return _value;
				// 	}
				// 	else if (varArgs.Length == 1)
				// 	{
				// 		_value = varArgs[0];
				// 		return _value;
				// 	}
				// 	throw new CompilerException("too many arguments");
				// });
				return Variables[variableName];
			});

			Functions["set"] = new CustomFunction(args =>
			{
				var func = args[0] as ScriptFunction;

				if (func == null)
					throw new RuntimeException();

				if (string.IsNullOrEmpty(func.SymbolName))
				{
					throw new RuntimeException("The first argument of the set function should be a valid symbol");
				}

				var varName = func.SymbolName;

				if (!Variables.ContainsKey(varName))
				{
					throw new RuntimeException($"Variable {varName} has not been declared: please declare it before calling set");
				}
				Variables[varName] = ((ScriptFunction)args[1]).Invoke();
				return args[1];
			});

			Functions["test"] = new CustomFunction(_ => "test output");
			Functions["test"] = new CustomFunction(_ => "test output");
			Functions["addTen"] = new CustomFunction(args =>
			{
				var f = (double)args[0];
				return f + 10;
			});
			Functions["add"] = new CustomFunction(args =>
			{
				if (args[0] is List<object>) {

				}

				return (double)args[0] + (double)args[1];
			});
			
			Functions["len"] = new CustomFunction(args =>
			{
				if (args[0] is string s) 
					return s.Length;
				if (args[0] is IList<object> l)
					return l.Count;

				throw new RuntimeException($"Cannot use len function on type {args[0].GetType()}");
			});

			Functions["inc"] = new CustomFunction(args =>
			{
				AssertArgCount(args, 1, "inc");
				var s = args[0] as ScriptFunction;
				dynamic value = s.Invoke();
				value++;
				if (s.SymbolName != null)
				{
					Variables[s.SymbolName] = value;
				}
				return value;
			});

			Functions["dec"] = new CustomFunction(args =>
			{
				AssertArgCount(args, 1, "inc");
				var s = args[0] as ScriptFunction;
				dynamic value = s.Invoke();
				value--;
				if (s.SymbolName != null)
				{
					Variables[s.SymbolName] = value;
				}
				return value;
			});

			Functions["eq"] = new CustomFunction(args =>
			{
				AssertArgCount(args, 2, "eq");
				var arg1 = args[0];

				// Comparing all other objects to the first.
				for (var i = 1; i < args.Length; i++)
				{
					var ix = args[i];
					if (arg1 == null && ix != null || arg1 != null && ix == null)
						return false;
					if (arg1 != null && !arg1.Equals(ix))
						return false;
				}

				// Equality!
				return (object)true;
			});

			Functions["if"] = new CustomFunction(args =>
			{
				if (Truthy(args[0]))
				{
					if (args[1] is ScriptFunction s)
					{
						return s.Invoke();
					}
					else
					{
						return args[1];
					}
				}
				else
				{
					if (args.Length > 2)
					{
						if (args[2] is ScriptFunction s)
						{
							return s.Invoke();
						}
						else
						{
							return args[2];
						}
					}
					return null;
				}
			});

			Functions["not"] = new CustomFunction(args =>
			{
				return !Truthy(args[0]);
			});

			Variables["null"] = null;
			Variables["true"] = true;
			Variables["false"] = false;
		}

		static bool Truthy(object arg)
		{
			if (arg is bool b)
			{
				return b;
			}
			if (arg == null)
			{
				return false;
			}
			if (arg is string s)
			{
				return s != string.Empty;
			}
			return true;
		}
	}
}
