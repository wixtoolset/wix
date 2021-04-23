// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using WixToolset.Data.WindowsInstaller;

    internal class PatchTransform
    {
        public PatchTransform(string baseline, WindowsInstallerData transform)
        {
            this.Baseline = baseline;
            this.Transform = transform;
        }

        public string Baseline { get; }

        public WindowsInstallerData Transform { get; }
    }
}
