// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Link
{
    using System.Collections.Generic;
    using WixToolset.Data;

    /// <summary>
    /// Object that connects things (components/modules) to features.
    /// </summary>
    internal class ConnectToFeature
    {
        /// <summary>
        /// Creates a new connect to feature.
        /// </summary>
        /// <param name="section">Section this connect belongs to.</param>
        /// <param name="childId">Id of the child.</param>
        /// <param name="primaryFeature">Sets the primary feature for the connection.</param>
        /// <param name="explicitPrimaryFeature">Sets if this is explicit primary.</param>
        public ConnectToFeature(IntermediateSection section, string childId, string primaryFeature, bool explicitPrimaryFeature)
        {
            this.Section = section;
            this.ChildId = childId;

            this.PrimaryFeature = primaryFeature;
            this.IsExplicitPrimaryFeature = explicitPrimaryFeature;
        }

        /// <summary>
        /// Gets the section.
        /// </summary>
        /// <value>Section.</value>
        public IntermediateSection Section { get; }

        /// <summary>
        /// Gets the child identifier.
        /// </summary>
        /// <value>The child identifier.</value>
        public string ChildId { get; }

        /// <summary>
        /// Gets or sets if the flag for if the primary feature was set explicitly.
        /// </summary>
        /// <value>The flag for if the primary feature was set explicitly.</value>
        public bool IsExplicitPrimaryFeature { get; set; }

        /// <summary>
        /// Gets or sets the primary feature.
        /// </summary>
        /// <value>The primary feature.</value>
        public string PrimaryFeature { get; set; }

        /// <summary>
        /// Gets the features connected to.
        /// </summary>
        /// <value>Features connected to.</value>
        public List<string> ConnectFeatures { get; } = new List<string>();
    }
}
