// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    public class VariableResolution
    {
        /// <summary>
        /// Indicates whether the variable should be delay resolved.
        /// </summary>
        public bool DelayedResolve { get; set; }

        /// <summary>
        /// Indicates whether the value is the default value of the variable.
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Indicates whether the value changed.
        /// </summary>
        public bool UpdatedValue { get; set; }

        /// <summary>
        /// Resolved value.
        /// </summary>
        public string Value { get; set; }
    }
}