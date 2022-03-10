// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    /// <summary>
    /// Implemented by extensions to create backends.
    /// </summary>
    public interface IBackendFactory
    {
        /// <summary>
        /// Called to find the backend used to produce the requested output type.
        /// </summary>
        /// <param name="outputType">Type of output being created.</param>
        /// <param name="outputPath">Path to the output to create.</param>
        /// <param name="backend">The backend for the output.</param>
        /// <returns>True if the backend was created, otherwise false.</returns>
        bool TryCreateBackend(string outputType, string outputPath, out IBackend backend);
    }
}
