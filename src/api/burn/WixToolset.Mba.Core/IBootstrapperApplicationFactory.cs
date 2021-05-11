// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    using System;
    using System.CodeDom.Compiler;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Interface used by WixToolset.Mba.Host to dynamically load the BA.
    /// </summary>
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("2965A12F-AC7B-43A0-85DF-E4B2168478A4")]
    [GeneratedCodeAttribute("WixToolset.Bootstrapper.InteropCodeGenerator", "1.0.0.0")]
    public interface IBootstrapperApplicationFactory
    {
        /// <summary>
        /// Low level method called by the native host.
        /// </summary>
        /// <param name="pArgs"></param>
        /// <param name="pResults"></param>
        void Create(
            IntPtr pArgs,
            IntPtr pResults
            );
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [GeneratedCodeAttribute("WixToolset.Bootstrapper.InteropCodeGenerator", "1.0.0.0")]
    internal struct Command
    {
        // Strings must be declared as pointers so that Marshaling doesn't free them.
        [MarshalAs(UnmanagedType.I4)] internal int cbSize;
        [MarshalAs(UnmanagedType.U4)] private readonly LaunchAction action;
        [MarshalAs(UnmanagedType.U4)] private readonly Display display;
        [MarshalAs(UnmanagedType.U4)] private readonly Restart restart;
        private readonly IntPtr wzCommandLine;
        [MarshalAs(UnmanagedType.I4)] private readonly int nCmdShow;
        [MarshalAs(UnmanagedType.U4)] private readonly ResumeType resume;
        private readonly IntPtr hwndSplashScreen;
        [MarshalAs(UnmanagedType.I4)] private readonly RelationType relation;
        [MarshalAs(UnmanagedType.Bool)] private readonly bool passthrough;
        private readonly IntPtr wzLayoutDirectory;
        private readonly IntPtr wzBootstrapperWorkingFolder;
        private readonly IntPtr wzBootstrapperApplicationDataPath;

        public IBootstrapperCommand GetBootstrapperCommand()
        {
            return new BootstrapperCommand(
                this.action,
                this.display,
                this.restart,
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
}
