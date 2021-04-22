// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    public enum EmptyRule
    {
        /// <summary>
        /// The trimmed value cannot be empty.
        /// </summary>
        MustHaveNonWhitespaceCharacters,

        /// <summary>
        /// The trimmed value can be empty, but the value itself cannot be empty.
        /// </summary>
        CanBeWhitespaceOnly,

        /// <summary>
        /// The value can be empty.
        /// </summary>
        CanBeEmpty
    }
}
