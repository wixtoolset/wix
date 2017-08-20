// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System;
    using WixToolset.Data;

    /// <summary>
    /// Class to define the identifier and access for a row.
    /// </summary>
    public class Identifier
    {
        public static Identifier Invalid = new Identifier(null, AccessModifier.Private);

        public Identifier(string id, AccessModifier access)
        {
            this.Id = id;
            this.Access = access;
        }

        /// <summary>
        /// Access modifier for a row.
        /// </summary>
        public AccessModifier Access { get; private set; }

        /// <summary>
        /// Identifier for the row.
        /// </summary>
        public string Id { get; private set; }
    }
}
