using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using scriptlang;

namespace scriptlang.tests
{
	[TestClass]
	public class ArgsTests
	{
		[TestMethod]
		public void Test()
		{
			object[] args = new object[1];
			args[0] = "10";
			var p = ArgsHelper.Expect<int>("test", args[0]);
			Assert.AreEqual(10, p);
		}
	}
}