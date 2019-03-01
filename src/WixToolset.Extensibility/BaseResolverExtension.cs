// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Base class for creating a resolver extension.
    /// </summary>
    public abstract class BaseResolverExtension : IResolverExtension
    {
        /// <summary>
        /// Context for use by the extension.
        /// </summary>
        protected IResolveContext Context { get; private set; }

        /// <summary>
        /// Messaging for use by the extension.
        /// </summary>
        protected IMessaging Messaging { get; private set; }

        /// <summary>
        /// Creates a resolve file result.
        /// </summary>
        protected IResolveFileResult CreateResolveFileResult() => this.Context.ServiceProvider.GetService<IResolveFileResult>();

        /// <summary>
        /// Called at the beginning of the resolving variables and files.
        /// </summary>
        public virtual void PreResolve(IResolveContext context)
        {
            this.Context = context;

            this.Messaging = context.ServiceProvider.GetService<IMessaging>();
        }

        public virtual IResolveFileResult ResolveFile(string source, IntermediateTupleDefinition tupleDefinition, SourceLineNumber sourceLineNumbers, BindStage bindStage)
        {
            return null;
        }

        /// <summary>
        /// Called at the end of resolve.
        /// </summary>
        public virtual void PostResolve(IResolveResult result)
        {
        }
    }
}
