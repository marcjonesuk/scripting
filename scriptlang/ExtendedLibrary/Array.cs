using System;
using System.Collections;
using System.Collections.Generic;

namespace scriptlang
{
	public class FunctionProvider
	{
		public static void ExpectSome(object[] args)
		{
			if (args.Length == 0)
				throw new RuntimeException();
		}

		public static T Expect<T>(string func, object arg)
		{
			if (arg.GetType() == typeof(T))
			{
				return (T)arg;
			}
			throw new RuntimeException($"{func} expected argument of type {typeof(T)} but got {arg.GetType()}");
		}

		public static (T1, T2) Expect<T1, T2>(object[] args)
		{
			if (args[0].GetType() != typeof(T1))
				throw new RuntimeException();
			var t1 = (T1)args[0];

			if (args[1].GetType() != typeof(T2))
				throw new RuntimeException();
			var t2 = (T2)args[1];

			return (t1, t2);
		}

		public static (T1, T2, T3) Expect<T1, T2, T3>(object[] args)
		{
			if (args[0].GetType() != typeof(T1))
				throw new RuntimeException();
			var t1 = (T1)args[0];

			if (args[1].GetType() != typeof(T2))
				throw new RuntimeException();
			var t2 = (T2)args[1];

			if (args[2].GetType() != typeof(T3))
				throw new RuntimeException();
			var t3 = (T3)args[2];

			return (t1, t2, t3);
		}
	}

	public class ListFunctions : FunctionProvider
	{
		public static void Bootstrap(State current)
		{
			current.Add("list.splice", (state, args) =>
			{
				var list = Expect<IList>("list.splice", args[0]);
				if (args[0] is IList l && args.Length > 1)
				{
					return null;
				}
				throw new RuntimeException($"Cannot use splice function on type {args[0].GetType()}");
			});
			current.Alias("len", "list.length");
			current.Alias("clear", "list.clear");
			current.Add("list.new", (state, args) =>
			{
				ExpectSome(args);
				var list = new List<object>();
				for (var i = 0; i < args.Length; i++)
				{
					list.Add(args[i]);
				}
				return list;
			});
			current.Add("list.indexOf", (state, args) =>
			{
				var (list, index) = Expect<IList, int>(args);
				return list.IndexOf(index);
			});
			current.Add("list.push", (state, args) =>
			{
				var list = Expect<IList>("list.push", args[0]);
				for (var i = 1; i < args.Length; i++)
				{
					list.Add(args[i]);
				}
				return list;
			});
			current.Add("list.pop", (state, args) =>
			{
				var list = Expect<IList>("list.pop", args[0]);
				return null;
			});
		}
	}
}
