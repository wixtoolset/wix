using System;
using CsprojClassLibraryMultiFramework;

namespace CsprojConsoleNetCore
{
    class Program
    {
        static void Main(string[] args)
        {
            var mfc = new MultiFrameworkClass();

            Console.WriteLine("Hello, {0}!", mfc.Name);
        }
    }
}
