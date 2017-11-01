// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Class to define the identifier and access for a row.
    /// </summary>
    [DebuggerDisplay("{Access} {Id,nq}")]
    public class Identifier
    {
        public static Identifier Invalid = new Identifier(null, AccessModifier.Private);

        public Identifier(string id, AccessModifier access)
        {
            this.Id = id;
            this.Access = access;
        }

        public Identifier(int id, AccessModifier access)
        {
            this.Id = id.ToString();
            this.Access = access;
        }

        /// <summary>
        /// Access modifier for a row.
        /// </summary>
        public AccessModifier Access { get; }

        /// <summary>
        /// Identifier for the row.
        /// </summary>
        public string Id { get; }
    }
}
