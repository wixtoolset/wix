// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    /// <summary>
    /// Options for building the cabinet.
    /// </summary>
    public enum CabinetBuildOption
    {
        /// <summary>
        /// Build the cabinet and move it to the target location.
        /// </summary>
        BuildAndMove,

        /// <summary>
        /// Build the cabinet and copy it to the target location.
        /// </summary>
        BuildAndCopy,

        /// <summary>
        /// Just copy the cabinet to the target location.
        /// </summary>
        Copy
    }
}
