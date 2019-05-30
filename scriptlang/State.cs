using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using Newtonsoft.Json;

namespace scriptlang
{
	public class State
	{
		public Stack<object[]> Args = new Stack<object[]>();
		public int StackDepth = 0;
		//public static Dictionary<string, object> Functions = new Dictionary<string, object>();

		public List<Dictionary<string, object>> Functions = new List<Dictionary<string, object>>();

		public Dictionary<string, object> Global = new Dictionary<string, object>();

		public HashSet<string> Const = new HashSet<string>();

		void AssertArgCount(object[] args, int count, string functionName)
		{
			if (args.Length != count)
			{
				throw new RuntimeException($"{functionName} should be invoked with {count} arg(s)");
			}
		}

		public (object, bool) Resolve(string name)
		{
			for (var stack = Functions.Count - 1; stack >= 0; stack--)
			{
				if (Functions[stack].ContainsKey(name))
					return (Functions[stack][name], true);
			}
			return (null, false);
		}

		public object GetValue(List<string> parts)
		{
			var (result, found) = Resolve(parts[0]);
			if (!found)
				throw new RuntimeException($"Unknown symbol: {parts[0]}");
			if (parts.Count == 1)
				return result;

			var current = Resolve(parts[0]).Item1;
			for (var i = 1; i < parts.Count; i++)
			{
				var p = parts[i];
				current = GetObjectProperty(current, p);
			}
			return current;
		}

		private object GetObjectProperty(object root, string property)
		{
			if (root is IDictionary<string, object> dict)
			{
				if (!dict.ContainsKey(property))
					throw new RuntimeException("Member does not exist");
				return dict[property];
			}
			throw new RuntimeException("TODO: cant get property");
		}

		private object SetObjectProperty(object obj, string property, object value)
		{
			if (obj is IDictionary<string, object> dict)
			{
				dict[property] = value;
				return value;
			}
			throw new RuntimeException("TODO: cant get property");
		}

		public object SetValue(List<string> parts, Function value)
		{
			if (parts.Count == 1)
				return ((Function)Global["set"]).Invoke(this, new object[] { parts[0], value });

			var (current, found) = Resolve(parts[0]);
			if (!found)
			{
				throw new RuntimeException($"symbol {parts[0]} has not been assigned");
			}

			for (var i = 1; i < parts.Count; i++)
			{
				var p = parts[i];
				if (i == parts.Count - 1)
					return SetObjectProperty(current, p, value.Invoke(this, null));
				current = GetObjectProperty(current, p);
			}
			throw new RuntimeException("SetValue failed");
		}

		public object InvokeWithStack(Function sf, object[] args)
		{
			Args.Push(args);
			StackDepth++;
			Functions.Add(new Dictionary<string, object>());
			var result = sf.Invoke(this, args);
			Args.Pop();
			Functions.RemoveAt(StackDepth);
			StackDepth--;
			return result;
		}

