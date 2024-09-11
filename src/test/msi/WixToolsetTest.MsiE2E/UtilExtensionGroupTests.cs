// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.MsiE2E
{
    using System;
    using WixTestTools;
    using Xunit;
    using Xunit.Abstractions;

    public class UtilExtensionGroupTests : MsiE2ETests
    {
        public UtilExtensionGroupTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        #region Non Domain
        // Verify that the users specified in the authoring are created as expected.
        [RuntimeFact]
        public void CanInstallAndUninstallNonDomainGroups()
        {
            UserGroupVerifier.CreateLocalGroup("testName3");
            var productA = this.CreatePackageInstaller("ProductA");

            productA.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            // Validate New User Information.
            Assert.True(UserGroupVerifier.GroupExists(String.Empty, "testName1"), String.Format("Group '{0}' was not created on Install", "testName1"));
            Assert.True(UserGroupVerifier.GroupExists(String.Empty, "testName2"), String.Format("Group '{0}' was not created on Install", "testName2"));
            Assert.True(UserGroupVerifier.GroupExists(String.Empty, "testName3"), String.Format("Group '{0}' was not created on Install", "testName3"));

            productA.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify Users marked as RemoveOnUninstall were removed.
            Assert.False(UserGroupVerifier.GroupExists(String.Empty, "testName1"), String.Format("Group '{0}' was not removed on Uninstall", "testName1"));
            Assert.True(UserGroupVerifier.GroupExists(String.Empty, "testName2"), String.Format("Group '{0}' was removed on Uninstall", "testName2"));

            // clean up
            UserGroupVerifier.DeleteLocalGroup("testName1");
            UserGroupVerifier.DeleteLocalGroup("testName2");
            UserGroupVerifier.DeleteLocalGroup("testName3");
        }

        // Verify the rollback action reverts all Users changes.
        [RuntimeFact]
        public void CanRollbackNonDomainGroups()
        {
            UserGroupVerifier.CreateLocalGroup("testName3");
            var productFail = this.CreatePackageInstaller("ProductFail");

            // make sure the user accounts are deleted before we start
            UserGroupVerifier.DeleteLocalGroup("testName1");
            UserGroupVerifier.DeleteLocalGroup("testName2");

            productFail.InstallProduct(MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

            // Verify added Users were removed on rollback.
            Assert.False(UserGroupVerifier.GroupExists(String.Empty, "testName1"), String.Format("Group '{0}' was not removed on Rollback", "testName1"));
            Assert.False(UserGroupVerifier.GroupExists(String.Empty, "testName2"), String.Format("Group '{0}' was not removed on Rollback", "testName2"));

            // clean up
            UserGroupVerifier.DeleteLocalGroup("testName1");
            UserGroupVerifier.DeleteLocalGroup("testName2");
            UserGroupVerifier.DeleteLocalGroup("testName3");
        }


        // Verify that command-line parameters aer not blocked by repair switches.
        // Original code signalled repair mode by using "-f ", which silently
        // terminated the command-line parsing, ignoring any parameters that followed.
        [RuntimeFact()]
        public void CanRepairNonDomainGroupsWithCommandLineParameters()
        {
            var arguments = new string[]
            {
                "TESTPARAMETER1=testName1",
            };
            var productWithCommandLineParameters = this.CreatePackageInstaller("ProductWithCommandLineParameters");

            // Make sure that the user doesn't exist when we start the test.
            UserGroupVerifier.DeleteLocalGroup("testName1");

            // Install
            productWithCommandLineParameters.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS, arguments);

            // Repair
            productWithCommandLineParameters.RepairProduct(MSIExec.MSIExecReturnCode.SUCCESS, arguments);


            // Install
            productWithCommandLineParameters.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS, arguments);

            // Clean up
            UserGroupVerifier.DeleteLocalGroup("testName1");
        }


        // Verify that the groups specified in the authoring are created as expected on repair.
        [RuntimeFact()]
        public void CanRepairNonDomainGroups()
        {
            UserGroupVerifier.CreateLocalGroup("testName3");
            var productA = this.CreatePackageInstaller("ProductA");

            productA.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            // Validate New User Information.
            UserGroupVerifier.DeleteLocalGroup("testName1");

            productA.RepairProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            // Validate New User Information.
            Assert.True(UserGroupVerifier.GroupExists(String.Empty, "testName1"), String.Format("User '{0}' was not installed on Repair", "testName1"));
            Assert.True(UserGroupVerifier.GroupExists(String.Empty, "testName2"), String.Format("User '{0}' was not installed after Repair", "testName2"));

            productA.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify Users marked as RemoveOnUninstall were removed.
            Assert.False(UserGroupVerifier.GroupExists(String.Empty, "testName1"), String.Format("User '{0}' was not removed on Uninstall", "testName1"));
            Assert.True(UserGroupVerifier.GroupExists(String.Empty, "testName2"), String.Format("User '{0}' was removed on Uninstall", "testName2"));

            // clean up
            UserGroupVerifier.DeleteLocalGroup("testName1");
            UserGroupVerifier.DeleteLocalGroup("testName2");
            UserGroupVerifier.DeleteLocalGroup("testName3");
        }

        // Verify that Installation fails if FailIfExists is set.
        [RuntimeFact]
        public void FailsIfNonDomainGroupExists()
        {
            var productFailIfExists = this.CreatePackageInstaller("ProductFailIfExists");

            // Create 'existinggroup'
            UserGroupVerifier.CreateLocalGroup("existinggroup");

            try
            {
                productFailIfExists.InstallProduct(MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

                // Verify User still exists.
                bool userExists = UserGroupVerifier.GroupExists(String.Empty, "existinggroup");

                Assert.True(userExists, String.Format("Group '{0}' was removed on Rollback", "existinggroup"));
            }
            finally
            {
                // clean up
                UserGroupVerifier.DeleteLocalGroup("existinggroup");
            }
        }

        // Verify that a group cannot be created on a domain on which you don't have create user permission.
        [RuntimeFact]
        public void FailsIfRestrictedDomain()
        {
            var productRestrictedDomain = this.CreatePackageInstaller("ProductRestrictedDomain");

            string logFile = productRestrictedDomain.InstallProduct(MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE, "TESTDOMAIN=DOESNOTEXIST");

            // Verify expected error message in the log file
            Assert.True(LogVerifier.MessageInLogFile(logFile, "CreateGroup:  Error 0x8007054b: failed to find Domain DOESNOTEXIST."));
        }

        // Verify that a group can be created with a group comment
        [RuntimeFact]
        public void CanCreateNewNonDomainGroupWithComment()
        {
            var productNewUserWithComment = this.CreatePackageInstaller("ProductNewGroupWithComment");

            productNewUserWithComment.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);
            UserGroupVerifier.VerifyGroupComment(String.Empty, "testName1", "testComment1");
            productNewUserWithComment.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            // clean up
            UserGroupVerifier.DeleteLocalGroup("testName1");
        }

        // Verify that a comment can be added to an existing group
        [RuntimeFact]
        public void CanAddCommentToExistingNonDomainGroup()
        {
            UserGroupVerifier.CreateLocalGroup("testName1");
            var productAddCommentToExistingUser = this.CreatePackageInstaller("ProductAddCommentToExistingGroup");

            productAddCommentToExistingUser.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            UserGroupVerifier.VerifyGroupComment(String.Empty, "testName1", "testComment1");

            productAddCommentToExistingUser.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            // clean up
            UserGroupVerifier.DeleteLocalGroup("testName1");
        }

        // Verify that a comment can be repaired for a new group
        [RuntimeFact]
        public void CanRepairCommentOfNewNonDomainGroup()
        {
            var productNewUserWithComment = this.CreatePackageInstaller("ProductNewGroupWithComment");

            productNewUserWithComment.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);
            UserGroupVerifier.SetGroupComment(String.Empty, "testName1", "");

            productNewUserWithComment.RepairProduct(MSIExec.MSIExecReturnCode.SUCCESS);
            UserGroupVerifier.VerifyGroupComment(String.Empty, "testName1", "testComment1");
            productNewUserWithComment.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            // clean up
            UserGroupVerifier.DeleteLocalGroup("testName1");
        }

        // Verify that a comment can be changed for an existing group
        [RuntimeFact]
        public void CanChangeCommentOfExistingNonDomainGroup()
        {
            UserGroupVerifier.CreateLocalGroup("testName1");
            UserGroupVerifier.SetGroupComment(String.Empty, "testName1", "initialTestComment1");
            var productNewUserWithComment = this.CreatePackageInstaller("ProductNewGroupWithComment");

            productNewUserWithComment.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);
            UserGroupVerifier.VerifyGroupComment(String.Empty, "testName1", "testComment1");
            productNewUserWithComment.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            // clean up
            UserGroupVerifier.DeleteLocalGroup("testName1");
        }

        // Verify that a comment can be rolled back for an existing group
        [RuntimeFact]
        public void CanRollbackCommentOfExistingNonDomainGroup()
        {
            UserGroupVerifier.CreateLocalGroup("testName1");
            UserGroupVerifier.SetGroupComment(String.Empty, "testName1", "initialTestComment1");
            var productCommentFail = this.CreatePackageInstaller("ProductCommentFail");

            productCommentFail.InstallProduct(MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

            // Verify that comment change was rolled back.
            UserGroupVerifier.VerifyGroupComment(String.Empty, "testName1", "initialTestComment1");

            // clean up
            UserGroupVerifier.DeleteLocalGroup("testName1");
        }

        // Verify that a comment can be deleted for an existing group
        [RuntimeFact]
        public void CanDeleteCommentOfExistingNonDomainGroup()
        {
            UserGroupVerifier.CreateLocalGroup("testName1");
            UserGroupVerifier.SetGroupComment(String.Empty, "testName1", "testComment1");
            var productCommentDelete = this.CreatePackageInstaller("ProductCommentDelete");

            productCommentDelete.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify that comment was removed.
            UserGroupVerifier.VerifyGroupComment(String.Empty, "testName1", "");


            productCommentDelete.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            // clean up
            UserGroupVerifier.DeleteLocalGroup("testName1");
        }

        #endregion

        #region Domain
        // Verify that a domain group can be nested within a local group
        [RuntimeFact(DomainRequired = true)]
        public void CanNestDomainGroups()
        {
            var testDomain = System.Environment.UserDomainName;
            var productNestedGroups = this.CreatePackageInstaller("ProductNestedGroups");

            productNestedGroups.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS, $"TESTDOMAIN={testDomain}");

            // Verify group nested membership
            UserGroupVerifier.VerifyIsMemberOf(testDomain, "Domain Users", new string[] { "testName1", "testName2" });
            //UserGroupVerifier.VerifyIsMemberOf(String.Empty, "Everyone", new string[] { "testName1" });

            UserGroupVerifier.VerifyIsNotMemberOf(testDomain, "Domain Users", new string[] { "testName3" });
            //UserGroupVerifier.VerifyIsNotMemberOf(String.Empty, "Everyone", new string[] { "testName2", "testName3" });

            productNestedGroups.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS, $"TESTDOMAIN={testDomain}");

            // clean up
            UserGroupVerifier.DeleteLocalGroup("testName1");
            UserGroupVerifier.DeleteLocalGroup("testName2");
            UserGroupVerifier.DeleteLocalGroup("testName3");
        }

        // Verify the rollback action reverts all Users changes.
        [RuntimeFact(DomainRequired = true)]
        public void CanRollbackDomainGroups()
        {
            var testDomain = System.Environment.UserDomainName;
            UserGroupVerifier.CreateLocalGroup("testName3");
            var productFail = this.CreatePackageInstaller("ProductFail");

            // make sure the user accounts are deleted before we start
            UserGroupVerifier.DeleteLocalGroup("testName1");
            UserGroupVerifier.DeleteLocalGroup("testName2");

            productFail.InstallProduct(MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE, $"TESTDOMAIN={testDomain}");

            // Verify added Users were removed on rollback.
            Assert.False(UserGroupVerifier.GroupExists(String.Empty, "testName1"), String.Format("Group '{0}' was not removed on Rollback", "testName1"));
            Assert.False(UserGroupVerifier.GroupExists(String.Empty, "testName2"), String.Format("Group '{0}' was not removed on Rollback", "testName2"));

            // clean up
            UserGroupVerifier.DeleteLocalGroup("testName1");
            UserGroupVerifier.DeleteLocalGroup("testName2");
            UserGroupVerifier.DeleteLocalGroup("testName3");
        }

        #endregion
    }
}
