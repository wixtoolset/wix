// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native
{
    /// <summary>
    /// Validation message type.
    /// </summary>
    public enum ValidationMessageType
    {
        /// <summary>
        /// Failure message reporting the failure of the ICE custom action.
        /// </summary>
        InternalFailure = 0,

        /// <summary>
        /// Error message reporting database authoring that case incorrect behavior.
        /// </summary>
        Error = 1,

        /// <summary>
        /// Warning message reporting database authoring that causes incorrect behavior in certain cases.
        /// Warnings can also report unexpected side-effects of database authoring.
        /// </summary>
        Warning = 2,

        /// <summary>
        /// Informational message.
        /// </summary>
        Info = 3,
    };
}