		public State()
		{
			Functions.Add(Global);

			Const.Add("var");

			Global["args"] = new Function((state, args) =>
			{
				if (args.Length == 0)
					return Args.Peek();

				var index = (int)Convert.ChangeType(args[0], typeof(int));
				return Args.Peek()[index];
			});

			Global["props"] = new Function((state, args) =>
			{
				if (args[0] is IDictionary<string, object> d)
				{
					return new List<string>(d.Keys);
				}
				throw new RuntimeException("props: unable to return props");
			});

			Const.Add("const");
			Global["const"] = new Function((state, args) =>
			{
				var func = args[0] as Function;

				if (func == null)
					throw new RuntimeException();

				var variableName = func.SymbolName;

				if (args.Length > 1)
				{
					var valueFunc = args[1] as Function;
					Global[variableName] = valueFunc.Invoke(state, null);
					Const.Add(variableName);
				}
				else
				{
					throw new RuntimeException($"const ({variableName}) must be declared with a value");
				}
				return Global[variableName];
			});

			Const.Add("set");
			Global["set"] = new Function((state, args) =>
			{
				string varName;
				if (args[0] is string s)
				{
					varName = s;
				}
				else
				{
					var func = args[0] as Function;
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
				var newValue = ((Function)args[1]).Invoke(state, null);
				if (newValue is Function sf)
				{
					newValue = new Function((st, lambdaArgs) =>
					{
						return InvokeWithStack(sf, lambdaArgs);
					});
				}

				Functions[StackDepth][varName] = newValue;
				return newValue;
			});

			Const.Add("throw");
			Global["throw"] = new Function((state, args) =>
			{
				throw new Exception(args[0].ToString());
			});

			Global["write"] = new Function((state, args) =>
			{
				Console.WriteLine(args[0].ToString());
				return null;
			});
			Global["new"] = new Function((state, args) =>
			{
				if (args.Length == 0)
					return new ExpandoObject();

				switch (args[0])
				{
					case string s:
						// todo: optimise this
						var st = s.TrimStart();
						if (st[0] == '[')
							return JsonConvert.DeserializeObject<List<object>>(st);
						else
							return JsonConvert.DeserializeObject<ExpandoObject>(st);
				}

				throw new RuntimeException();
			});

			Global["add"] = new Function((state, args) =>
			{
				return (dynamic)args[0] + (dynamic)args[1];
			});

			Global["len"] = new Function((state, args) =>
			{
				if (args[0] is string s)
					return s.Length;
				if (args[0] is IList l)
					return l.Count;
				if (args[0] is ICollection c)
					return c.Count;

				throw new RuntimeException($"Cannot use len function on type {args[0].GetType()}");
			});

			Global["clear"] = new Function((state, args) =>
			{
				if (args[0] is IList l)
				{
					l.Clear();
					return null;
				}
				throw new RuntimeException($"Cannot use len function on type {args[0].GetType()}");
			});

			Global["inc"] = new Function((state, args) =>
			{
				// todo handle local state
				// AssertArgCount(args, 1, "inc");
				// var s = args[0] as ScriptFunction;
				// dynamic value = s.Invoke();
				// value++;
				// if (s.SymbolName != null)
				// {
				//     Global[s.SymbolName] = value;
				// }
				//return value;
				return null;
			});

			Global["dec"] = new Function((state, args) =>
			{
				// AssertArgCount(args, 1, "inc");
				// var s = args[0] as ScriptFunction;
				// dynamic value = s.Invoke();
				// value--;
				// if (s.SymbolName != null)
				// {
				//     Global[s.SymbolName] = value;
				// }
				// return value;
				return null;
			});

			Global["eq"] = new Function((state, args) =>
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
			Global["if"] = new Function((state, args) =>
			{
				if (Truthy(args[0]))
				{
					if (args[1] is Function s)
					{
						return s.Invoke(state, null);
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
						if (args[2] is Function s)
						{
							return s.Invoke(state, null);
						}
						else
						{
							return args[2];
						}
					}
					return null;
				}
			});

			Global["json"] = new Function((state, args) =>
			{
				return JsonConvert.SerializeObject(args[0]);
			});

			Const.Add("not");
			Global["not"] = new Function((state, args) =>
			{
				return !Truthy(args[0]);
			});

			Const.Add("try");
			Global["try"] = new Function((state, args) =>
			{
				if (args.Length == 0)
				{
					throw new RuntimeException("try function does not have any try block");
				}

				try
				{
					if (args[0] is Function t)
					{
						InvokeWithStack(t, null);
					}
					return null;
				}
				catch (Exception ex)
				{
					if (args.Length > 1)
					{
						if (args[1] is Function c)
						{
							return InvokeWithStack(c, new object[] { ex.Message });
						}
					}
					return ex;
				}
			});

			Const.Add("null");
			Global["null"] = null;
			Const.Add("true");
			Global["true"] = true;
			Const.Add("false");
			Global["false"] = false;
			Array.Do(Global);
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
