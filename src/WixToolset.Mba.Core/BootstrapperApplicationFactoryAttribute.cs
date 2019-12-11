// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperCore
{
    using System;

    /// <summary>
    /// Identifies the bootstrapper application factory class.
    /// </summary>
    /// <remarks>
    /// This required assembly attribute identifies the bootstrapper application factory class.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class BootstrapperApplicationFactoryAttribute : Attribute
    {
        private Type bootstrapperApplicationFactoryType;

        /// <summary>
        /// Creates a new instance of the <see cref="BootstrapperApplicationFactoryAttribute"/> class.
        /// </summary>
        /// <param name="bootstrapperApplicationFactoryType">The <see cref="Type"/> of the BA factory.</param>
        public BootstrapperApplicationFactoryAttribute(Type bootstrapperApplicationFactoryType)
        {
            this.bootstrapperApplicationFactoryType = bootstrapperApplicationFactoryType;
        }

        /// <summary>
        /// Gets the type of the bootstrapper application factory class to create.
        /// </summary>
        public Type BootstrapperApplicationFactoryType
        {
            get { return this.bootstrapperApplicationFactoryType; }
        }
    }
}
