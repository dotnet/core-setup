using System;

namespace StartupHook
{
    public class StartupHook
    {
        public static void Initialize()
        {
            Console.WriteLine("Hello from startup hook!");
        }
    }

    public class StartupHookWithOverload
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

    public class StartupHookWithIncorrectSignature
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

    public class StartupHookWithNonPublicMethod
    {
        static void Initialize()
        {
            Console.WriteLine("Hello from startup hook with non-public method!");
        }
    }

    public class StartupHookWithInstanceMethod
    {
        public void Initialize()
        {
            Console.WriteLine("Hello from startup hook with instance method!");
        }
    }

    public class StartupHookWithoutInitializeMethod
    {
        public static void Init()
        {
            Console.WriteLine("Hello from startup hook!");
        }
    }
}
