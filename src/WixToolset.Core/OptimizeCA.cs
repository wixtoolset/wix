// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;

    /// <summary>
    /// Values for the OptimizeCA MsiPatchMetdata property, which indicates whether custom actions can be skipped when applying the patch.
    /// </summary>
    [Flags]
    public enum OptimizeCA // TODO: review where to place this data so it can not be exposed by WixToolset.Core
    {
        /// <summary>
        /// No custom actions are skipped.
        /// </summary>
        None = 0,

        /// <summary>
        /// Skip property (type 51) and directory (type 35) assignment custom actions.
        /// </summary>
        SkipAssignment = 1,

        /// <summary>
        /// Skip immediate custom actions that are not property or directory assignment custom actions.
        /// </summary>
        SkipImmediate = 2,

        /// <summary>
        /// Skip custom actions that run within the script.
        /// </summary>
        SkipDeferred = 4,
    }
}
