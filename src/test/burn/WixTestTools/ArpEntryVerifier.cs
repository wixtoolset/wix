// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTestTools
{
    using Xunit;

    public partial class ArpEntryInstaller
    {
        public bool TryGetRegistration(out GenericArpRegistration registration)
        {
            bool success = !this.PerMachine ? GenericArpRegistration.TryGetPerUserRegistrationById(this.ArpId, out registration)
                                            : GenericArpRegistration.TryGetPerMachineRegistrationById(this.ArpId, this.X64, out registration);

            return success;
        }

        public void VerifyRegistered(bool registered)
        {
            bool success = this.TryGetRegistration(out _);

            Assert.Equal(registered, success);
        }
    }
}
