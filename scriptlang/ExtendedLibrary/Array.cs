using System;
using System.Collections;
using System.Collections.Generic;

namespace scriptlang
{
	public class ListFunctions : ArgsHelper
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
				var (list, index) = Expect<IList, int>("list.indexOf", args);
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
