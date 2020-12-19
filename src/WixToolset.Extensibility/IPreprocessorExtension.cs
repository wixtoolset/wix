// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System.Xml.Linq;
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Interface for extending the WiX toolset preprocessor.
    /// </summary>
    public interface IPreprocessorExtension
    {
        /// <summary>
        /// Gets the variable prefixes for the extension.
        /// </summary>
        /// <value>The variable prefixes for the extension.</value>
        string[] Prefixes { get; }

        /// <summary>
        /// Called at the beginning of the preprocessing of a source file.
        /// </summary>
        void PrePreprocess(IPreprocessContext context);

        /// <summary>
        /// Gets the value of a variable whose prefix matches the extension.
        /// </summary>
        /// <param name="prefix">The prefix of the variable to be processed by the extension.</param>
        /// <param name="name">The name of the variable.</param>
        /// <returns>The value of the variable or null if the variable is undefined.</returns>
        string GetVariableValue(string prefix, string name);

        /// <summary>
        /// Evaluates a function defined in the extension.
        /// </summary>
        /// <param name="prefix">The prefix of the function to be processed by the extension.</param>
        /// <param name="function">The name of the function.</param>
        /// <param name="args">The list of arguments.</param>
        /// <returns>The value of the function or null if the function is not defined.</returns>
        string EvaluateFunction(string prefix, string function, string[] args);

        /// <summary>
        /// Processes a pragma defined in the extension.
        /// </summary>
        /// <param name="prefix">The prefix of the pragma to be processed by the extension.</param>
        /// <param name="pragma">The name of the pragma.</param>
        /// <param name="args">The pragma's arguments.</param>
        /// <param name="parent">The parent node of the pragma.</param>
        /// <returns>false if the pragma is not defined.</returns>
        /// <comments>Don't return false for any condition except for unrecognized pragmas. Use Core.OnMessage for errors, warnings and messages.</comments>
        bool ProcessPragma(string prefix, string pragma, string args, XContainer parent);

        /// <summary>
        /// Called at the end of the preprocessing of a source file.
        /// </summary>
        void PostPreprocess(IPreprocessResult result);
    }
}
