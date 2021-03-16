// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native
{
    using System.Collections.Generic;

    /// <summary>
    /// Message from ICE
    /// </summary>
    public class ValidationMessage
    {
        /// <summary>
        /// Name of the ICE providing the message.
        /// </summary>
        public string IceName { get; set; }

        /// <summary>
        /// Validation type.
        /// </summary>
        public ValidationMessageType Type { get; set; }

        /// <summary>
        /// Message text.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Optional help URL for the message.
        /// </summary>
        public string HelpUrl { get; set; }

        /// <summary>
        /// Optional table causing the message.
        /// </summary>
        public string Table { get; set; }

        /// <summary>
        /// Optional column causing the message.
        /// </summary>
        public string Column { get; set; }

        /// <summary>
        /// Optional primary keys causing the message.
        /// </summary>
        public IEnumerable<string> PrimaryKeys { get; set; }
    }
}
