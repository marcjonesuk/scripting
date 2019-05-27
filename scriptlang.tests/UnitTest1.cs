using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using scriptlang;

namespace scriptlang.tests
{
	[TestClass]
	public class UnitTest1
	{
		public void Test(string code, object expected)
		{
			var tokenizer = new Tokenizer(new LizzieTokenizer());
			var func = Compiler.Compile(tokenizer.Tokenize(code));
			Assert.AreEqual(expected, func());
		}

		[TestMethod]
		public void Numbers()
		{
			Test("10", 10.0);
			Test("-10", -10.0);
			Test("-10.9012", -10.9012);
			Test("106223.23423443", 106223.23423443);
		}

		[TestMethod]
		public void Keywords()
		{
			Test("null", null);
			Test("false", false);
			Test("true", true);
		}

		[TestMethod]
		public void Strings()
		{
			Test("'hello, world'", "hello, world");
			Test("''", "");
		}

		[TestMethod]
		public void Variables()
		{
			Test("var(x); x", null);
			Test("var(x, true); x", true);
			Test("var(x, false); x", false);
			Test("var(x, -100.0); x", -100.0);
		}

		[TestMethod]
		public void Variables_sets()
		{
			Test("var(x); set(x, 199); x", 199.0);
			Test("var(x, true); set(x, null); x", null);
			Test("var(x, true); set(x, null); set(x, 'hello, world'); x", "hello, world");
		}

		[TestMethod]
		public void Add_Inc_Dec()
		{
			Test("var(x, 10.5); inc(x)", 11.5);
			Test("var(x, 10.5); inc(x)", 11.5);
			Test("var(x, -100.5); inc(x); inc(x)", -98.5);
			Test("var(x, 10.5); dec(x)", 9.5);
			Test("var(x, -100.5); dec(x); dec(x)", -102.5);
		}

		[TestMethod]
		public void Equality()
		{
			Test("eq(null, false)", false);
			Test("eq(null, true)", false);
			Test("eq(null, 'null')", false);
			Test("eq(null, '')", false);
			Test("var(x); eq(null, x)", true);
			Test("eq(true, true)", true);
			Test("eq(false, false)", true);
			Test("eq(true, 1)", false);
			Test("var(x, 'hello, world'); eq('hello, world', x)", true);
			Test("var(x, 'hello, world'); eq('hello, world ', x)", false);
			Test("eq(1.0, 1)", true);
		}
		
		[TestMethod]
		public void If()
		{
			Test("var(x, false); if(true, { set(x, true) }); x", true);
			Test("var(x, false); if(false, { set(x, true) }); x", false);
			Test("if(true, { 'ok' })", "ok");
			Test("if(eq(null, false), { 5 }, { 10 })", 10.0);
			Test("if('hello, world', 'ok', 'not ok')", "ok");
			Test("var(x, if(false, 'not ok', 'ok')); x", "ok");
		}

		[TestMethod]
		public void Not()
		{
			Test("not(true)", false);
			Test("not(false)", true);
			Test("not(null)", true);
			Test("not('')", true);
			Test("not('hello, world')", false);
		}

		[TestMethod]
		public void Assignments()
		{
			Test("var(x); x = -101.2;", -101.2);
			Test("var(x); x = 5; x", 5.0);
			Test("var(x); if(true, { set(x, 10.1) }); x", 10.1);
			Test("var(x); x = 10.1; x", 10.1);
			Test("var(x); if(true, { x = 10.2 }); x", 10.2);
			Test("var(x); var(y); y = { x = 10.2; true }; y()", true);
		}

		[TestMethod]
		public void Lists()
		{
			var code = "[]";
			var tokenizer = new Tokenizer(new LizzieTokenizer());
			var func = Compiler.Compile(tokenizer.Tokenize(code))();
			Assert.AreEqual(typeof(List<object>), func.GetType());

			code = "[3,5,8]";
			var list = (List<object>)Compiler.Compile(tokenizer.Tokenize(code))();
			Assert.AreEqual(3, list.Count);
			Assert.AreEqual(8.0, list[2]);
		}
	}
}