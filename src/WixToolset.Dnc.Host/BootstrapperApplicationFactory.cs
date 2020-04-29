// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Dnc.Host
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Entry point for the .NET Core host to create and return the BA to the engine.
    /// Reflection is used instead of referencing WixToolset.Mba.Core directly to avoid requiring it in the AssemblyLoadContext.
    /// </summary>
    public sealed class BootstrapperApplicationFactory : IBootstrapperApplicationFactory
    {
        private string baFactoryAssemblyName;
        private string baFactoryAssemblyPath;

        public BootstrapperApplicationFactory(string baFactoryAssemblyName, string baFactoryAssemblyPath)
        {
            this.baFactoryAssemblyName = baFactoryAssemblyName;
            this.baFactoryAssemblyPath = baFactoryAssemblyPath;
        }

        /// <summary>
        /// Loads the bootstrapper application assembly and calls its IBootstrapperApplicationFactory.Create method.
        /// </summary>
        /// <param name="pArgs">Pointer to BOOTSTRAPPER_CREATE_ARGS struct.</param>
        /// <param name="pResults">Pointer to BOOTSTRAPPER_CREATE_RESULTS struct.</param>
        /// <exception cref="MissingAttributeException">The bootstrapper application assembly
        /// does not define the <see cref="BootstrapperApplicationFactoryAttribute"/>.</exception>
        public void Create(IntPtr pArgs, IntPtr pResults)
        {
            // Load the BA's IBootstrapperApplicationFactory.
            var baFactoryType = BootstrapperApplicationFactory.GetBAFactoryTypeFromAssembly(this.baFactoryAssemblyName, this.baFactoryAssemblyPath);
            var baFactory = Activator.CreateInstance(baFactoryType);
            if (null == baFactory)
            {
                throw new InvalidBootstrapperApplicationFactoryException();
            }

            var createMethod = baFactoryType.GetMethod(nameof(Create), new[] { typeof(IntPtr), typeof(IntPtr) });
            if (null == createMethod)
            {
                throw new InvalidBootstrapperApplicationFactoryException();
            }
            createMethod.Invoke(baFactory, new object[] { pArgs, pResults });
        }

        /// <summary>
        /// Locates the <see cref="BootstrapperApplicationFactoryAttribute"/> and returns the specified type.
        /// </summary>
        /// <param name="assemblyName">The assembly that defines the IBootstrapperApplicationFactory implementation.</param>
        /// <returns>The bootstrapper application factory <see cref="Type"/>.</returns>
        private static Type GetBAFactoryTypeFromAssembly(string assemblyName, string assemblyPath)
        {
            // The default ALC shouldn't need help loading the assembly, since the host should have provided the deps.json
            // when starting the runtime. But it doesn't hurt so keep this in case an isolated ALC is ever needed.
            var alc = new DnchostAssemblyLoadContext(assemblyPath, false);
            var asm = alc.LoadFromAssemblyName(new AssemblyName(assemblyName));

            var attr = asm.GetCustomAttributes()
                          .Where(a => a.GetType().FullName == "WixToolset.Mba.Core.BootstrapperApplicationFactoryAttribute")
                          .SingleOrDefault();

            if (null == attr)
            {
                throw new MissingAttributeException();
            }

            var baFactoryTypeProperty = attr.GetType().GetProperty("BootstrapperApplicationFactoryType", typeof(Type));
            if (baFactoryTypeProperty == null || baFactoryTypeProperty.GetMethod == null)
            {
                throw new MissingAttributeException();
            }

            var baFactoryType = (Type)baFactoryTypeProperty.GetMethod.Invoke(attr, null);
            return baFactoryType;
        }

        // Entry point for the DNC host.
        public static IBootstrapperApplicationFactory CreateBAFactory([MarshalAs(UnmanagedType.LPWStr)] string baFactoryAssemblyName, [MarshalAs(UnmanagedType.LPWStr)] string baFactoryAssemblyPath)
        {
            return new BootstrapperApplicationFactory(baFactoryAssemblyName, baFactoryAssemblyPath);
        }
    }
}
