using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using scriptlang;

namespace scriptlang.tests
{
    [TestClass]
    public class Literals: UnitTest1
    {
        [TestMethod]
        public void Numbers()
        {
            Test("10", 10.0);
            Test("-10", -10.0);
            Test("-10.9012", -10.9012);
            Test("106223.23423443", 106223.23423443);
        }

        [TestMethod]
        public void Constants()
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
			Test("'null'", "null");
			// Test("'dfkjhfdk\nsfdsdfsdf'", "dfkjhfdk\nsfdsdfsdf");
        }
	}
}