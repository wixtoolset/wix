// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    public enum FileTransferType
    {
        /// <summary>
        /// Transfer of a file built during this build.
        /// </summary>
        Built,

        /// <summary>
        /// Transfer of a file contained in the output.
        /// </summary>
        Content,
    }
}
