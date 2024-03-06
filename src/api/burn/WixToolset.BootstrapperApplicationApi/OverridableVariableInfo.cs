// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperApplicationApi
{
    /// <summary>
    /// Default implementation of <see cref="IOverridableVariableInfo"/>.
    /// </summary>
    internal class OverridableVariableInfo : IOverridableVariableInfo
    {
        /// <inheritdoc />
        public string Name { get; internal set; }

        internal OverridableVariableInfo() { }
    }
}
