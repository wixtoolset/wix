// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using WixInternal.Core.TestPackage;
    using WixInternal.TestSupport;
    using Xunit;

    public class HarvestFilesFixture
    {
        [Fact]
        public void MustIncludeSomeFiles()
        {
            Build("BadAuthoring.wxs", (_, result) =>
            {
                var messages = result.Messages.Select(m => m.Id);
                Assert.Equal(new[]
                {
                    10,
                }, messages);
            });
        }

        [Fact]
        public void ZeroFilesHarvestedIsAWarning()
        {
            Build("ZeroFiles.wxs", (_, result) =>
            {
                var messages = result.Messages.Select(m => m.Id);
                Assert.Equal(new[]
                {
                    8600,
                }, messages);
            });
        }

        [Fact]
        public void MissingHarvestDirectoryIsAWarning()
        {
            Build("BadDirectory.wxs", (_, result) =>
            {
                var messages = result.Messages.Select(m => m.Id);
                Assert.Equal(new[]
                {
                    8601,
                    8601,
                }, messages);
            });
        }

        [Fact]
        public void DuplicateFilesSomethingSomething()
        {
            Build("DuplicateFiles.wxs", (_, result) =>
            {
                var messages = result.Messages.Select(m => m.Id);
                Assert.Equal(new[]
                {
                    8602,
                    8602,
                    8602,
                    8602,
                }, messages);
            });
        }

        [Fact]
        public void HarvestedFilesUnderPackageWithAuthoredFeatureAreOrphaned()
        {
            Build("PackageWithoutDefaultFeature.wxs", (_, result) =>
            {
                var messages = result.Messages.Select(m => m.Id);
                Assert.Equal(new[]
                {
                    267,
                    267,
                    267,
                    267,
                }, messages);
            });
        }

        [Fact]
        public void CanHarvestFilesInComponentGroup()
        {
            var expected = new[]
            {
                @"fls4di7CtiJhJnEwixr8_c5G8k8aNY=PFiles\MsiPackage\test3.txt",
                @"flsk5E532wcxn9MfpYQYRnrPi3dsOA=PFiles\MsiPackage\test4.txt",
                @"flsYh0SwdEpJEootWp2keHMLnnpeb4=PFiles\MsiPackage\files2_sub2\test20.txt",
                @"flsI4uo74epTPY4TLIcWKVaH.HTbVQ=PFiles\MsiPackage\files2_sub2\test21.txt",
                @"fls5xOtTcUOA9ZDnUsz1D4Arbw7l_A=PFiles\MsiPackage\files2_sub3\FileName.Extension",
            };

            Build("ComponentGroup.wxs", (msiPath, _) => AssertFileIdsAndTargetPaths(msiPath, expected));
        }

        [Fact]
        public void CanHarvestFilesInDirectory()
        {
            var expected = new[]
            {
                @"flsD7JQZm.Ts2375WMT.zsTxqCAf.s=PFiles\MsiPackage\files1_sub1\test10.txt",
                @"flslrDWblm4pE.4i4jR58_XyYMmR8I=PFiles\MsiPackage\files1_sub1\files1_sub2\test120.txt",
                @"flsj.cb0sFWqIPHPFSKJSEEaPDuAQ4=PFiles\MsiPackage\test2.txt",
                @"flsaFu0CvigRX6Psea0ic6ZWevzLmI=PFiles\MsiPackage\test3.txt",
                @"flsJBy_HKCNejalUyud4HisGqhd72E=PFiles\MsiPackage\test4.txt",
            };

            Build("Directory.wxs", (msiPath, _) => AssertFileIdsAndTargetPaths(msiPath, expected));
        }

        [Fact]
        public void CanHarvestFilesInDirectoryRef()
        {
            var expected = new[]
            {
                @"flsYgiwrDUkZnBEK6iUMkxxaJlD8yQ=PFiles\MsiPackage\test1.txt",
                @"flsj.cb0sFWqIPHPFSKJSEEaPDuAQ4=PFiles\MsiPackage\test2.txt",
                @"flslrDWblm4pE.4i4jR58_XyYMmR8I=PFiles\MsiPackage\files1_sub1\files1_sub2\test120.txt",
                @"flsIpBotASYdALXxudRqekdQdKEKdQ=PFiles\MsiPackage\notatest.txt",
                @"flsaFu0CvigRX6Psea0ic6ZWevzLmI=PFiles\MsiPackage\test3.txt",
                @"flsJBy_HKCNejalUyud4HisGqhd72E=PFiles\MsiPackage\test4.txt",
                @"flsLXU67KiOVU00lZL1jaDaBVpg.Dw=PFiles\MsiPackage\files2_sub2\pleasedontincludeme.dat",
                @"fls05.yw49T0FVAq3Wvq2ihNp3KWfI=PFiles\MsiPackage\files2_sub2\test20.txt",
                @"flsf0falU_gCTJjtbSCNiFpJQ1d8EM=PFiles\MsiPackage\files2_sub2\test21.txt",
            };

            Build("DirectoryRef.wxs", (msiPath, _) => AssertFileIdsAndTargetPaths(msiPath, expected));
        }

        [Fact]
        public void CanHarvestFilesInFeature()
        {
            var expected = new[]
            {
                @"flsFGp4MRR_h3Qm.CuBFwC0AJo6b6M=PFiles\Example Product\test2.txt",
                @"flsTCB.ifIor30C7HfezIDjMB3mrdk=PFiles\Example Product\Assets\test3.txt",
                @"flsTIP5QnXmYzSzwEM2casYcyn1eR4=PFiles\Example Product\Assets\test4.txt",
            };

            Build("Feature.wxs", (msiPath, _) => AssertFileIdsAndTargetPaths(msiPath, expected));
        }

        [Fact]
        public void CanHarvestFilesInFeatureGroup()
        {
            var expected = new[]
            {
                @"flsYgiwrDUkZnBEK6iUMkxxaJlD8yQ=PFiles\MsiPackage\test1.txt",
                @"flsj.cb0sFWqIPHPFSKJSEEaPDuAQ4=PFiles\MsiPackage\test2.txt",
                @"flsXCzqPnLIc2S0BUeTH_BHeaHJgbw=PFiles\MsiPackage\assets\notatest.txt",
                @"flsiPGfLix0PMEZQ.BGdIfMp43g0m0=PFiles\MsiPackage\assets\test3.txt",
                @"flsQp7vl8wg1R6a9pWOuj0y8X9tFdk=PFiles\MsiPackage\assets\test4.txt",
                @"flsROfaiHyEp8AGBy4ZIkz26B8x0QE=PFiles\MsiPackage\assets\files2_sub2\pleasedontincludeme.dat",
                @"flsR7cMdABXp6bHg2a89trPLf9NuKU=PFiles\MsiPackage\assets\files2_sub2\test20.txt",
                @"flse0V.F.q.LFFjytlCECxjK5io7g0=PFiles\MsiPackage\assets\files2_sub2\test21.txt",
                @"flsQk6.O4lkg78pGc4Ye64mosdl3hY=PFiles\MsiPackage\assets\files2_sub3\FileName.Extension",
            };

            Build("FeatureGroup.wxs", (msiPath, _) => AssertFileIdsAndTargetPaths(msiPath, expected));
        }

        [Fact]
        public void CanHarvestFilesInStraightAndCrookedTrees()
        {
            var expected = new[]
            {
                @"flsKwXKj_Znab7ED_V5tgmdm6I35Mk=PFiles\root\a\file.ext",
                @"flsG_e554dKs4hJ6VckjXfvHVbrQ_c=PFiles\root\b\file.ext",
                @"fls6QPA6Lio8i18jzGOvfZpb2vxh9M=PFiles\root\c\file.ext",
                @"flsfPnakkQEQynhlS0Z9wVZMXLjKx4=PFiles\root\d\file.ext",
                @"fls9z8lqK0ZvjlnUEzCDIE7bF01zXU=PFiles\root\e\file.ext",
                @"flsFqGICaLl5ZLPsdJY.Z2m6xC2Khs=PFiles\root\f\file.ext",
                @"flsUT96SVd6kTNE4X7vBFL7r3Zl4sM=PFiles\root\g\file.ext",
                @"fls.i3qm35eVOQoWBJFaj3c9792GKc=PFiles\root\h\file.ext",
                @"flsgKGasxQIkFWfpTA6kQurqEL4Afg=PFiles\root\file.ext",
                @"flsZGaHZm_l9M8jaQS8XSgxnlykcKI=PFiles\root\z\y\x\w\v\u\t\s\r\q\file.ext",
            };

            Build("CrookedTree.wxs", (msiPath, _) => AssertFileIdsAndTargetPaths(msiPath, expected));
        }

        [Fact]
        public void CanHarvestFilesInFeatureRef()
        {
            var expected = new[]
            {
                @"flsYgiwrDUkZnBEK6iUMkxxaJlD8yQ=PFiles\MsiPackage\test1.txt",
                @"flsj.cb0sFWqIPHPFSKJSEEaPDuAQ4=PFiles\MsiPackage\test2.txt",
                @"flsXCzqPnLIc2S0BUeTH_BHeaHJgbw=PFiles\MsiPackage\assets\notatest.txt",
                @"flsiPGfLix0PMEZQ.BGdIfMp43g0m0=PFiles\MsiPackage\assets\test3.txt",
                @"flsQp7vl8wg1R6a9pWOuj0y8X9tFdk=PFiles\MsiPackage\assets\test4.txt",
                @"flsROfaiHyEp8AGBy4ZIkz26B8x0QE=PFiles\MsiPackage\assets\files2_sub2\pleasedontincludeme.dat",
                @"flsR7cMdABXp6bHg2a89trPLf9NuKU=PFiles\MsiPackage\assets\files2_sub2\test20.txt",
                @"flse0V.F.q.LFFjytlCECxjK5io7g0=PFiles\MsiPackage\assets\files2_sub2\test21.txt",
                @"flsQk6.O4lkg78pGc4Ye64mosdl3hY=PFiles\MsiPackage\assets\files2_sub3\FileName.Extension",
            };

            Build("FeatureRef.wxs", (msiPath, _) => AssertFileIdsAndTargetPaths(msiPath, expected));
        }

        [Fact]
        public void CanHarvestFilesInFragments()
        {
            var expected = new[]
            {
                @"flsOQDmBHyBKZnyRziE3.z5HjmBv4k=PFiles\MsiPackage\test1.txt",
                @"flsHjzoVbTTY2jQbthZHKG7Rn4yMZo=PFiles\MsiPackage\test2.txt",
                @"flsPLx4KqFVkbnYyg3Uo4QdiRLFbL8=PFiles\MsiPackage\notatest.txt",
                @"fls4di7CtiJhJnEwixr8_c5G8k8aNY=PFiles\MsiPackage\test3.txt",
                @"flsk5E532wcxn9MfpYQYRnrPi3dsOA=PFiles\MsiPackage\test4.txt",
            };

            Build("Fragment.wxs", (msiPath, _) => AssertFileIdsAndTargetPaths(msiPath, expected));
        }

        [Fact]
        public void CanHarvestFilesInModules()
        {
            var expected = new[]
            {
                @"flsgrgAVAsCQ8tCCxfnbBNis66623c.E535B765_1019_4A4F_B3EA_AE28870E6D73=PFiles\MergeModule\test1.txt",
                @"flsDBWSWjpVSU3Zs33bREsJa2ygSQM.E535B765_1019_4A4F_B3EA_AE28870E6D73=PFiles\MergeModule\test2.txt",
                @"flsehdwEdXusUijRShuTszSxwf8joA.E535B765_1019_4A4F_B3EA_AE28870E6D73=PFiles\MergeModule\test3.txt",
                @"flsBvxG729t7hKBa4KOmfvNMPptZkM.E535B765_1019_4A4F_B3EA_AE28870E6D73=PFiles\MergeModule\test4.txt",
                @"flskqOUVMfAE13k2h.ZkPhurwO4Y1c.E535B765_1019_4A4F_B3EA_AE28870E6D73=PFiles\MergeModule\notatest.txt",
            };

            Build("Module.wxs", (msiPath, _) => AssertFileIdsAndTargetPaths(msiPath, expected), isPackage: false);
        }

        [Fact]
        public void CanHarvestFilesWithNamedBindPaths()
        {
            var expected = new[]
            {
                @"flsNNsTNrgmjASmTBbP.45J1F50dEc=PFiles\HarvestedFiles\test1.txt",
                @"flsASLR5pHQzLmWRE.Snra7ndH7sIA=PFiles\HarvestedFiles\test2.txt",
                @"flsTZFPiMHb.qfUxdGKQYrnXOhZ.8M=PFiles\HarvestedFiles\files1_sub1\test10.txt",
                @"flsLGcTTZPIU3ELiWybqnm.PQ0Ih_g=PFiles\HarvestedFiles\files1_sub1\files1_sub2\test120.txt",
                @"fls1Jx2Y9Vea_.WZBH_h2e79arvDRU=PFiles\HarvestedFiles\test3.txt",
                @"flsJ9gNxWaau2X3ufphQuCV9WwAgcw=PFiles\HarvestedFiles\test4.txt",
                @"flswcmX9dpMQytmD_5QA5aJ5szoQVA=PFiles\HarvestedFiles\files2_sub2\test20.txt",
                @"flskKeCKFUtCYMuvw564rgPLJmyBx0=PFiles\HarvestedFiles\files2_sub2\test21.txt",
                @"fls2agLZFnQwjoijShwT9Z0RwHyGrI=PFiles\HarvestedFiles\files2_sub3\FileName.Extension",
            };

            Build("BindPaths.wxs", (msiPath, _) => AssertFileIdsAndTargetPaths(msiPath, expected));
        }

        [Fact]
        public void CanHarvestFilesWithUnnamedBindPaths()
        {
            var expected = new[]
            {
                @"flsNNsTNrgmjASmTBbP.45J1F50dEc=PFiles\HarvestedFiles\test1.txt",
                @"flsASLR5pHQzLmWRE.Snra7ndH7sIA=PFiles\HarvestedFiles\test2.txt",
                @"flsTZFPiMHb.qfUxdGKQYrnXOhZ.8M=PFiles\HarvestedFiles\files1_sub1\test10.txt",
                @"flsLGcTTZPIU3ELiWybqnm.PQ0Ih_g=PFiles\HarvestedFiles\files1_sub1\files1_sub2\test120.txt",
                @"fls1Jx2Y9Vea_.WZBH_h2e79arvDRU=PFiles\HarvestedFiles\test3.txt",
                @"flsJ9gNxWaau2X3ufphQuCV9WwAgcw=PFiles\HarvestedFiles\test4.txt",
                @"flswcmX9dpMQytmD_5QA5aJ5szoQVA=PFiles\HarvestedFiles\files2_sub2\test20.txt",
                @"flskKeCKFUtCYMuvw564rgPLJmyBx0=PFiles\HarvestedFiles\files2_sub2\test21.txt",
                @"fls2agLZFnQwjoijShwT9Z0RwHyGrI=PFiles\HarvestedFiles\files2_sub3\FileName.Extension",
                @"fls9UMOE.TOv61JuYF8IhvCKb8eous=PFiles\HarvestedFiles\namedfile.txt",
                @"flsu53T_9CcaBegDflAImGHTajDbJ0=PFiles\HarvestedFiles\unnamedfile.txt",
            };

            Build("BindPathsUnnamed.wxs", (msiPath, _) => AssertFileIdsAndTargetPaths(msiPath, expected), addUnnamedBindPath: true);
        }

        [Fact]
        public void CanHarvestFilesInStandardDirectory()
        {
            var expected = new[]
            {
                @"flsxKKnXWKGChnZD8KSNY4Mwb48nHc=PFiles\MsiPackage\test1.txt",
                @"flsVa.JZ23qQ1vXrc1jjOLIyJMyuqM=PFiles\MsiPackage\test2.txt",
                @"fls4_TTxMCivlzpVJiyNHQRa2eU2ZA=PFiles\MsiPackage\files1_sub1\test10.txt",
                @"flsBQqnyJp3XnmzomCYT_1qJqKLeSA=PFiles\MsiPackage\files1_sub1\files1_sub2\test120.txt",
                @"flsbr7Ii_L1MRmxOImvY3N7np5FqAU=PFiles\MsiPackage\notatest.txt",
                @"fls5VacfuX4Iub..BmTvssAOUcUI1o=PFiles\MsiPackage\test3.txt",
                @"flsTtxen6j9kDjhKw2rnKSlYn5e2_k=PFiles\MsiPackage\test4.txt",
                @"flsKEAYr7hAov7KfiRTjHg1VBg6T38=PFiles\MsiPackage\files2_sub2\pleasedontincludeme.dat",
                @"flsvw4WMs4foeBokT9VUN3qjPDg8jc=PFiles\MsiPackage\files2_sub2\test20.txt",
                @"flspraz8bfD0UXdCBW9CS2jr49hl1k=PFiles\MsiPackage\files2_sub2\test21.txt",
                @"flsCgt3Noa1VJHlHG5HOVjD5vdJm5Q=PFiles\MsiPackage\files2_sub3\FileName.Extension",
            };

            Build("StandardDirectory.wxs", (msiPath, _) => AssertFileIdsAndTargetPaths(msiPath, expected));
        }

        [Fact]
        public void CanHarvestFilesInFiveLines()
        {
            var expected = new[]
            {
                @"flsIpBotASYdALXxudRqekdQdKEKdQ=PFiles\Example Corporation MsiPackage\notatest.txt",
                @"flsaFu0CvigRX6Psea0ic6ZWevzLmI=PFiles\Example Corporation MsiPackage\test3.txt",
                @"flsJBy_HKCNejalUyud4HisGqhd72E=PFiles\Example Corporation MsiPackage\test4.txt",
                @"flsLXU67KiOVU00lZL1jaDaBVpg.Dw=PFiles\Example Corporation MsiPackage\files2_sub2\pleasedontincludeme.dat",
                @"fls05.yw49T0FVAq3Wvq2ihNp3KWfI=PFiles\Example Corporation MsiPackage\files2_sub2\test20.txt",
                @"flsf0falU_gCTJjtbSCNiFpJQ1d8EM=PFiles\Example Corporation MsiPackage\files2_sub2\test21.txt",
                @"fls6Dd0lNq_VzYLYjK7ty5WxNy5KCs=PFiles\Example Corporation MsiPackage\files2_sub3\FileName.Extension",
            };

            Build("PackageFiveLiner.wxs", (msiPath, _) => AssertFileIdsAndTargetPaths(msiPath, expected));
        }

        [Fact]
        public void CanGetVerboseHarvestingDetails()
        {
            Build("Feature.wxs", (_, result) =>
            {
                var messages = result.Messages.Select(m => m.Id).Where(i => i >= 8700 && i < 8800);
                Assert.Equal(new[]
                {
                    8701,
                    8700,
                    8701,
                    8700,
                    8700,
                }, messages);
            }, additionalCommandLineArguments: "-v");
        }

        private static void AssertFileIdsAndTargetPaths(string msiPath, string[] expected)
        {
            var pkg = new WixToolset.Dtf.WindowsInstaller.Package.InstallPackage(msiPath,
                WixToolset.Dtf.WindowsInstaller.DatabaseOpenMode.ReadOnly);
            var sortedExpected = expected.OrderBy(s => s);
            var actual = pkg.Files.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key}={kvp.Value.TargetPath}");

            Assert.Equal(sortedExpected, actual);
        }

        private static void Build(string file, Action<string, WixRunnerResult> tester, bool isPackage = true, bool addUnnamedBindPath = false, params string[] additionalCommandLineArguments)
        {
            var folder = TestData.Get("TestData", "HarvestFiles");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var binFolder = Path.Combine(baseFolder, "bin");
                var msiPath = Path.Combine(binFolder, isPackage ? "test.msi" : "test.msm");

                var arguments = new List<string>()
                {
                    "build",
                    Path.Combine(folder, file),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", @$"ToBeHarvested={folder}\files1",
                    "-bindpath", @$"ToBeHarvested={folder}\files2",
                    "-o", msiPath,
                };

                if (addUnnamedBindPath)
                {
                    arguments.Add("-bindpath");
                    arguments.Add(Path.Combine(folder, "unnamedbindpath"));
                }

                if (additionalCommandLineArguments.Length > 0)
                {
                    arguments.AddRange(additionalCommandLineArguments);
                }

                var result = WixRunner.Execute(arguments.ToArray());

                tester(msiPath, result);
            }
        }
    }
}
