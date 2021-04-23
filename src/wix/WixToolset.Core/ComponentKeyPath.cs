// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;

    internal class ComponentKeyPath : IComponentKeyPath
    {
        /// <summary>
        /// Identifier of the resource to be a key path.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Indicates whether the key path was explicitly set for this resource.
        /// </summary>
        public bool Explicit { get; set; }

        /// <summary>
        /// Type of resource to be the key path.
        /// </summary>
        public PossibleKeyPathType Type { get; set; }
    }
}
