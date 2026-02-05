// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTestTools
{
    using Xunit;

    public partial class ArpEntryInstaller
    {
        public bool TryGetRegistration(out GenericArpRegistration registration)
        {
            var success = this.PerMachine
                ? GenericArpRegistration.TryGetPerMachineRegistrationById(this.ArpId, this.X64, this.TestContext.TestOutputHelper, out registration)
                : GenericArpRegistration.TryGetPerUserRegistrationById(this.ArpId, this.TestContext.TestOutputHelper, out registration);

            return success;
        }

        public void VerifyRegistered(bool registered)
        {
            var success = this.TryGetRegistration(out _);

            if (registered)
            {
                Assert.True(success);
            }
            else
            {
                Assert.False(success);
            }
        }
    }
}
