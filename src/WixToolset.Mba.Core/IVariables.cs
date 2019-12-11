// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperCore
{
    using System;

    /// <summary>
    /// An accessor for numeric, string, and version variables for the engine.
    /// </summary>
    public interface IVariables<T>
    {
        /// <summary>
        /// Gets or sets the variable given by <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the variable to get/set.</param>
        /// <returns>The value of the given variable.</returns>
        /// <exception cref="Exception">An error occurred getting the variable.</exception>
        T this[string name] { get; set; }

        /// <summary>
        /// Gets whether the variable given by <paramref name="name"/> exists.
        /// </summary>
        /// <param name="name">The name of the variable to check.</param>
        /// <returns>True if the variable given by <paramref name="name"/> exists; otherwise, false.</returns>
        bool Contains(string name);
    }
}
