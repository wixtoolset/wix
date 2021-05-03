// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Dnc.Host
{
    using System;
    using System.Reflection;
    using System.Runtime.Loader;

    public sealed class DnchostAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver resolver;

        public DnchostAssemblyLoadContext(string assemblyPath, bool isolateFromDefault)
            : base(nameof(DnchostAssemblyLoadContext), isolateFromDefault)
        {
            this.resolver = new AssemblyDependencyResolver(assemblyPath);

            if (!this.IsCollectible)
            {
                AssemblyLoadContext.Default.Resolving += this.ResolveAssembly;
                AssemblyLoadContext.Default.ResolvingUnmanagedDll += this.ResolveUnmanagedDll;
            }
        }

        private Assembly ResolveAssembly(AssemblyLoadContext defaultAlc, AssemblyName assemblyName)
        {
            var path = this.resolver.ResolveAssemblyToPath(assemblyName);
            if (path != null)
            {
                var targetAlc = this.IsCollectible ? this : defaultAlc;
                return targetAlc.LoadFromAssemblyPath(path);
            }

            return null;
        }

        private IntPtr ResolveUnmanagedDll(Assembly assembly, string unmanagedDllName)
        {
            var path = this.resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (path != null)
            {
                return this.LoadUnmanagedDllFromPath(path);
            }

            return IntPtr.Zero;
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            return this.ResolveAssembly(AssemblyLoadContext.Default, assemblyName);
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            return this.ResolveUnmanagedDll(null, unmanagedDllName);
        }
    }
}
