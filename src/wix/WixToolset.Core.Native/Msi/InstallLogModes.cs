// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native.Msi
{
    using System;

    /// <summary>
    /// Windows Installer log modes.
    /// </summary>
    [Flags]
    public enum InstallLogModes
    {
        /// <summary>
        /// Premature termination of installation.
        /// </summary>
        FatalExit = (1 << ((int)InstallMessage.FatalExit >> 24)),

        /// <summary>
        /// The error messages are logged.
        /// </summary>
        Error = (1 << ((int)InstallMessage.Error >> 24)),

        /// <summary>
        /// The warning messages are logged.
        /// </summary>
        Warning = (1 << ((int)InstallMessage.Warning >> 24)),

        /// <summary>
        /// The user requests are logged.
        /// </summary>
        User = (1 << ((int)InstallMessage.User >> 24)),

        /// <summary>
        /// The status messages that are not displayed are logged.
        /// </summary>
        Info = (1 << ((int)InstallMessage.Info >> 24)),

        /// <summary>
        /// Request to determine a valid source location.
        /// </summary>
        ResolveSource = (1 << ((int)InstallMessage.ResolveSource >> 24)),

        /// <summary>
        /// The was insufficient disk space.
        /// </summary>
        OutOfDiskSpace = (1 << ((int)InstallMessage.OutOfDiskSpace >> 24)),

        /// <summary>
        /// The start of new installation actions are logged.
        /// </summary>
        ActionStart = (1 << ((int)InstallMessage.ActionStart >> 24)),

        /// <summary>
        /// The data record with the installation action is logged.
        /// </summary>
        ActionData = (1 << ((int)InstallMessage.ActionData >> 24)),

        /// <summary>
        /// The parameters for user-interface initialization are logged.
        /// </summary>
        CommonData = (1 << ((int)InstallMessage.CommonData >> 24)),

        /// <summary>
        /// Logs the property values at termination.
        /// </summary>
        PropertyDump = (1 << ((int)InstallMessage.Progress >> 24)),

        /// <summary>
        /// Sends large amounts of information to a log file not generally useful to users.
        /// May be used for technical support.
        /// </summary>
        Verbose = (1 << ((int)InstallMessage.Initilize >> 24)),

        /// <summary>
        /// Sends extra debugging information, such as handle creation information, to the log file.
        /// </summary>
        ExtraDebug = (1 << ((int)InstallMessage.Terminate >> 24)),

        /// <summary>
        /// Progress bar information. This message includes information on units so far and total number of units.
        /// See MsiProcessMessage for an explanation of the message format.
        /// This message is only sent to an external user interface and is not logged.
        /// </summary>
        Progress = (1 << ((int)InstallMessage.Progress >> 24)),

        /// <summary>
        /// If this is not a quiet installation, then the basic UI has been initialized.
        /// If this is a full UI installation, the full UI is not yet initialized.
        /// This message is only sent to an external user interface and is not logged.
        /// </summary>
        Initialize = (1 << ((int)InstallMessage.Initilize >> 24)),

        /// <summary>
        /// If a full UI is being used, the full UI has ended.
        /// If this is not a quiet installation, the basic UI has not yet ended.
        /// This message is only sent to an external user interface and is not logged.
        /// </summary>
        Terminate = (1 << ((int)InstallMessage.Terminate >> 24)),

        /// <summary>
        /// Sent prior to display of the full UI dialog.
        /// This message is only sent to an external user interface and is not logged.
        /// </summary>
        ShowDialog = (1 << ((int)InstallMessage.ShowDialog >> 24)),

        /// <summary>
        /// Files in use information. When this message is received, a FilesInUse Dialog should be displayed.
        /// </summary>
        FilesInUse = (1 << ((int)InstallMessage.FilesInUse >> 24))
    }
}
