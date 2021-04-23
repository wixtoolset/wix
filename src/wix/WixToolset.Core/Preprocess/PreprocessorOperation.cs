// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Preprocess
{
    /// <summary>
    /// Enumeration for preprocessor operations in if statements.
    /// </summary>
    internal enum PreprocessorOperation
    {
        /// <summary>The and operator.</summary>
        And,

        /// <summary>The or operator.</summary>
        Or,

        /// <summary>The not operator.</summary>
        Not
    }
}
