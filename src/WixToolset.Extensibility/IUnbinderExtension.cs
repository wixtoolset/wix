// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System;
    using WixToolset.Data;

    /// <summary>
    /// Base class for creating an unbinder extension.
    /// </summary>
    public interface IUnbinderExtension
    {
        /// <summary>
        /// Called during the generation of sectionIds for an admin image.
        /// </summary>
        void GenerateSectionIds(Output output);
    }
}
