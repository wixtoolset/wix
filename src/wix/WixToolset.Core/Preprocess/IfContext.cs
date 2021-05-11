// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Preprocess
{
    /// <summary>
    /// Context for an if statement in the preprocessor.
    /// </summary>
    internal class IfContext
    {
        private bool keep;

        /// <summary>
        /// Creates a default if context object, which are used for if's within an inactive preprocessor block
        /// </summary>
        public IfContext()
        {
            this.WasEverTrue = true;
            this.IfState = IfState.If;
        }

        /// <summary>
        /// Creates an if context object.
        /// </summary>
        /// <param name="active">Flag if context is currently active.</param>
        /// <param name="keep">Flag if context is currently true.</param>
        /// <param name="state">State of context to start in.</param>
        public IfContext(bool active, bool keep, IfState state)
        {
            this.Active = active;
            this.keep = keep;
            this.WasEverTrue = keep;
            this.IfState = IfState.If;
        }

        /// <summary>
        /// Gets and sets if this if context is currently active.
        /// </summary>
        /// <value>true if context is active.</value>
        public bool Active { get; set; }

        /// <summary>
        /// Gets and sets if context is current true.
        /// </summary>
        /// <value>true if context is currently true.</value>
        public bool IsTrue
        {
            get
            {
                return this.keep;
            }

            set
            {
                this.keep = value;
                if (this.keep)
                {
                    this.WasEverTrue = true;
                }
            }
        }

        /// <summary>
        /// Gets if the context was ever true.
        /// </summary>
        /// <value>True if context was ever true.</value>
        public bool WasEverTrue { get; private set; }

        /// <summary>
        /// Gets the current state of the if context.
        /// </summary>
        /// <value>Current state of context.</value>
        public IfState IfState { get; set; }
    }
}
