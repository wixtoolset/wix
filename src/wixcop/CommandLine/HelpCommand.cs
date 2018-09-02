using System;
using WixToolset.Extensibility.Data;

namespace WixCop.CommandLine
{
    internal class HelpCommand : ICommandLineCommand
    {
        public int Execute()
        {
            Console.WriteLine(" usage:  wixcop.exe sourceFile [sourceFile ...]");
            Console.WriteLine();
            Console.WriteLine("   -f       fix errors automatically for writable files");
            Console.WriteLine("   -nologo  suppress displaying the logo information");
            Console.WriteLine("   -s       search for matching files in current dir and subdirs");
            Console.WriteLine("   -set1<file> primary settings file");
            Console.WriteLine("   -set2<file> secondary settings file (overrides primary)");
            Console.WriteLine("   -indent:<n> indentation multiple (overrides default of 4)");
            Console.WriteLine("   -?       this help information");
            Console.WriteLine();
            Console.WriteLine("   sourceFile may use wildcards like *.wxs");

            return 0;
        }
    }
}
