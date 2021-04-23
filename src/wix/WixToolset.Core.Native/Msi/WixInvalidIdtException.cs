// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native.Msi
{
    using System;
    using WixToolset.Data;

    /// <summary>
    /// WiX invalid idt exception.
    /// </summary>
    [Serializable]
    public sealed class WixInvalidIdtException : WixException
    {
        /// <summary>
        /// Instantiate a new WixInvalidIdtException.
        /// </summary>
        public WixInvalidIdtException()
        {
        }

        /// <summary>
        /// Instantiate a new WixInvalidIdtException.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public WixInvalidIdtException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Instantiate a new WixInvalidIdtException.
        /// </summary>
        /// <param name="idtFile">The invalid idt file.</param>
        public WixInvalidIdtException(string idtFile) :
            base(ErrorMessages.InvalidIdt(new SourceLineNumber(idtFile), idtFile))
        {
        }

        /// <summary>
        /// Instantiate a new WixInvalidIdtException.
        /// </summary>
        /// <param name="idtFile">The invalid idt file.</param>
        /// <param name="tableName">The table name of the invalid idt file.</param>
        public WixInvalidIdtException(string idtFile, string tableName) :
            base(ErrorMessages.InvalidIdt(new SourceLineNumber(idtFile), idtFile, tableName))
        {
        }
    }
}
