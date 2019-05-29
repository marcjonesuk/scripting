using System;
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

        public void TestThrows<TException>(string code)
        {
            Exception ex = null;
            try
            {
                var tokenizer = new Tokenizer(new LizzieTokenizer());
                var func = Compiler.Compile(tokenizer.Tokenize(code));
                func();
            }
            catch (Exception e)
            {
                ex = e;
            }
            Assert.IsNotNull(ex, "Did not throw exception");
            Assert.AreEqual(typeof(TException), ex.GetType());
        }

        [TestMethod]
        public void Numbers()
        {
            //Test("10", 10.0);
            //Test("-10", -10.0);
            Test("-10.9012", -10.9012);
            //Test("106223.23423443", 106223.23423443);
        }

        [TestMethod]
        public void Keywords()
        {
            Test("null", null);
            Test("false", false);
            Test("true", true);
        }

        [TestMethod]
        public void New()
        {
            Test("len(props(new()))", 0);
            Test("first = new(); first.x = 100; first.y = 'hello, world'; j = json(first); second = new(j); second.y", "hello, world");
            Test("l = [1,2]; k = json(l); len(new(k))", 2);
            // Test("p = new(); p.age = 100; l = []; push(l, p); new(json(l)); ", );
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
            Test("x = null; x", null);
            Test("x = true; x", true);
            Test("x = false; x", false);
            Test("x = -100.0; x", -100.0);
        }

        // [TestMethod]
        // public void Const()
        // {
        // 	TestThrows<RuntimeException>("const(x); x");
        // 	TestThrows<RuntimeException>("const(x, 5.0); x = 6.0");
        // 	Test("const(x, 5.0)", 5.0);
        // 	Test("const(x, 5.0); x", 5.0);
        // 	Test("const(x, 5.0); try({ x = 6.0}); x", 5.0);
        // 	TestThrows<RuntimeException>("if = { }");
        // 	TestThrows<RuntimeException>("try = { }");
        // 	TestThrows<RuntimeException>("set = { }");
        // 	TestThrows<RuntimeException>("var = { }");
        // }

        [TestMethod]
        public void Variables_sets()
        {
            Test("var(x); set(x, 199); x", 199.0);
            Test("x = null; x", null);
            Test("var(x, true); set(x, null); set(x, 'hello, world'); x", "hello, world");
        }

        [TestMethod]
        public void Add_Inc_Dec()
        {
            // Test("var(x, 10.5); inc(x)", 11.5);
            // Test("var(x, 10.5); inc(x)", 11.5);
            // Test("var(x, -100.5); inc(x); inc(x)", -98.5);
            // Test("var(x, 10.5); dec(x)", 9.5);
            // Test("var(x, -100.5); dec(x); dec(x)", -102.5);
        }

        [TestMethod]
        public void Equality()
        {
            Test("eq(null, false)", false);
            Test("eq(null, true)", false);
            Test("eq(null, 'null')", false);
            Test("eq(null, '')", false);
            Test("x = null; eq(null, x)", true);
            Test("eq(true, true)", true);
            Test("eq(false, false)", true);
            Test("eq(true, 1)", false);
            Test("x = 'hello, world'; eq('hello, world', x)", true);
            Test("x = 'hello, world'; eq('hello, world ', x)", false);
            Test("eq(1.0, 1)", true);
        }

        [TestMethod]
        public void If()
        {
            Test("x = false; if(true, { x = true }); x", true);
            Test("x = false; if(false, { set(x, true) }); x", false);
            Test("if(true, { 'ok' })", "ok");
            Test("if(eq(null, false), { 5 }, { 10 })", 10.0);
            Test("if('hello, world', 'ok', 'not ok')", "ok");
            // Test("var(x, if(false, 'not ok', 'ok')); x", "ok");
            TestThrows<CompilerException>("if");
            TestThrows<CompilerException>("if { }");
            TestThrows<RuntimeException>("if = null");
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
            Test("x = -101.2;", -101.2);
            Test("x = 5; x", 5.0);
            Test("if(true, { x = 10.1 }); x", 10.1);
            Test("x = 10.1; x", 10.1);
            Test("if(true, { x = 10.2 }); x", 10.2);
            Test("y = { x = 10.2; true }; not(y())", false);
        }

        [TestMethod]
        public void Try_catch()
        {
            // Test("try({ throw('error') })", null);
            Test("try({ throw('error') }, { true })", true);
            Test("try({ throw('error') }, { args(0) })", "error");
            // Test("var(x); try({ x = 19; 15 }, { true })", 15.0);
            // TestThrows<CompilerException>("try");
            // TestThrows<CompilerException>("try { }");
        }

        [TestMethod]
        public void Lambda_expression()
        {
            //Test("y = { inc(args(0)) }; y(100)", 101.0);
            Test("y = { z = { add(args(0), args(1)) }; z(args(0), 20) }; y(10)", 30.0);
            Test("y = { z = { 10 } }; y()()", 10.0);
            Test("y = { z = { add(10, args(0)) } }; y()(10)", 20.0);
            //Test("y = { a = new(); a.name = args(0); a }; y('bob')");
            Test("x = { add(args(0), 20) }; y = { args(0)(10) }; y(x)", 30.0);
            Test("y = { args(0)(10) }; y({ add(args(0), 20) })", 30.0); // anonymous invocation gets args
        }

        [TestMethod]
        public void Objects()
        {
            //Test("x = new(); x.y = 'hello, world'; len(props(x));", 1);
            Test("x = new(); x.y = 'hello, world'; x.y;", "hello, world");
            TestThrows<RuntimeException>("x = new(); x.y = 'hello, world'; x.z;");
            TestThrows<RuntimeException>("x = new(); x.y.z = 'hello, world'");
        }

        [TestMethod]
        public void Todo()
        {
            // objects
            //Test("x = new(); x.y = 'hello, world'; len(props(x));", 1);

            // integers 
            // Test("1", 1);

            // string interpolation
            // Test("x = 'hello'; '{x}, world'", "hello, world");

			// function variable scope
			Test("v = 'test'; f = { v = 'hello, world' }; f(); v", "test");
        }

 		[TestMethod]
        public void Scope()
        {     
			Test("v = 'test'; f = { v = 'hello, world' }; f(); v", "test");
			Test("v = 'test'; f = { v = 'hello, world' { v = 'yoyoyo' } }; y = f()(); v", "test");
		}

        [TestMethod]
        public void Indexers()
        {           
            Test("l = [1,2]; l[1]", 2);
            Test("l = [1,'hello, world']; l[1]", "hello, world");
            TestThrows<RuntimeException>("l = [1,2]; l[2]");
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

            Test("var(y); y = [3,5,8,'a',-1]; list.push(y, 1); list.length(y)", 6);
            Test("var(y); y = [3,5,8]; clear(y); len(y)", 0);
            Test("var(y); y = list.new(1, 2); list.push(y, 1, 2, 3, -10, -9); list.length(y)", 7);
            Test("var(y); y = [3,5,8]; list.indexOf(y, 5)", 1);
            Test("var(y); y = [3,5,8]; list.indexOf(y, 7)", -1);
            // Test("var(y); y = [3,5,8,12]; len(list.splice(1, 2));", 2);
        }

        [TestMethod]
        public void Arguments()
        {
            Test("y = { args(0) }; y(10)", 10.0);
        }
    }
}