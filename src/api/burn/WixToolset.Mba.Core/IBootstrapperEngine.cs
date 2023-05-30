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
        void GetPackageCount(
            [MarshalAs(UnmanagedType.U4)] out int pcPackages
            );

        /// <summary>
        /// See <see cref="IEngine.GetVariableNumeric(string)"/>.
        /// </summary>
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
        void EvaluateCondition(
            [MarshalAs(UnmanagedType.LPWStr)] string wzCondition,
            [MarshalAs(UnmanagedType.Bool)] out bool pf
            );

        /// <summary>
        /// See <see cref="IEngine.Log(LogLevel, string)"/>.
        /// </summary>
        void Log(
            [MarshalAs(UnmanagedType.U4)] LogLevel level,
            [MarshalAs(UnmanagedType.LPWStr)] string wzMessage
            );

        /// <summary>
        /// See <see cref="IEngine.SendEmbeddedError(int, string, int)"/>.
        /// </summary>
        void SendEmbeddedError(
            [MarshalAs(UnmanagedType.U4)] int dwErrorCode,
            [MarshalAs(UnmanagedType.LPWStr)] string wzMessage,
            [MarshalAs(UnmanagedType.U4)] int dwUIHint,
            [MarshalAs(UnmanagedType.I4)] out int pnResult
            );

        /// <summary>
        /// See <see cref="IEngine.SendEmbeddedProgress(int, int)"/>.
        /// </summary>
        void SendEmbeddedProgress(
            [MarshalAs(UnmanagedType.U4)] int dwProgressPercentage,
            [MarshalAs(UnmanagedType.U4)] int dwOverallProgressPercentage,
            [MarshalAs(UnmanagedType.I4)] out int pnResult
            );

        /// <summary>
        /// See <see cref="IEngine.SetUpdate(string, string, long, UpdateHashType, string)"/>.
        /// </summary>
        void SetUpdate(
            [MarshalAs(UnmanagedType.LPWStr)] string wzLocalSource,
            [MarshalAs(UnmanagedType.LPWStr)] string wzDownloadSource,
            [MarshalAs(UnmanagedType.U8)] long qwValue,
            [MarshalAs(UnmanagedType.U4)] UpdateHashType hashType,
            [MarshalAs(UnmanagedType.LPWStr)] string wzHash
            );

        /// <summary>
        /// See <see cref="IEngine.SetLocalSource(string, string, string)"/>.
        /// </summary>
        void SetLocalSource(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageOrContainerId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPath
            );

        /// <summary>
        /// See <see cref="IEngine.SetDownloadSource(string, string, string, string, string)"/>.
        /// </summary>
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
        void SetVariableNumeric(
            [MarshalAs(UnmanagedType.LPWStr)] string wzVariable,
            long llValue
            );

        /// <summary>
        /// See <see cref="IEngine.SetVariableString(string, string, bool)"/>.
        /// </summary>
        void SetVariableString(
            [MarshalAs(UnmanagedType.LPWStr)] string wzVariable,
                                              IntPtr wzValue,
            [MarshalAs(UnmanagedType.Bool)]   bool fFormatted
            );

        /// <summary>
        /// See <see cref="IEngine.SetVariableVersion(string, string)"/>.
        /// </summary>
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
        void Detect(
            IntPtr hwndParent
            );

        /// <summary>
        /// See <see cref="IEngine.Plan(LaunchAction)"/>.
        /// </summary>
        void Plan(
            [MarshalAs(UnmanagedType.U4)] LaunchAction action
            );

        /// <summary>
        /// See <see cref="IEngine.Elevate(IntPtr)"/>.
        /// </summary>
        [PreserveSig]
        int Elevate(
            IntPtr hwndParent
            );

        /// <summary>
        /// See <see cref="IEngine.Apply(IntPtr)"/>.
        /// </summary>
        void Apply(
            IntPtr hwndParent
            );

        /// <summary>
        /// See <see cref="IEngine.Quit(int)"/>.
        /// </summary>
        void Quit(
            [MarshalAs(UnmanagedType.U4)] int dwExitCode
            );

        /// <summary>
        /// See <see cref="IEngine.LaunchApprovedExe(IntPtr, string, string, int)"/>.
        /// </summary>
        void LaunchApprovedExe(
            IntPtr hwndParent,
            [MarshalAs(UnmanagedType.LPWStr)] string wzApprovedExeForElevationId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzArguments,
            [MarshalAs(UnmanagedType.U4)] int dwWaitForInputIdleTimeout
            );

        /// <summary>
        /// Sets the URL to the update feed.
        /// </summary>
        void SetUpdateSource(
            [MarshalAs(UnmanagedType.LPWStr)] string url
            );

        /// <summary>
        /// See <see cref="IEngine.CompareVersions(string, string)"/>.
        /// </summary>
        void CompareVersions(
            [MarshalAs(UnmanagedType.LPWStr)] string wzVersion1,
            [MarshalAs(UnmanagedType.LPWStr)] string wzVersion2,
            [MarshalAs(UnmanagedType.I4)] out int pnResult
            );

        /// <summary>
        /// See <see cref="IEngine.GetRelatedBundleVariable(string, string)"/>.
        /// </summary>
        [PreserveSig]
        int GetRelatedBundleVariable(
            [MarshalAs(UnmanagedType.LPWStr)] string wzBundleId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzVariable,
                                              IntPtr wzValue,
                                          ref IntPtr pcchValue
            );
    }

    /// <summary>
    /// The installation action for the bundle or current package.
    /// </summary>
    public enum ActionState
    {
        /// <summary>
        /// No action.
        /// </summary>
        None,

        /// <summary>
        /// Uninstall action.
        /// </summary>
        Uninstall,

        /// <summary>
        /// Install action.
        /// </summary>
        Install,

        /// <summary>
        /// Modify action.
        /// </summary>
        Modify,

        /// <summary>
        /// Repair action.
        /// </summary>
        Repair,

        /// <summary>
        /// Minor upgrade action.
        /// </summary>
        MinorUpgrade,
    }

    /// <summary>
    /// The action for the bundle to perform.
    /// </summary>
    public enum LaunchAction
    {
        /// <summary>
        /// Invalid action.
        /// </summary>
        Unknown,

        /// <summary>
        /// Provide help information.
        /// </summary>
        Help,

        /// <summary>
        /// Layout the bundle on disk, normally to prepare for offline installation.
        /// </summary>
        Layout,

        /// <summary>
        /// Same as Uninstall, except it will always remove itself from the package cache and Add/Remove Programs.
        /// This should only be used to remove corrupt bundles since it might not properly clean up its packages.
        /// </summary>
        UnsafeUninstall,

        /// <summary>
        /// Uninstall the bundle.
        /// </summary>
        Uninstall,

        /// <summary>
        /// Cache the bundle and its packages.
        /// </summary>
        Cache,

        /// <summary>
        /// Install the bundle.
        /// </summary>
        Install,

        /// <summary>
        /// Modify the bundle.
        /// </summary>
        Modify,

        /// <summary>
        /// Repair the bundle
        /// </summary>
        Repair,

        /// <summary>
        /// Launch the update registered with <see cref="IEngine.SetUpdate(string, string, long, UpdateHashType, string)"/> and then exit without waiting for it to complete.
        /// </summary>
        UpdateReplace,

        /// <summary>
        /// Launch the update registered with <see cref="IEngine.SetUpdate(string, string, long, UpdateHashType, string)"/> as an embedded bundle.
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
        /// SHA-512 based hash provided.
        /// </summary>
        Sha512,
    }

    /// <summary>
    /// Describes the state of an installation package.
    /// </summary>
    public enum PackageState
    {
        /// <summary>
        /// Invalid state.
        /// </summary>
        Unknown,

        /// <summary>
        /// The package is not on the machine (except possibly MspPackage) and should not be installed.
        /// </summary>
        Obsolete,

        /// <summary>
        /// The package is not installed.
        /// </summary>
        Absent,

        /// <summary>
        /// The package is installed.
        /// </summary>
        Present,

        /// <summary>
        /// The package is on the machine but not active, so only uninstall operations are allowed.
        /// </summary>
        Superseded,

        /// <summary>
        /// This value is no longer used. See the DetectPackageCompleteEventArgs.Cached value instead.
        /// </summary>
        [Obsolete("Use DetectPackageCompleteEventArgs.Cached instead.")]
        Cached = Present,
    }

    /// <summary>
    /// Indicates the state desired for an installation package.
    /// </summary>
    public enum RequestState
    {
        /// <summary>
        /// No change requested.
        /// </summary>
        None,

        /// <summary>
        /// As long as there are no dependents, the package will be uninstalled.
        /// There are some packages that can't be uninstalled, such as an ExePackage without an UninstallCommand.
        /// </summary>
        ForceAbsent,

        /// <summary>
        /// Request the package to not be installed on the machine.
        /// </summary>
        Absent,

        /// <summary>
        /// Request the package to be cached and not be installed on the machine.
        /// </summary>
        Cache,

        /// <summary>
        /// Request the package to be installed on the machine.
        /// </summary>
        Present,

        /// <summary>
        /// Force the bundle to install the package.
        /// </summary>
        ForcePresent,

        /// <summary>
        /// Request the package to be repaired.
        /// </summary>
        Repair,
    }

    /// <summary>
    /// Indicates the state of a feature.
    /// See https://learn.microsoft.com/en-us/windows/win32/api/msi/nf-msi-msiqueryfeaturestatew.
    /// </summary>
    public enum FeatureState
    {
        /// <summary>
        /// Invalid state.
        /// </summary>
        Unknown,

        /// <summary>
        /// INSTALLSTATE_ABSENT
        /// </summary>
        Absent,

        /// <summary>
        /// INSTALLSTATE_ADVERTISED
        /// </summary>
        Advertised,

        /// <summary>
        /// INSTALLSTATE_LOCAL
        /// </summary>
        Local,

        /// <summary>
        /// INSTALLSTATE_SOURCE
        /// </summary>
        Source,
    }
}
