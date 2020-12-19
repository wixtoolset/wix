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
        /// See <see cref="ICompilerExtension.ParseAttribute(Intermediate, IntermediateSection, XElement, XAttribute, IDictionary{string, string})"/>
        /// </summary>
        public virtual void ParseAttribute(Intermediate intermediate, IntermediateSection section, XElement parentElement, XAttribute attribute, IDictionary<string, string> context)
        {
            this.ParseHelper.UnexpectedAttribute(parentElement, attribute);
        }

        /// <summary>
        /// See <see cref="ICompilerExtension.ParseElement(Intermediate, IntermediateSection, XElement, XElement, IDictionary{string, string})"/>
        /// </summary>
        public virtual void ParseElement(Intermediate intermediate, IntermediateSection section, XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            this.ParseHelper.UnexpectedElement(parentElement, element);
        }

        /// <summary>
        /// See <see cref="ICompilerExtension.ParsePossibleKeyPathElement(Intermediate, IntermediateSection, XElement, XElement, IDictionary{string, string})"/>
        /// </summary>
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
