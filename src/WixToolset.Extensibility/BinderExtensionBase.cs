// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Data;
    using WixToolset.Data.Bind;
    using WixToolset.Extensibility.Services;

    public abstract class BinderExtensionBase : IBinderExtension
    {
        protected IBindContext Context { get; private set; }

        /// <summary>
        /// Called before binding occurs.
        /// </summary>
        public virtual void PreBind(IBindContext context)
        {
            this.Context = context;
        }

        /// <summary>
        /// Called after variable resolution occurs.
        /// </summary>
        public virtual void AfterResolvedFields(Intermediate intermediate)
        {
        }

        public virtual string ResolveFile(string source, string type, SourceLineNumber sourceLineNumbers, BindStage bindStage)
        {
            return null;
        }

        public virtual bool? CompareFiles(string targetFile, string updatedFile)
        {
            return null;
        }

        public virtual bool CopyFile(string source, string destination, bool overwrite)
        {
            return false;
        }

        public virtual bool MoveFile(string source, string destination, bool overwrite)
        {
            return false;
        }

        /// <summary>
        /// Called after binding is complete before the files are moved to their final locations.
        /// </summary>
        public virtual void PostBind(BindResult result)
        {
        }
    }
}
