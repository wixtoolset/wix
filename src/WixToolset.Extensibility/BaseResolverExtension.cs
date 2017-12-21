// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Data;
    using WixToolset.Data.Bind;

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
        /// Called at the beginning of the resolving variables and files.
        /// </summary>
        public virtual void PreResolve(IResolveContext context)
        {
            this.Context = context;
        }

        public virtual string ResolveFile(string source, IntermediateTupleDefinition tupleDefinition, SourceLineNumber sourceLineNumbers, BindStage bindStage)
        {
            return null;
        }

        /// <summary>
        /// Called at the end of resolve.
        /// </summary>
        public virtual void PostResolve(ResolveResult result)
        {
        }
    }
}
