// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTestTools
{
    using System;
    using System.Text;
    using System.DirectoryServices;
    using System.DirectoryServices.AccountManagement;
    using System.Security.Principal;
    using Xunit;

    /// <summary>
    /// Contains methods for User Group verification
    /// </summary>
    public static class UserGroupVerifier
    {
        /// <summary>
        /// Create a local group on the machine
        /// </summary>
        /// <param name="groupName"></param>
        /// <remarks>Has to be run as an Admin</remarks>
        public static void CreateLocalGroup(string groupName)
        {
            DeleteLocalGroup(groupName);
            GroupPrincipal newGroup = new GroupPrincipal(new PrincipalContext(ContextType.Machine));
            newGroup.Name = groupName;
            newGroup.Description = String.Empty;
            newGroup.Save();
        }

        /// <summary>
        /// Deletes a local gorup from the machine
        /// </summary>
        /// <param name="groupName">group name to delete</param>
        /// <remarks>Has to be run as an Admin</remarks>
        public static void DeleteLocalGroup(string groupName)
        {
            GroupPrincipal newGroup = GetGroup(String.Empty, groupName);
            if (null != newGroup)
            {
                newGroup.Delete();
            }
        }

        /// <summary>
        /// Verifies that a group exists or not
        /// </summary>
        /// <param name="domainName">domain name for the group, empty for local groups</param>
        /// <param name="groupName">the group name</param>
        public static bool GroupExists(string domainName, string groupName)
        {
            GroupPrincipal group = GetGroup(domainName, groupName);

            return null != group;
        }

        /// <summary>
        /// Sets the group comment for a given group
        /// </summary>
        /// <param name="domainName">domain name for the group, empty for local users</param>
        /// <param name="groupName">the group name</param>
        /// <param name="comment">comment to be set for the group</param>
        public static void SetGroupComment(string domainName, string groupName, string comment)
        {
            GroupPrincipal group = GetGroup(domainName, groupName);

            Assert.False(null == group, String.Format("Group '{0}' was not found under domain '{1}'.", groupName, domainName));

            var directoryEntry = group.GetUnderlyingObject() as DirectoryEntry;
            Assert.False(null == directoryEntry);
            directoryEntry.Properties["Description"].Value = comment;
            group.Save();
        }

        /// <summary>
        /// Adds the specified group to the specified local group
        /// </summary>
        /// <param name="memberName">Member to add</param>
        /// <param name="groupName">Group to add too</param>
        public static void AddGroupToGroup(string memberName, string groupName)
        {
            DirectoryEntry localMachine;
            DirectoryEntry localGroup;

            localMachine = new DirectoryEntry("WinNT://" + Environment.MachineName.ToString());
            localGroup = localMachine.Children.Find(groupName, "group");
            Assert.False(null == localGroup, String.Format("Group '{0}' was not found.", groupName));
            DirectoryEntry group = FindActiveDirectoryGroup(memberName);
            localGroup.Invoke("Add", new object[] { group.Path.ToString() });
        }

        /// <summary>
        /// Find the specified group in AD
        /// </summary>
        /// <param name="groupName">group name to lookup</param>
        /// <returns>DirectoryEntry of the group</returns>
        private static DirectoryEntry FindActiveDirectoryGroup(string groupName)
        {
            var mLocalMachine = new DirectoryEntry("WinNT://" + Environment.MachineName.ToString());
            var mLocalEntries = mLocalMachine.Children;

            var theGroup = mLocalEntries.Find(groupName);
            return theGroup;
        }

        /// <summary>
        /// Verifies the group comment for a given group
        /// </summary>
        /// <param name="domainName">domain name for the group, empty for local users</param>
        /// <param name="groupName">the group name</param>
        /// <param name="comment">the comment to be verified</param>
        public static void VerifyGroupComment(string domainName, string groupName, string comment)
        {
            GroupPrincipal group = GetGroup(domainName, groupName);

            Assert.False(null == group, String.Format("Group '{0}' was not found under domain '{1}'.", groupName, domainName));

            var directoryEntry = group.GetUnderlyingObject() as DirectoryEntry;
            Assert.False(null == directoryEntry);
            Assert.True(comment == (string)(directoryEntry.Properties["Description"].Value));
        }

        /// <summary>
        /// Verify that a given group is member of a local group
        /// </summary>
        /// <param name="domainName">domain name for the group, empty for local groups</param>
        /// <param name="memberName">the member name</param>
        /// <param name="groupNames">list of groups to check for membership</param>
        public static void VerifyIsMemberOf(string domainName, string memberName, params string[] groupNames)
        {
            IsMemberOf(domainName, memberName, true, groupNames);
        }

        /// <summary>
        /// Verify that a given group is NOT member of a local group
        /// </summary>
        /// <param name="domainName">domain name for the group, empty for local groups</param>
        /// <param name="memberName">the member name</param>
        /// <param name="groupNames">list of groups to check for membership</param>
        public static void VerifyIsNotMemberOf(string domainName, string memberName, params string[] groupNames)
        {
            IsMemberOf(domainName, memberName, false, groupNames);
        }

        /// <summary>
        /// Verify that a given user is member of a local group
        /// </summary>
        /// <param name="domainName">domain name for the group, empty for local groups</param>
        /// <param name="memberName">the member name</param>
        /// <param name="shouldBeMember">whether the group is expected to be a member of the groups or not</param>
        /// <param name="groupNames">list of groups to check for membership</param>
        private static void IsMemberOf(string domainName, string memberName, bool shouldBeMember, params string[] groupNames)
        {
            GroupPrincipal group = GetGroup(domainName, memberName);
            Assert.False(null == group, String.Format("Group '{0}' was not found under domain '{1}'.", memberName, domainName));

            bool missedAGroup = false;
            string message = String.Empty;
            foreach (string groupName in groupNames)
            {
                try
                {
                    bool found = group.IsMemberOf(new PrincipalContext(ContextType.Machine), IdentityType.Name, groupName);
                    if (found != shouldBeMember)
                    {
                        missedAGroup = true;
                        message += String.Format("Group '{0}/{1}' is {2} a member of local group '{3}'. \r\n", domainName, memberName, found ? String.Empty : "NOT", groupName);
                    }
                }
                catch (System.DirectoryServices.AccountManagement.PrincipalOperationException)
                {
                    missedAGroup = true;
                    message += String.Format("Local group '{0}' was not found. \r\n", groupName);
                }
            }
            Assert.False(missedAGroup, message);
        }

        /// <summary>
        /// Returns the GroupPrincipal object for a given group
        /// </summary>
        /// <param name="domainName">Domain name to look under, if Empty the LocalMachine is assumed as the domain</param>
        /// <param name="groupName"></param>
        /// <returns>UserPrincipal Object for the group if found, or null other wise</returns>
        private static GroupPrincipal GetGroup(string domainName, string groupName)
        {
            if (String.IsNullOrEmpty(domainName))
            {
                return GroupPrincipal.FindByIdentity(new PrincipalContext(ContextType.Machine), IdentityType.Name, groupName);
            }
            else
            {
                return GroupPrincipal.FindByIdentity(new PrincipalContext(ContextType.Domain,domainName), IdentityType.Name, groupName);
            }
        }
    }
}

