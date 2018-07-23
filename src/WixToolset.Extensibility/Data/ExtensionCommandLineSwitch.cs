// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    /// <summary>
    /// A command line option.
    /// </summary>
    public struct ExtensionCommandLineSwitch
    {
        public string Switch { get; set; }

        public string Description { get; set; }
    }
}
