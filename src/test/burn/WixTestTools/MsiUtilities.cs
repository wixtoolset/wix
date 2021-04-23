// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTestTools
{
    using System;
    using WixToolset.Dtf.WindowsInstaller;

    public class MsiUtilities
    {
        /// <summary>
        /// Return true if it finds the given productcode in system otherwise it returns false
        /// </summary>
        /// <param name="prodCode"></param>
        /// <returns></returns>
        public static bool IsProductInstalled(string prodCode)
        {
            //look in all user's products (both per-machine and per-user)
            foreach (ProductInstallation product in ProductInstallation.GetProducts(null, "s-1-1-0", UserContexts.All))
            {
                if (product.ProductCode == prodCode)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Return true if it finds the given productcode in system with the specified version otherwise it returns false
        /// </summary>
        /// <param name="prodCode"></param>
        /// <param name="prodVersion"></param>
        /// <returns></returns>
        public static bool IsProductInstalledWithVersion(string prodCode, Version prodVersion)
        {
            //look in all user's products (both per-machine and per-user)
            foreach (ProductInstallation product in ProductInstallation.GetProducts(null, "s-1-1-0", UserContexts.All))
            {
                if (product.ProductCode == prodCode && product.ProductVersion == prodVersion)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
