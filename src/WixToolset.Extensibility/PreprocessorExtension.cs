// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System.Xml.Linq;
    using WixToolset.Data;

    /// <summary>
    /// Base class for creating a preprocessor extension.
    /// </summary>
    public abstract class PreprocessorExtension : IPreprocessorExtension
    {
        /// <summary>
        /// Gets or sets the preprocessor core for the extension.
        /// </summary>
        /// <value>Preprocessor core for the extension.</value>
        public IPreprocessorCore Core { get; set; }

        /// <summary>
        /// Gets or sets the variable prefixes for the extension.
        /// </summary>
        /// <value>The variable prefixes for the extension.</value>
        public virtual string[] Prefixes
        {
            get { return null; }
        }

        /// <summary>
        /// Called at the beginning of the preprocessing of a source file.
        /// </summary>
        public virtual void Initialize()
        {
        }

        /// <summary>
        /// Gets the value of a variable whose prefix matches the extension.
        /// </summary>
        /// <param name="prefix">The prefix of the variable to be processed by the extension.</param>
        /// <param name="name">The name of the variable.</param>
        /// <returns>The value of the variable or null if the variable is undefined.</returns>
        public virtual string GetVariableValue(string prefix, string name)
        {
            return null;
        }

        /// <summary>
        /// Evaluates a function defined in the extension.
        /// </summary>
        /// <param name="prefix">The prefix of the function to be processed by the extension.</param>
        /// <param name="function">The name of the function.</param>
        /// <param name="args">The list of arguments.</param>
        /// <returns>The value of the function or null if the function is not defined.</returns>
        public virtual string EvaluateFunction(string prefix, string function, string[] args)
        {
            return null;
        }

        /// <summary>
        /// Processes a pragma defined in the extension.
        /// </summary>
        /// <param name="sourceLineNumbers">The location of this pragma's PI.</param>
        /// <param name="prefix">The prefix of the pragma to be processed by the extension.</param>
        /// <param name="pragma">The name of the pragma.</param>
        /// <param name="args">The pragma's arguments.</param>
        /// <param name="parent">The parent node of the pragma.</param>
        /// <returns>false if the pragma is not defined.</returns>
        /// <comments>Don't return false for any condition except for unrecognized pragmas. Throw errors that are fatal to the compile. use core.OnMessage for warnings and messages.</comments>
        public virtual bool ProcessPragma(SourceLineNumber sourceLineNumbers, string prefix, string pragma, string args, XContainer parent)
        {
            return false;
        }

        /// <summary>
        /// Preprocess a document after normal preprocessing has completed.
        /// </summary>
        /// <param name="document">The document to preprocess.</param>
        public virtual void PreprocessDocument(XDocument document)
        {
        }

        /// <summary>
        /// Preprocesses a parameter.
        /// </summary>
        /// <param name="name">Name of parameter that matches extension.</param>
        /// <returns>The value of the parameter after processing.</returns>
        /// <remarks>By default this method will cause an error if its called.</remarks>
        public virtual string PreprocessParameter(string name)
        {
            return null;
        }

        /// <summary>
        /// Called at the end of the preprocessing of a source file.
        /// </summary>
        public virtual void Finish()
        {
        }
    }
}
