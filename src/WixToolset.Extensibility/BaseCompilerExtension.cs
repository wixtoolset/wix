// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Base class for creating a compiler extension.
    /// </summary>
    public abstract class BaseCompilerExtension : ICompilerExtension
    {
        /// <summary>
        /// Context for use by the extension.
        /// </summary>
        protected ICompileContext Context { get; private set; }

        /// <summary>
        /// Messaging for use by the extension.
        /// </summary>
        protected IMessaging Messaging { get; private set; }

        /// <summary>
        /// ParserHelper for use by the extension.
        /// </summary>
        protected IParseHelper ParseHelper { get; private set; }

        /// <summary>
        /// Gets the schema namespace for this extension.
        /// </summary>
        /// <value>Schema namespace supported by this extension.</value>
        public abstract XNamespace Namespace { get; }

        /// <summary>
        /// Creates a component key path.
        /// </summary>
        protected IComponentKeyPath CreateComponentKeyPath() => this.Context.ServiceProvider.GetService<IComponentKeyPath>();

        /// <summary>
        /// Called at the beginning of the compilation of a source file.
        /// </summary>
        public virtual void PreCompile(ICompileContext context)
        {
            this.Context = context;

            this.Messaging = context.ServiceProvider.GetService<IMessaging>();

            this.ParseHelper = context.ServiceProvider.GetService<IParseHelper>();
        }

        /// <summary>
        /// Processes an attribute for the Compiler.
        /// </summary>
        /// <param name="parentElement">Parent element of attribute.</param>
        /// <param name="attribute">Attribute to process.</param>
        /// <param name="context">Extra information about the context in which this element is being parsed.</param>
        public virtual void ParseAttribute(Intermediate intermediate, IntermediateSection section, XElement parentElement, XAttribute attribute, IDictionary<string, string> context)
        {
            this.ParseHelper.UnexpectedAttribute(parentElement, attribute);
        }

        /// <summary>
        /// Processes an element for the Compiler.
        /// </summary>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="element">Element to process.</param>
        /// <param name="contextValues">Extra information about the context in which this element is being parsed.</param>
        public virtual void ParseElement(Intermediate intermediate, IntermediateSection section, XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            this.ParseHelper.UnexpectedElement(parentElement, element);
        }

        /// <summary>
        /// Processes an element for the Compiler, with the ability to supply a component keypath.
        /// </summary>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="element">Element to process.</param>
        /// <param name="keyPath">Explicit key path.</param>
        /// <param name="contextValues">Extra information about the context in which this element is being parsed.</param>
        public virtual IComponentKeyPath ParsePossibleKeyPathElement(Intermediate intermediate, IntermediateSection section, XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            this.ParseElement(intermediate, section, parentElement, element, context);
            return null;
        }

        /// <summary>
        /// Called at the end of the compilation of a source file.
        /// </summary>
        public virtual void PostCompile(Intermediate intermediate)
        {
        }
    }
}
