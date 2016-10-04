﻿using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Data.SqlClient;
using System.Data;
using System.Threading;

namespace OfficeDevPnP.Core.Tests
{
    static class TestCommon
    {
        #region Constructor
        static TestCommon()
        {
            // Read configuration data
            TenantUrl = ConfigurationManager.AppSettings["SPOTenantUrl"];
            DevSiteUrl = ConfigurationManager.AppSettings["SPODevSiteUrl"];

            if (string.IsNullOrEmpty(TenantUrl) || string.IsNullOrEmpty(DevSiteUrl))
            {
                throw new ConfigurationErrorsException("Tenant site Url or Dev site url in App.config are not set up.");
            }

            // Trim trailing slashes
            TenantUrl = TenantUrl.TrimEnd(new[] { '/' });
            DevSiteUrl = DevSiteUrl.TrimEnd(new[] { '/' });

            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["SPOCredentialManagerLabel"]))
            {
                var tempCred = Core.Utilities.CredentialManager.GetCredential(ConfigurationManager.AppSettings["SPOCredentialManagerLabel"]);

                // username in format domain\user means we're testing in on-premises
                if (tempCred.UserName.IndexOf("\\") > 0)
                {
                    string[] userParts = tempCred.UserName.Split('\\');
                    Credentials = new NetworkCredential(userParts[1], tempCred.SecurePassword, userParts[0]);
                }
                else
                {
                    Credentials = new SharePointOnlineCredentials(tempCred.UserName, tempCred.SecurePassword);
                }                                
            }
            else
            {
                if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["SPOUserName"]) &&
                    !String.IsNullOrEmpty(ConfigurationManager.AppSettings["SPOPassword"]))
                {
                    UserName = ConfigurationManager.AppSettings["SPOUserName"];
                    var password = ConfigurationManager.AppSettings["SPOPassword"];

                    Password = GetSecureString(password);
                    Credentials = new SharePointOnlineCredentials(UserName, Password);
                }
                else if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["OnPremUserName"]) &&
                         !String.IsNullOrEmpty(ConfigurationManager.AppSettings["OnPremDomain"]) &&
                         !String.IsNullOrEmpty(ConfigurationManager.AppSettings["OnPremPassword"]))
                {
                    Password = GetSecureString(ConfigurationManager.AppSettings["OnPremPassword"]);
                    Credentials = new NetworkCredential(ConfigurationManager.AppSettings["OnPremUserName"], Password, ConfigurationManager.AppSettings["OnPremDomain"]);
                }
                else if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["AppId"]) &&
                         !String.IsNullOrEmpty(ConfigurationManager.AppSettings["AppSecret"]))
                {
                    AppId = ConfigurationManager.AppSettings["AppId"];
                    AppSecret = ConfigurationManager.AppSettings["AppSecret"];
                }
                else
                {
                    throw new ConfigurationErrorsException("Tenant credentials in App.config are not set up.");
                }
            }
        }
        #endregion

        #region Properties
        public static string TenantUrl { get; set; }
        public static string DevSiteUrl { get; set; }
        static string UserName { get; set; }
        static SecureString Password { get; set; }
        public static ICredentials Credentials { get; set; }
        static string AppId { get; set; }
        static string AppSecret { get; set; }

        public static String AzureStorageKey
        {
            get
            {
                return ConfigurationManager.AppSettings["AzureStorageKey"];
            }
        }
        public static String TestAutomationDatabaseConnectionString
        {
            get
            {
                return ConfigurationManager.AppSettings["TestAutomationDatabaseConnectionString"];
            }
        }
        #endregion

        #region Methods
        public static ClientContext CreateClientContext()
        {
            return CreateContext(DevSiteUrl, Credentials);
        }

        public static ClientContext CreateClientContext(string url)
        {
            return CreateContext(url, Credentials);
        }

        public static ClientContext CreateTenantClientContext()
        {
            return CreateContext(TenantUrl, Credentials);
        }

        public static PnPClientContext CreatePnPClientContext(int retryCount = 10, int delay = 500)
        {
            PnPClientContext context;
            if (!String.IsNullOrEmpty(AppId) && !String.IsNullOrEmpty(AppSecret))
            {
                AuthenticationManager am = new AuthenticationManager();
                ClientContext clientContext = null;

                if (new Uri(DevSiteUrl).DnsSafeHost.Contains("spoppe.com"))
                {
                    clientContext = am.GetAppOnlyAuthenticatedContext(DevSiteUrl, Core.Utilities.TokenHelper.GetRealmFromTargetUrl(new Uri(DevSiteUrl)), AppId, AppSecret, acsHostUrl: "windows-ppe.net", globalEndPointPrefix: "login");
                }
                else
                {
                    clientContext = am.GetAppOnlyAuthenticatedContext(DevSiteUrl, AppId, AppSecret);
                }
                context = PnPClientContext.ConvertFrom(clientContext, retryCount, delay);
            }
            else
            {
                context = new PnPClientContext(DevSiteUrl, retryCount, delay);
                context.Credentials = Credentials;
            }

            context.RequestTimeout = Timeout.Infinite;
            return context;
        }


        public static bool AppOnlyTesting()
        {
            if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["AppId"]) &&
                !String.IsNullOrEmpty(ConfigurationManager.AppSettings["AppSecret"]) &&
                String.IsNullOrEmpty(ConfigurationManager.AppSettings["SPOCredentialManagerLabel"]) &&
                String.IsNullOrEmpty(ConfigurationManager.AppSettings["SPOUserName"]) &&
                String.IsNullOrEmpty(ConfigurationManager.AppSettings["SPOPassword"]) &&
                String.IsNullOrEmpty(ConfigurationManager.AppSettings["OnPremUserName"]) &&
                String.IsNullOrEmpty(ConfigurationManager.AppSettings["OnPremDomain"]) &&
                String.IsNullOrEmpty(ConfigurationManager.AppSettings["OnPremPassword"]))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool TestAutomationSQLDatabaseAvailable()
        {
            string connectionString = TestAutomationDatabaseConnectionString;
            if (!String.IsNullOrEmpty(connectionString))
            {
                try
                {
                    var con = new SqlConnectionStringBuilder(connectionString);
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        return (conn.State == ConnectionState.Open);
                    }
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        private static ClientContext CreateContext(string contextUrl, ICredentials credentials)
        {
            ClientContext context;
            if (!String.IsNullOrEmpty(AppId) && !String.IsNullOrEmpty(AppSecret))
            {
                AuthenticationManager am = new AuthenticationManager();

                if (new Uri(DevSiteUrl).DnsSafeHost.Contains("spoppe.com"))
                {
                    context = am.GetAppOnlyAuthenticatedContext(contextUrl, Core.Utilities.TokenHelper.GetRealmFromTargetUrl(new Uri(DevSiteUrl)), AppId, AppSecret, acsHostUrl: "windows-ppe.net", globalEndPointPrefix:"login");
                }
                else
                {
                    context = am.GetAppOnlyAuthenticatedContext(contextUrl, AppId, AppSecret);
                }
            }
            else
            {
                context = new ClientContext(contextUrl);
                context.Credentials = credentials;
            }

            context.RequestTimeout = Timeout.Infinite;
            return context;
        }

        private static SecureString GetSecureString(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Input string is empty and cannot be made into a SecureString", "input");

            var secureString = new SecureString();
            foreach (char c in input.ToCharArray())
                secureString.AppendChar(c);

            return secureString;
        }
        #endregion
    }
}
