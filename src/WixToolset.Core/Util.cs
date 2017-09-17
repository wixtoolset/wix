// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset
{
    using System;

    /// <summary>
    /// Common Wix utility methods and types.
    /// </summary>
    public sealed class Util
    {
        /// <summary>
        /// Set by WixToolTasks to indicate WIX is running inside MSBuild
        /// </summary>
        public static bool RunningInMsBuild { get; set; }
    }
}
