// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Dependency
{
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// The compiler for the WiX Toolset Dependency Extension.
    /// </summary>
    public sealed class DependencyCompiler : BaseCompilerExtension
    {
        public override XNamespace Namespace => "http://wixtoolset.org/schemas/v4/wxs/dependency";

        /// <summary>
        /// Processes an attribute for the Compiler.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="parentElement">Parent element of attribute.</param>
        /// <param name="attribute">Attribute to process.</param>
        public override void ParseAttribute(Intermediate intermediate, IntermediateSection section, XElement parentElement, XAttribute attribute, IDictionary<string, string> context)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(parentElement);
            var addCheck = YesNoType.NotSet;
            var addRequire = YesNoType.NotSet;

            switch (parentElement.Name.LocalName)
            {
                case "Provides":
                    if (attribute.Name.LocalName == "Check" && parentElement.Parent?.Name.LocalName == "Component")
                    {
                        addCheck = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attribute);
                    }
                    break;
                case "Requires":
                case "RequiresRef":
                    if (attribute.Name.LocalName == "Enforce" && parentElement.Parent?.Parent?.Name.LocalName == "Component")
                    {
                        addRequire = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attribute);
                    }
                    break;
            }

            if (addCheck == YesNoType.NotSet && addRequire == YesNoType.NotSet)
            {
                this.ParseHelper.UnexpectedAttribute(parentElement, attribute);
            }
            else if (addCheck == YesNoType.Yes)
            {
                this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4DependencyCheck", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
            }
            else if (addRequire == YesNoType.Yes)
            {
                this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4DependencyRequire", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
            }
        }
    }
}
