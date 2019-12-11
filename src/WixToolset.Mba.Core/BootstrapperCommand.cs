// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperCore
{
    using System;
    using System.ComponentModel;
    using System.Runtime.InteropServices;

    public sealed class BootstrapperCommand : IBootstrapperCommand
    {
        private readonly string commandLine;

        public BootstrapperCommand(
            LaunchAction action,
            Display display,
            Restart restart,
            string commandLine,
            int cmdShow,
            ResumeType resume,
            IntPtr splashScreen,
            RelationType relation,
            bool passthrough,
            string layoutDirectory)
        {
            this.Action = action;
            this.Display = display;
            this.Restart = restart;
            this.commandLine = commandLine;
            this.CmdShow = cmdShow;
            this.Resume = resume;
            this.SplashScreen = splashScreen;
            this.Relation = relation;
            this.Passthrough = passthrough;
            this.LayoutDirectory = layoutDirectory;
        }

        public LaunchAction Action { get; }

        public Display Display { get; }

        public Restart Restart { get; }

        public string[] CommandLineArgs => GetCommandLineArgs(this.commandLine);

        public int CmdShow { get; }

        public ResumeType Resume { get; }

        public IntPtr SplashScreen { get; }

        public RelationType Relation { get; }

        public bool Passthrough { get; }

        public string LayoutDirectory { get; }

        /// <summary>
        /// Gets the command line arguments as a string array.
        /// </summary>
        /// <returns>
        /// Array of command line arguments.
        /// </returns>
        /// <exception type="Win32Exception">The command line could not be parsed into an array.</exception>
        /// <remarks>
        /// This method uses the same parsing as the operating system which handles quotes and spaces correctly.
        /// </remarks>
        public static string[] GetCommandLineArgs(string commandLine)
        {
            if (null == commandLine)
            {
                return new string[0];
            }

            // Parse the filtered command line arguments into a native array.
            int argc = 0;

            // CommandLineToArgvW tries to treat the first argument as the path to the process,
            // which fails pretty miserably if your first argument is something like
            // FOO="C:\Program Files\My Company". So give it something harmless to play with.
            IntPtr argv = NativeMethods.CommandLineToArgvW("ignored " + commandLine, out argc);

            if (IntPtr.Zero == argv)
            {
                // Throw an exception with the last error.
                throw new Win32Exception();
            }

            // Marshal each native array pointer to a managed string.
            try
            {
                // Skip "ignored" argument/hack.
                string[] args = new string[argc - 1];
                for (int i = 1; i < argc; ++i)
                {
                    IntPtr argvi = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                    args[i - 1] = Marshal.PtrToStringUni(argvi);
                }

                return args;
            }
            finally
            {
                NativeMethods.LocalFree(argv);
            }
        }
    }
}
