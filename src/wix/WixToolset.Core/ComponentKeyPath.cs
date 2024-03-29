// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;

    internal class ComponentKeyPath : IComponentKeyPath
    {
        public Identifier Id { get; set; }

        public bool Explicit { get; set; }

        public PossibleKeyPathType Type { get; set; }
    }
}
