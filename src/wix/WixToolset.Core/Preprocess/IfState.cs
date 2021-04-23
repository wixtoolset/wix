// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Preprocess
{
    /// <summary>
    /// Current state of the if context.
    /// </summary>
    internal enum IfState
    {
        /// <summary>Context currently in unknown state.</summary>
        Unknown,

        /// <summary>Context currently inside if statement.</summary>
        If,

        /// <summary>Context currently inside elseif statement..</summary>
        ElseIf,

        /// <summary>Conext currently inside else statement.</summary>
        Else,
    }
}
