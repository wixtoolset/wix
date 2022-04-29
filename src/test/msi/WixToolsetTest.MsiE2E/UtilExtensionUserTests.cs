// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.MsiE2E
{
    using System;
    using WixTestTools;
    using Xunit;
    using Xunit.Abstractions;

    public class UtilExtensionUserTests : MsiE2ETests
    {
        public UtilExtensionUserTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        const string TempDomain = "USERDOMAIN";
        const string TempUsername = "USERNAME";

        // Verify that the users specified in the authoring are created as expected.
        [Fact]
        public void CanInstallAndUninstallUsers()
        {
            var arguments = new string[]
            {
                $"TEMPDOMAIN={Environment.GetEnvironmentVariable(TempDomain)}",
                $"TEMPUSERNAME={Environment.GetEnvironmentVariable(TempUsername)}",
            };
            var productA = this.CreatePackageInstaller("ProductA");

            productA.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS, arguments);

            // Validate New User Information.
            UserVerifier.VerifyUserInformation(String.Empty, "testName1", true, false, false);
            UserVerifier.VerifyUserIsMemberOf(String.Empty, "testName1", "Administrators", "Power Users");

            UserVerifier.VerifyUserInformation(String.Empty, "testName2", true, true, true);
            UserVerifier.VerifyUserIsMemberOf(String.Empty, "testName2", "Power Users");

            UserVerifier.VerifyUserIsMemberOf(Environment.GetEnvironmentVariable(TempDomain), Environment.GetEnvironmentVariable(TempUsername), "Power Users");

            productA.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS, arguments);

            // Verify Users marked as RemoveOnUninstall were removed.
            Assert.False(UserVerifier.UserExists(String.Empty, "testName1"), String.Format("User '{0}' was not removed on Uninstall", "testName1"));
            Assert.True(UserVerifier.UserExists(String.Empty, "testName2"), String.Format("User '{0}' was removed on Uninstall", "testName2"));

            // clean up
            UserVerifier.DeleteLocalUser("testName2");

            UserVerifier.VerifyUserIsNotMemberOf(Environment.GetEnvironmentVariable(TempDomain), Environment.GetEnvironmentVariable(TempUsername), "Power Users");
        }

        // Verify the rollback action reverts all Users changes.
        [Fact]
        public void CanRollbackUsers()
        {
            var arguments = new string[]
            {
                $"TEMPDOMAIN={Environment.GetEnvironmentVariable(TempDomain)}",
                $"TEMPUSERNAME={Environment.GetEnvironmentVariable(TempUsername)}",
            };
            var productFail = this.CreatePackageInstaller("ProductFail");

            // make sure the user accounts are deleted before we start
            UserVerifier.DeleteLocalUser("testName1");
            UserVerifier.DeleteLocalUser("testName2");
            UserVerifier.VerifyUserIsNotMemberOf(Environment.GetEnvironmentVariable(TempDomain), Environment.GetEnvironmentVariable(TempUsername), "Power Users");

            productFail.InstallProduct(MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE, arguments);

            // Verify Users marked as RemoveOnUninstall were removed.
            Assert.False(UserVerifier.UserExists(String.Empty, "testName1"), String.Format("User '{0}' was not removed on Rollback", "testName1"));
            Assert.False(UserVerifier.UserExists(String.Empty, "testName2"), String.Format("User '{0}' was not removed on Rollback", "testName2"));

            UserVerifier.VerifyUserIsNotMemberOf(Environment.GetEnvironmentVariable(TempDomain), Environment.GetEnvironmentVariable(TempUsername), "Power Users");
        }

        // Verify that the users specified in the authoring are created as expected on repair.
        [Fact(Skip = "Test demonstrates failure")]
        public void CanRepairUsers()
        {
            var arguments = new string[]
            {
                $"TEMPDOMAIN={Environment.GetEnvironmentVariable(TempDomain)}",
                $"TEMPUSERNAME={Environment.GetEnvironmentVariable(TempUsername)}",
            };
            var productA = this.CreatePackageInstaller("ProductA");

            productA.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS, arguments);

            UserVerifier.DeleteLocalUser("testName1");
            UserVerifier.SetUserInformation(String.Empty, "testName2", true, false, false);

            productA.RepairProduct(MSIExec.MSIExecReturnCode.SUCCESS, arguments);

            // Validate New User Information.
            UserVerifier.VerifyUserInformation(String.Empty, "testName1", true, false, false);
            UserVerifier.VerifyUserIsMemberOf(String.Empty, "testName1", "Administrators", "Power Users");

            UserVerifier.VerifyUserInformation(String.Empty, "testName2", true, true, true);
            UserVerifier.VerifyUserIsMemberOf(String.Empty, "testName2", "Power Users");

            UserVerifier.VerifyUserIsMemberOf(Environment.GetEnvironmentVariable(TempDomain), Environment.GetEnvironmentVariable(TempUsername), "Power Users");

            productA.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS, arguments);

            // Verify Users marked as RemoveOnUninstall were removed.
            Assert.False(UserVerifier.UserExists(String.Empty, "testName1"), String.Format("User '{0}' was not removed on Uninstall", "testName1"));
            Assert.True(UserVerifier.UserExists(String.Empty, "testName2"), String.Format("User '{0}' was removed on Uninstall", "testName2"));

            // clean up
            UserVerifier.DeleteLocalUser("testName2");

            UserVerifier.VerifyUserIsNotMemberOf(Environment.GetEnvironmentVariable(TempDomain), Environment.GetEnvironmentVariable(TempUsername), "Power Users");
        }

        // Verify that Installation fails if FailIfExisits is set.
        [Fact]
        public void FailsIfUserExists()
        {
            var productFailIfExists = this.CreatePackageInstaller("ProductFailIfExists");

            // Create 'existinguser'
            UserVerifier.CreateLocalUser("existinguser", "test123!@#");

            try
            {
                productFailIfExists.InstallProduct(MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

                // Verify User still exists.
                bool userExists = UserVerifier.UserExists(String.Empty, "existinguser");

                Assert.True(userExists, String.Format("User '{0}' was removed on Rollback", "existinguser"));
            }
            finally
            {
                // clean up
                UserVerifier.DeleteLocalUser("existinguser");
            }

        }

        // Verify that a user cannot be created on a domain on which you dont have create user permission.
        [Fact]
        public void FailsIfRestrictedDomain()
        {
            var productRestrictedDomain = this.CreatePackageInstaller("ProductRestrictedDomain");

            string logFile = productRestrictedDomain.InstallProduct(MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE, "TEMPDOMAIN=DOESNOTEXIST");

            // Verify expected error message in the log file
            Assert.True(LogVerifier.MessageInLogFile(logFile, "ConfigureUsers:  Failed to check existence of domain: DOESNOTEXIST, user: testName1 (error code 0x800706ba) - continuing"));
        }

        // Verify that adding a user to a non-existent group does not fail the install when non-vital.
        [Fact]
        public void IgnoresMissingGroupWhenNonVital()
        {
            var productNonVitalGroup = this.CreatePackageInstaller("ProductNonVitalUserGroup");

            productNonVitalGroup.InstallProduct();
        }
    }
}
