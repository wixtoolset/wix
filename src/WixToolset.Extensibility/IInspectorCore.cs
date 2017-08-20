// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Data;

    /// <summary>
    /// Core facilities for inspector extensions.
    /// </summary>
    public interface IInspectorCore : IMessageHandler
    {
        /// <summary>
        /// Gets whether an error occured.
        /// </summary>
        bool EncounteredError { get; }
    }
}
