// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Mba.Core
{
    using System;
    using System.Runtime.InteropServices;
    using WixToolset.Mba.Core;
    using Xunit;

    public class BaseBootstrapperApplicationFactoryFixture
    {
        [Fact]
        public void CanCreateBA()
        {
            var command = new TestCommand
            {
                action = LaunchAction.Install,
                cbSize = Marshal.SizeOf(typeof(TestCommand)),
                display = Display.Full,
                wzCommandLine = "this \"is a\" test",
            };
            var pCommand = Marshal.AllocHGlobal(command.cbSize);
            try
            {
                Marshal.StructureToPtr(command, pCommand, false);
                var createArgs = new BootstrapperCreateArgs(0, IntPtr.Zero, IntPtr.Zero, pCommand);
                var pArgs = Marshal.AllocHGlobal(createArgs.cbSize);
                try
                {
                    Marshal.StructureToPtr(createArgs, pArgs, false);
                    var createResults = new TestCreateResults
                    {
                        cbSize = Marshal.SizeOf<TestCreateResults>(),
                    };
                    var pResults = Marshal.AllocHGlobal(createResults.cbSize);
                    try
                    {
                        var baFactory = new TestBAFactory();
                        baFactory.Create(pArgs, pResults);

                        createResults = Marshal.PtrToStructure<TestCreateResults>(pResults);
                        Assert.Equal(baFactory.BA, createResults.pBA);
                        Assert.Equal(baFactory.BA.Command.Action, command.action);
                        Assert.Equal(baFactory.BA.Command.Display, command.display);
                        Assert.Equal(baFactory.BA.Command.CommandLineArgs, new string[] { "this", "is a", "test" });
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(pResults);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(pArgs);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pCommand);
            }
        }

        internal class TestBAFactory : BaseBootstrapperApplicationFactory
        {
            public TestBA BA { get; private set; }

            protected override IBootstrapperApplication Create(IEngine engine, IBootstrapperCommand bootstrapperCommand)
            {
                this.BA = new TestBA(engine, bootstrapperCommand);
                return this.BA;
            }
        }

        internal class TestBA : BootstrapperApplication
        {
            public IBootstrapperCommand Command { get; }

            public TestBA(IEngine engine, IBootstrapperCommand command)
                : base(engine)
            {
                this.Command = command;
            }

            protected override void Run()
            {
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TestCommand
        {
            public int cbSize;
            public LaunchAction action;
            public Display display;
            public Restart restart;
            [MarshalAs(UnmanagedType.LPWStr)] public string wzCommandLine;
            public int nCmdShow;
            public ResumeType resume;
            public IntPtr hwndSplashScreen;
            public RelationType relation;
            [MarshalAs(UnmanagedType.Bool)] public bool passthrough;
            [MarshalAs(UnmanagedType.LPWStr)] public string wzLayoutDirectory;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TestCreateResults
        {
            public int cbSize;
            public IntPtr pBAProc;
            [MarshalAs(UnmanagedType.Interface)] public IBootstrapperApplication pBA;
        }
    }
}
