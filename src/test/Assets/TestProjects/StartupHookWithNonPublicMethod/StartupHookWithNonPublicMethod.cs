using System;

internal class StartupHook
{
    static void Initialize()
    {
        Console.WriteLine("Hello from startup hook with non-public method!");
    }
}
