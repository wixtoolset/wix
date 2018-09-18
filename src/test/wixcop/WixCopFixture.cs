using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WixBuildTools.TestSupport;
using WixCop.CommandLine;
using WixCop.Interfaces;
using WixToolset.Core;
using WixToolset.Core.TestPackage;
using WixToolset.Extensibility;
using WixToolset.Extensibility.Services;
using Xunit;

namespace WixCopTests
{
    public class WixCopFixture
    {
        [Fact(Skip = "Problematic at the moment.")]
        public void CanConvertSingleFile()
        {
            const string beforeFileName = "SingleFile.wxs";
            const string afterFileName = "ConvertedSingleFile.wxs";
            var folder = TestData.Get(@"TestData\SingleFile");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder(true);
                var targetFile = Path.Combine(baseFolder, beforeFileName);
                File.Copy(Path.Combine(folder, beforeFileName), Path.Combine(baseFolder, beforeFileName));

                var runner = new WixCopRunner
                {
                    FixErrors = true,
                    SearchPatterns =
                    {
                        targetFile,
                    },
                };

                var result = runner.Execute(out var messages);
                
                Assert.Equal(2, result);

                var actualLines = File.ReadAllLines(targetFile);
                var expectedLines = File.ReadAllLines(Path.Combine(folder, afterFileName));
                Assert.Equal(expectedLines, actualLines);

                var runner2 = new WixCopRunner
                {
                    FixErrors = true,
                    SearchPatterns =
                    {
                        targetFile,
                    },
                };

                var result2 = runner2.Execute(out var messages2);

                Assert.Equal(0, result2);
            }
        }

        private class WixCopRunner
        {
            public bool FixErrors { get; set; }

            public List<string> SearchPatterns { get; } = new List<string>();

            public int Execute(out List<string> messages)
            {
                var argList = new List<string>();
                if (this.FixErrors)
                {
                    argList.Add("-f");
                }

                foreach (string searchPattern in this.SearchPatterns)
                {
                    argList.Add(searchPattern);
                }

                return WixCopRunner.Execute(argList.ToArray(), out messages);
            }

            public static int Execute(string[] args, out List<string> messages)
            {
                var listener = new TestMessageListener();

                var serviceProvider = new WixToolsetServiceProvider();
                serviceProvider.AddService<IMessageListener>((x, y) => listener);
                serviceProvider.AddService<IWixCopCommandLineParser>((x, y) => new WixCopCommandLineParser(x));

                var result = Execute(serviceProvider, args);

                var messaging = serviceProvider.GetService<IMessaging>();
                messages = listener.Messages.Select(x => messaging.FormatMessage(x)).ToList();
                return result;
            }

            public static int Execute(IServiceProvider serviceProvider, string[] args)
            {
                var wixcop = new WixCop.Program();
                return wixcop.Run(serviceProvider, args);
            }
        }
    }
}
