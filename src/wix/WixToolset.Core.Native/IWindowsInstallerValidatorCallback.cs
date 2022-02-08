// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native
{
    using WixToolset.Data;

    /// <summary>
    /// Callbacks during validation.
    /// </summary>
    public interface IWindowsInstallerValidatorCallback
    {
        /// <summary>
        /// Indicates if the validator callback encountered an error.
        /// </summary>
        bool EncounteredError { get; }

        /// <summary>
        /// Validation message from an ICE.
        /// </summary>
        /// <param name="message">The validation message.</param>
        /// <returns>True if validation should continue; otherwise cancel the validation.</returns>
        bool ValidationMessage(ValidationMessage message);

        /// <summary>
        /// Normal message encountered while preparing for ICE validation.
        /// </summary>
        /// <param name="message">The message to write.</param>
        void WriteMessage(Message message);
    }
}
