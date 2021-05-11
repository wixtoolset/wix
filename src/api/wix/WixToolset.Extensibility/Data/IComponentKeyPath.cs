// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
#pragma warning disable 1591 // TODO: add documentation
    public interface IComponentKeyPath
    {
        bool Explicit { get; set; }

        string Id { get; set; }

        PossibleKeyPathType Type { get; set; }
    }
}
