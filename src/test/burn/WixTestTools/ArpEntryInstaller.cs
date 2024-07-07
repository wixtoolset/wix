// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTestTools
{
    using System;
    using Xunit;

    public partial class ArpEntryInstaller : IDisposable
    {
        public ArpEntryInstaller(WixTestContext testContext, string id, bool perMachine = true, bool x64 = false)
        {
            this.ArpId = id;
            this.PerMachine = perMachine;
            this.X64 = x64;
            this.TestContext = testContext;
        }

        public string ArpId { get; }

        public bool PerMachine { get; }

        public bool X64 { get; }

        private WixTestContext TestContext { get; }

        public void Unregister(bool assertIfMissing = true)
        {
            if (this.TryGetRegistration(out var registration))
            {
                registration.Delete();
            }
            else
            {
                Assert.Fail("Tried to unregister when not registered.");
            }
        }

        public void Dispose()
        {
            this.Unregister(false);
        }
    }
}
