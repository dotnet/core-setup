using System;

internal class StartupHook
{
    public static int Initialize()
    {
        Console.WriteLine("Hello from startup hook returning int!");
        return 10;
    }
}
