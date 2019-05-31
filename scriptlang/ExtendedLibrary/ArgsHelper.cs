using System;

namespace scriptlang
{
	public class ArgsHelper
	{
		public static void ExpectSome(object[] args)
		{
			if (args.Length == 0)
				throw new RuntimeException();
		}

		public static T ConvertTo<T>(object x) 
		{
			return (T)Convert.ChangeType(x, typeof(T));
		}

		public static T Expect<T>(string func, object arg)
		{
			try
			{	
				if (typeof(T).IsAssignableFrom(arg.GetType()))
					return (T)arg;

				return ConvertTo<T>(arg);
			}
			catch 
			{
				throw new RuntimeException($"{func} expected argument of type {typeof(T)} but got {arg.GetType()}");
			}
		}

		public static T ExpectExactly<T>(string func, object[] args)
		{
			if (args.Length > 1)
				throw new RuntimeException($"{func} expected exactly one argument");

			try
			{
				return ConvertTo<T>(args[0]);
			}
			catch 
			{
				throw new RuntimeException($"{func} expected argument of type {typeof(T)} but got {args.GetType()}");
			}
		}

		public static void ExpectNoArgs(string func, object[] args)
		{
			if (args.Length > 0)
				throw new RuntimeException($"{func} expects no arguments");
		}

		public static void ExpectOneArg(string func, object[] args)
		{
			if (args.Length != 1)
				throw new RuntimeException($"{func} expects no arguments");
		}

		public static (T1, T2) Expect<T1, T2>(string func, object[] args)
		{
			return (Expect<T1>(func, args[0]), Expect<T2>(func, args[1]));
		}

		public static (T1, T2, T3) Expect<T1, T2, T3>(string func, object[] args)
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
}
