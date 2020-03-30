// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    using System;
    using System.CodeDom.Compiler;
    using System.Runtime.InteropServices;

    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("2965A12F-AC7B-43A0-85DF-E4B2168478A4")]
    [GeneratedCodeAttribute("WixToolset.Bootstrapper.InteropCodeGenerator", "1.0.0.0")]
    public interface IBootstrapperApplicationFactory
    {
        void Create(
            IntPtr pArgs,
            IntPtr pResults
            );
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [GeneratedCodeAttribute("WixToolset.Bootstrapper.InteropCodeGenerator", "1.0.0.0")]
    public struct Command
    {
        [MarshalAs(UnmanagedType.I4)] internal int cbSize;
        [MarshalAs(UnmanagedType.U4)] private readonly LaunchAction action;
        [MarshalAs(UnmanagedType.U4)] private readonly Display display;
        [MarshalAs(UnmanagedType.U4)] private readonly Restart restart;
        [MarshalAs(UnmanagedType.LPWStr)] private readonly string wzCommandLine;
        [MarshalAs(UnmanagedType.I4)] private readonly int nCmdShow;
        [MarshalAs(UnmanagedType.U4)] private readonly ResumeType resume;
        private readonly IntPtr hwndSplashScreen;
        [MarshalAs(UnmanagedType.I4)] private readonly RelationType relation;
        [MarshalAs(UnmanagedType.Bool)] private readonly bool passthrough;
        [MarshalAs(UnmanagedType.LPWStr)] private readonly string wzLayoutDirectory;
        [MarshalAs(UnmanagedType.LPWStr)] private readonly string wzBootstrapperWorkingFolder;
        [MarshalAs(UnmanagedType.LPWStr)] private readonly string wzBootstrapperApplicationDataPath;

        public IBootstrapperCommand GetBootstrapperCommand()
        {
            return new BootstrapperCommand(
                this.action,
                this.display,
                this.restart,
                this.wzCommandLine,
                this.nCmdShow,
                this.resume,
                this.hwndSplashScreen,
                this.relation,
                this.passthrough,
                this.wzLayoutDirectory,
                this.wzBootstrapperWorkingFolder,
                this.wzBootstrapperApplicationDataPath);
        }
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [GeneratedCodeAttribute("WixToolset.Bootstrapper.InteropCodeGenerator", "1.0.0.0")]
    public struct BootstrapperCreateArgs
    {
        [MarshalAs(UnmanagedType.I4)] public readonly int cbSize;
        [MarshalAs(UnmanagedType.I8)] public readonly long qwEngineAPIVersion;
        public readonly IntPtr pfnBootstrapperEngineProc;
        public readonly IntPtr pvBootstrapperEngineProcContext;
        public readonly IntPtr pCommand;

        public BootstrapperCreateArgs(long version, IntPtr pEngineProc, IntPtr pEngineContext, IntPtr pCommand)
        {
            this.cbSize = Marshal.SizeOf(typeof(BootstrapperCreateArgs));
            this.qwEngineAPIVersion = version;
            this.pfnBootstrapperEngineProc = pEngineProc;
            this.pvBootstrapperEngineProcContext = pEngineContext;
            this.pCommand = pCommand;
        }
    }
}
