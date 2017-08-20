// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Data;

    public interface IPreprocessorCore : IMessageHandler
    {
        /// <summary>
        /// Gets or sets the platform which the compiler will use when defaulting 64-bit attributes and elements.
        /// </summary>
        /// <value>The platform which the compiler will use when defaulting 64-bit attributes and elements.</value>
        Platform CurrentPlatform { get; }

        /// <summary>
        /// Gets whether the core encountered an error while processing.
        /// </summary>
        /// <value>Flag if core encountered an error during processing.</value>
        bool EncounteredError { get; }
    }
}
