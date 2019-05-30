using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace scriptlang
{
	public class MathFunctions
	{
		public static T Expect<T>(object[] args)
		{
			if (args[0].GetType() == typeof(T))
			{
				return (T)args[0];
			}
			throw new RuntimeException();
		}

		public static T ConvertIt<T>(object x) 
		{
			return (T)Convert.ChangeType(x, typeof(T));
		}

		public static void Bootstrap(State current)
		{
			current.Add("math.round", (state, args) =>
			{
				if (args.Length == 1)
					return Math.Round((dynamic)args[0]);
				return Math.Round((dynamic)args[0], ConvertIt<int>(args[1]));
			});

			current.Add("stopwatch.new", (state, args) =>
			{
				return Stopwatch.StartNew();
			});

			current.Add("stopwatch.stop", (state, args) =>
			{
				var sw = Expect<Stopwatch>(args);
				sw.Stop();
				return sw.ElapsedMilliseconds;
			});
		}
	}
}
