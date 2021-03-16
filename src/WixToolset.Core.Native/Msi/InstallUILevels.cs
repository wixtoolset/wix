// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native.Msi
{
    using System;

    /// <summary>
    /// Windows Installer UI levels.
    /// </summary>
    [Flags]
    public enum InstallUILevels
    {
        /// <summary>
        /// No change in the UI level. However, if phWnd is not Null, the parent window can change.
        /// </summary>
        NoChange = 0,

        /// <summary>
        /// The installer chooses an appropriate user interface level.
        /// </summary>
        Default = 1,

        /// <summary>
        /// Completely silent installation.
        /// </summary>
        None = 2,

        /// <summary>
        /// Simple progress and error handling.
        /// </summary>
        Basic = 3,

        /// <summary>
        /// Authored user interface with wizard dialog boxes suppressed.
        /// </summary>
        Reduced = 4,

        /// <summary>
        /// Authored user interface with wizards, progress, and errors.
        /// </summary>
        Full = 5,

        /// <summary>
        /// If combined with the Basic value, the installer shows simple progress dialog boxes but
        /// does not display a Cancel button on the dialog. This prevents users from canceling the install.
        /// Available with Windows Installer version 2.0.
        /// </summary>
        HideCancel = 0x20,

        /// <summary>
        /// If combined with the Basic value, the installer shows simple progress
        /// dialog boxes but does not display any modal dialog boxes or error dialog boxes.
        /// </summary>
        ProgressOnly = 0x40,

        /// <summary>
        /// If combined with any above value, the installer displays a modal dialog
        /// box at the end of a successful installation or if there has been an error.
        /// No dialog box is displayed if the user cancels.
        /// </summary>
        EndDialog = 0x80,

        /// <summary>
        /// If this value is combined with the None value, the installer displays only the dialog
        /// boxes used for source resolution. No other dialog boxes are shown. This value has no
        /// effect if the UI level is not INSTALLUILEVEL_NONE. It is used with an external user
        /// interface designed to handle all of the UI except for source resolution. In this case,
        /// the installer handles source resolution. This value is only available with Windows Installer 2.0 and later.
        /// </summary>
        SourceResOnly = 0x100
    }
}
