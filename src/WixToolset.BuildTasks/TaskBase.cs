// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BuildTasks
{
    using Microsoft.Build.Utilities;

    public abstract class TaskBase : Task
    {
        public string ToolPath { get; set; }

        public string AdditionalOptions { get; set; }

        public bool RunAsSeparateProcess { get; set; }

        /// <summary>
        /// Gets or sets whether all warnings should be suppressed.
        /// </summary>
        public bool SuppressAllWarnings { get; set; }

        /// <summary>
        /// Gets or sets a list of specific warnings to be suppressed.
        /// </summary>
        public string[] SuppressSpecificWarnings { get; set; }

        /// <summary>
        /// Gets or sets whether all warnings should be treated as errors.
        /// </summary>
        public bool TreatWarningsAsErrors { get; set; }

        /// <summary>
        /// Gets or sets a list of specific warnings to treat as errors.
        /// </summary>
        public string[] TreatSpecificWarningsAsErrors { get; set; }

        /// <summary>
        /// Gets or sets whether to display verbose output.
        /// </summary>
        public bool VerboseOutput { get; set; }

        /// <summary>
        /// Gets or sets whether to display the logo.
        /// </summary>
        public bool NoLogo { get; set; }

        public override bool Execute()
        {
            try
            {
                this.ExecuteCore();
            }
            catch (BuildException e)
            {
                this.Log.LogErrorFromException(e);
            }
            catch (Data.WixException e)
            {
                this.Log.LogErrorFromException(e);
            }

            return !this.Log.HasLoggedErrors;
        }

        protected abstract void ExecuteCore();
    }
}
