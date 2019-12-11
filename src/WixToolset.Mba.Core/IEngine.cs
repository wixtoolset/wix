// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperCore
{
    using System;
    using System.ComponentModel;
    using System.Security;

    public interface IEngine
    {
        /// <summary>
        /// Gets or sets numeric variables for the engine.
        /// </summary>
        IVariables<long> NumericVariables { get; }

        /// <summary>
        /// Gets the number of packages in the bundle.
        /// </summary>
        int PackageCount { get; }

        /// <summary>
        /// Gets or sets string variables for the engine using SecureStrings.
        /// </summary>
        IVariables<SecureString> SecureStringVariables { get; }

        /// <summary>
        /// Gets or sets string variables for the engine.
        /// </summary>
        IVariables<string> StringVariables { get; }

        /// <summary>
        /// Gets or sets <see cref="Version"/> variables for the engine.
        /// 
        /// The <see cref="Version"/> class can keep track of when the build and revision fields are undefined, but the engine can't.
        /// Therefore, the build and revision fields must be defined when setting a <see cref="Version"/> variable.
        /// Use the NormalizeVersion method to make sure the engine can accept the Version.
        /// 
        /// To keep track of versions without build or revision fields, use StringVariables instead.
        /// </summary>
        /// <exception cref="OverflowException">The given <see cref="Version"/> was invalid.</exception>
        IVariables<Version> VersionVariables { get; }

        /// <summary>
        /// Install the packages.
        /// </summary>
        /// <param name="hwndParent">The parent window for the installation user interface.</param>
        void Apply(IntPtr hwndParent);

        /// <summary>
        /// Close the splash screen if it is still open. Does nothing if the splash screen is not or
        /// never was opened.
        /// </summary>
        void CloseSplashScreen();

        /// <summary>
        /// Determine if all installation conditions are fulfilled.
        /// </summary>
        void Detect();

        /// <summary>
        /// Determine if all installation conditions are fulfilled.
        /// </summary>
        /// <param name="hwndParent">The parent window for the installation user interface.</param>
        void Detect(IntPtr hwndParent);

        /// <summary>
        /// Elevate the install.
        /// </summary>
        /// <param name="hwndParent">The parent window of the elevation dialog.</param>
        /// <returns>true if elevation succeeded; otherwise, false if the user cancelled.</returns>
        /// <exception cref="Win32Exception">A Win32 error occurred.</exception>
        bool Elevate(IntPtr hwndParent);

        /// <summary>
        /// Escapes the input string.
        /// </summary>
        /// <param name="input">The string to escape.</param>
        /// <returns>The escaped string.</returns>
        /// <exception cref="Win32Exception">A Win32 error occurred.</exception>
        string EscapeString(string input);

        /// <summary>
        /// Evaluates the <paramref name="condition"/> string.
        /// </summary>
        /// <param name="condition">The string representing the condition to evaluate.</param>
        /// <returns>Whether the condition evaluated to true or false.</returns>
        bool EvaluateCondition(string condition);

        /// <summary>
        /// Formats the input string.
        /// </summary>
        /// <param name="format">The string to format.</param>
        /// <returns>The formatted string.</returns>
        /// <exception cref="Win32Exception">A Win32 error occurred.</exception>
        string FormatString(string format);

        /// <summary>
        /// Launches a preapproved executable elevated.  As long as the engine already elevated, there will be no UAC prompt.
        /// </summary>
        /// <param name="hwndParent">The parent window of the elevation dialog (if the engine hasn't elevated yet).</param>
        /// <param name="approvedExeForElevationId">Id of the ApprovedExeForElevation element specified when the bundle was authored.</param>
        /// <param name="arguments">Optional arguments.</param>
        void LaunchApprovedExe(IntPtr hwndParent, string approvedExeForElevationId, string arguments);

        /// <summary>
        /// Launches a preapproved executable elevated.  As long as the engine already elevated, there will be no UAC prompt.
        /// </summary>
        /// <param name="hwndParent">The parent window of the elevation dialog (if the engine hasn't elevated yet).</param>
        /// <param name="approvedExeForElevationId">Id of the ApprovedExeForElevation element specified when the bundle was authored.</param>
        /// <param name="arguments">Optional arguments.</param>
        /// <param name="waitForInputIdleTimeout">Timeout in milliseconds. When set to something other than zero, the engine will call WaitForInputIdle for the new process with this timeout before calling OnLaunchApprovedExeComplete.</param>
        void LaunchApprovedExe(IntPtr hwndParent, string approvedExeForElevationId, string arguments, int waitForInputIdleTimeout);

        /// <summary>
        /// Logs the <paramref name="message"/>.
        /// </summary>
        /// <param name="level">The logging level.</param>
        /// <param name="message">The message to log.</param>
        void Log(LogLevel level, string message);

        /// <summary>
        /// Determine the installation sequencing and costing.
        /// </summary>
        /// <param name="action">The action to perform when planning.</param>
        void Plan(LaunchAction action);

        /// <summary>
        /// Set the update information for a bundle.
        /// </summary>
        /// <param name="localSource">Optional local source path for the update. Default is "update\[OriginalNameOfBundle].exe".</param>
        /// <param name="downloadSource">Optional download source for the update.</param>
        /// <param name="size">Size of the expected update.</param>
        /// <param name="hashType">Type of the hash expected on the update.</param>
        /// <param name="hash">Optional hash expected for the update.</param>
        void SetUpdate(string localSource, string downloadSource, long size, UpdateHashType hashType, byte[] hash);

        /// <summary>
        /// Set the local source for a package or container.
        /// </summary>
        /// <param name="packageOrContainerId">The id that uniquely identifies the package or container.</param>
        /// <param name="payloadId">The id that uniquely identifies the payload.</param>
        /// <param name="path">The new source path.</param>
        void SetLocalSource(string packageOrContainerId, string payloadId, string path);

        /// <summary>
        /// Set the new download URL for a package or container.
        /// </summary>
        /// <param name="packageOrContainerId">The id that uniquely identifies the package or container.</param>
        /// <param name="payloadId">The id that uniquely identifies the payload.</param>
        /// <param name="url">The new url.</param>
        /// <param name="user">The user name for proxy authentication.</param>
        /// <param name="password">The password for proxy authentication.</param>
        void SetDownloadSource(string packageOrContainerId, string payloadId, string url, string user, string password);

        /// <summary>
        /// Sends error message when embedded.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Error message.</param>
        /// <param name="uiHint">UI buttons to show on error dialog.</param>
        int SendEmbeddedError(int errorCode, string message, int uiHint);

        /// <summary>
        /// Sends progress percentages when embedded.
        /// </summary>
        /// <param name="progressPercentage">Percentage completed thus far.</param>
        /// <param name="overallPercentage">Overall percentage completed.</param>
        int SendEmbeddedProgress(int progressPercentage, int overallPercentage);

        /// <summary>
        /// Shuts down the engine.
        /// </summary>
        /// <param name="exitCode">Exit code indicating reason for shut down.</param>
        void Quit(int exitCode);
    }
}
