using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;

namespace scriptlang
{
	public class Array
	{
		public static void Do(Dictionary<string, object> functions)
		{
			functions["list.splice"] = new CustomFunction(args =>
			{
				if (args[0] is IList l && args.Length > 1)
				{
					return null;
				}
				throw new RuntimeException($"Cannot use len function on type {args[0].GetType()}");
			});
			functions["list.length"] = functions["len"];
			functions["list.clear"] = functions["clear"];
			functions["list.new"] = new CustomFunction(args =>
			{
				var list = new List<object>();
				for (var i = 0; i < args.Length; i++)
				{
					list.Add(args[i]);
				}
				return list;
			});


			functions["list.indexOf"] = new CustomFunction(args =>
			{
				if (args[0] is IList l)
				{
					var f = args.Length > 1 ? args[1] : null;
					return l.IndexOf(f);
				}
				throw new RuntimeException("The first argument to list.indexof(..) should be a list object.");
			});
			functions["list.push"] = new CustomFunction(args =>
			{
				if (args[0] is IList l)
				{
					for (var i = 1; i < args.Length; i++)
					{
						l.Add(args[i]);
					}
				}
				else
				{
					throw new RuntimeException("The first argument to list.add(..) should be a list.");
				}
				return new List<object>();
			});
			functions["list.pop"] = new CustomFunction(args =>
			{

				return new List<object>();
			});
		}
	}

	public class State
	{
		public static Stack<object[]> Args = new Stack<object[]>();
		public static Dictionary<string, object> Functions = new Dictionary<string, object>();
		public static HashSet<string> Const = new HashSet<string>();

		static void AssertArgCount(object[] args, int count, string functionName)
		{
			if (args.Length != count)
			{
				throw new RuntimeException($"{functionName} should be invoked with {count} arg(s)");
			}
		}

		static void Register(string name, CustomFunction customFunction) {

		}

		static State()
		{
			Const.Add("var");
			Functions["var"] = new CustomFunction(args =>
			{
				var func = args[0] as ScriptFunction;

				if (func == null)
					throw new RuntimeException();

				var variableName = func.SymbolName;

				if (args.Length > 1)
				{
					var valueFunc = args[1] as ScriptFunction;
					Functions[variableName] = valueFunc.Invoke();
				}
				else
				{
					Functions[variableName] = null;
				}
				return Functions[variableName];
			});

			Functions["args"] = new CustomFunction(args => 
			{
				var index = (int)Convert.ChangeType(args[0], typeof(int));
				return Args.Peek()[index];
			});

			Const.Add("const");
			Functions["const"] = new CustomFunction(args =>
			{
				var func = args[0] as ScriptFunction;

				if (func == null)
					throw new RuntimeException();

				var variableName = func.SymbolName;

				if (args.Length > 1)
				{
					var valueFunc = args[1] as ScriptFunction;
					Functions[variableName] = valueFunc.Invoke();
					Const.Add(variableName);
				}
				else
				{
					throw new RuntimeException($"const ({variableName}) must be declared with a value");
				}
				return Functions[variableName];
			});

			Const.Add("set");
			Functions["set"] = new CustomFunction(args =>
			{
				string varName;
				if (args[0] is string s)
				{
					varName = s;
				}
				else
				{
					var func = args[0] as ScriptFunction;
					if (func == null)
						throw new RuntimeException();
					if (string.IsNullOrEmpty(func.SymbolName))
					{
						throw new RuntimeException("The first argument of the set function should be a valid symbol");
					}
					varName = func.SymbolName;
				}
				if (Const.Contains(varName))
				{
					throw new RuntimeException($"Cannot assign to const variable {varName}");
				}
				var newValue = ((ScriptFunction)args[1]).Invoke();
				if (newValue is ScriptFunction sf)
				{
					newValue = new CustomFunction(lambdaArgs => 
					{ 
						Args.Push(lambdaArgs);
						var result = sf.Invoke();
						Args.Pop();
						return result;
					});
				}

				Functions[varName] = newValue;
				return newValue;
			});

			Const.Add("throw");
			Functions["throw"] = new CustomFunction(args =>
			{
				throw new Exception(args[0].ToString());
			});

			Functions["write"] = new CustomFunction(args =>
			{
				Console.WriteLine(args[0].ToString());
				return null;
			});
			Functions["new"] = new CustomFunction(args =>
			{
				return new ExpandoObject();
			});

			Functions["add"] = new CustomFunction(args =>
			{
				return (dynamic)args[0] + (dynamic)args[1];
			});

			Functions["len"] = new CustomFunction(args =>
			{
				if (args[0] is string s)
					return s.Length;
				if (args[0] is IList l)
					return l.Count;

				throw new RuntimeException($"Cannot use len function on type {args[0].GetType()}");
			});

			Functions["clear"] = new CustomFunction(args =>
			{
				if (args[0] is IList l)
				{
					l.Clear();
					return null;
				}
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
					Functions[s.SymbolName] = value;
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
					Functions[s.SymbolName] = value;
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

			Const.Add("if");
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

			Const.Add("not");
			Functions["not"] = new CustomFunction(args =>
			{
				return !Truthy(args[0]);
			});

			Const.Add("try");
			Functions["try"] = new CustomFunction(args =>
			{
				if (args.Length == 0)
				{
					throw new RuntimeException("try function does not have any try block");
				}

				try
				{
					if (args[0] is ScriptFunction t)
					{
						return t.Invoke();
					}
					return null;
				}
				catch(Exception ex)
				{
					if (args.Length > 1)
					{
						if (args[1] is ScriptFunction c)
						{
							Args.Push(new object[] { ex.Message });
							var result = c.Invoke();
							Args.Pop();
							return result;
						}
					}
					return ex;
				}
			});

			Const.Add("null");
			Functions["null"] = null;
			Const.Add("true");
			Functions["true"] = true;
			Const.Add("false");
			Functions["false"] = false;

			Array.Do(Functions);
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
