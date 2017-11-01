// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Text.RegularExpressions;

    internal static class Common
    {
        private static readonly Regex LegalIdentifierCharacters = new Regex(@"^[_A-Za-z][0-9A-Za-z_\.]*$", RegexOptions.Compiled);

        public static bool IsIdentifier(string value)
        {
            if (!String.IsNullOrEmpty(value))
            {
                if (LegalIdentifierCharacters.IsMatch(value))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
