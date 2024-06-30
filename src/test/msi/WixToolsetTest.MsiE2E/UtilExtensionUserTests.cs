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

        // Verify that the users specified in the authoring are created as expected.
        [RuntimeFact]
        public void CanInstallAndUninstallUsers()
        {
            UserVerifier.CreateLocalUser("testName3", "test123!@#");
            var productA = this.CreatePackageInstaller("ProductA");

            productA.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            // Validate New User Information.
            UserVerifier.VerifyUserInformation(String.Empty, "testName1", true, false, false);
            UserVerifier.VerifyUserIsMemberOf(String.Empty, "testName1", "Administrators", "Power Users");

            UserVerifier.VerifyUserInformation(String.Empty, "testName2", true, true, true);
            UserVerifier.VerifyUserIsMemberOf(String.Empty, "testName2", "Power Users");

            UserVerifier.VerifyUserIsMemberOf("", "testName3", "Power Users");

            productA.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify Users marked as RemoveOnUninstall were removed.
            Assert.False(UserVerifier.UserExists(String.Empty, "testName1"), String.Format("User '{0}' was not removed on Uninstall", "testName1"));
            Assert.True(UserVerifier.UserExists(String.Empty, "testName2"), String.Format("User '{0}' was removed on Uninstall", "testName2"));

            // Verify that user added to power users group is removed on uninstall.
            UserVerifier.VerifyUserIsNotMemberOf("", "testName3", "Power Users");

            // clean up
            UserVerifier.DeleteLocalUser("testName1");
            UserVerifier.DeleteLocalUser("testName2");
            UserVerifier.DeleteLocalUser("testName3");
        }

        // Verify the rollback action reverts all Users changes.
        [RuntimeFact]
        public void CanRollbackUsers()
        {
            UserVerifier.CreateLocalUser("testName3", "test123!@#", "User3 comment");
            UserVerifier.AddUserToGroup("testName3", "Backup Operators");
            var productFail = this.CreatePackageInstaller("ProductFail");

            // make sure the user accounts are deleted before we start
            UserVerifier.DeleteLocalUser("testName1");
            UserVerifier.DeleteLocalUser("testName2");

            productFail.InstallProduct(MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

            // Verify added Users were removed on rollback.
            Assert.False(UserVerifier.UserExists(String.Empty, "testName1"), String.Format("User '{0}' was not removed on Rollback", "testName1"));
            Assert.False(UserVerifier.UserExists(String.Empty, "testName2"), String.Format("User '{0}' was not removed on Rollback", "testName2"));

            // Verify that user added to power users group is removed from power users group on rollback.
            UserVerifier.VerifyUserIsNotMemberOf("", "testName3", "Power Users");
            // but is not removed from Backup Operators
            UserVerifier.VerifyUserIsMemberOf(string.Empty, "testName3", "Backup Operators");
            // and has their original comment set back
            UserVerifier.VerifyUserComment(string.Empty, "testName3", "User3 comment");

            // clean up
            UserVerifier.DeleteLocalUser("testName1");
            UserVerifier.DeleteLocalUser("testName2");
            UserVerifier.DeleteLocalUser("testName3");
        }


        // Verify that command-line parameters are not blocked by repair switches.
        // Original code signalled repair mode by using "-f ", which silently
        // terminated the command-line parsing, ignoring any parameters that followed.
        [RuntimeFact()]
        public void CanRepairUsersWithCommandLineParameters()
        {
            var arguments = new string[]
            {
                "TESTPARAMETER1=testName1",
            };
            var productWithCommandLineParameters = this.CreatePackageInstaller("ProductWithCommandLineParameters");

            // Make sure that the user doesn't exist when we start the test.
            UserVerifier.DeleteLocalUser("testName1");

            // Install
            productWithCommandLineParameters.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS, arguments);

            // Repair
            productWithCommandLineParameters.RepairProduct(MSIExec.MSIExecReturnCode.SUCCESS, arguments);

            // Clean up
            UserVerifier.DeleteLocalUser("testName1");
        }


        // Verify that the users specified in the authoring are created as expected on repair.
        [RuntimeFact()]
        public void CanRepairUsers()
        {
            UserVerifier.CreateLocalUser("testName3", "test123!@#");
            var productA = this.CreatePackageInstaller("ProductA");

            productA.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            // Validate New User Information.
            UserVerifier.DeleteLocalUser("testName1");
            UserVerifier.SetUserInformation(String.Empty, "testName2", true, false, false);

            productA.RepairProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            // Validate New User Information.
            UserVerifier.VerifyUserInformation(String.Empty, "testName1", true, false, false);
            UserVerifier.VerifyUserIsMemberOf(String.Empty, "testName1", "Administrators", "Power Users");

            UserVerifier.VerifyUserInformation(String.Empty, "testName2", true, true, true);
            UserVerifier.VerifyUserIsMemberOf(String.Empty, "testName2", "Power Users");

            UserVerifier.VerifyUserIsMemberOf("", "testName3", "Power Users");

            productA.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify Users marked as RemoveOnUninstall were removed.
            Assert.False(UserVerifier.UserExists(String.Empty, "testName1"), String.Format("User '{0}' was not removed on Uninstall", "testName1"));
            Assert.True(UserVerifier.UserExists(String.Empty, "testName2"), String.Format("User '{0}' was removed on Uninstall", "testName2"));

            // Verify that user added to power users group is removed on uninstall.
            UserVerifier.VerifyUserIsNotMemberOf("", "testName3", "Power Users");

            // clean up
            UserVerifier.DeleteLocalUser("testName1");
            UserVerifier.DeleteLocalUser("testName2");
            UserVerifier.DeleteLocalUser("testName3");
        }

        // Verify that Installation fails if FailIfExists is set.
        [RuntimeFact]
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
        [RuntimeFact]
        public void FailsIfRestrictedDomain()
        {
            var productRestrictedDomain = this.CreatePackageInstaller("ProductRestrictedDomain");

            string logFile = productRestrictedDomain.InstallProduct(MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE, "TEMPDOMAIN=DOESNOTEXIST");

            // Verify expected error message in the log file
            Assert.True(LogVerifier.MessageInLogFile(logFile, "ConfigureUsers:  Failed to check existence of domain: DOESNOTEXIST, user: testName1 (error code 0x800706ba) - continuing"));
        }

        // Verify that adding a user to a non-existent group does not fail the install when non-vital.
        [RuntimeFact]
        public void IgnoresMissingGroupWhenNonVital()
        {
            var productNonVitalGroup = this.CreatePackageInstaller("ProductNonVitalUserGroup");

            productNonVitalGroup.InstallProduct();
        }

        // Verify that a user can be created with a user comment
        [RuntimeFact]
        public void CanCreateNewUserWithComment()
        {
            var productNewUserWithComment = this.CreatePackageInstaller("ProductNewUserWithComment");

            productNewUserWithComment.InstallProduct();
            UserVerifier.VerifyUserComment(String.Empty, "testName1", "testComment1");

            // clean up
            UserVerifier.DeleteLocalUser("testName1");
        }

        // Verify that a comment can be added to an existing user
        [RuntimeFact]
        public void CanAddCommentToExistingUser()
        {
            UserVerifier.CreateLocalUser("testName1", "test123!@#");
            var productAddCommentToExistingUser = this.CreatePackageInstaller("ProductAddCommentToExistingUser");

            productAddCommentToExistingUser.InstallProduct();

            UserVerifier.VerifyUserComment(String.Empty, "testName1", "testComment1");

            // clean up
            UserVerifier.DeleteLocalUser("testName1");
        }

        // Verify that a comment can be repaired for a new user
        [RuntimeFact]
        public void CanRepairCommentOfNewUser()
        {
            var productNewUserWithComment = this.CreatePackageInstaller("ProductNewUserWithComment");

            productNewUserWithComment.InstallProduct();
            UserVerifier.SetUserComment(String.Empty, "testName1", "");

            productNewUserWithComment.RepairProduct();
            UserVerifier.VerifyUserComment(String.Empty, "testName1", "testComment1");

            // clean up
            UserVerifier.DeleteLocalUser("testName1");
        }

        // Verify that a comment can be changed for an existing user
        [RuntimeFact]
        public void CanChangeCommentOfExistingUser()
        {
            UserVerifier.CreateLocalUser("testName1", "test123!@#");
            UserVerifier.SetUserComment(String.Empty, "testName1", "initialTestComment1");
            var productNewUserWithComment = this.CreatePackageInstaller("ProductNewUserWithComment");

            productNewUserWithComment.InstallProduct();
            UserVerifier.VerifyUserComment(String.Empty, "testName1", "testComment1");

            // clean up
            UserVerifier.DeleteLocalUser("testName1");
        }

        // Verify that a comment can be rolled back for an existing user
        [RuntimeFact]
        public void CanRollbackCommentOfExistingUser()
        {
            UserVerifier.CreateLocalUser("testName1", "test123!@#");
            UserVerifier.SetUserComment(String.Empty, "testName1", "initialTestComment1");
            var productCommentFail = this.CreatePackageInstaller("ProductCommentFail");

            productCommentFail.InstallProduct(MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

            // Verify that comment change was rolled back.
            UserVerifier.VerifyUserComment(String.Empty, "testName1", "initialTestComment1");

            // clean up
            UserVerifier.DeleteLocalUser("testName1");
        }

        // Verify that a comment can be deleted for an existing user
        [RuntimeFact]
        public void CanDeleteCommentOfExistingUser()
        {
            UserVerifier.CreateLocalUser("testName1", "test123!@#");
            UserVerifier.SetUserComment(String.Empty, "testName1", "testComment1");
            var productCommentDelete = this.CreatePackageInstaller("ProductCommentDelete");

            productCommentDelete.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify that comment was removed.
            UserVerifier.VerifyUserComment(String.Empty, "testName1", "");

            // clean up
            UserVerifier.DeleteLocalUser("testName1");
        }
    }
}
