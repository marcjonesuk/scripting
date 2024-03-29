using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

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

	public class TokenEnumerator : IEnumerator<Token>
	{
		private IEnumerator<Token> _wrapped;
		private List<Token> _tokens = new List<Token>();

		public Token Current => _wrapped.Current;

		object IEnumerator.Current => _wrapped.Current;

		public TokenEnumerator(IEnumerator<Token> wrapped)
		{
			_wrapped = wrapped;
		}

		public void FlushTokens()
		{
			_tokens.Clear();
		}

		public List<Token> Tokens()
		{
			return new List<Token>(_tokens);
		}

		public void Dispose()
		{
			_wrapped.Dispose();
		}

		public bool MoveNext()
		{
			var next = _wrapped.MoveNext();
			_tokens.Add(Current);
			return next;
		}

		public void Reset()
		{
			_wrapped.Reset();
		}
	}

	public class Compiler
	{
		/*
         * Compiles a constant string down to a symbol and returns to caller.
         */
		static (Function, bool) CompileString(TokenEnumerator en)
		{
			// Storing type of string literal quote.
			var quote = en.Current;

			// Sanity checking tokenizer's content.
			if (!en.MoveNext())
				throw new CompilerException($"Unexpected EOF after {quote}.");

			// Retrieving actual string constant, and discarding closing quote character.
			var stringConstant = en.Current.ToString();
			en.MoveNext();

			return (new Function((state, _) => stringConstant, FunctionType.StringConst), !en.MoveNext());
		}

		public static (Function, bool) CompileNumber(TokenEnumerator en)
		{
			var success = double.TryParse(en.Current.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double dblResult);
			if (!success)
				throw new CompilerException();

			var result = (object)dblResult;

			return (new Function((state, _) => result, FunctionType.NumberConst), !en.MoveNext());
		}

		public static Func<Task<object>> Compile(State state, IEnumerable<Token> tokens)
		{
			var en = new TokenEnumerator(tokens.GetEnumerator());
			var (functions, eof) = Compiler.CompileStatements(en, false);
			return async () =>
			{
				object result = null;
				foreach (var ix in functions)
				{
					result = ix.IsAsync ? await ix.InvokeAsync(state, null) : ix.Invoke(state, null);
				}
				return result;
			};
		}

		public static Func<object> Compile(IEnumerable<Token> tokens)
		{
			var state = new State();
			StandardLibrary.Bootstrap(state);
			ListFunctions.Bootstrap(state);
			MathFunctions.Bootstrap(state);
			return () => Compile(state, tokens)().Result; //rewrite this, very slow
		}

		public static Func<Task<object>> CompileAsync(IEnumerable<Token> tokens)
		{
			var state = new State();
			StandardLibrary.Bootstrap(state);
			ListFunctions.Bootstrap(state);
			MathFunctions.Bootstrap(state);
			return Compile(state, tokens);
		}

		static (Function, bool) CompileStatement(TokenEnumerator en)
		{
			// Checking type of token, and acting accordingly.

			switch (en.Current.ToString())
			{
				case "[":
					return CompileList(en);
				case "{":
					return CompileLambda(en);
				case "'":
					return CompileString(en);
				case "if":
					return CompileIf(en);
				// case "true":
				// case "false":
				// case "null":

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
		static (Function, bool) CompileLambda(TokenEnumerator en)
		{
			// Compiling body, and retrieving functions.
			var (functions, eof) = CompileStatements(en);

			Function function;
			if (functions.Count == 1)
			{
				// Avoid unnecessary wrapping if only 1 statement
				function = functions[0];
			}
			else
			{
				/*
				* Creating a function that evaluates every function sequentially, and
				* returns the result of the last function evaluation to the caller.
				*/
				function = new Function(async (state, _) =>
				{
					object result = null;
					foreach (var ix in functions)
					{
						result = ix.IsAsync ? await ix.InvokeAsync(state, null) : ix.Invoke(state, null);
					}
					return result;
				}, FunctionType.Lambda);
			}

			var lazyFunction = new Function((state, _) => function, FunctionType.Lazy);
			return (lazyFunction, eof || !en.MoveNext());
		}

		/*
         * Compiles a lambda segment, which might be the root level
         * content of a Lizzie lambda object, or the stuff between '{' and '}'.
         *
         * If "forceClose" is true, it will expect a brace '}' to end the lambda segment,
         * and throw an exception if it finds EOF before it finds the closing '}'.
         */
		static (List<Function>, bool) CompileStatements(TokenEnumerator en, bool forceClose = true)
		{
			// Creating a list of functions and returning these to caller.
			var functions = new List<Function>();
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

		static (Function, bool) CompileList(TokenEnumerator en)
		{
			var items = new List<Function>();

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
			return (new Function(async (state, _) =>
			{
				var list = new List<object>();
				foreach (var ix in items)
				{
					var item = ix.IsAsync ? await ix.InvokeAsync(state, null) : ix.Invoke(state, null);
					list.Add(item);
				}
				return list;
			}, FunctionType.ListConst), !en.MoveNext());
		}

		static (Function, bool) CompileChainedCall(TokenEnumerator en, Function sf)
		{
			// Used to hold arguments before they're being applied inside of function evaluation.
			var arguments = new List<Function>();

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
			}

			return (new Function(async (state, _) =>
			{
				var args = new object[arguments.Count];
				for (var a = 0; a < arguments.Count; a++)
				{
					var value = arguments[a].IsAsync ? await arguments[a].InvokeAsync(state, null) : arguments[a].InvokeAsync(state, null);
					args[a] = value;
				}

				var result = await sf.InvokeAsync(state, null);
				if (result is Function c)
				{
					//if (sf.AsyncFunction)
					return await state.InvokeWithStackAsync(sf, args);
					//return state.InvokeWithStack(sf, args);
				}
				throw new RuntimeException("Unable to invoke result");

			}, FunctionType.Lambda), !en.MoveNext());
		}

		static (Function, bool) CompileIf(TokenEnumerator en)
		{
			var eof = !en.MoveNext();
			if (en.Current != "(")
				throw new CompilerException("If statement expected (");
			eof = !en.MoveNext();
			var (condition, eof_) = CompileStatement(en);

			if (en.Current != ")")
				throw new CompilerException($"if statement: expected ')' but got '{en.Current}' at '{string.Join("", en.Tokens().TakeLast(10))}'");

			eof = !en.MoveNext();
			Function ifLambda = null;
			(ifLambda, eof) = CompileStatement(en);

			Function elseLambda = null;
			if (en.Current == "else")
			{
				eof = !en.MoveNext();
				(elseLambda, eof) = CompileStatement(en);
			}

			return (new Function(async (state, args) =>
			{
				var condResult = condition.IsAsync ? await condition.InvokeAsync(state, args) : condition.Invoke(state, args);
				if (State.Truthy(condResult))
				{
					var l = (ifLambda.IsAsync ? await ifLambda.InvokeAsync(state, args) : ifLambda.Invoke(state, args));
					if (l is Function f)
						return f.IsAsync ? await f.InvokeAsync(state, args) : f.Invoke(state, args);
					return l; // not a lambda expression so return value, e.g if (true) x else y;
				}
				else
				{
					if (elseLambda != null)
					{
						var l = (elseLambda.IsAsync ? await elseLambda.InvokeAsync(state, args) : elseLambda.Invoke(state, args)) as Function;
						if (l is Function f)
							return f.IsAsync ? await f.InvokeAsync(state, args) : f.Invoke(state, args);
						return l; // not a lambda expression so return value, e.g if (true) x else y;
					}
				}
				return null;
			}), eof);
		}

		/*
         * Compiles a symbolic reference down to a function invocation and returns
         * that function to caller.
         */
		static (Function, bool) CompileSymbol(TokenEnumerator en)
		{
			// Retrieving symbol's name and sanity checking it.
			var symbolName = en.Current.ToString();
			//SanityCheckSymbolName(symbolName);

			// Discarding "(" token and checking if we're at EOF.
			var eof = !en.MoveNext();
			var parts = new List<string>();
			parts.Add(symbolName);

			while (!eof && en.Current.ToString() == ".")
			{
				eof = !en.MoveNext();
				parts.Add(en.Current);
				eof = !en.MoveNext();
			}
			// Checking if this is a function invocation.
			if (!eof && en.Current.ToString() == "(")
			{
				if (parts[0] == "list" || parts[0] == "math" || parts[0] == "stopwatch")
					symbolName = parts[0] + "." + parts[1];
				// Function invocation, making sure we apply arguments,
				//return ApplyArguments<TContext>(symbolName, en);
				var x = (ApplyArguments(symbolName, en));
				if (en.Current == "(")
				{
					return CompileChainedCall(en, x.Item1);
				}
				return x;
			}
			else if (!eof && en.Current.ToString() == "[")
			{
				// Indexer
				eof = !en.MoveNext();
				var success = int.TryParse(en.Current, out int index);

				if (!success)
					throw new CompilerException("Indexer");

				eof = !en.MoveNext();
				if (en.Current != "]")
					throw new CompilerException($"Expected ], got {en.Current}");

				return (new Function((state, _) =>
				{
					dynamic v = state.GetValue(parts);
					try
					{
						return v[index];
					}
					catch (ArgumentOutOfRangeException ex)
					{
						throw new RuntimeException("Index out of range");
					}
				}, FunctionType.Indexer), !en.MoveNext());
			}
			else if (!eof && en.Current.ToString() == "=")
			{
				// Variable assignment
				en.MoveNext();
				var (value, neof) = CompileStatement(en);
				return (new Function(async (state, _) => await state.SetValueAsync(parts, value), FunctionType.Assignment)
				{ SymbolName = symbolName }, neof);
			}
			else
			{
				// Avoid dictionary lookup
				if (symbolName == "true") return (new Function((state, _) => true), eof);
				if (symbolName == "false") return (new Function((state, _) => false), eof);
				// Func casted needed here to avoid using the async Task<object> overload
				if (symbolName == "null") return (new Function((Func<State, object[], object>)((state, _) => null)), eof);

				if (symbolName == "try")
				{
					throw new CompilerException("try function does not have any arguments. Example: try({ ... }, /* catch */ { ... })");
				}
				return (new Function((state, _) => state.GetValue(parts), FunctionType.Getter)
				{ SymbolName = symbolName }, eof);
			}
		}

		/*
         * Applies arguments to a function invoction, such that they're evaluated at runtime.
         */
		static (Function, bool) ApplyArguments(string symbolName, TokenEnumerator en)
		{
			// Used to hold arguments before they're being applied inside of function evaluation.
			var arguments = new List<Function>();

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

			return (new Function(async (state, _) =>
			{
				var args = new object[arguments.Count];
				for (var a = 0; a < arguments.Count; a++)
				{
					if (symbolName != "var" && symbolName != "const" && symbolName != "set" && symbolName != "inc" && symbolName != "dec")
					{
						var function = arguments[a];
						var value = function.IsAsync ? await function.InvokeAsync(state, null) : function.Invoke(state, null);
						args[a] = value;
					}
					else
					{
						args[a] = arguments[a];
					}
				}
				var (func, found) = state.Resolve(symbolName);
				if (!found)
					throw new RuntimeException($"Cannot resolve symbol {symbolName}");

				var f = func as Function;
				return f.IsAsync ? await f.InvokeAsync(state, args) : f.Invoke(state, args);
			}, FunctionType.InvocationWithArgs), !en.MoveNext());
		}
	}
}
