using System;

internal class StartupHook
{
    public static void Initialize()
    {
        Initialize(123);
    }

    public static void Initialize(int input)
    {
        Console.WriteLine("Hello from startup hook with overload! Input: " + input);
    }
}
