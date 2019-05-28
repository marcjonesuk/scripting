using System;
using System.Collections.Generic;
using System.Globalization;

namespace scriptlang
{
	[System.Serializable]
	public class CompilerException : System.Exception
	{
		public CompilerException() { }
		public CompilerException(string message) : base(message) { }
		public CompilerException(string message, System.Exception inner) : base(message, inner) { }
		protected CompilerException(
			System.Runtime.Serialization.SerializationInfo info,
			System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}

	[System.Serializable]
	public class RuntimeException : System.Exception
	{
		public RuntimeException() { }
		public RuntimeException(string message) : base(message) { }
		public RuntimeException(string message, System.Exception inner) : base(message, inner) { }
		protected RuntimeException(
			System.Runtime.Serialization.SerializationInfo info,
			System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}

	public class Compiler
	{
		/*
         * Compiles a constant string down to a symbol and returns to caller.
         */
		static (ScriptFunction, bool) CompileString(IEnumerator<Token> en)
		{
			// Storing type of string literal quote.
			var quote = en.Current;

			// Sanity checking tokenizer's content.
			if (!en.MoveNext())
				throw new CompilerException($"Unexpected EOF after {quote}.");

			// Retrieving actual string constant, and discarding closing quote character.
			var stringConstant = en.Current.ToString();
			en.MoveNext();

			return (new ScriptFunction(() =>
			{
				return stringConstant;
			}), !en.MoveNext());
		}

		public static (ScriptFunction, bool) CompileNumber(IEnumerator<Token> en)
		{
			var success = double.TryParse(en.Current.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double dblResult);
			if (!success)
				throw new CompilerException();

			var result = (object)dblResult;

			return (new ScriptFunction(() =>
			{
				return result;
			}), !en.MoveNext());
		}

		public static Func<object> Compile(IEnumerable<Token> tokens)
		{
			var en = tokens.GetEnumerator();
			var (functions, eof) = Compiler.CompileStatements(en, false);
			return () =>
			{
				object result = null;
				foreach (var ix in functions)
				{
					result = ix.Invoke();
				}
				return result;
			};
		}

		static (ScriptFunction, bool) CompileStatement(IEnumerator<Token> en)
		{
			// Checking type of token, and acting accordingly.
			switch (en.Current.ToString())
			{
				case "[":
				 	return CompileList(en);
				case "{":
				 	return CompileLambda(en);
				case "@":
					return CompileSymbolReference(en);
				// case "\"":
				case "'":
					return CompileString(en);
				default:
					if (NumberParser.IsNumeric(en.Current.ToString()))
						return CompileNumber(en);
					else
						return CompileSymbol(en);
			}

			throw new CompilerException();
		}

		/*
         * Compiles a lambda down to a function and returns the function to caller.
         */
		static (ScriptFunction, bool) CompileLambda(IEnumerator<Token> en)
		{
			// Compiling body, and retrieving functions.
			var (functions, eof) = CompileStatements(en);

			/*
             * Creating a function that evaluates every function sequentially, and
             * returns the result of the last function evaluation to the caller.
             */
			var function = new ScriptFunction(() =>
			{
				object result = null;
				foreach (var ix in functions)
				{
					result = ix.Invoke();
				}
				return result;
			});

			var lazyFunction = new ScriptFunction(() =>
			{
				return function;
			});
			return (lazyFunction, eof || !en.MoveNext());
		}

		/*
         * Compiles a lambda segment, which might be the root level
         * content of a Lizzie lambda object, or the stuff between '{' and '}'.
         *
         * If "forceClose" is true, it will expect a brace '}' to end the lambda segment,
         * and throw an exception if it finds EOF before it finds the closing '}'.
         */
		static (List<ScriptFunction>, bool) CompileStatements(IEnumerator<Token> en, bool forceClose = true)
		{
			// Creating a list of functions and returning these to caller.
			var functions = new List<ScriptFunction>();
			var eof = !en.MoveNext();
			while (!eof && en.Current != "}")
			{
				// Compiling currently tokenized symbol.
				var (statement, isEnd) = CompileStatement(en);
				eof = isEnd;

				// Adding function invocation to list of functions.
				functions.Add(statement);

				// Checking if we're done compiling body.
				if (eof || en.Current == "}")
					break; // Even if we're not at EOF, we might be at '}', ending the current body.
			}

			// Sanity checking tokenizer's content, before returning functions to caller.
			if (forceClose && en.Current != "}")
				throw new CompilerException("Premature EOF while parsing code, missing an '}' character.");
			if (!forceClose && !eof && en.Current == "}")
				throw new CompilerException("Unexpected closing brace '}' in code, did you add one too many '}' characters?");

			return (functions, eof);
		}

		// static (ScriptFunction, bool) ApplyArguments(string symbolName, IEnumerator<Token> en)
		// {
			

		// 	return (new ScriptFunction(() =>
		// 	{
		// 		var args = new object[arguments.Count];
		// 		for (var a = 0; a < arguments.Count; a++)
		// 		{
		// 			if (symbolName != "var" && symbolName != "set" && symbolName != "inc" && symbolName != "dec")
		// 			{
		// 				var value = arguments[a].Invoke();
		// 				args[a] = value;
		// 			}
		// 			else
		// 			{
		// 				args[a] = arguments[a];
		// 			}
		// 		}

		// 		return State.Functions[symbolName].Invoke(args);
		// 	}), !en.MoveNext());

