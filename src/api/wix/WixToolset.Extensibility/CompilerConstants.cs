// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System;

    /// <summary>
    /// Constants used by compiler.
    /// </summary>
    public static class CompilerConstants
    {
        /// <summary>
        /// 
        /// </summary>
        public const int IntegerNotSet = int.MinValue;

        /// <summary>
        /// 
        /// </summary>
        public const int IllegalInteger = int.MinValue + 1;

        /// <summary>
        /// 
        /// </summary>
        public const long LongNotSet = long.MinValue;

        /// <summary>
        /// 
        /// </summary>
        public const long IllegalLong = long.MinValue + 1;

        /// <summary>
        /// 
        /// </summary>
        public const string IllegalGuid = "IllegalGuid";

        /// <summary>
        /// 
        /// </summary>
        public static readonly Version IllegalVersion = new Version(Int32.MaxValue, Int32.MaxValue, Int32.MaxValue, Int32.MaxValue);
    }
}
