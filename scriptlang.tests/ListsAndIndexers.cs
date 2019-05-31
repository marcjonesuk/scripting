using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using scriptlang;

namespace scriptlang.tests
{
	[TestClass]
	public class ListsAndIndexers : UnitTest1
	{
		[TestMethod]
		public void Indexers()
		{
			Test("l = [1,2]; l[0]", 1.0);
			Test("l = [1,2]; l[1]", 2.0);

			// TODO:
			// Test("y = { [1,2] }; y()[1]", 2.0);

			Test("l = [1,'hello, world']; l[1]", "hello, world");
		}

		[TestMethod]
		public void Indexers_Errors()
		{
			TestThrows<RuntimeException>("l = [1,2]; l[2]");
			TestThrows<RuntimeException>("l = [1,2]; l[-1]");
		}

		[TestMethod]
		public void Lists()
		{
			Test("y = [3,5,8,'a',-1]; list.push(y, 1); list.length(y)", 6);
			// Test("y = [3,5,8]; clear(y); len(y)", 0);
			// Test("y = list.new(1, 2); list.push(y, 1, 2, 3, -10, -9); list.length(y)", 7);
			// Test("y = [3,5,8]; list.indexOf(y, 5)", 1);
			// Test("y = [3,5,8]; list.indexOf(y, 7)", -1);
			// Test("var(y); y = [3,5,8,12]; len(list.splice(1, 2));", 2);
		}
	}
}