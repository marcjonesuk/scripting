using System;
using System.Collections;
using System.Collections.Generic;

namespace scriptlang
{
	public class MathFunctions
	{
		public static void Bootstrap(State current)
		{
			current.Add("math.round", (state, args) =>
			{
				if (args.Length == 1)
					return Math.Round((dynamic)args[0]);
				return Math.Round((dynamic)args[0], (int)Convert.ChangeType(args[1], typeof(int)));
			});
		}
	}
}
