using Microsoft.DotNet.Cli.Build;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public class RepoDirectoriesProvider
    {
        private string _repoRoot;
        private string _artifacts;
        private string _hostArtifacts;
        private string _builtDotnet;
        private string _nugetPackages;
        private string _corehostPackages;
        private string _corehostDummyPackages;
        private string _dotnetSDK;

        private string _targetRID;

        public string TargetRID => _targetRID;
        public string RepoRoot => _repoRoot;
        public string Artifacts => _artifacts;
        public string HostArtifacts => _hostArtifacts;
        public string BuiltDotnet => _builtDotnet;
        public string NugetPackages => _nugetPackages;
        public string CorehostPackages => _corehostPackages;
        public string CorehostDummyPackages => _corehostDummyPackages;
        public string DotnetSDK => _dotnetSDK;

        public RepoDirectoriesProvider(
            string repoRoot = null,
            string artifacts = null,
            string builtDotnet = null,
            string nugetPackages = null,
            string corehostPackages = null,
            string corehostDummyPackages = null,
            string dotnetSdk = null)
        {
            _repoRoot = repoRoot ?? Directory.GetParent((Directory.GetCurrentDirectory())).Parent.FullName;

            string baseArtifactsFolder = artifacts ?? Path.Combine(_repoRoot, "bin");

            _dotnetSDK = dotnetSdk ?? Path.Combine(_repoRoot, "Tools", "dotnetcli");

            _targetRID = Environment.GetEnvironmentVariable("TEST_TARGETRID");

            _artifacts = Path.Combine(baseArtifactsFolder, _targetRID+".Debug");
            if(!Directory.Exists(_artifacts))
                _artifacts = Path.Combine(baseArtifactsFolder, _targetRID+".Release");

            _hostArtifacts = artifacts ?? Path.Combine(_artifacts, "corehost");

            _nugetPackages = nugetPackages ?? Path.Combine(_repoRoot, "packages");

            _corehostPackages = corehostPackages ?? Path.Combine(_artifacts, "corehost");

            _corehostDummyPackages = corehostDummyPackages ?? Path.Combine(baseArtifactsFolder, "packages");

            _builtDotnet = builtDotnet ?? Path.Combine(baseArtifactsFolder, "obj", _targetRID+".Debug", "sharedFrameworkPublish");
            if(!Directory.Exists(_builtDotnet))
                _builtDotnet = builtDotnet ?? Path.Combine(baseArtifactsFolder, "obj", _targetRID+".Release", "sharedFrameworkPublish");
        }
    }
}
