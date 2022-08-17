// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Harvesters
{
    using WixToolset.Data.WindowsInstaller;
    using Wix = WixToolset.Harvesters.Serialize;

    internal static class DirectoryHelper
    {
        public static Wix.DirectoryBase CreateDirectory(string id)
        {
            if (WindowsInstallerStandard.IsStandardDirectory(id))
            {
                return new Wix.StandardDirectory()
                {
                    Id = id
                };
            }

            return new Wix.Directory()
            {
                Id = id
            };
        }

        public static Wix.DirectoryBase CreateDirectoryReference(string id)
        {
            if (WindowsInstallerStandard.IsStandardDirectory(id))
            {
                return new Wix.StandardDirectory()
                {
                    Id = id
                };
            }

            return new Wix.DirectoryRef()
            {
                Id = id
            };
        }
    }
}
