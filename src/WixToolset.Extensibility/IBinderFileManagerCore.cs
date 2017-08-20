// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System.Collections.Generic;
    using WixToolset.Data;

    public interface IBinderFileManagerCore : IMessageHandler
    {
        /// <summary>
        /// Gets or sets the path to cabinet cache.
        /// </summary>
        /// <value>The path to cabinet cache.</value>
        string CabCachePath { get; }

        /// <summary>
        /// Gets or sets the active subStorage used for binding.
        /// </summary>
        /// <value>The subStorage object.</value>
        SubStorage ActiveSubStorage { get; }

        /// <summary>
        /// Gets or sets the output object used for binding.
        /// </summary>
        /// <value>The output object.</value>
        Output Output { get; }

        /// <summary>
        /// Gets or sets the path to the temp files location.
        /// </summary>
        /// <value>The path to the temp files location.</value>
        string TempFilesLocation { get; }

        /// <summary>
        /// Gets the property if re-basing target is true or false
        /// </summary>
        /// <value>It returns true if target bind path is to be replaced, otherwise false.</value>
        bool RebaseTarget { get; }

        /// <summary>
        /// Gets the property if re-basing updated build is true or false
        /// </summary>
        /// <value>It returns true if updated bind path is to be replaced, otherwise false.</value>
        bool RebaseUpdated { get; }

        /// <summary>
        /// Gets the collection of paths to locate files during ResolveFile for the provided BindStage and name.
        /// </summary>
        /// <param name="stage">Optional stage to get bind paths for. Default is normal.</param>
        /// <param name="name">Optional name of the bind paths to get. Default is the unnamed paths.</param>
        /// <value>The bind paths to locate files.</value>
        IEnumerable<string> GetBindPaths(BindStage stage = BindStage.Normal, string name = null);
    }
}
