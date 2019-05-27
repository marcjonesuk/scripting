/*
 * Copyright (c) 2018 Thomas Hansen - thomas@gaiasoul.com
 *
 * Licensed under the terms of the MIT license, see the enclosed LICENSE
 * file for details.
 */

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace scriptlang
{
	public class LizzieTokenizerException : Exception
	{
		public LizzieTokenizerException(string message) : base(message)
		{
		}
	}

	/*
	 * Copyright (c) 2018 Thomas Hansen - thomas@gaiasoul.com
	 *
	 * Licensed under the terms of the MIT license, see the enclosed LICENSE
	 * file for details.
	 */


	/// <summary>
	/// Common tokenizer interface, in case you want to implement your own tokenizer,
	/// and override the default implementation, that expects Lizzie code.
	/// 
	/// If you do, you'll probably also want to implement your own Compiler class.
	/// If you implement your own tokenizer and compiler, you can still take
	/// advantage of the helper methods found in the generic Tokenizer class.
	/// </summary>
	public interface ITokenizer
	{
		/// <summary>
		/// Returns the next token available from the specified reader.
		///
		/// Will return null if no more tokens are found, and EOF have been
		/// encountered.
		/// </summary>
		/// <returns>The next token found in the reader, if any.</returns>
		/// <param name="reader">Reader to read tokens from.</param>
		string Next(StreamReader reader);
	}
	/// <summary>
	/// The main Lizzie tokenizer, that will tokenize Lizzie code, which you can
	/// then use as input for the compiler that compiles a lambda object from your
	/// code.
	/// </summary>
	public class LizzieTokenizer : ITokenizer
	{
		/*
         * Occassionally we might need to read more than one token ahead, at which point we
         * store these tokens in this stack, and returns them once the next token is requested.
         */
		readonly Stack<string> _cachedTokens = new Stack<string>();

		/// <summary>
		/// Gets or sets the maximum size of strings the tokenizer will allow  before throwing an exception.
		/// </summary>
		/// <value>The size of the max string.</value>
		public int MaxStringSize { get; set; } = -1;

		#region [ -- Interface implementation -- ]

		/// <summary>
		/// Retrieves the next token from the specified reader.
		/// </summary>
		/// <returns>The next token from the reader.</returns>
		/// <param name="reader">Reader to retrieve token from.</param>
		public string Next(StreamReader reader)
		{
			// Checking if we have cached tokens.
			if (_cachedTokens.Count > 0)
				return _cachedTokens.Pop(); // Returning cached token and popping it off our stack.

			// Eating white space from stream.
			Tokenizer.EatSpace(reader);
			if (reader.EndOfStream)
				return null; // No more tokens.

			// Finding next token from reader.
			string retVal = null;
			while (!reader.EndOfStream)
			{

				// Peeking next character in stream, and checking its classification.
				var ch = (char)reader.Peek();
				if (ch == ';')
					ch = ' ';

				switch (ch)
				{
					/*
                     * End of token characters.
                     */

					case ' ':
					case '\r':
					case '\n':
					case '\t':

						/*
                         * This is the end of our token.
                         *
                         * Notice, since we start out method by eating white space,
                         * purely logically "retVal" must contain something now.
                         */
						return retVal;

					/*
                     * Single character tokens.
                     */

					case '@':
					case ',':
					case '(':
					case ')':
					case '[':
					case ']':
					case '{':
					case '}':
					case '=':

						if (retVal == null)
						{

							// This is our token.
							return ((char)reader.Read()).ToString();

						}
						else
						{

							// This is the end of our token.
							return retVal;
						}

					/*
                     * String literal token.
                     */

					case '"':
					case '\'':

						reader.Read(); //  Skipping '"'.
						var strLiteral = Tokenizer.ReadString(reader, ch, MaxStringSize);

						/*
                         * This time we need to cache our tokens for future invocations.
                         * 
                         * Notice, we need to push tokens in reversed order (LIFO).
                         */
						_cachedTokens.Push(ch == '\'' ? "'" : "\"");
						_cachedTokens.Push(strLiteral);
						return ch == '\'' ? "'" : "\"";

					/*
                     * Possible single line comment token.
                     */

					case '/':

						reader.Read(); // Discarding "/" first.
						ch = (char)reader.Peek();
						if (ch == '/')
						{

							// Single line comment.
							Tokenizer.EatLine(reader);

							// There might be some spaces at the front of our stream now ...
							Tokenizer.EatSpace(reader);

							// Checking if we currently have a token.
							if (retVal != null)
								return retVal;

						}
						else if (ch == '*')
						{

							// Multiline comment, making sure we discard opening "*" character from stream.
							reader.Read();
							Tokenizer.EatUntil(reader, "*/", true);

							// There might be some spaces at the front of our stream now ...
							Tokenizer.EatSpace(reader);

							// Checking if we currently have a token.
							if (!string.IsNullOrEmpty(retVal))
								return retVal;

						}
						else
						{

							// Returning '/' as a token.
							return "/";
						}
						break;

					/*
                     * Default, simply appending character to token buffer.
                     */

					default:

						// Eating next character, and appending to retVal.
						retVal += (char)reader.Read();
						break;
				}
			}
			return retVal;
		}

		#endregion
	}
	/// <summary>
	/// Main tokenizer instance, used as input to the compilation process.
	///
	/// If you implement your own tokenizer, you might benefit from taking
	/// advantage of someof the static methods in this class.
	/// </summary>
	public class Tokenizer
	{
		readonly ITokenizer _tokenizer;

		/// <summary>
		/// Creates a new tokenizer instance that is used as input to the compiler.
		/// </summary>
		/// <param name="tokenizer">Tokenizer implementation, normally an instance of the LizzieTokenizer class.</param>
		public Tokenizer(ITokenizer tokenizer)
		{
			// Not passing in a tokenizer is a logical runtime error!
			_tokenizer = tokenizer ?? throw new LizzieTokenizerException("No tokenizer implementation given to tokenizer.");
		}

		/// <summary>
		/// Main method invoked by the compiler to request tokens from a stream.
		/// </summary>
		/// <returns>Each token found in your code.</returns>
		/// <param name="stream">Stream containing Lizzie code. Notice, this method does not claim ownership over
		/// your stream, and you are responsible for correctly disposing it yourself.</param>
		/// <param name="encoding">Encoding to use for stream, if not given this defaults to UTF8.</param>
		public IEnumerable<string> Tokenize(Stream stream, Encoding encoding = null)
		{
			// Notice! We do NOT take ownership over stream!
			StreamReader reader = new StreamReader(stream, encoding ?? Encoding.UTF8, true, 1024);
			while (true)
			{
				var token = _tokenizer.Next(reader);
				if (token == null)
					break;
				yield return token;
			}
			yield break;
		}

		/// <summary>
		/// Main method invoked by the compiler to request tokens from a single string.
		/// </summary>
		/// <returns>Each token found in your code.</returns>
		/// <param name="code">Code to tokenize.</param>
		public IEnumerable<Token> Tokenize(string code)
		{
			var lines = code.Split("\n");

			using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(code)))
			{
				foreach (var ix in Tokenize(stream))
				{
					yield return new Token(ix);
				}
			}
		}

		/// <summary>
		/// Eats and discards all whitespace characters found at the beginning of your reader.
		/// 
		/// A white space character is any of the following characters; ' ', '\t',
		/// '\r' and '\n'.
		/// </summary>
		/// <param name="reader">Reader to eat whitespace characters from.</param>
		public static void EatSpace(StreamReader reader)
		{
			while (!reader.EndOfStream)
			{
				var ch = (char)reader.Peek();
				if (ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n' || ch == ';')
				{
					reader.Read();
					continue;
				}
				break;
			}
		}

		/// <summary>
		/// Eats and discards the rest of the line from your reader.
		/// </summary>
		/// <param name="reader">Reader to eat the rest of the lines from.</param>
		public static void EatLine(StreamReader reader)
		{
			reader.ReadLine();
		}

		/// <summary>
		/// Eats and discards characters from the reader until the specified sequence is found.
		/// </summary>
		/// <param name="reader">Reader to eat from.</param>
		/// <param name="sequence">Sequence to look for that will end further eating.</param>
		/// <param name="throwIfNotFound">If true, will throw an exception if end sequence is not found before end of stream.</param>
		public static void EatUntil(StreamReader reader, string sequence, bool throwIfNotFound = false)
		{
			// Sanity checking invocation.
			if (string.IsNullOrEmpty(sequence))
				throw new LizzieTokenizerException("No stop sequence specified to EatUntil.");

			/*
             * Not sure if this is the optimal method to do this, but I think it
             * shouldn't be too far away from optimal either ...
             */
			var buffer = new List<char>(sequence.Length + 1);
			while (!reader.EndOfStream)
			{
				buffer.Add((char)reader.Read());
				if (buffer.Count > sequence.Length)
				{
					buffer.RemoveAt(0);
				}
				if (buffer[0] == sequence[0])
				{
					if (sequence == new string(buffer.ToArray()))
						return; // Done!
				}
			}

			// Sanity checking that stream is not corrupted, if we're told to do so.
			if (throwIfNotFound)
				throw new LizzieTokenizerException($"The '{sequence}' sequence was not found before EOF.");
		}

		/// <summary>
		/// Reads a single line string literal from the reader, escaping characters if necessary,
		/// and also supporting UNICODE hex syntax to reference UNICODE characters.
		/// </summary>
		/// <returns>The string literal.</returns>
		/// <param name="reader">Reader to read from.</param>
		/// <param name="stop">Stop what character that ends the string.</param>
		/// <param name="maxStringSize">The maximum sise of strings the tokenizer accepts before throwing a Lizzie exception.</param>
		public static string ReadString(StreamReader reader, char stop = '"', int maxStringSize = -1)
		{
			var builder = new StringBuilder();
			for (var c = reader.Read(); c != -1; c = reader.Read())
			{
				switch (c)
				{
					case '\\':
						builder.Append(GetEscapedCharacter(reader, stop));
						break;
					case '\n':
					case '\r':
						throw new LizzieTokenizerException($"String literal contains CR or LF characters close to '{builder.ToString()}'.");
					default:
						if (c == stop)
							return builder.ToString();
						if (maxStringSize != -1 && builder.Length >= maxStringSize)
							throw new LizzieTokenizerException($"String size exceeded maximum allowed size of '{maxStringSize}' characters.");
						builder.Append((char)c);
						break;
				}
			}
			throw new LizzieTokenizerException($"Syntax error, string literal not closed before EOF near '{builder.ToString()}'");
		}

		/*
         * Returns escape character.
         */
		static string GetEscapedCharacter(StreamReader reader, char stop)
		{
			var ch = reader.Read();
			if (ch == -1)
				throw new LizzieTokenizerException("EOF found before string literal was closed");
			switch ((char)ch)
			{
				case '\\':
					return "\\";
				case 'a':
					return "\a";
				case 'b':
					return "\b";
				case 'f':
					return "\f";
				case 't':
					return "\t";
				case 'v':
					return "\v";
				case 'n':
					return "\n";
				case 'r':
					return "\r";
				case 'x':
					return HexCharacter(reader);
				default:
					if (ch == stop)
						return stop.ToString();
					throw new LizzieTokenizerException($"Invalid escape sequence character '{Convert.ToInt32(ch)}' found in string literal");
			}
		}

		/*
         * Returns hex encoded character.
         */
		static string HexCharacter(StreamReader reader)
		{
			var hexNumberString = "";
			for (var idxNo = 0; idxNo < 4; idxNo++)
			{
				var tmp = reader.Read();
				if (tmp == -1)
					return ""; // Incomplete hex char ...!!
				hexNumberString += (char)tmp;
			}
			var integerNo = Convert.ToInt32(hexNumberString, 16);
			return Encoding.UTF8.GetString(BitConverter.GetBytes(integerNo).Reverse().ToArray());
		}
	}
}
