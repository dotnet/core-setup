using System;

namespace StartupHook
{
    public class StartupHookWithDependency
    {
        public static void Initialize()
        {
            Console.WriteLine("Hello from startup hook with dependency!");

            // A small operation involving NewtonSoft.Json to ensure the assembly is loaded properly
            var t = typeof(Newtonsoft.Json.JsonReader);
        }
    }
}
