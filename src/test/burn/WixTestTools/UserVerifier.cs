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
    /// Contains methods for User account verification
    /// </summary>
    public static class UserVerifier
    {
        public static class SIDStrings
        {
            // Built-In Local Groups
            public static readonly string BUILTIN_ADMINISTRATORS = "S-1-5-32-544";
            public static readonly string BUILTIN_USERS = "S-1-5-32-545";
            public static readonly string BUILTIN_GUESTS = "S-1-5-32-546";
            public static readonly string BUILTIN_ACCOUNT_OPERATORS = "S-1-5-32-548";
            public static readonly string BUILTIN_SERVER_OPERATORS = "S-1-5-32-549";
            public static readonly string BUILTIN_PRINT_OPERATORS = "S-1-5-32-550";
            public static readonly string BUILTIN_BACKUP_OPERATORS = "S-1-5-32-551";
            public static readonly string BUILTIN_REPLICATOR = "S-1-5-32-552";

            // Special Groups                                          
            public static readonly string CREATOR_OWNER = "S-1-3-0";
            public static readonly string EVERYONE = "S-1-1-0";
            public static readonly string NT_AUTHORITY_NETWORK = "S-1-5-2";
            public static readonly string NT_AUTHORITY_INTERACTIVE = "S-1-5-4";
            public static readonly string NT_AUTHORITY_SYSTEM = "S-1-5-18";
            public static readonly string NT_AUTHORITY_Authenticated_Users = "S-1-5-11";
            public static readonly string NT_AUTHORITY_LOCAL_SERVICE = "S-1-5-19";
            public static readonly string NT_AUTHORITY_NETWORK_SERVICE = "S-1-5-20";
        }

        /// <summary>
        /// Create a local user on the machine
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <remarks>Has to be run as an Admin</remarks>
        public static void CreateLocalUser(string userName, string password)
        {
            DeleteLocalUser(userName);
            UserPrincipal newUser = new UserPrincipal(new PrincipalContext(ContextType.Machine));
            newUser.SetPassword(password);
            newUser.Name = userName;
            newUser.Description = String.Empty;
            newUser.UserCannotChangePassword = true;
            newUser.PasswordNeverExpires = false;
            newUser.Save();
        }

        /// <summary>
        /// Deletes a local user from the machine
        /// </summary>
        /// <param name="userName">user name to delete</param>
        /// <remarks>Has to be run as an Admin</remarks>
        public static void DeleteLocalUser(string userName)
        {
            UserPrincipal newUser = GetUser(String.Empty, userName);
            if (null != newUser)
            {
                newUser.Delete();
            }
        }

        /// <summary>
        /// Verifies that a user exisits or not
        /// </summary>
        /// <param name="domainName">domain name for the user, empty for local users</param>
        /// <param name="userName">the user name</param>
        public static bool UserExists(string domainName, string userName)
        {
            UserPrincipal user = GetUser(domainName, userName);

            return null != user;
        }

        /// <summary>
        /// Sets the user information for a given user
        /// </summary>
        /// <param name="domainName">domain name for the user, empty for local users</param>
        /// <param name="userName">the user name</param>
        /// <param name="passwordExpired">user is required to change the password on first login</param>
        /// <param name="passwordNeverExpires">password never expires</param>
        /// <param name="disabled">account is disabled</param>
        public static void SetUserInformation(string domainName, string userName, bool passwordExpired, bool passwordNeverExpires, bool disabled)
        {
            UserPrincipal user = GetUser(domainName, userName);

            Assert.False(null == user, String.Format("User '{0}' was not found under domain '{1}'.", userName, domainName));
            user.PasswordNeverExpires = passwordNeverExpires;
            user.Enabled = !disabled;
            if (passwordExpired)
            {
                user.ExpirePasswordNow();
            }
            else
            {
                // extend the expiration date to a month
                user.AccountExpirationDate = DateTime.Now.Add(new TimeSpan(30, 0, 0, 0, 0));
            }
            user.Save();
        }

        /// <summary>
        /// Sets the user comment for a given user
        /// </summary>
        /// <param name="domainName">domain name for the user, empty for local users</param>
        /// <param name="userName">the user name</param>
        /// <param name="comment">comment to be set for the user</param>
        public static void SetUserComment(string domainName, string userName, string comment)
        {
            UserPrincipal user = GetUser(domainName, userName);

            Assert.False(null == user, String.Format("User '{0}' was not found under domain '{1}'.", userName, domainName));

            var directoryEntry = user.GetUnderlyingObject() as DirectoryEntry;
            Assert.False(null == directoryEntry);
            directoryEntry.Properties["Description"].Value = comment;
            user.Save();
        }

        /// <summary>
        /// Adds the specified user to the specified local group
        /// </summary>
        /// <param name="userName">User to add</param>
        /// <param name="groupName">Group to add too</param>
        public static void AddUserToGroup(string userName, string groupName)
        {
            DirectoryEntry localMachine;
            DirectoryEntry localGroup;

            localMachine = new DirectoryEntry("WinNT://" + Environment.MachineName.ToString());
            localGroup = localMachine.Children.Find(groupName, "group");
            Assert.False(null == localGroup, String.Format("Group '{0}' was not found.", groupName));
            DirectoryEntry user = FindActiveDirectoryUser(userName);
            localGroup.Invoke("Add", new object[] { user.Path.ToString() });
        }

        /// <summary>
        /// Find the specified user in AD
        /// </summary>
        /// <param name="UserName">user name to lookup</param>
        /// <returns>DirectoryEntry of the user</returns>
        private static DirectoryEntry FindActiveDirectoryUser(string UserName)
        {
            var mLocalMachine = new DirectoryEntry("WinNT://" + Environment.MachineName.ToString());
            var mLocalEntries = mLocalMachine.Children;

            var theUser = mLocalEntries.Find(UserName);
            return theUser;
        }

        /// <summary>
        /// Verifies the user information for a given user
        /// </summary>
        /// <param name="domainName">domain name for the user, empty for local users</param>
        /// <param name="userName">the user name</param>
        /// <param name="passwordExpired">user is required to change the password on first login</param>
        /// <param name="passwordNeverExpires">password never expires</param>
        /// <param name="disabled">account is disabled</param>
        public static void VerifyUserInformation(string domainName, string userName, bool passwordExpired, bool passwordNeverExpires, bool disabled)
        {
            UserPrincipal user = GetUser(domainName, userName);

            Assert.False(null == user, String.Format("User '{0}' was not found under domain '{1}'.", userName, domainName));

            Assert.True(passwordNeverExpires == user.PasswordNeverExpires, String.Format("Password Never Expires for user '{0}\\{1}' is: '{2}', expected: '{3}'.", domainName, userName, user.PasswordNeverExpires, passwordNeverExpires));
            Assert.True(disabled != user.Enabled, String.Format("Account disabled for user '{0}\\{1}' is: '{2}', expected: '{3}'.", domainName, userName, !user.Enabled, disabled));

            DateTime expirationDate = user.AccountExpirationDate.GetValueOrDefault();
            bool accountExpired = expirationDate.ToLocalTime().CompareTo(DateTime.Now) <= 0;
            Assert.True(passwordExpired == accountExpired, String.Format("Password Expired for user '{0}\\{1}' is: '{2}', expected: '{3}'.", domainName, userName, accountExpired, passwordExpired));
        }

        /// <summary>
        /// Verifies the user comment for a given user
        /// </summary>
        /// <param name="domainName">domain name for the user, empty for local users</param>
        /// <param name="userName">the user name</param>
        /// <param name="comment">the comment to be verified</param>
        public static void VerifyUserComment(string domainName, string userName, string comment)
        {
            UserPrincipal user = GetUser(domainName, userName);

            Assert.False(null == user, String.Format("User '{0}' was not found under domain '{1}'.", userName, domainName));

            var directoryEntry = user.GetUnderlyingObject() as DirectoryEntry;
            Assert.False(null == directoryEntry);
            Assert.True(comment == (string)(directoryEntry.Properties["Description"].Value));
        }

        /// <summary>
        /// Verify that a given user is member of a local group
        /// </summary>
        /// <param name="domainName">domain name for the user, empty for local users</param>
        /// <param name="userName">the user name</param>
        /// <param name="groupNames">list of groups to check for membership</param>
        public static void VerifyUserIsMemberOf(string domainName, string userName, params string[] groupNames)
        {
            IsUserMemberOf(domainName, userName, true, groupNames);
        }

        /// <summary>
        /// Verify that a givin user is NOT member of a local group
        /// </summary>
        /// <param name="domainName">domain name for the user, empty for local users</param>
        /// <param name="userName">the user name</param>
        /// <param name="groupNames">list of groups to check for membership</param>
        public static void VerifyUserIsNotMemberOf(string domainName, string userName, params string[] groupNames)
        {
            IsUserMemberOf(domainName, userName, false, groupNames);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="SID">SID to search for</param>
        /// <returns>AccountName</returns>
        public static string GetLocalUserNameFromSID(string sidString)
        {
            SecurityIdentifier sid = new SecurityIdentifier(sidString);
            NTAccount account = (NTAccount)sid.Translate(typeof(NTAccount));
            return account.Value;
        }

        /// <summary>
        /// Get the SID string for a given user name
        /// </summary>
        /// <param name="Domain"></param>
        /// <param name="UserName"></param>
        /// <returns>SID string</returns>
        public static string GetSIDFromUserName(string Domain, string UserName)
        {
            string retVal = null;
            string domain = Domain;
            string name = UserName;

            if (String.IsNullOrEmpty(domain))
            {
                domain = System.Environment.MachineName;
            }

            try
            {
                DirectoryEntry de = new DirectoryEntry("WinNT://" + domain + "/" + name);

                long iBigVal = 5;
                byte[] bigArr = BitConverter.GetBytes(iBigVal);
                System.DirectoryServices.PropertyCollection coll = de.Properties;
                object obVal = coll["objectSid"].Value;
                if (null != obVal)
                {
                    retVal = ConvertByteToSidString((byte[])obVal);
                }
            }
            catch (Exception ex)
            {
                retVal = String.Empty;
                Console.Write(ex.Message);
            }
            
            return retVal;
        }

        /// <summary>
        /// converts a byte array containing a SID into a string
        /// </summary>
        /// <param name="sidBytes"></param>
        /// <returns>SID string</returns>
        private static string ConvertByteToSidString(byte[] sidBytes)
        {
            short sSubAuthorityCount;
            StringBuilder strSid = new StringBuilder();
            strSid.Append("S-");
            try
            {
                // Add SID revision.
                strSid.Append(sidBytes[0].ToString());

                sSubAuthorityCount = Convert.ToInt16(sidBytes[1]);

                // Next six bytes are SID authority value.
                if (sidBytes[2] != 0 || sidBytes[3] != 0)
                {
                    string strAuth = String.Format("0x{0:2x}{1:2x}{2:2x}{3:2x}{4:2x}{5:2x}",
                                (short)sidBytes[2],
                                (short)sidBytes[3],
                                (short)sidBytes[4],
                                (short)sidBytes[5],
                                (short)sidBytes[6],
                                (short)sidBytes[7]);
                    strSid.Append("-");
                    strSid.Append(strAuth);
                }
                else
                {
                    long iVal = (int)(sidBytes[7]) +
                            (int)(sidBytes[6] << 8) +
                            (int)(sidBytes[5] << 16) +
                            (int)(sidBytes[4] << 24);
                    strSid.Append("-");
                    strSid.Append(iVal.ToString());
                }

                // Get sub authority count...
                int idxAuth = 0;
                for (int i = 0; i < sSubAuthorityCount; i++)
                {
                    idxAuth = 8 + i * 4;
                    uint iSubAuth = BitConverter.ToUInt32(sidBytes, idxAuth);
                    strSid.Append("-");
                    strSid.Append(iSubAuth.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "";
            }
            return strSid.ToString();
        }

        /// <summary>
        /// Verify that a given user is member of a local group
        /// </summary>
        /// <param name="domainName">domain name for the user, empty for local users</param>
        /// <param name="userName">the user name</param>
        /// <param name="shouldBeMember">whether the user is expected to be a member of the groups or not</param>
        /// <param name="groupNames">list of groups to check for membership</param>
        private static void IsUserMemberOf(string domainName, string userName, bool shouldBeMember, params string[] groupNames)
        {
            UserPrincipal user = GetUser(domainName, userName);
            Assert.False(null == user, String.Format("User '{0}' was not found under domain '{1}'.", userName, domainName));

            bool missedAGroup = false;
            string message = String.Empty;
            foreach (string groupName in groupNames)
            {
                try
                {
                    bool found = user.IsMemberOf(new PrincipalContext(ContextType.Machine), IdentityType.Name, groupName);
                    if (found != shouldBeMember)
                    {
                        missedAGroup = true;
                        message += String.Format("User '{0}\\{1}' is {2} a member of local group '{3}'. \r\n", domainName, userName, found ? String.Empty : "NOT", groupName);
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
        /// Returns the UserPrincipal object for a given user
        /// </summary>
        /// <param name="domainName">Domain name to look under, if Empty the LocalMachine is assumned as the domain</param>
        /// <param name="userName"></param>
        /// <returns>UserPrinicipal Object for the user if found, or null other wise</returns>
        private static UserPrincipal GetUser(string domainName, string userName)
        {
            if (String.IsNullOrEmpty(domainName))
            {
                return UserPrincipal.FindByIdentity(new PrincipalContext(ContextType.Machine), IdentityType.Name, userName);
            }
            else
            {
                return UserPrincipal.Current;//.FindByIdentity(new PrincipalContext(ContextType.Domain,domainName), IdentityType.Name, userName);
            }
        }
    }
}

