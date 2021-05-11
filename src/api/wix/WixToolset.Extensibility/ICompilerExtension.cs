// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Interface all compiler extensions implement.
    /// </summary>
    public interface ICompilerExtension
    {
        /// <summary>
        /// Gets the schema namespace for this extension.
        /// </summary>
        /// <value>Schema namespace supported by this extension.</value>
        XNamespace Namespace { get; }

        /// <summary>
        /// Called at the beginning of the compilation of a source file.
        /// </summary>
        void PreCompile(ICompileContext context);

        /// <summary>
        /// Processes an attribute for the Compiler.
        /// </summary>
        /// <param name="intermediate">Parent intermediate.</param>
        /// <param name="section">Parent section.</param>
        /// <param name="parentElement">Parent element of attribute.</param>
        /// <param name="attribute">Attribute to process.</param>
        /// <param name="context">Extra information about the context in which this element is being parsed.</param>
        void ParseAttribute(Intermediate intermediate, IntermediateSection section, XElement parentElement, XAttribute attribute, IDictionary<string, string> context);

        /// <summary>
        /// Processes an element for the Compiler.
        /// </summary>
        /// <param name="intermediate">Parent intermediate.</param>
        /// <param name="section">Parent section.</param>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="element">Element to process.</param>
        /// <param name="context">Extra information about the context in which this element is being parsed.</param>
        void ParseElement(Intermediate intermediate, IntermediateSection section, XElement parentElement, XElement element, IDictionary<string, string> context);

        /// <summary>
        /// Processes an element for the Compiler, with the ability to supply a component keypath.
        /// </summary>
        /// <param name="intermediate">Parent intermediate.</param>
        /// <param name="section">Parent section.</param>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="element">Element to process.</param>
        /// <param name="context">Extra information about the context in which this element is being parsed.</param>
        IComponentKeyPath ParsePossibleKeyPathElement(Intermediate intermediate, IntermediateSection section, XElement parentElement, XElement element, IDictionary<string, string> context);

        /// <summary>
        /// Called at the end of the compilation of a source file.
        /// </summary>
        void PostCompile(Intermediate intermediate);
    }
}
