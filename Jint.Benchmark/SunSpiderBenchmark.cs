﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Jint;

namespace Esprima.Benchmark
{
    [Config(typeof(Config))]
    [MemoryDiagnoser]
    public class SunSpiderBenchmark
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                // if Jint array performance gets better we can go towards defaul 16/16
                Add(Job.ShortRun.WithInvocationCount(4).WithUnrollFactor(4));
            }
        }

        private static readonly Dictionary<string, string> files = new Dictionary<string, string>
        {
            {"3d-cube", null},
            {"3d-morph", null},
            {"3d-raytrace", null},
            {"access-binary-trees", null},
            {"access-fannkuch", null},
            {"access-nbody", null},
            {"access-nsieve", null},
            {"bitops-3bit-bits-in-byte", null},
            {"bitops-bits-in-byte", null},
            {"bitops-bitwise-and", null},
            {"bitops-nsieve-bits", null},
            {"controlflow-recursive", null},
            {"crypto-aes", null},
            {"crypto-md5", null},
            {"crypto-sha1", null},
            {"date-format-tofte", null},
            {"date-format-xparb", null},
            {"math-cordic", null},
            {"math-partial-sums", null},
            {"math-spectral-norm", null},
            {"regexp-dna", null},
            {"string-base64", null},
            {"string-fasta", null},
            {"string-tagcloud", null},
            {"string-unpack-code", null},
            {"string-validate-input", null}
        };

        private Engine engine;

        [GlobalSetup]
        public void Setup()
        {
            foreach (var fileName in files.Keys.ToList())
            {
                files[fileName] = File.ReadAllText($"SunSpider/{fileName}.js");
            }

            engine = new Engine()
                .SetValue("log", new Action<object>(Console.WriteLine))
                .SetValue("assert", new Action<bool>(b => { }));
        }

        [ParamsSource(nameof(FileNames))]
        public string FileName { get; set; }

        public IEnumerable<string> FileNames()
        {
            foreach (var entry in files)
            {
                yield return entry.Key;
            }
        }

        [Benchmark]
        public void Run()
        {
            engine.Execute(files[FileName]);
        }
    }
}