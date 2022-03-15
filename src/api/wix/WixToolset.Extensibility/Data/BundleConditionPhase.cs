// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    /// <summary>
    /// The Burn execution phase during which a Condition will be evaluated.
    /// </summary>
    public enum BundleConditionPhase
    {
        /// <summary>
        /// Condition is evaluated by the engine before loading the BootstrapperApplication (Bundle/@Condition).
        /// </summary>
        Startup,

        /// <summary>
        /// Condition is evaluated during Detect (ExePackage/@DetectCondition).
        /// </summary>
        Detect,

        /// <summary>
        /// Condition is evaluated during Plan (ExePackage/@InstallCondition).
        /// </summary>
        Plan,

        /// <summary>
        /// Condition is evaluated during Apply (MsiProperty/@Condition).
        /// </summary>
        Execute,

        /// <summary>
        /// Condition is evaluated after Apply.
        /// </summary>
        Shutdown,
    }
}
