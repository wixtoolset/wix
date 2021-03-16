// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native.Ole32
{
    /// <summary>
    /// Specifies the access mode to use when opening, creating, or deleting a storage object.
    /// </summary>
    internal enum StorageMode
    {
        /// <summary>
        /// Indicates that the object is read-only, meaning that modifications cannot be made.
        /// </summary>
        Read = 0x0,

        /// <summary>
        /// Enables you to save changes to the object, but does not permit access to its data.
        /// </summary>
        Write = 0x1,

        /// <summary>
        /// Enables access and modification of object data.
        /// </summary>
        ReadWrite = 0x2,

        /// <summary>
        /// Specifies that subsequent openings of the object are not denied read or write access.
        /// </summary>
        ShareDenyNone = 0x40,

        /// <summary>
        /// Prevents others from subsequently opening the object in Read mode.
        /// </summary>
        ShareDenyRead = 0x30,

        /// <summary>
        /// Prevents others from subsequently opening the object for Write or ReadWrite access.
        /// </summary>
        ShareDenyWrite = 0x20,

        /// <summary>
        /// Prevents others from subsequently opening the object in any mode.
        /// </summary>
        ShareExclusive = 0x10,

        /// <summary>
        /// Opens the storage object with exclusive access to the most recently committed version.
        /// </summary>
        Priority = 0x40000,

        /// <summary>
        /// Indicates that an existing storage object or stream should be removed before the new object replaces it.
        /// </summary>
        Create = 0x1000,
    }
}
