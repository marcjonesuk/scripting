using System.Collections;
using System.Collections.Generic;

namespace scriptlang
{
    public class ListFunctions
	{
		public static void Bootstrap(State current)
		{
			current.Add("list.splice", (state, args) =>
			{
				if (args[0] is IList l && args.Length > 1)
				{
					return null;
				}
				throw new RuntimeException($"Cannot use len function on type {args[0].GetType()}");
			});
			current.Alias("len", "list.length");
			current.Alias("clear", "list.clear");
			current.Add("list.new", (state, args) =>
			{
				var list = new List<object>();
				for (var i = 0; i < args.Length; i++)
				{
					list.Add(args[i]);
				}
				return list;
			});
			current.Add("list.indexOf", (state, args) =>
			{
				if (args[0] is IList l)
				{
					var f = args.Length > 1 ? args[1] : null;
					return l.IndexOf(f);
				}
				throw new RuntimeException("The first argument to list.indexof(..) should be a list object.");
			});
			current.Add("list.push", (state, args) =>
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
			current.Add("list.pop", (state, args) =>
			{
				return new List<object>();
			});
		}
	}
}
