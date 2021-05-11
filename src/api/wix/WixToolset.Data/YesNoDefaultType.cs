// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    /// <summary>
    /// Yes, No, Default xml simple type.
    /// </summary>
    public enum YesNoDefaultType
    {
        /// <summary>Not a valid yes, no or default value.</summary>
        IllegalValue = -2,

        /// <summary>Value not set; equivalent to null for reference types.</summary>
        NotSet = -1,

        /// <summary>The no value.</summary>
        No,

        /// <summary>The yes value.</summary>
        Yes,

        /// <summary>The default value.</summary>
        Default,
    }
}
