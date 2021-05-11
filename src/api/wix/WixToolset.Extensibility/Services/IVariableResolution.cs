// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    /// <summary>
    /// Result when resolving a variable.
    /// </summary>
    public interface IVariableResolution
    {
        /// <summary>
        /// Indicates if the value contains variables that cannot be resolved yet.
        /// </summary>
        bool DelayedResolve { get; set; }

        /// <summary>
        /// Indicates whether a bind variables default value was used in the resolution.
        /// </summary>
        bool IsDefault { get; set; }

        /// <summary>
        /// Indicates whether the resolution updated the value.
        /// </summary>
        bool UpdatedValue { get; set; }

        /// <summary>
        /// The resolved value.
        /// </summary>
        string Value { get; set; }
    }
}
