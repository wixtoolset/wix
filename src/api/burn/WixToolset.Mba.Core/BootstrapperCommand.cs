// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    using System;
    using System.ComponentModel;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Default implementation of <see cref="IBootstrapperCommand"/>.
    /// </summary>
    public sealed class BootstrapperCommand : IBootstrapperCommand
    {
        private readonly string commandLine;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="display"></param>
        /// <param name="restart"></param>
        /// <param name="commandLine"></param>
        /// <param name="cmdShow"></param>
        /// <param name="resume"></param>
        /// <param name="splashScreen"></param>
        /// <param name="relation"></param>
        /// <param name="passthrough"></param>
        /// <param name="layoutDirectory"></param>
        /// <param name="bootstrapperWorkingFolder"></param>
        /// <param name="bootstrapperApplicationDataPath"></param>
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
            string layoutDirectory,
            string bootstrapperWorkingFolder,
            string bootstrapperApplicationDataPath)
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
            this.BootstrapperWorkingFolder = bootstrapperWorkingFolder;
            this.BootstrapperApplicationDataPath = bootstrapperApplicationDataPath;
        }

        /// <inheritdoc/>
        public LaunchAction Action { get; }

        /// <inheritdoc/>
        public Display Display { get; }

        /// <inheritdoc/>
        public Restart Restart { get; }

        /// <inheritdoc/>
        public string[] CommandLineArgs => GetCommandLineArgs(this.commandLine);

        /// <inheritdoc/>
        public int CmdShow { get; }

        /// <inheritdoc/>
        public ResumeType Resume { get; }

        /// <inheritdoc/>
        public IntPtr SplashScreen { get; }

        /// <inheritdoc/>
        public RelationType Relation { get; }

        /// <inheritdoc/>
        public bool Passthrough { get; }

        /// <inheritdoc/>
        public string LayoutDirectory { get; }

        /// <inheritdoc/>
        public string BootstrapperWorkingFolder { get; }

        /// <inheritdoc/>
        public string BootstrapperApplicationDataPath { get; }

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
