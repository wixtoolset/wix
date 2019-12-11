// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperCore
{
    using System;
    using System.Configuration;

    /// <summary>
    /// Handler for the wix.bootstrapper configuration section group.
    /// </summary>
    public class BootstrapperSectionGroup : ConfigurationSectionGroup
    {
        /// <summary>
        /// Creates a new instance of the <see cref="BootstrapperSectionGroup"/> class.
        /// </summary>
        public BootstrapperSectionGroup()
        {
        }

        /// <summary>
        /// Gets the <see cref="HostSection"/> handler for the mba configuration section.
        /// </summary>
        [ConfigurationProperty("host")]
        public HostSection Host
        {
            get { return (HostSection)base.Sections["host"]; }
        }
    }
}
