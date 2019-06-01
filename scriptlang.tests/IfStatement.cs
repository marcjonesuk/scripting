using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using scriptlang;

namespace scriptlang.tests
{
	[TestClass]
	public class IfStatement : UnitTest1
	{
		[TestMethod]
		public void If_Else_Simple()
		{
			Test("if(true) { true } else { false }", true);
			Test("if(false) { true } else { false }", false);
			Test("if(true) { 'true' } else { 'false' }", "true");
			Test("if(false) { 'true' } else { 'false' }", "false");	
			Test("x = false; if(true) { x = true }; x", true);
			Test("x = false; if(false) { x = true }; x", false);	
		}

		[TestMethod]
		public void If_Not()
		{
			Test("if(not(false)) { true } else { false }", true);
			Test("if(not(true)) { true } else { false }", false);
		}

		[TestMethod]
		public void If_Lambda()
		{
			Test("y = { 'true' }; if(true) { y() }", "true");	
			Test("x = { 'false' }; y = { 'true' }; if(false) { x() } else { y() }", "true");
			Test("x = false; if(true) { x = true }; x", true);
			Test("y = { true }; if(y()) { 'true' } else { 'false' }", "true");	
		}

		[TestMethod]
		public void Truthy()
		{
			Test("if(null) { true } else { false }", false);
			Test("if(0) true else false", true);
			// Test("if(-1, true, false)", true);
			// Test("if(new(), true, false)", true);
			// Test("if('true', true, false)", true);
			// Test("if('false', true, false)", true);
		}

		[TestMethod]
		public void If_Is_Const()
		{
			// TestThrows<RuntimeException>("if = null");
			// TestThrows<RuntimeException>("if = if");
			// TestThrows<RuntimeException>("if = try");
			// TestThrows<RuntimeException>("if = false");
		}

		[TestMethod]
		public void If_Invalid()
		{
			TestThrows<CompilerException>("if");
			TestThrows<CompilerException>("if { }");
		}
	}
}