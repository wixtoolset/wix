// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Command-line provided to the bootstrapper application.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [GeneratedCodeAttribute("WixToolset.Bootstrapper.InteropCodeGenerator", "1.0.0.0")]
    public struct Command
    {
        // Strings must be declared as pointers so that Marshaling doesn't free them.
        [MarshalAs(UnmanagedType.I4)] internal int cbSize;
        [MarshalAs(UnmanagedType.U4)] private readonly LaunchAction action;
        [MarshalAs(UnmanagedType.U4)] private readonly Display display;
        private readonly IntPtr wzCommandLine;
        [MarshalAs(UnmanagedType.I4)] private readonly int nCmdShow;
        [MarshalAs(UnmanagedType.U4)] private readonly ResumeType resume;
        private readonly IntPtr hwndSplashScreen;
        [MarshalAs(UnmanagedType.I4)] private readonly RelationType relation;
        [MarshalAs(UnmanagedType.Bool)] private readonly bool passthrough;
        private readonly IntPtr wzLayoutDirectory;
        private readonly IntPtr wzBootstrapperWorkingFolder;
        private readonly IntPtr wzBootstrapperApplicationDataPath;

        /// <summary>
        /// Gets the IBootstrapperCommand for this Command.
        /// </summary>
        /// <returns>IBootstrapperCommand</returns>
        public IBootstrapperCommand GetBootstrapperCommand()
        {
            return new BootstrapperCommand(
                this.action,
                this.display,
                Marshal.PtrToStringUni(this.wzCommandLine),
                this.nCmdShow,
                this.resume,
                this.hwndSplashScreen,
                this.relation,
                this.passthrough,
                Marshal.PtrToStringUni(this.wzLayoutDirectory),
                Marshal.PtrToStringUni(this.wzBootstrapperWorkingFolder),
                Marshal.PtrToStringUni(this.wzBootstrapperApplicationDataPath));
        }
    }

    /// <summary>
    /// Default implementation of <see cref="IBootstrapperCommand"/>.
    /// </summary>
    public sealed class BootstrapperCommand : IBootstrapperCommand
    {
        /// <summary>
        /// See <see cref="IBootstrapperCommand"/>.
        /// </summary>
        public BootstrapperCommand(
            LaunchAction action,
            Display display,
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
            this.CommandLine = commandLine;
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
        public string CommandLine { get; }

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

        /// <inheritdoc/>
        public IMbaCommand ParseCommandLine()
        {
            var args = ParseCommandLineToArgs(this.CommandLine);
            var unknownArgs = new List<string>();
            var variables = new List<KeyValuePair<string, string>>();
            var restart = Restart.Unknown;

            foreach (var arg in args)
            {
                var unknownArg = false;

                if (arg[0] == '-' || arg[0] == '/')
                {
                    var parameter = arg.Substring(1).ToLowerInvariant();
                    switch (parameter)
                    {
                        case "norestart":
                            if (restart == Restart.Unknown)
                            {
                                restart = Restart.Never;
                            }
                            break;
                        case "forcerestart":
                            if (restart == Restart.Unknown)
                            {
                                restart = Restart.Always;
                            }
                            break;
                        default:
                            unknownArg = true;
                            break;
                    }
                }
                else
                {
                    var index = arg.IndexOf('=');
                    if (index == -1)
                    {
                        unknownArg = true;
                    }
                    else
                    {
                        var name = arg.Substring(0, index);
                        var value = arg.Substring(index + 1);
                        variables.Add(new KeyValuePair<string, string>(name, value));
                    }
                }

                if (unknownArg)
                {
                    unknownArgs.Add(arg);
                }
            }

            if (restart == Restart.Unknown)
            {
                restart = this.Display < Display.Full ? Restart.Automatic : Restart.Prompt;
            }

            return new MbaCommand
            {
                Restart = restart,
                UnknownCommandLineArgs = unknownArgs.ToArray(),
                Variables = variables.ToArray(),
            };
        }

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
        public static string[] ParseCommandLineToArgs(string commandLine)
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
