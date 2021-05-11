// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using System.Xml.Linq;
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Interface provided to help preprocessor extensions.
    /// </summary>
    public interface IPreprocessHelper
    {
        /// <summary>
        /// Add a variable.
        /// </summary>
        /// <param name="context">The preprocess context.</param>
        /// <param name="name">The variable name.</param>
        /// <param name="value">The variable value.</param>
        void AddVariable(IPreprocessContext context, string name, string value);

        /// <summary>
        /// Add a variable.
        /// </summary>
        /// <param name="context">The preprocess context.</param>
        /// <param name="name">The variable name.</param>
        /// <param name="value">The variable value.</param>
        /// <param name="showWarning">Set to true to show variable overwrite warning.</param>
        void AddVariable(IPreprocessContext context, string name, string value, bool showWarning);

        /// <summary>
        /// Evaluate a function.
        /// </summary>
        /// <param name="context">The preprocess context.</param>
        /// <param name="function">The function expression including the prefix and name.</param>
        /// <returns>The function value.</returns>
        string EvaluateFunction(IPreprocessContext context, string function);

        /// <summary>
        /// Evaluate a function.
        /// </summary>
        /// <param name="context">The preprocess context.</param>
        /// <param name="prefix">The function prefix.</param>
        /// <param name="function">The function name.</param>
        /// <param name="args">The arguments for the function.</param>
        /// <returns>The function value or null if the function is not defined.</returns>
        string EvaluateFunction(IPreprocessContext context, string prefix, string function, string[] args);

        /// <summary>
        /// Get the value of a variable expression like var.name.
        /// </summary>
        /// <param name="context">The preprocess context.</param>
        /// <param name="variable">The variable expression including the optional prefix and name.</param>
        /// <param name="allowMissingPrefix">true to allow the variable prefix to be missing.</param>
        /// <returns>The variable value.</returns>
        string GetVariableValue(IPreprocessContext context, string variable, bool allowMissingPrefix);

        /// <summary>
        /// Get the value of a variable.
        /// </summary>
        /// <param name="context">The preprocess context.</param>
        /// <param name="prefix">The variable prefix.</param>
        /// <param name="name">The variable name.</param>
        /// <returns>The variable value or null if the variable is not set.</returns>
        string GetVariableValue(IPreprocessContext context, string prefix, string name);

        /// <summary>
        /// Evaluate a Pragma.
        /// </summary>
        /// <param name="context">The preprocess context.</param>
        /// <param name="pragmaName">The pragma's full name (&lt;prefix&gt;.&lt;pragma&gt;).</param>
        /// <param name="args">The arguments to the pragma.</param>
        /// <param name="parent">The parent element of the pragma.</param>
        void PreprocessPragma(IPreprocessContext context, string pragmaName, string args, XContainer parent);

        /// <summary>
        /// Replaces parameters in the source text.
        /// </summary>
        /// <param name="context">The preprocess context.</param>
        /// <param name="value">Text that may contain parameters to replace.</param>
        /// <returns>Text after parameters have been replaced.</returns>
        string PreprocessString(IPreprocessContext context, string value);

        /// <summary>
        /// Remove a variable.
        /// </summary>
        /// <param name="context">The preprocess context.</param>
        /// <param name="name">The variable name.</param>
        void RemoveVariable(IPreprocessContext context, string name);
    }
}
