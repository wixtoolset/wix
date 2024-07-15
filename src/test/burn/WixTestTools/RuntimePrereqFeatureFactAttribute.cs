// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTestTools
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Management;
    using System.Collections.Generic;
    using System.Security.Principal;
    using WixInternal.TestSupport.XunitExtensions;

    public class RuntimePrereqFeatureFactAttribute : RuntimeFactAttribute
    {
        public static HashSet<string> OptionalFeatures = new(StringComparer.OrdinalIgnoreCase);
        public static HashSet<string> ServerFeatures = new(StringComparer.OrdinalIgnoreCase);
        static RuntimePrereqFeatureFactAttribute()
        {
            AddFeaturesToSet(ServerFeatures, "Win32_ServerFeature");
            AddFeaturesToSet(OptionalFeatures, "Win32_OptionalFeature");
        }

        private static void AddFeaturesToSet(HashSet<string> featureSet, string featureSetName)
        {
            try
            {
                var objMC = new ManagementClass(featureSetName);
                var objMOC = objMC?.GetInstances();
                if (objMOC is not null)
                {
                    foreach (var objMO in objMOC)
                    {
                        string featureName = (string)objMO.Properties["Name"].Value;
                        if ((uint)objMO.Properties["InstallState"].Value == 1)
                        {
                            featureSet.Add(featureName);
                        }
                    }
                }
            }
            catch
            {
            }
        }

        public RuntimePrereqFeatureFactAttribute(params string[] prerequisiteFeatures) : base()
        {
            var missingRequirements = prerequisiteFeatures.Select(x => x).Where(x => !ServerFeatures.Contains(x) && !OptionalFeatures.Contains(x));

            if (missingRequirements.Any())
            {
                this.Skip = "This test is missing the following Feature pre-requisites: " + String.Join(", ", missingRequirements);
            }
        }
    }
}
