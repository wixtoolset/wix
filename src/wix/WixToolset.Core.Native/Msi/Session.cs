// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native.Msi
{
    using System.Globalization;

    /// <summary>
    /// Controls the installation process.
    /// </summary>
    public sealed class Session : MsiHandle
    {
        /// <summary>
        /// Instantiate a new Session.
        /// </summary>
        /// <param name="database">The database to open.</param>
        public Session(Database database)
        {
            var packagePath = "#" + database.Handle.ToString(CultureInfo.InvariantCulture);

            var error = MsiInterop.MsiOpenPackage(packagePath, out var handle);
            if (0 != error)
            {
                throw new MsiException(error);
            }

            this.Handle = handle;
        }

        /// <summary>
        /// Executes a built-in action, custom action, or user-interface wizard action.
        /// </summary>
        /// <param name="action">Specifies the action to execute.</param>
        public void DoAction(string action)
        {
            var error = MsiInterop.MsiDoAction(this.Handle, action);
            if (0 != error)
            {
                throw new MsiException(error);
            }
        }
    }
}
