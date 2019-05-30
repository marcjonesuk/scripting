using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using Newtonsoft.Json;

namespace scriptlang
{
	public class StandardLibrary
	{
		public static void Bootstrap(State current)
		{
			current.Add("args", (state, args) =>
			{
				var currentArgs = state.Args();
				if (args.Length == 0)
					return currentArgs;

				var index = (int)Convert.ChangeType(args[0], typeof(int));
				return currentArgs[index];
			});

			current.Add("props", (state, args) =>
			{
				if (args[0] is IDictionary<string, object> d)
				{
					return new List<string>(d.Keys);
				}
				throw new RuntimeException("props: unable to return props");
			});

			current.MakeConst("const");
			current.Add("const", (state, args) =>
			{
				return null;
				// state.MakeConst(args[0]);
				// var func = args[0] as Function;

				// if (func == null)
				// 	throw new RuntimeException();

				// var variableName = func.SymbolName;

				// if (args.Length > 1)
				// {
				// 	var valueFunc = args[1] as Function;
				// 	Global[variableName] = valueFunc.Invoke(state, null);
				// 	Const.Add(variableName);
				// }
				// else
				// {
				// 	throw new RuntimeException($"const ({variableName}) must be declared with a value");
				// }
				// return Global[variableName];
			});

			current.MakeConst("set");
			current.Add("set", (state, args) =>
			{
				return state.Set(args);
			});

			current.MakeConst("throw");
			current.Add("throw", (state, args) =>
			{
				throw new Exception(args[0].ToString());
			});

			current.Add("write", (state, args) =>
			{
				Console.WriteLine(args[0].ToString());
				return null;
			});

			current.Add("new", (state, args) =>
			{
				if (args.Length == 0)
					return new ExpandoObject();

				switch (args[0])
				{
					case string str:
						// todo: optimise this
						var st = str.TrimStart();
						if (st[0] == '[')
							return JsonConvert.DeserializeObject<List<object>>(st);
						else
							return JsonConvert.DeserializeObject<ExpandoObject>(st);
				}

				throw new RuntimeException();
			});

			current.Add("add", (state, args) =>
			{
				return (dynamic)args[0] + (dynamic)args[1];
			});

			current.Add("len", (state, args) =>
			{
				if (args[0] is string s)
					return s.Length;
				if (args[0] is IList l)
					return l.Count;
				if (args[0] is ICollection c)
					return c.Count;

				throw new RuntimeException($"Cannot use len function on type {args[0].GetType()}");
			});

			current.Add("clear", (state, args) =>
			{
				if (args[0] is IList l)
				{
					l.Clear();
					return null;
				}
				throw new RuntimeException($"Cannot use len function on type {args[0].GetType()}");
			});

			current.Add("inc", (state, args) =>
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

			current.Add("dec", (state, args) =>
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

			current.Add("eq", (state, args) =>
			{
				// AssertArgCount(args, 2, "eq");
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

			current.MakeConst("if");
			current.Add("if", (state, args) =>
			{
				if (State.Truthy(args[0]))
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

			current.Add("json", (state, args) =>
			{
				return JsonConvert.SerializeObject(args[0]);
			});

			current.MakeConst("not");
			current.Add("not", (state, args) =>
			{
				return !State.Truthy(args[0]);
			});

			current.MakeConst("try");
			current.Add("try", (state, args) =>
			{
				if (args.Length == 0)
				{
					throw new RuntimeException("try function does not have any try block");
				}
				try
				{
					if (args[0] is Function t)
					{
						state.InvokeWithStack(t, null);
					}
					return null;
				}
				catch (Exception ex)
				{
					if (args.Length > 1)
					{
						if (args[1] is Function c)
						{
							return state.InvokeWithStack(c, new object[] { ex.Message });
						}
					}
					return ex;
				}
			});
		}
	}
}
