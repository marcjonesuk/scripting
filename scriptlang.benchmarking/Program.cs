﻿using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using scriptlang;

namespace MyBenchmarks
{
	[CoreJob]
	public class Benchmarks
	{
		private Func<Task<object>> ifStatement;
		private Func<Task<object>> constant;
		private Func<Task<object>> lists;
		private Func<Task<object>> stringConstant;

		[GlobalSetup]
		public void Setup()
		{
			var tokenizer = new Tokenizer(new LizzieTokenizer());
			constant = Compiler.CompileAsync(tokenizer.Tokenize("true")); //82ns
			ifStatement = Compiler.CompileAsync(tokenizer.Tokenize("if(true) { true } else { false }")); // 164ns
			lists = Compiler.CompileAsync(tokenizer.Tokenize("y = list.new(1, 2);list.length(y)")); // 894ns
			stringConstant = Compiler.CompileAsync(tokenizer.Tokenize("'hello, world'")); // 894ns
		}

		// [Benchmark]
		// public void IfStatement()
		// {
		// 	var t = ifStatement().Result;
		// }

		// [Benchmark]
		// public void Constant()
		// {
		// 	var t = constant().Result;
		// }

		[Benchmark]
		public void StringConstant()
		{
			var t = stringConstant().Result;
		}

		// [Benchmark]
		// public void Lists()
		// {
		// 	var t = lists().Result;
		// }
	}

	public class Program
	{
		public static void Main(string[] args)
		{
			var summary = BenchmarkRunner.Run<Benchmarks>();
		}
	}
}