// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Data
{
    using System;
    using System.Data;
    using System.Threading.Tasks;
    using WixToolset.Data;
    using Xunit;

    public class ExceptionFixture
    {
        [Fact]
        public async Task CanGetSimpleWixException()
        {
            var e = await Assert.ThrowsAsync<WixException>(() => throw new WixException("All hands, abandon ship"));
            Assert.Equal("All hands, abandon ship", e.Message);
        }

        [Fact]
        public async Task CanGetSimpleWixExceptionFromMessage()
        {
            var message = ErrorMessages.CommandLineCommandRequired();
            var e = await Assert.ThrowsAsync<WixException>(() => throw new WixException(message, new ArgumentNullException(), detailed: true));
            Assert.Equal("A command is required. Add -h for list of available subcommands. : System.ArgumentNullException: Value cannot be null.", e.Message);
        }

        [Fact]
        public async Task CanGetModeratelyDetailedWixException()
        {
            var source = SourceLineNumber.CreateFromEncoded("uhoh.wxs*47");
            var message = new Message(source, MessageLevel.Error, 666, "The monsters have escaped. Run for your lives.");

            var e = await Assert.ThrowsAsync<WixException>(() => throw new WixException(message, exception: null, detailed: true));
            Assert.Equal("uhoh.wxs(47) : The monsters have escaped. Run for your lives.", e.Message);
        }

        [Fact]
        public async Task CanGetVeryDetailedWixException()
        {
            var source = SourceLineNumber.CreateFromEncoded("errorful.wxs*42");
            var message = new Message(source, MessageLevel.Error, 666, "The unexpected has occurred. Prepare yourself.");

            var e = await Assert.ThrowsAsync<WixException>(() => throw new WixException(message, new ArgumentNullException(), detailed: true));
            Assert.Equal("errorful.wxs(42) : The unexpected has occurred. Prepare yourself. : System.ArgumentNullException: Value cannot be null.", e.Message);
        }
    }
}
