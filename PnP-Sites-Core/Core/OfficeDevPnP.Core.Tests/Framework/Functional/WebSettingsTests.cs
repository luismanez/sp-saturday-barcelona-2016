﻿using Microsoft.SharePoint.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfficeDevPnP.Core.Enums;
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
    /// <summary>
    /// Test cases for the provisioning engine web settings functionality
    /// </summary>
    [TestClass]
   public class WebSettingsTests : FunctionalTestBase
    {
        #region Construction
        public WebSettingsTests()
        {
            //debugMode = true;
            //centralSiteCollectionUrl = "https://bertonline.sharepoint.com/sites/TestPnPSC_12345_01efe7c1-e516-4a84-905d-d2763cfed349";
            //centralSubSiteUrl = "https://bertonline.sharepoint.com/sites/TestPnPSC_12345_01efe7c1-e516-4a84-905d-d2763cfed349/sub";
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

        #region Site collection test cases
        /// <summary>
        /// Site WebSettings Test
        /// </summary>
        [TestMethod]
        public void SiteCollectionWebSettingsTest()
        {
            using (var cc = TestCommon.CreateClientContext(centralSiteCollectionUrl))
            {
                // Add supporting files
                TestProvisioningTemplate(cc, "websettings_files.xml", Handlers.Files);

                var result = TestProvisioningTemplate(cc, "websettings_add.xml", Handlers.WebSettings);
                WebSettingsValidator wv = new WebSettingsValidator();
                Assert.IsTrue(wv.Validate(result.SourceTemplate.WebSettings, result.TargetTemplate.WebSettings, result.TargetTokenParser));
            }
        }

        /// <summary>
        /// Site Auditsettings Test
        /// </summary>
        [TestMethod]
        public void SiteCollectionAuditSettingsTest()
        {
            using (var cc = TestCommon.CreateClientContext(centralSiteCollectionUrl))
            {
                var result = TestProvisioningTemplate(cc, "auditsettings_add.xml", Handlers.AuditSettings);
                AuditSettingsValidator av = new AuditSettingsValidator();
                Assert.IsTrue(av.Validate(result.SourceTemplate.AuditSettings, result.TargetTemplate.AuditSettings, result.TargetTokenParser));
            }
        }
        #endregion

        #region Web test cases
        /// <summary>
        /// Web WebSettings test
        /// </summary>
        [TestMethod]
        public void WebWebSettingsTest()
        {
            using (var cc = TestCommon.CreateClientContext(centralSiteCollectionUrl))
            {
                // Add supporting files
                TestProvisioningTemplate(cc, "websettings_files.xml", Handlers.Files);
            }

            using (var cc = TestCommon.CreateClientContext(centralSubSiteUrl))
            {
                var result = TestProvisioningTemplate(cc, "websettings_add.xml", Handlers.WebSettings);
                WebSettingsValidator wv = new WebSettingsValidator();
                Assert.IsTrue(wv.Validate(result.SourceTemplate.WebSettings, result.TargetTemplate.WebSettings, result.TargetTokenParser));
            }
        }

        // Audit settings are only possible on site collection level, hence no test at web level!
        #endregion
    }
}
