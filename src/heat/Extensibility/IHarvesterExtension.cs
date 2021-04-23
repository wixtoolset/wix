// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Harvesters.Extensibility
{
    using Wix = WixToolset.Harvesters.Serialize;

#pragma warning disable 1591 // TODO: add documentation
    public interface IHarvesterExtension
    {
        IHarvesterCore Core { get; set; }

        Wix.Fragment[] Harvest(string argument);
    }
}
