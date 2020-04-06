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
    using WixToolset.Extensibility.Data;

    public class BalBurnBackendExtension : BaseBurnBackendExtension
    {
        public override void PostBackendBind(IBindResult result)
        {
            base.PostBackendBind(result);

            if (result.Wixout == null)
            {
                this.Messaging.Write(new Message(null, MessageLevel.Warning, 1, "BurnBackend didn't provide Wixout so skipping BalExtension PostBind verification."));
                return;
            }

            var intermediate = Intermediate.Load(result.Wixout);
            var section = intermediate.Sections.Single();

            var baTuple = section.Tuples.OfType<WixBootstrapperApplicationTuple>().SingleOrDefault();
            var baId = baTuple?.Id?.Id;
            if (null == baId)
            {
                return;
            }

            var isStdBA = baId.StartsWith("WixStandardBootstrapperApplication");
            var isMBA = baId.StartsWith("ManagedBootstrapperApplicationHost");

            if (isStdBA || isMBA)
            {
                this.VerifyBAFunctions(section);
            }

            if (isMBA)
            {
                this.VerifyPrereqPackages(section);
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

        private void VerifyPrereqPackages(IntermediateSection section)
        {
            var prereqInfoTuples = section.Tuples.OfType<WixMbaPrereqInformationTuple>().ToList();
            if (prereqInfoTuples.Count == 0)
            {
                this.Messaging.Write(BalErrors.MissingPrereq());
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
    }
}
