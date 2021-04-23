// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    internal class ParsedWixVariable
    {
        public int Index { get; set; }

        public int Length { get; set; }

        public string Namespace { get; set; }

        public string Name { get; set; }

        public string Scope { get; set; }

        public string DefaultValue { get; set; }
    }
}
