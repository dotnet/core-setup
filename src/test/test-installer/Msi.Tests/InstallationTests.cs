// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Collections.Generic;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Msi.Tests
{
    public class InstallationTests 
    {
        private ExeManager _exeMgr;

        public InstallationTests()
        {
	    // test assume that the exe to be tested is available via environment variable %RUNTIME_EXE%
            var exeFile = Environment.GetEnvironmentVariable("RUNTIME_EXE");
	    if(string.IsNullOrEmpty(exeFile))
            {
                throw new InvalidOperationException("%RUNTIME_EXE% must point to the exe that is to be tested");
            }
	    _exeMgr = new ExeManager(exeFile);
        } 

	public static IEnumerable<object[]> ListMsiMgr(){

		// test assume that the list of msi to be tested is available via environment variable %MSI_LIST%
		var msiList = Environment.GetEnvironmentVariable("MSI_LIST");
	        if(string.IsNullOrEmpty(msiList))
      		{
                	throw new InvalidOperationException("%MSI_LIST% must point to the list of msi that is to be tested");
            	}
		string[] lines = System.IO.File.ReadAllLines(msiList);
		foreach( string msi in lines){
		    yield return new object[] { new MsiManager(msi) }; 
		}
	}

	[Theory]
	[MemberData(nameof(ListMsiMgr))]
	public void MsiInstallTest(MsiManager msiMgr){
		InstallTest(msiMgr, _exeMgr);
	}

	public void InstallTest(MsiManager _msiMgr, ExeManager _exeMgr)
        {
            // make sure that the msi is not already installed, if so the machine is in a bad state
            Assert.False(_msiMgr.IsInstalled, "The dotnet CLI msi is already installed");

            _exeMgr.Install();
            Assert.True(_msiMgr.IsInstalled);

            _exeMgr.UnInstall();
            Assert.False(_msiMgr.IsInstalled);
        }

   }
}
