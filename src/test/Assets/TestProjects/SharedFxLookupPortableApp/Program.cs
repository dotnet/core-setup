using System;
using System.Reflection;

namespace SharedFxLookupPortableApp
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Console.WriteLine(string.Join(Environment.NewLine, args));

            Console.WriteLine($"Framework Version:{GetFrameworkVersionFromAppDomain()}");
			
			// A small operation involving NewtonSoft.Json to ensure the assembly is loaded properly
            var t = typeof(Newtonsoft.Json.JsonReader);
        }
		
        public static string GetFrameworkVersionFromAppDomain()
        {
            Type appDomainType = typeof(object).GetTypeInfo().Assembly.GetType("System.AppDomain");
            object currentDomain = appDomainType.GetProperty("CurrentDomain").GetValue(null);
            object fxVersion = appDomainType.GetMethod("GetData").Invoke(currentDomain, new[] {"FX_PRODUCT_VERSION"});

            return fxVersion as string;
        }		
    }
}
