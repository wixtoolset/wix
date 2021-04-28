// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    using System;
    using System.CodeDom.Compiler;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    /// Allows calls into the bootstrapper engine.
    /// </summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("6480D616-27A0-44D7-905B-81512C29C2FB")]
    [GeneratedCodeAttribute("WixToolset.Bootstrapper.InteropCodeGenerator", "1.0.0.0")]
    public interface IBootstrapperEngine
    {
        /// <summary>
        /// See <see cref="IEngine.PackageCount"/>.
        /// </summary>
        /// <param name="pcPackages"></param>
        void GetPackageCount(
            [MarshalAs(UnmanagedType.U4)] out int pcPackages
            );

        /// <summary>
        /// See <see cref="IEngine.GetVariableNumeric(string)"/>.
        /// </summary>
        /// <param name="wzVariable"></param>
        /// <param name="pllValue"></param>
        /// <returns></returns>
        [PreserveSig]
        int GetVariableNumeric(
            [MarshalAs(UnmanagedType.LPWStr)] string wzVariable,
            out long pllValue
            );

        /// <summary>
        /// See <see cref="IEngine.GetVariableString(string)"/>.
        /// </summary>
        [PreserveSig]
        int GetVariableString(
            [MarshalAs(UnmanagedType.LPWStr)] string wzVariable,
                                              IntPtr wzValue,
                                          ref IntPtr pcchValue
            );

        /// <summary>
        /// See <see cref="IEngine.GetVariableVersion(string)"/>.
        /// </summary>
        [PreserveSig]
        int GetVariableVersion(
            [MarshalAs(UnmanagedType.LPWStr)] string wzVariable,
                                              IntPtr wzValue,
                                          ref IntPtr pcchValue
            );

        /// <summary>
        /// See <see cref="IEngine.FormatString(string)"/>.
        /// </summary>
        [PreserveSig]
        int FormatString(
            [MarshalAs(UnmanagedType.LPWStr)] string wzIn,
            [MarshalAs(UnmanagedType.LPWStr), Out] StringBuilder wzOut,
            ref IntPtr pcchOut
            );

        /// <summary>
        /// See <see cref="IEngine.EscapeString(string)"/>.
        /// </summary>
        [PreserveSig]
        int EscapeString(
            [MarshalAs(UnmanagedType.LPWStr)] string wzIn,
            [MarshalAs(UnmanagedType.LPWStr), Out] StringBuilder wzOut,
            ref IntPtr pcchOut
            );

        /// <summary>
        /// See <see cref="IEngine.EvaluateCondition(string)"/>.
        /// </summary>
        /// <param name="wzCondition"></param>
        /// <param name="pf"></param>
        void EvaluateCondition(
            [MarshalAs(UnmanagedType.LPWStr)] string wzCondition,
            [MarshalAs(UnmanagedType.Bool)] out bool pf
            );

        /// <summary>
        /// See <see cref="IEngine.Log(LogLevel, string)"/>.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="wzMessage"></param>
        void Log(
            [MarshalAs(UnmanagedType.U4)] LogLevel level,
            [MarshalAs(UnmanagedType.LPWStr)] string wzMessage
            );

        /// <summary>
        /// See <see cref="IEngine.SendEmbeddedError(int, string, int)"/>.
        /// </summary>
        /// <param name="dwErrorCode"></param>
        /// <param name="wzMessage"></param>
        /// <param name="dwUIHint"></param>
        /// <param name="pnResult"></param>
        void SendEmbeddedError(
            [MarshalAs(UnmanagedType.U4)] int dwErrorCode,
            [MarshalAs(UnmanagedType.LPWStr)] string wzMessage,
            [MarshalAs(UnmanagedType.U4)] int dwUIHint,
            [MarshalAs(UnmanagedType.I4)] out int pnResult
            );

        /// <summary>
        /// See <see cref="IEngine.SendEmbeddedProgress(int, int)"/>.
        /// </summary>
        /// <param name="dwProgressPercentage"></param>
        /// <param name="dwOverallProgressPercentage"></param>
        /// <param name="pnResult"></param>
        void SendEmbeddedProgress(
            [MarshalAs(UnmanagedType.U4)] int dwProgressPercentage,
            [MarshalAs(UnmanagedType.U4)] int dwOverallProgressPercentage,
            [MarshalAs(UnmanagedType.I4)] out int pnResult
            );

        /// <summary>
        /// See <see cref="IEngine.SetUpdate(string, string, long, UpdateHashType, byte[])"/>.
        /// </summary>
        /// <param name="wzLocalSource"></param>
        /// <param name="wzDownloadSource"></param>
        /// <param name="qwValue"></param>
        /// <param name="hashType"></param>
        /// <param name="rgbHash"></param>
        /// <param name="cbHash"></param>
        void SetUpdate(
            [MarshalAs(UnmanagedType.LPWStr)] string wzLocalSource,
            [MarshalAs(UnmanagedType.LPWStr)] string wzDownloadSource,
            [MarshalAs(UnmanagedType.U8)] long qwValue,
            [MarshalAs(UnmanagedType.U4)] UpdateHashType hashType,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=4)] byte[] rgbHash,
            [MarshalAs(UnmanagedType.U4)] int cbHash
            );

        /// <summary>
        /// See <see cref="IEngine.SetLocalSource(string, string, string)"/>.
        /// </summary>
        /// <param name="wzPackageOrContainerId"></param>
        /// <param name="wzPayloadId"></param>
        /// <param name="wzPath"></param>
        void SetLocalSource(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageOrContainerId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPath
            );

        /// <summary>
        /// See <see cref="IEngine.SetDownloadSource(string, string, string, string, string)"/>.
        /// </summary>
        /// <param name="wzPackageOrContainerId"></param>
        /// <param name="wzPayloadId"></param>
        /// <param name="wzUrl"></param>
        /// <param name="wzUser"></param>
        /// <param name="wzPassword"></param>
        void SetDownloadSource(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageOrContainerId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzUrl,
            [MarshalAs(UnmanagedType.LPWStr)] string wzUser,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPassword
            );

        /// <summary>
        /// See <see cref="IEngine.SetVariableNumeric(string, long)"/>.
        /// </summary>
        /// <param name="wzVariable"></param>
        /// <param name="llValue"></param>
        void SetVariableNumeric(
            [MarshalAs(UnmanagedType.LPWStr)] string wzVariable,
            long llValue
            );

        /// <summary>
        /// See <see cref="IEngine.SetVariableString(string, string, bool)"/>.
        /// </summary>
        /// <param name="wzVariable"></param>
        /// <param name="wzValue"></param>
        /// <param name="fFormatted"></param>
        void SetVariableString(
            [MarshalAs(UnmanagedType.LPWStr)] string wzVariable,
                                              IntPtr wzValue,
            [MarshalAs(UnmanagedType.Bool)]   bool fFormatted
            );

        /// <summary>
        /// See <see cref="IEngine.SetVariableVersion(string, string)"/>.
        /// </summary>
        /// <param name="wzVariable"></param>
        /// <param name="wzValue"></param>
        void SetVariableVersion(
            [MarshalAs(UnmanagedType.LPWStr)] string wzVariable,
                                              IntPtr wzValue
            );

        /// <summary>
        /// See <see cref="IEngine.CloseSplashScreen"/>.
        /// </summary>
        void CloseSplashScreen();

        /// <summary>
        /// See <see cref="IEngine.Detect(IntPtr)"/>.
        /// </summary>
        /// <param name="hwndParent"></param>
        void Detect(
            IntPtr hwndParent
            );

        /// <summary>
        /// See <see cref="IEngine.Plan(LaunchAction)"/>.
        /// </summary>
        /// <param name="action"></param>
        void Plan(
            [MarshalAs(UnmanagedType.U4)] LaunchAction action
            );

        /// <summary>
        /// See <see cref="IEngine.Elevate(IntPtr)"/>.
        /// </summary>
        /// <param name="hwndParent"></param>
        /// <returns></returns>
        [PreserveSig]
        int Elevate(
            IntPtr hwndParent
            );

        /// <summary>
        /// See <see cref="IEngine.Apply(IntPtr)"/>.
        /// </summary>
        /// <param name="hwndParent"></param>
        void Apply(
            IntPtr hwndParent
            );

        /// <summary>
        /// See <see cref="IEngine.Quit(int)"/>.
        /// </summary>
        /// <param name="dwExitCode"></param>
        void Quit(
            [MarshalAs(UnmanagedType.U4)] int dwExitCode
            );

        /// <summary>
        /// See <see cref="IEngine.LaunchApprovedExe(IntPtr, string, string, int)"/>.
        /// </summary>
        /// <param name="hwndParent"></param>
        /// <param name="wzApprovedExeForElevationId"></param>
        /// <param name="wzArguments"></param>
        /// <param name="dwWaitForInputIdleTimeout"></param>
        void LaunchApprovedExe(
            IntPtr hwndParent,
            [MarshalAs(UnmanagedType.LPWStr)] string wzApprovedExeForElevationId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzArguments,
            [MarshalAs(UnmanagedType.U4)] int dwWaitForInputIdleTimeout
            );

        /// <summary>
        /// Sets the URL to the update feed.
        /// </summary>
        /// <param name="url">URL of the update feed.</param>
        void SetUpdateSource(
            [MarshalAs(UnmanagedType.LPWStr)] string url
            );

        /// <summary>
        /// See <see cref="IEngine.CompareVersions(string, string)"/>.
        /// </summary>
        /// <param name="wzVersion1"></param>
        /// <param name="wzVersion2"></param>
        /// <param name="pnResult"></param>
        void CompareVersions(
            [MarshalAs(UnmanagedType.LPWStr)] string wzVersion1,
            [MarshalAs(UnmanagedType.LPWStr)] string wzVersion2,
            [MarshalAs(UnmanagedType.I4)] out int pnResult
            );
    }

    /// <summary>
    /// The installation action for the bundle or current package.
    /// </summary>
    public enum ActionState
    {
        /// <summary>
        /// 
        /// </summary>
        None,

        /// <summary>
        /// 
        /// </summary>
        Uninstall,

        /// <summary>
        /// 
        /// </summary>
        Install,

        /// <summary>
        /// 
        /// </summary>
        Modify,

        /// <summary>
        /// 
        /// </summary>
        Mend,

        /// <summary>
        /// 
        /// </summary>
        Repair,

        /// <summary>
        /// 
        /// </summary>
        MinorUpgrade,
    }

    /// <summary>
    /// The action for the BA to perform.
    /// </summary>
    public enum LaunchAction
    {
        /// <summary>
        /// 
        /// </summary>
        Unknown,

        /// <summary>
        /// 
        /// </summary>
        Help,

        /// <summary>
        /// 
        /// </summary>
        Layout,

        /// <summary>
        /// 
        /// </summary>
        Uninstall,

        /// <summary>
        /// 
        /// </summary>
        Cache,

        /// <summary>
        /// 
        /// </summary>
        Install,

        /// <summary>
        /// 
        /// </summary>
        Modify,

        /// <summary>
        /// 
        /// </summary>
        Repair,

        /// <summary>
        /// 
        /// </summary>
        UpdateReplace,

        /// <summary>
        /// 
        /// </summary>
        UpdateReplaceEmbedded,
    }

    /// <summary>
    /// The message log level.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// No logging level (generic).
        /// </summary>
        None,

        /// <summary>
        /// User messages.
        /// </summary>
        Standard,

        /// <summary>
        /// Verbose messages.
        /// </summary>
        Verbose,

        /// <summary>
        /// Messages for debugging.
        /// </summary>
        Debug,

        /// <summary>
        /// Error messages.
        /// </summary>
        Error,
    }

    /// <summary>
    /// Type of hash used for update bundle.
    /// </summary>
    public enum UpdateHashType
    {
        /// <summary>
        /// No hash provided.
        /// </summary>
        None,

        /// <summary>
        /// SHA-1 based hash provided.
        /// </summary>
        Sha1,
    }

    /// <summary>
    /// Describes the state of an installation package.
    /// </summary>
    public enum PackageState
    {
        /// <summary>
        /// 
        /// </summary>
        Unknown,

        /// <summary>
        /// 
        /// </summary>
        Obsolete,

        /// <summary>
        /// 
        /// </summary>
        Absent,

        /// <summary>
        /// 
        /// </summary>
        Cached,

        /// <summary>
        /// 
        /// </summary>
        Present,

        /// <summary>
        /// 
        /// </summary>
        Superseded,
    }

    /// <summary>
    /// Indicates the state desired for an installation package.
    /// </summary>
    public enum RequestState
    {
        /// <summary>
        /// 
        /// </summary>
        None,

        /// <summary>
        /// /
        /// </summary>
        ForceAbsent,

        /// <summary>
        /// 
        /// </summary>
        Absent,

        /// <summary>
        /// 
        /// </summary>
        Cache,

        /// <summary>
        /// 
        /// </summary>
        Present,

        /// <summary>
        /// 
        /// </summary>
        Mend,

        /// <summary>
        /// 
        /// </summary>
        Repair,
    }

    /// <summary>
    /// Indicates the state of a feature.
    /// </summary>
    public enum FeatureState
    {
        /// <summary>
        /// 
        /// </summary>
        Unknown,

        /// <summary>
        /// 
        /// </summary>
        Absent,

        /// <summary>
        /// 
        /// </summary>
        Advertised,

        /// <summary>
        /// 
        /// </summary>
        Local,

        /// <summary>
        /// 
        /// </summary>
        Source,
    }
}
