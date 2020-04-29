// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bal
{
    using System;
    using System.Linq;
    using WixToolset.Bal.Tuples;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Data.Tuples;
    using WixToolset.Extensibility;

    public class BalBurnBackendExtension : BaseBurnBackendExtension
    {
        public override void BundleFinalize()
        {
            base.BundleFinalize();

            var intermediate = this.Context.IntermediateRepresentation;
            var section = intermediate.Sections.Single();

            var baTuple = section.Tuples.OfType<WixBootstrapperApplicationTuple>().SingleOrDefault();
            var baId = baTuple?.Id?.Id;
            if (null == baId)
            {
                return;
            }

            var isStdBA = baId.StartsWith("WixStandardBootstrapperApplication");
            var isMBA = baId.StartsWith("ManagedBootstrapperApplicationHost");
            var isDNC = baId.StartsWith("DotNetCoreBootstrapperApplicationHost");
            var isSCD = isDNC && this.VerifySCD(section);

            if (isStdBA || isMBA || isDNC)
            {
                this.VerifyBAFunctions(section);
            }

            if (isMBA || (isDNC && !isSCD))
            {
                this.VerifyPrereqPackages(section, isDNC);
            }
        }

        private void VerifyBAFunctions(IntermediateSection section)
        {
            WixBalBAFunctionsTuple baFunctionsTuple = null;
            foreach (var tuple in section.Tuples.OfType<WixBalBAFunctionsTuple>())
            {
                if (null == baFunctionsTuple)
                {
                    baFunctionsTuple = tuple;
                }
                else
                {
                    this.Messaging.Write(BalErrors.MultipleBAFunctions(tuple.SourceLineNumbers));
                }
            }

            var payloadPropertiesTuples = section.Tuples.OfType<WixBundlePayloadTuple>().ToList();
            if (null == baFunctionsTuple)
            {
                foreach (var payloadPropertiesTuple in payloadPropertiesTuples)
                {
                    // TODO: Make core WiX canonicalize Name (this won't catch '.\bafunctions.dll').
                    if (string.Equals(payloadPropertiesTuple.Name, "bafunctions.dll", StringComparison.OrdinalIgnoreCase))
                    {
                        this.Messaging.Write(BalWarnings.UnmarkedBAFunctionsDLL(payloadPropertiesTuple.SourceLineNumbers));
                    }
                }
            }
            else
            {
                var payloadId = baFunctionsTuple.Id;
                var bundlePayloadTuple = payloadPropertiesTuples.Single(x => payloadId == x.Id);
                if (BurnConstants.BurnUXContainerName != bundlePayloadTuple.ContainerRef)
                {
                    this.Messaging.Write(BalErrors.BAFunctionsPayloadRequiredInUXContainer(baFunctionsTuple.SourceLineNumbers));
                }
            }
        }

        private void VerifyPrereqPackages(IntermediateSection section, bool isDNC)
        {
            var prereqInfoTuples = section.Tuples.OfType<WixMbaPrereqInformationTuple>().ToList();
            if (prereqInfoTuples.Count == 0)
            {
                var message = isDNC ? BalErrors.MissingDNCPrereq() : BalErrors.MissingMBAPrereq();
                this.Messaging.Write(message);
                return;
            }

            var foundLicenseFile = false;
            var foundLicenseUrl = false;

            foreach (var prereqInfoTuple in prereqInfoTuples)
            {
                if (null != prereqInfoTuple.LicenseFile)
                {
                    if (foundLicenseFile || foundLicenseUrl)
                    {
                        this.Messaging.Write(BalErrors.MultiplePrereqLicenses(prereqInfoTuple.SourceLineNumbers));
                        return;
                    }

                    foundLicenseFile = true;
                }

                if (null != prereqInfoTuple.LicenseUrl)
                {
                    if (foundLicenseFile || foundLicenseUrl)
                    {
                        this.Messaging.Write(BalErrors.MultiplePrereqLicenses(prereqInfoTuple.SourceLineNumbers));
                        return;
                    }

                    foundLicenseUrl = true;
                }
            }
        }

        private bool VerifySCD(IntermediateSection section)
        {
            var isSCD = false;

            var dncOptions = section.Tuples.OfType<WixDncOptionsTuple>().SingleOrDefault();
            if (dncOptions != null)
            {
                isSCD = dncOptions.SelfContainedDeployment != 0;
            }

            return isSCD;
        }
    }
}
