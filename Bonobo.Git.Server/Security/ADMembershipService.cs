﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using System.Data.Entity.Core;
using System.Diagnostics;
using Bonobo.Git.Server.Data;
using Bonobo.Git.Server.Models;
using System.Web.Security;
using System.Security.Principal;
using Bonobo.Git.Server.Configuration;

namespace Bonobo.Git.Server.Security
{
    public class ADMembershipService : IMembershipService
    {
        public bool IsReadOnly()
        {
            return true;
        }

        public ValidationResult ValidateUser(string username, string password)
        {
            ValidationResult result = ValidationResult.Failure;

            if (String.IsNullOrEmpty(username)) throw new ArgumentException("Value cannot be null or empty", "username");
            if (String.IsNullOrEmpty(password)) throw new ArgumentException("Value cannot be null or empty", "password");

            try
            {
                string domain = username.GetDomain();
                if (String.IsNullOrEmpty(domain))
                {
                    domain = Configuration.ActiveDirectorySettings.DefaultDomain;
                }

                using (PrincipalContext principalContext = new PrincipalContext(ContextType.Domain, domain))
                {
                    if (principalContext.ValidateCredentials(username, password, ContextOptions.Negotiate))
                    {
                        using (UserPrincipal user = UserPrincipal.FindByIdentity(principalContext, username))
                        {
							using (GroupPrincipal group = GroupPrincipal.FindByIdentity(principalContext, Configuration.ActiveDirectorySettings.MemberGroupName))
							{
								if (group == null)
									result = ValidationResult.Failure;

								if (user != null)
								{
									if (!group.GetMembers(true).Contains(user))
									{
										result = ValidationResult.NotAuthorized;
									}
									else
									{
										result = ValidationResult.Success;
									}
								}
							}
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Trace.TraceError("AD.ValidateUser Exception: " + ex);
                result = ValidationResult.Failure;
            }

            return result;
        }

        public bool CreateUser(string username, string password, string givenName, string surname, string email, Guid id)
        {
            return false;
        }

        public bool CreateUser(string username, string password, string givenName, string surname, string email)
        {
            return false;
        }

        public IList<UserModel> GetAllUsers()
        {
            var users = ADBackend.Instance.Users.ToList();
            return users;
        }

        public UserModel GetUserModel(string username)
        {
            if (!UsernameContainsDomain(username))
            {
                using (PrincipalContext principalContext = new PrincipalContext(ContextType.Domain, ActiveDirectorySettings.DefaultDomain))
                using (UserPrincipal user = UserPrincipal.FindByIdentity(principalContext, username))
                {
                    // assuming all users have a guid on AD
                    return ADBackend.Instance.Users.FirstOrDefault(n => n.Id == user.Guid.Value);
                }
            }
            else if (!string.IsNullOrEmpty(username))
            {
                return ADBackend.Instance.Users.Where(x => x.Username.Equals(username, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            }

            return null;
        }

        public UserModel GetUserModel(Guid id)
        {
            return ADBackend.Instance.Users[id];
        }

        private static bool UsernameContainsDomain(string username)
        {
            return String.IsNullOrEmpty(username) && !string.IsNullOrEmpty(username.GetDomain());
        }

        public void UpdateUser(Guid id, string username, string givenName, string surname, string email, string password)
        {
            throw new NotImplementedException();
        }

        public void DeleteUser(Guid id)
        {
            throw new NotImplementedException();
        }

        public string GenerateResetToken(string username)
        {
            throw new NotImplementedException();
        }
    }
}
