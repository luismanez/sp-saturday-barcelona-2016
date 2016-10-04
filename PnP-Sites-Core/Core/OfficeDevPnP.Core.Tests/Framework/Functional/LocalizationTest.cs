﻿using Microsoft.SharePoint.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfficeDevPnP.Core.Enums;
using OfficeDevPnP.Core.Framework.Provisioning.Connectors;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers;
using OfficeDevPnP.Core.Tests.Framework.Functional.Validators;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace OfficeDevPnP.Core.Tests.Framework.Functional
{
#if !SP2013    
    /// <summary>
    /// Test cases for the provisioning engine Publishing functionality
    /// </summary>
    [TestClass]
    public class LocalizationTest : FunctionalTestBase
    {
        #region Construction
        public LocalizationTest()
        {
            //debugMode = true;
            //centralSiteCollectionUrl = "https://bertonline.sharepoint.com/sites/TestPnPSC_12345_6dbf8f61-89ae-4960-b5ef-f87768efc812";
            //centralSubSiteUrl = "https://bertonline.sharepoint.com/sites/TestPnPSC_12345_6dbf8f61-89ae-4960-b5ef-f87768efc812/sub";
        }
        #endregion

        #region Test setup
        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            ClassInitBase(context);
        }

        [ClassCleanup()]
        public static void ClassCleanup()
        {
            ClassCleanupBase();
        }
        #endregion

        /// <summary>
        /// PnPLocalizationTest test
        /// </summary>
        [TestMethod]
        public void SiteCollectionsLocalizationTest()
        {
            using (var cc = TestCommon.CreateClientContext(centralSiteCollectionUrl))
            {
                CleanUpTestData(cc);

                ProvisioningTemplateCreationInformation ptci = new ProvisioningTemplateCreationInformation(cc.Web);
                ptci.PersistMultiLanguageResources = true;
                ptci.FileConnector = new FileSystemConnector(string.Format(@"{0}\..\..\Framework\Functional", AppDomain.CurrentDomain.BaseDirectory), "Templates");
                ptci.HandlersToProcess = Handlers.Fields|Handlers.ContentTypes|Handlers.Lists|Handlers.SupportedUILanguages|Handlers.CustomActions ;
                
                var result = TestProvisioningTemplate(cc, "localization_add.xml", Handlers.Fields|Handlers.ContentTypes|Handlers.Lists | Handlers.SupportedUILanguages | Handlers.CustomActions, null, ptci);
                LocalizationValidator Validator = new LocalizationValidator();
                Assert.IsTrue(Validator.Validate(result.SourceTemplate, result.TargetTemplate, result.SourceTokenParser, result.TargetTokenParser));
            }

        }
        /// <summary>
        /// PnPLocalizationTest test
        /// </summary>
        [TestMethod]
        public void WebLocalizationTest()
        {
            using (var cc = TestCommon.CreateClientContext(centralSubSiteUrl))
            {
                CleanUpTestData(cc);

                ProvisioningTemplateCreationInformation ptci = new ProvisioningTemplateCreationInformation(cc.Web);
                ptci.PersistMultiLanguageResources = true;
                ptci.FileConnector = new FileSystemConnector(string.Format(@"{0}\..\..\Framework\Functional", AppDomain.CurrentDomain.BaseDirectory), "Templates");
                ptci.HandlersToProcess = Handlers.Fields|Handlers.ContentTypes|Handlers.Lists|Handlers.SupportedUILanguages|Handlers.CustomActions;
                
                var result = TestProvisioningTemplate(cc, "localization_add.xml", Handlers.Fields|Handlers.ContentTypes|Handlers.Lists | Handlers.SupportedUILanguages | Handlers.CustomActions, null, ptci);
                LocalizationValidator Validator = new LocalizationValidator();
                Assert.IsTrue(Validator.Validate(result.SourceTemplate, result.TargetTemplate, result.SourceTokenParser, result.TargetTokenParser));
            }
        }

        #region Helper methods
        private void CleanUpTestData(ClientContext cc)
        {
            DeleteLists(cc);
            DeleteContentTypes(cc);
            DeleteCustomActions(cc);
        }

        private void DeleteLists(ClientContext cc)
        {
            DeleteListsImplementation(cc);
        }

        private static void DeleteListsImplementation(ClientContext cc)
        {
            cc.Load(cc.Web.Lists, f => f.Include(t => t.DefaultViewUrl));
            cc.ExecuteQueryRetry();

            foreach (var list in cc.Web.Lists.ToList())
            {
                if (list.DefaultViewUrl.Contains("LI_"))
                {
                    list.DeleteObject();
                }
            }
            cc.ExecuteQueryRetry();
        }

        private void DeleteContentTypes(ClientContext cc)
        {
            // Drop the content types
            cc.Load(cc.Web.ContentTypes, f => f.Include(t => t.Group));
            cc.ExecuteQueryRetry();

            foreach (var ct in cc.Web.ContentTypes.ToList())
            {
                if (ct.Group.Equals("PnP Localization Demo"))
                {
                    ct.DeleteObject();
                }
            }
            cc.ExecuteQueryRetry();

            // Drop the fields
            DeleteFields(cc);
        }

        private void DeleteFields(ClientContext cc)
        {
            cc.Load(cc.Web.Fields, f => f.Include(t => t.InternalName));
            cc.ExecuteQueryRetry();

            foreach (var field in cc.Web.Fields.ToList())
            {
                // First drop the fields that have 2 _'s...convention used to name the fields dependent on a lookup.
                if (field.InternalName.Replace("FLD_CT_", "").IndexOf("_") > 0)
                {
                    if (field.InternalName.StartsWith("FLD_CT_"))
                    {
                        field.DeleteObject();
                    }
                }
            }

            foreach (var field in cc.Web.Fields.ToList())
            {
                if (field.InternalName.StartsWith("FLD_CT_"))
                {
                    field.DeleteObject();
                }
            }

            cc.ExecuteQueryRetry();

        }
        private void DeleteCustomActions(ClientContext cc)
        {
            var siteActions = cc.Site.GetCustomActions();
            foreach (var action in siteActions)
            {
                if (action.Name.StartsWith("CA_"))
                {
                    cc.Site.DeleteCustomAction(action.Id);
                }
            }

            var webActions = cc.Web.GetCustomActions();
            foreach (var action in webActions)
            {
                if (action.Name.StartsWith("CA_"))
                {
                    cc.Web.DeleteCustomAction(action.Id);
                }
            }
        }
        #endregion
    }
#endif
}
