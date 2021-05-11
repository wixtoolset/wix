// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    /// <summary>
    /// Interface for a bind path.
    /// </summary>
    public interface IBindPath
    {
        /// <summary>
        /// Name of the bind path or String.Empty if the path is unnamed.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Path for the bind path.
        /// </summary>
        string Path { get; set; }

        /// <summary>
        /// Stage for the bind path.
        /// </summary>
        BindStage Stage { get; set; }
    }
}
