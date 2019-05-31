using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace scriptlang
{
	public class MathFunctions : ArgsHelper
	{
		public static void Bootstrap(State current)
		{
			current.Add("math.round", (state, args) =>
			{
				if (args.Length == 1)
					return Math.Round((dynamic)args[0]);

				var (arg1, arg2) = Expect<object, int>("math.round", args);
				return Math.Round((dynamic)arg1, arg2);
			});

			current.Add("stopwatch.new", (state, args) =>
			{
				ExpectNoArgs("stopwatch.stop", args);
				return Stopwatch.StartNew();
			});

			current.Add("stopwatch.stop", (state, args) =>
			{
				var sw = ExpectExactly<Stopwatch>("stopwatch.stop", args);
				sw.Stop();
				return sw.ElapsedMilliseconds;
			});
		}
	}
}
