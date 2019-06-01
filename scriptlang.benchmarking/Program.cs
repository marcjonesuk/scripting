using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using scriptlang;

namespace MyBenchmarks
{
    [CoreJob]
    [RPlotExporter, RankColumn]
    public class Md5VsSha256
    {
        private SHA256 sha256 = SHA256.Create();
        private MD5 md5 = MD5.Create();
        private byte[] data;


		private Func<Task<object>> ifStatement;
		private Func<Task<object>> constant;
		private Func<Task<object>> lists;

		[GlobalSetup]
        public void Setup()
        {
			var tokenizer = new Tokenizer(new LizzieTokenizer());
			constant = Compiler.CompileAsync(tokenizer.Tokenize("true")); //82ns
			ifStatement = Compiler.CompileAsync(tokenizer.Tokenize("if(true) { true } else { false }")); // 164ns
			lists = Compiler.CompileAsync(tokenizer.Tokenize("y = list.new(1, 2);list.length(y)")); // 894ns
        }

        [Benchmark]
        public void IfStatement() 
		{
			var t = ifStatement().Result;
		}

        // [Benchmark]
        // public void Constant() 
		// {
		// 	var t = constant().Result;
		// }
		
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
            var summary = BenchmarkRunner.Run<Md5VsSha256>();
        }
    }
}