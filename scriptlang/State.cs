using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace scriptlang
{
	public class State
	{
		public const bool Debug = true;

		private Stack<object[]> _args = new Stack<object[]>();
		private int StackDepth = 0;
		private List<Dictionary<string, object>> Functions = new List<Dictionary<string, object>>();
		private Dictionary<string, object> Global = new Dictionary<string, object>();
		private HashSet<string> _const = new HashSet<string>();
		private HashSet<string> _namespace = new HashSet<string>();

		public State()
		{
			MakeConst("null");
			Global["null"] = null;
			MakeConst("true");
			Global["true"] = true;
			MakeConst("false");
			Global["false"] = false;
			Functions.Add(Global);
		}

		public object[] Args()
		{
			return _args.Peek();
		}

		public void MakeNamespace(string name)
		{
			MakeConst(name);
			_namespace.Add(name);
		}

		public void Add(string name, Func<State, object[], object> func)
		{
			if (!Debug)
			{
				SetObject(name, func);
			}
			else
			{
				SetObject(name, new Function((s, a) =>
				{
					try
					{
						return func(s, a);
					}
					catch
					{
						throw;
					}
				}));
			}
		}

		public void Add(string name, Func<State, object[], Task<object>> func)
		{
			SetObject(name, new Function(func));
		}

		void AssertArgCount(object[] args, int count, string functionName)
		{
			if (args.Length != count)
			{
				throw new RuntimeException($"{functionName} should be invoked with {count} arg(s)");
			}
		}

		public void MakeConst(string name)
		{
			_const.Add(name);
		}

		public async Task<object> Set(object[] args)
		{
			string varName;
			if (args[0] is string str)
			{
				varName = str;
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

			if (_const.Contains(varName))
			{
				throw new RuntimeException($"Cannot assign to const variable {varName}");
			}

			var newValue = await ((Function)args[1]).InvokeAsync(this, null);
			if (newValue is Function sf)
			{
				newValue = new Function(async (st, lambdaArgs) =>
				{
					return await InvokeWithStackAsync(sf, lambdaArgs);
				});
			}

			return SetObject(varName, newValue);
		}

		public object SetObject(string name, object value)
		{
			Functions[StackDepth][name] = value;
			return value;
		}

		public async Task<object> SetAsync(string name, Function function)
		{
			var value = function.IsAsync ? await function.InvokeAsync(this, null) : function.Invoke(this, null);

			if (value is Function f)
			{
				if (f.FunctionType == FunctionType.Lambda)
					value = StackWrapAsync(f);
			}

			Functions[StackDepth][name] = value;
			return value;
		}

		public async Task<object> SetValueAsync(List<string> parts, Function value)
		{
			if (parts.Count == 1)
				return await SetAsync(parts[0], value);

			var (current, found) = Resolve(parts[0]);
			if (!found)
			{
				throw new RuntimeException($"symbol {parts[0]} has not been assigned");
			}

			for (var i = 1; i < parts.Count; i++)
			{
				var p = parts[i];
				if (i == parts.Count - 1)
				{
					var v = value.IsAsync ? await value.InvokeAsync(this, null) : value.Invoke(this, null);
					return SetObjectProperty(current, p, v);
				}
				current = GetObjectProperty(current, p);
			}
			throw new RuntimeException("SetValue failed");
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

		public void Alias(string name, string aliasName)
		{
			Global[aliasName] = Global[name];
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

		public async Task<object> InvokeWithStackAsync(Function sf, object[] args)
		{
			_args.Push(args);
			StackDepth++;
			Functions.Add(new Dictionary<string, object>());
			var result = await sf.InvokeAsync(this, args);
			_args.Pop();
			Functions.RemoveAt(StackDepth);
			StackDepth--;
			return result;
		}

		// public asobject InvokeWithStack(Function sf, object[] args)
		// {
		// 	_args.Push(args);
		// 	StackDepth++;
		// 	Functions.Add(new Dictionary<string, object>());
		// 	var result = await sf.InvokeAsync(this, args);
		// 	_args.Pop();
		// 	Functions.RemoveAt(StackDepth);
		// 	StackDepth--;
		// 	return result;
		// }

		public object StackWrapAsync(Function sf)
		{
			return new Function(async (_, a) =>
			{
				_args.Push(a);
				StackDepth++;
				Functions.Add(new Dictionary<string, object>());
				var result = await sf.InvokeAsync(this, a);
				_args.Pop();
				Functions.RemoveAt(StackDepth);
				StackDepth--;
				return result;
			});
		}

		public static bool Truthy(object arg)
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

		public void Register<T>()
		{

		}
	}
}
