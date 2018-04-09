using System;
using System.IO;
using Microsoft.Deployment.WindowsInstaller;
using Xunit;
using System.Collections.Generic;

namespace Msi.Tests
{
    public class PropertyTests
    {
	private string _prodVersion;

	public PropertyTests()
	{
	     _prodVersion = Environment.GetEnvironmentVariable("PROD_VERSION");
	}
	
	public static IEnumerable<object[]> ListMsi()
	{

		// test assume that the list of msi to be tested is available via environment variable %MSI_LIST%
                var msiList = Environment.GetEnvironmentVariable("MSI_LIST");
		if(string.IsNullOrEmpty(msiList))
                {
                        throw new InvalidOperationException("%MSI_LIST% must point to the list of msi that is to be tested");
                }
                string[] lines = System.IO.File.ReadAllLines(msiList);
                foreach( string msi in lines){
                    yield return new object[] { msi };
                }
        }

        [Theory]
        [MemberData(nameof(ListMsi))]
        public void ProductNameTest(string msi)
	{
                 using (var database = new Database(msi, DatabaseOpenMode.ReadOnly))
		    {
			    string prodName = database.ExecutePropertyQuery("ProductName");
			    Assert.True(prodName.Contains(_prodVersion), "Different brand name");
		    }
        }

    }
}
