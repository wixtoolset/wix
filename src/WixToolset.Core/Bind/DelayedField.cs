// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Bind
{
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Structure used to hold a row and field that contain binder variables, which need to be resolved
    /// later, once the files have been resolved.
    /// </summary>
    internal class DelayedField : IDelayedField
    {
        /// <summary>
        /// Basic constructor for struct
        /// </summary>
        /// <param name="row">Row for the field.</param>
        /// <param name="field">Field needing further resolution.</param>
        public DelayedField(IntermediateTuple row, IntermediateField field)
        {
            this.Row = row;
            this.Field = field;
        }

        /// <summary>
        /// The row containing the field.
        /// </summary>
        public IntermediateTuple Row { get; }

        /// <summary>
        /// The field needing further resolving.
        /// </summary>
        public IntermediateField Field { get; }
    }
}
