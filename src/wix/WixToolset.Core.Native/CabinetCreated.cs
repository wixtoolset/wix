// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native
{
    /// <summary>
    /// Cabinet created by compressing files.
    /// </summary>
    public sealed class CabinetCreated
    {
        /// <summary>
        /// Constructs CabinetCreated.
        /// </summary>
        /// <param name="cabinetName">Name of cabinet.</param>
        /// <param name="firstFileToken">Token of first file compressed in cabinet.</param>
        public CabinetCreated(string cabinetName, string firstFileToken)
        {
            this.CabinetName = cabinetName;
            this.FirstFileToken = firstFileToken;
        }

        /// <summary>
        /// Gets the name of the cabinet.
        /// </summary>
        public string CabinetName { get; }

        /// <summary>
        /// Gets the token of the first file in the cabinet.
        /// </summary>
        public string FirstFileToken { get; }
    }
}
