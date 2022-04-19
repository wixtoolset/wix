// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Dnc.HostGenerator
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Testing;
    using Microsoft.CodeAnalysis.Text;
    using WixToolset.Dnc.HostGenerator;
    using WixToolset.Mba.Core;
    using Xunit;

    using VerifyCS = CSharpSourceGeneratorVerifier<WixToolset.Dnc.HostGenerator.DncHostGenerator>;

    public class DncHostGeneratorTests
    {
        static readonly MetadataReference MbaCoreAssembly = MetadataReference.CreateFromFile(typeof(BootstrapperApplicationFactoryAttribute).Assembly.Location);

        [Fact]
        public async Task FailsBuildWhenMissingAttribute()
        {
            var code = @"
//[assembly: WixToolset.Mba.Core.BootstrapperApplicationFactory(typeof(Test.BAFactory))]
namespace Test
{
    using WixToolset.Mba.Core;

    public class BAFactory : BaseBootstrapperApplicationFactory
    {
        protected override IBootstrapperApplication Create(IEngine engine, IBootstrapperCommand bootstrapperCommand)
        {
            return null;
        }
    }
}
";

            await new VerifyCS.Test
            {
                TestState = 
                {
                    Sources = { code },
                    AdditionalReferences = { MbaCoreAssembly },
                    ExpectedDiagnostics =
                    {
                        new DiagnosticResult(DncHostGenerator.MissingFactoryAttributeDescriptor),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task GeneratesEntryPoint()
        {
            var code = @"
[assembly: WixToolset.Mba.Core.BootstrapperApplicationFactory(typeof(Test.BAFactory))]
namespace Test
{
    using WixToolset.Mba.Core;

    public class BAFactory : BaseBootstrapperApplicationFactory
    {
        protected override IBootstrapperApplication Create(IEngine engine, IBootstrapperCommand bootstrapperCommand)
        {
            return null;
        }
    }
}
";
            var generated = String.Format(DncHostGenerator.Template, DncHostGenerator.Version, "Test.BAFactory");

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { code },
                    GeneratedSources =
                    {
                        (typeof(DncHostGenerator), "WixToolset.Dnc.Host.g.cs", SourceText.From(generated, Encoding.UTF8, SourceHashAlgorithm.Sha256)),
                    },
                    AdditionalReferences = { MbaCoreAssembly },
                },
            }.RunAsync();
        }
    }
}
