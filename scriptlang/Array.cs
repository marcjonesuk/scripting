using System.Collections;
using System.Collections.Generic;

namespace scriptlang
{
    public class Array
	{
		public static void Do(Dictionary<string, object> functions)
		{
			// functions["list.splice"] = new Function((state, args) =>
			// {
			// 	if (args[0] is IList l && args.Length > 1)
			// 	{
			// 		return null;
			// 	}
			// 	throw new RuntimeException($"Cannot use len function on type {args[0].GetType()}");
			// });
			// functions["list.length"] = functions["len"];
			// functions["list.clear"] = functions["clear"];
			// functions["list.new"] = new Function((state, args) =>
			// {
			// 	var list = new List<object>();
			// 	for (var i = 0; i < args.Length; i++)
			// 	{
			// 		list.Add(args[i]);
			// 	}
			// 	return list;
			// });
			// functions["list.indexOf"] = new Function((state, args) =>
			// {
			// 	if (args[0] is IList l)
			// 	{
			// 		var f = args.Length > 1 ? args[1] : null;
			// 		return l.IndexOf(f);
			// 	}
			// 	throw new RuntimeException("The first argument to list.indexof(..) should be a list object.");
			// });
			// functions["list.push"] = new Function((state, args) =>
			// {
			// 	if (args[0] is IList l)
			// 	{
			// 		for (var i = 1; i < args.Length; i++)
			// 		{
			// 			l.Add(args[i]);
			// 		}
			// 	}
			// 	else
			// 	{
			// 		throw new RuntimeException("The first argument to list.add(..) should be a list.");
			// 	}
			// 	return new List<object>();
			// });
			// functions["list.pop"] = new Function((state, args) =>
			// {
			// 	return new List<object>();
			// });
		}
	}
}
