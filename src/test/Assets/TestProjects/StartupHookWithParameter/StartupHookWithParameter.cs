using System;

internal class StartupHook
{
    public static void Initialize(int input)
    {
        Console.WriteLine("Hello from startup hook taking int! Input: " + input);
    }
}
