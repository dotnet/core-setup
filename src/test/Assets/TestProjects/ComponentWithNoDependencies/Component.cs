using System;

namespace Component
{
    public class Component
    {
        public static int ComponentEntryPoint(IntPtr arg, int size)
        {
            Console.WriteLine($"Called ComponentEntryPoint(0x{arg.ToString("x")}, {size})");

            return size >> 1;
        }
    }
}