// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
#pragma warning disable 1591 // TODO: add documentation
    public interface IVariableResolution
    {
        bool DelayedResolve { get; set; }

        bool IsDefault { get; set; }

        bool UpdatedValue { get; set; }

        string Value { get; set; }
    }
}
