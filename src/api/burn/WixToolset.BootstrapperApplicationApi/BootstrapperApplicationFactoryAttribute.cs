// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperApplicationApi
{
    using System;

    /// <summary>
    /// This is no longer used.
    /// </summary>
    [Obsolete("Bootstrapper applications now run out of proc and do not use a BootstrapperApplicationFactory. Remove your BootstrapperApplicationFactory class. See https://wixtoolset.org/docs/fiveforfour/ for more details.")]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class BootstrapperApplicationFactoryAttribute : Attribute
    {
        /// <summary>
        /// This is no longer used.
        /// </summary>
        /// <param name="bootstrapperApplicationFactoryType">This is no longer used</param>
        public BootstrapperApplicationFactoryAttribute(Type bootstrapperApplicationFactoryType)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This is no longer used.
        /// </summary>
        public Type BootstrapperApplicationFactoryType
        {
            get { throw new NotImplementedException(); }
        }
    }
}
