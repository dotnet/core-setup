using System;

namespace StartupHook
{
    //
    // Correct startup hooks
    //

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

    //
    // Invalid startup hooks (incorrect signatures)
    //

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

    public class StartupHookWithParameter
    {
        public static void Initialize(int input)
        {
            Console.WriteLine("Hello from startup hook taking int! Input: " + input);
        }
    }

    public class StartupHookWithReturnType
    {
        public static int Initialize()
        {
            Console.WriteLine("Hello from startup hook returning int!");
            return 10;
        }
    }

    public class StartupHookWithMultipleIncorrectSignatures
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

    // Missing startup hooks (no Initialize method defined)

    public class StartupHookWithoutInitializeMethod
    {
        public static void Init()
        {
            Console.WriteLine("Hello from startup hook!");
        }
    }
}
