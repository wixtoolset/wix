// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    /// <summary>
    /// Location where the localization was loaded.
    /// </summary>
    public enum LocalizationLocation
    {
        /// <summary>
        /// Localization loaded from .wxl source file.
        /// </summary>
        Source,

        /// <summary>
        /// Localization loaded from .wixlib library.
        /// </summary>
        Library,

        /// <summary>
        /// Localization loaded from .wixlib library within .wixext WixExtension.
        /// </summary>
        Extension,

        /// <summary>
        /// Localization placed in .wixext WixExtension as the default culture localization for the
        /// WixExtension.
        /// </summary>
        ExtensionDefaultCulture
    }
}
