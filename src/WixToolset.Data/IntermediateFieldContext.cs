// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;

    public class IntermediateFieldContext : IDisposable
    {
        private readonly string previous;
        private bool disposed;

        public IntermediateFieldContext(string context)
        {
            this.previous = IntermediateFieldExtensions.valueContext;

            IntermediateFieldExtensions.valueContext = context;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    IntermediateFieldExtensions.valueContext = this.previous;
                }

                this.disposed = true;
            }
        }
    }
}
