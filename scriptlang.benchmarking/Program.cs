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

		[GlobalSetup]
        public void Setup()
        {
			var tokenizer = new Tokenizer(new LizzieTokenizer());
			ifStatement = Compiler.CompileAsync(tokenizer.Tokenize("if(0) { true }"));
			//ifStatement = Compiler.CompileAsync(tokenizer.Tokenize("if(true) { true } else { false }"));
        }

        [Benchmark]
        public void IfStatement() 
		{
			var t = ifStatement().Result;
		}
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<Md5VsSha256>();
        }
    }
}