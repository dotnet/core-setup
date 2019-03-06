// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Build.Bundle
{
    /// <summary>
    ///  The main driver for Bundle and Extract operations.
    /// </summary>
    
    public static class Program
    {
        enum RunMode
        {
            Help,
            Bundle,
            Extract
        };

        static RunMode Mode = RunMode.Bundle;

        // Common Options:
        static bool Verbose = false;
        static string OutputDir;

        // Bundle options:
        static bool EmbedPDBs = false;
        static string HostName;
        static string ContentDir;

        // Extract options:
        static string BundleToExtract;

        // Typical usages are:
        // Bundle: bundle -d <publish-dir> -a <host-exe>
        // Extract: bundle -e <single-exe>
        static void Usage()
        {
            Console.WriteLine($".NET Core Bundler ({Bundler.Version})");
            Console.WriteLine("bundle [<mode>] [<options>]");
            Console.WriteLine("where <Mode> is one of:");
            Console.WriteLine("  Embed mode (by default)");
            Console.WriteLine("  Extract mode (triggered by -e)");
            Console.WriteLine("Embed mode options:");
            Console.WriteLine("  -d <path>  Directory containing the files to bundle");
            Console.WriteLine("  -a <name>  Application host (within the content directory)");
            Console.WriteLine(" [-pdb+]     Embed the PDB file");
            Console.WriteLine("Extract mode options:");
            Console.WriteLine("  -e <path>  Path to the bundle file to extract");
            Console.WriteLine("Common options:");
            Console.WriteLine(" [-o <path>] Output directory (default: current)");
            Console.WriteLine(" [-v]        Generate verbose output");
            Console.WriteLine(" [-?]        Display usage information");
        }

        public static void Log(string fmt, params object[] args)
        {
            if (Verbose)
            {
                Console.WriteLine("LOG: " + fmt, args);
            }
        }

        static void Fail(string type, string message)
        {
            Console.Error.WriteLine($"{type}: {message}");
        }

        static void ParseArgs(string[] args)
        {
            int i = 0;
            Func<string, string> NextArg = (string option) =>
            {
                if (++i >= args.Length)
                    throw new BundleException("Argument missing for" + option);
                return args[i];
            };

            for (; i < args.Length; i++)
            {
                string arg = args[i];
                switch (arg.ToLower())
                {
                    case "-?":
                    case "-h":
                        Mode = RunMode.Help;
                        break;

                    case "-e":
                        Mode = RunMode.Extract;
                        BundleToExtract = NextArg(arg);
                        break;

                    case "-v":
                        Verbose = true;
                        break;

                    case "-a":
                        HostName = NextArg(arg);
                        break;

                    case "-d":
                        ContentDir = NextArg(arg);
                        break;

                    case "-o":
                        OutputDir = NextArg(arg);
                        break;

                    case "-pdb+":
                        EmbedPDBs = true;
                        break;
                }
            }

            if (Mode == RunMode.Bundle)
            {
                if (ContentDir == null)
                    throw new BundleException("Missing argument: -d");

                if (HostName == null)
                    throw new BundleException("Missing argument: -a");
            }

            if (OutputDir == null)
                OutputDir = Environment.CurrentDirectory;
        }

        static void Run()
        {
            switch (Mode)
            {
                case RunMode.Help:
                    Usage();
                    break;

                case RunMode.Bundle:
                    Log($"Bundle from dir: {ContentDir}");
                    Log($"Output Directory: {OutputDir}");
                    Bundler bundle = new Bundler(HostName, ContentDir, OutputDir, EmbedPDBs);
                    bundle.MakeBundle();
                    break;

                case RunMode.Extract:
                    Log($"Extract from file: {BundleToExtract}");
                    Log($"Output Directory: {OutputDir}");
                    Extractor extract = new Extractor(BundleToExtract, OutputDir);
                    extract.Spill();
                    break;
            }
        }

        public static int Main(string[] args)
        {
            try
            {
                Log($"Bundler version: {Bundler.Version}");

                try
                {
                    ParseArgs(args);
                }
                catch (BundleException e)
                {
                    Fail("ERROR", e.Message);
                    Usage();
                    return -1;
                }

                try
                {
                    Run();
                }
                catch (BundleException e)
                {
                    Fail("ERROR", e.Message);
                    return -2;
                }
            }
            catch (Exception e)
            {
                Fail("INTERNAL ERROR", e.Message);
                return -3;
            }

            return 0;
        }
    }
}

