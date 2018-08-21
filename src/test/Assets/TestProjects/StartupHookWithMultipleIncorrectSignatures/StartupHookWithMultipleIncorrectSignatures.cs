using System;

internal class StartupHook
{
    public static int Initialize()
    {
        Console.WriteLine("Hello from startup hook returning int!");
        return 10;
    }

    public static void Initialize(int input)
    {
        Console.WriteLine("Hello from startup hook taking int! Input: " + input);
    }
}
