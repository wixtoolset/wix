// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native.Msi
{
    using System;

    /// <summary>
    /// Windows Installer message types.
    /// </summary>
    [Flags]
    public enum InstallMessage
    {
        /// <summary>
        /// Premature termination, possibly fatal out of memory.
        /// </summary>
        FatalExit = 0x00000000,

        /// <summary>
        /// Formatted error message, [1] is message number in Error table.
        /// </summary>
        Error = 0x01000000,

        /// <summary>
        /// Formatted warning message, [1] is message number in Error table.
        /// </summary>
        Warning = 0x02000000,

        /// <summary>
        /// User request message, [1] is message number in Error table.
        /// </summary>
        User = 0x03000000,

        /// <summary>
        /// Informative message for log, not to be displayed.
        /// </summary>
        Info = 0x04000000,

        /// <summary>
        /// List of files in use that need to be replaced.
        /// </summary>
        FilesInUse = 0x05000000,

        /// <summary>
        /// Request to determine a valid source location.
        /// </summary>
        ResolveSource = 0x06000000,

        /// <summary>
        /// Insufficient disk space message.
        /// </summary>
        OutOfDiskSpace = 0x07000000,

        /// <summary>
        /// Progress: start of action, [1] action name, [2] description, [3] template for ACTIONDATA messages.
        /// </summary>
        ActionStart = 0x08000000,

        /// <summary>
        /// Action data. Record fields correspond to the template of ACTIONSTART message.
        /// </summary>
        ActionData = 0x09000000,

        /// <summary>
        /// Progress bar information. See the description of record fields below.
        /// </summary>
        Progress = 0x0A000000,

        /// <summary>
        /// To enable the Cancel button set [1] to 2 and [2] to 1. To disable the Cancel button set [1] to 2 and [2] to 0.
        /// </summary>
        CommonData = 0x0B000000,

        /// <summary>
        /// Sent prior to UI initialization, no string data.
        /// </summary>
        Initilize = 0x0C000000,

        /// <summary>
        /// Sent after UI termination, no string data.
        /// </summary>
        Terminate = 0x0D000000,

        /// <summary>
        /// Sent prior to display or authored dialog or wizard.
        /// </summary>
        ShowDialog = 0x0E000000
    }
}
