// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    public class BinderFileManagerCore : IBinderFileManagerCore
    {
        private Dictionary<string, List<string>>[] bindPaths;

        /// <summary>
        /// Instantiate a new BinderFileManager.
        /// </summary>
        public BinderFileManagerCore()
        {
            this.bindPaths = new Dictionary<string, List<string>>[3];
            this.bindPaths[(int)BindStage.Normal] = new Dictionary<string, List<string>>();
            this.bindPaths[(int)BindStage.Target] = new Dictionary<string, List<string>>();
            this.bindPaths[(int)BindStage.Updated] = new Dictionary<string, List<string>>();
        }

        /// <summary>
        /// Gets or sets the path to cabinet cache.
        /// </summary>
        /// <value>The path to cabinet cache.</value>
        public string CabCachePath { get; set; }

        /// <summary>
        /// Gets or sets the active subStorage used for binding.
        /// </summary>
        /// <value>The subStorage object.</value>
        public SubStorage ActiveSubStorage { get; set; }

        /// <summary>
        /// Gets or sets the output object used for binding.
        /// </summary>
        /// <value>The output object.</value>
        public Output Output { get; set; }

        /// <summary>
        /// Gets or sets the path to the temp files location.
        /// </summary>
        /// <value>The path to the temp files location.</value>
        public string TempFilesLocation { get; set; }

        /// <summary>
        /// Gets the property if re-basing target is true or false
        /// </summary>
        /// <value>It returns true if target bind path is to be replaced, otherwise false.</value>
        public bool RebaseTarget
        {
            get { return this.bindPaths[(int)BindStage.Target].Any(); }
        }

        /// <summary>
        /// Gets the property if re-basing updated build is true or false
        /// </summary>
        /// <value>It returns true if updated bind path is to be replaced, otherwise false.</value>
        public bool RebaseUpdated
        {
            get { return this.bindPaths[(int)BindStage.Updated].Any(); }
        }

        public void AddBindPaths(IEnumerable<BindPath> paths, BindStage stage)
        {
            Dictionary<string, List<string>> dict = this.bindPaths[(int)stage];

            foreach (BindPath bindPath in paths)
            {
                List<string> values;
                if (!dict.TryGetValue(bindPath.Name, out values))
                {
                    values = new List<string>();
                    dict.Add(bindPath.Name, values);
                }

                if (!values.Contains(bindPath.Path))
                {
                    values.Add(bindPath.Path);
                }
            }
        }

        public IEnumerable<string> GetBindPaths(BindStage stage = BindStage.Normal, string name = null)
        {
            List<string> paths;
            if (this.bindPaths[(int)stage].TryGetValue(name ?? String.Empty, out paths))
            {
                return paths;
            }

            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// Sends a message to the message delegate if there is one.
        /// </summary>
        /// <param name="e">Message event arguments.</param>
        public void OnMessage(MessageEventArgs e)
        {
            Messaging.Instance.OnMessage(e);
        }
    }
}
