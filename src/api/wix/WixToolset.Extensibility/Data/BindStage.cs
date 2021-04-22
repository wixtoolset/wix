// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    /// <summary>
    /// 
    /// </summary>
    public enum BindStage
    {
        /// <summary>
        /// Normal binding
        /// </summary>
        Normal,

        /// <summary>
        /// Bind the file path of the target build file
        /// </summary>
        Target,

        /// <summary>
        /// Bind the file path of the updated build file
        /// </summary>
        Updated,
    }
}
