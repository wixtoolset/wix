// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Diagnostics;
    using SimpleJson;

    /// <summary>
    /// Class to define the identifier and access for a tuple.
    /// </summary>
    [DebuggerDisplay("{Access} {Id,nq}")]
    public class Identifier
    {
        public static Identifier Invalid = new Identifier(AccessModifier.Private, (string)null);

        [Obsolete]
        public Identifier(string id, AccessModifier access)
        {
            this.Id = id;
            this.Access = access;
        }

        public Identifier(AccessModifier access, string id)
        {
            this.Access = access;
            this.Id = id;
        }

        public Identifier(AccessModifier access, params string[] ids)
        {
            this.Access = access;
            this.Id = String.Join("/", ids);
        }

        public Identifier(AccessModifier access, params object[] ids)
        {
            this.Access = access;
            this.Id = String.Join("/", ids);
        }

        public Identifier(AccessModifier access, int id)
        {
            this.Access = access;
            this.Id = id.ToString();
        }

        /// <summary>
        /// Access modifier for a tuple.
        /// </summary>
        public AccessModifier Access { get; }

        /// <summary>
        /// Identifier for the tuple.
        /// </summary>
        public string Id { get; }

        internal static Identifier Deserialize(JsonObject jsonObject)
        {
            var id = jsonObject.GetValueOrDefault<string>("id");
            var accessValue = jsonObject.GetValueOrDefault<string>("access");
            Enum.TryParse(accessValue, true, out AccessModifier access);

            return new Identifier(access, id);
        }

        internal JsonObject Serialize()
        {
            var jsonObject = new JsonObject
            {
                { "id", this.Id },
                { "access", this.Access.ToString().ToLowerInvariant() }
            };

            return jsonObject;
        }
    }
}