		static (ScriptFunction, bool) CompileList(IEnumerator<Token> en)
		{
			var items = new List<ScriptFunction>();

			// Sanity checking tokenizer's content.
			if (!en.MoveNext())
				throw new CompilerException("Unexpected EOF while parsing function invocation.");

			// Looping through all arguments, if there are any.
			while (en.Current != "]")
			{
				// Compiling current element.
				var (func, eof) = CompileStatement(en);
				items.Add(func);
				if (en.Current == "]")
					break; // And we are done parsing list.

				// Sanity checking tokenizer's content, and discarding "," token.
				if (en.Current != ",")
					throw new CompilerException($"Syntax error parsing list object.");
				if (!en.MoveNext())
					throw new CompilerException("Unexpected EOF while parsing list.");
			}
			return (new ScriptFunction(() =>
			{
				var list = new List<object>();
				foreach (var ix in items)
				{
					list.Add(ix.Invoke());
				}
				return list;
			}), !en.MoveNext());
		}

		/*
         * Compiles a symbolic reference down to a function invocation and returns
         * that function to caller.
         */
		static (ScriptFunction, bool) CompileSymbol(IEnumerator<Token> en)
		{
			// Retrieving symbol's name and sanity checking it.
			var symbolName = en.Current.ToString();
			//SanityCheckSymbolName(symbolName);

			// Discarding "(" token and checking if we're at EOF.
			var eof = !en.MoveNext();

			// Checking if this is a function invocation.
			if (!eof && en.Current.ToString() == "(")
			{
				// Function invocation, making sure we apply arguments,
				//return ApplyArguments<TContext>(symbolName, en);
				return (ApplyArguments(symbolName, en));
			}
			else if (!eof && en.Current.ToString() == "=") 
			{
				// Variable assignment
				en.MoveNext();
				var (value, neof) = CompileStatement(en);
				return (new ScriptFunction(() =>
				{
					// TODO can cache this one
					return ((CustomFunction)State.Functions["set"]).Invoke(new object[] { symbolName, value });
				})
				{ SymbolName = symbolName }, neof);
			}
			else
			{
				if (symbolName == "try") {
					throw new CompilerException("try function does not have any arguments. Example: try({ ... }, /* catch */ { ... })");
				}
				if (symbolName == "if") {
					throw new CompilerException("if function does not have any arguments. Example: if({ ... }, /* else */ { ... })");
				}
				return (new ScriptFunction(() =>
				{
					if (!State.Functions.ContainsKey(symbolName))
						throw new RuntimeException($"Unknown symbol: {symbolName}");
					return State.Functions[symbolName];
				})
				{ SymbolName = symbolName }, eof);
			}
		}

		/*
         * Applies arguments to a function invoction, such that they're evaluated at runtime.
         */
		static (ScriptFunction, bool) ApplyArguments(string symbolName, IEnumerator<Token> en)
		{
			// Used to hold arguments before they're being applied inside of function evaluation.
			var arguments = new List<ScriptFunction>();

			// Sanity checking tokenizer's content.
			if (!en.MoveNext())
				throw new CompilerException("Unexpected EOF while parsing function invocation.");

			// Looping through all arguments, if there are any.
			while (en.Current != ")")
			{
				// Compiling current argument.
				var (func, eof) = CompileStatement(en);
				arguments.Add(func);
				if (en.Current == ")")
					break; // And we are done parsing arguments.

				// Sanity checking tokenizer's content, and discarding "," token.
				if (en.Current != ",")
					throw new CompilerException($"Syntax error in arguments to '{symbolName}', expected ',' separating arguments and found '{en.Current}'.");
				if (!en.MoveNext())
					throw new CompilerException("Unexpected EOF while parsing arguments to function invocation.");
			}

			return (new ScriptFunction(() =>
			{
				var args = new object[arguments.Count];
				for (var a = 0; a < arguments.Count; a++)
				{
					if (symbolName != "var" && symbolName != "const" && symbolName != "set" && symbolName != "inc" && symbolName != "dec")
					{
						var value = arguments[a].Invoke();
						args[a] = value;
					}
					else
					{
						args[a] = arguments[a];
					}
				}

				return ((CustomFunction)State.Functions[symbolName]).Invoke(args);
			}), !en.MoveNext());
		}

		static (ScriptFunction, bool) CompileSymbolReference(IEnumerator<Token> en)
		{
			// Sanity checking tokenizer's content, since an '@' must reference an actual symbol.
			if (!en.MoveNext())
				throw new CompilerException("Unexpected EOF after '@'.");

			// Storing symbol's name and sanity checking its name.
			var symbolName = en.Current;

			// Sanity checking symbol name.
			//SanityCheckSymbolName(symbolName);

			// Discarding "(" token and checking if we're at EOF.
			var eof = !en.MoveNext();

			// Checking if this is a function invocation.
			if (!eof && en.Current == "(")
			{
				/*
                 * Notice, since this is a literally referenced function invocation, we
                 * don't want to apply its arguments if the function is being passed around,
                 * but rather return the function as a function, which once evaluated, applies
                 * its arguments. Hence, this becomes a "lazy function evaluation", allowing us
                 * to pass in a function evaluation, that is not evaluated before the caller
                 * explicitly evaluates the function wrapping our "inner function".
                 */
				var (func, neof) = ApplyArguments(symbolName, en);

				// return new Tuple<object, bool>(new Function<TContext>((ctx, binder, arguments) =>
				// {
				// 	return functor;
				// }), tuple.Item2);
				return (new ScriptFunction(() =>
				{
					return func;
				}), neof);
			}
			else
			{
				/*
                 * Creating a function that evaluates to the constant value of the symbol's name.
                 * When you use the '@' character with a symbol, this implies simply returning the
                 * symbol's name.
                 */
				return (new ScriptFunction(() =>
				{
					return symbolName;
				}), eof);
			}
		}
	}
}
