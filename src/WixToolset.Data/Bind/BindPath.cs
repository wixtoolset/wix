// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using WixToolset.Data.Bind;

    /// <summary>
    /// Bind path representation.
    /// </summary>
    public class BindPath
    {
        /// <summary>
        /// Creates an unnamed bind path.
        /// </summary>
        /// <param name="path">Path for the bind path.</param>
        public BindPath(string path) : this(String.Empty, path, BindStage.Normal)
        {
        }

        /// <summary>
        /// Creates a named bind path.
        /// </summary>
        /// <param name="name">Name of the bind path.</param>
        /// <param name="path">Path for the bind path.</param>
        /// <param name="stage">Stage for the bind path.</param>
        public BindPath(string name, string path, BindStage stage = BindStage.Normal)
        {
            this.Name = name;
            this.Path = path;
            this.Stage = stage;
        }

        /// <summary>
        /// Name of the bind path or String.Empty if the path is unnamed.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Path for the bind path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Stage for the bind path.
        /// </summary>
        public BindStage Stage { get; set; }

        /// <summary>
        /// Parses a normal bind path from its string representation
        /// </summary>
        /// <param name="bindPath">String representation of bind path that looks like: [name=]path</param>
        /// <returns>Parsed normal bind path.</returns>
        public static BindPath Parse(string bindPath)
        {
            string[] namedPath = bindPath.Split(new char[] { '=' }, 2);
            return (1 == namedPath.Length) ? new BindPath(namedPath[0]) : new BindPath(namedPath[0], namedPath[1]);
        }
    }
}
