using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfficeDevPnP.Core;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers;
using OfficeDevPnP.Core.Framework.Provisioning.Connectors;
using OfficeDevPnP.Core.Framework.Provisioning.Providers.Xml;
using System.Collections.Generic;

namespace SPSBcn.GulpAndPnp.Test
{
    [TestClass]
    public class DeployTest
    {
        [TestMethod]
        public void ApplyProvisioningTemplateTest()
        {
            string url = "https://lmanez.sharepoint.com/sites/spsbcn1";
            string distFolder = @"C:\_Community\SPSBcn\Demos\Done\SPSBcn.GulpAndPnp\SPSBcn.GulpAndPnp.Package\dist\src\pnp";

            //get ClientContext
            AuthenticationManager authManager = new AuthenticationManager();
            ClientContext context =
                authManager.GetSharePointOnlineAuthenticatedContextTenant(
                    url, StringExtensions.GetUser(), StringExtensions.GetPassword());

            //Load template from XMLFileSystemTemplateProvider
            XMLFileSystemTemplateProvider provider = new XMLFileSystemTemplateProvider(distFolder, "");

            //Aplying options ProvisioningTemplateApplyingInformation
            ProvisioningTemplateApplyingInformation ptai = new ProvisioningTemplateApplyingInformation();
            ptai.HandlersToProcess = OfficeDevPnP.Core.Framework.Provisioning.Model.Handlers.All;
            ptai.OverwriteSystemPropertyBagValues = false;

            var template = provider.GetTemplate("ProvisioningTemplate.Package.xml");
            template.Connector = new FileSystemConnector(distFolder, "");

            //Apply template
            context.Web.ApplyProvisioningTemplate(template, ptai);
        }

        [TestMethod]
        public void TestXIncludeTemplate()
        {
            string distFolder = @"C:\_Community\SPSBcn\Demos\Done\SPSBcn.GulpAndPnp\SPSBcn.GulpAndPnp.Package\dist\src\pnp";            
           
            XMLFileSystemTemplateProvider provider = new XMLFileSystemTemplateProvider(distFolder, "");            

            var template = provider.GetTemplate("ProvisioningTemplate.Package.xml");

            Assert.IsNotNull(template.Files);
            Assert.IsTrue(template.Files.Count >= 10);            
        }

        [TestMethod]
        public void ApplyAllProvisioningTemplates()
        {
            string url = "https://tenant.sharepoint.com/sites/kk";
            string distFolder = @"C:\_Community\SPSBcn\Demos\Done\SPSBcn.GulpAndPnp\SPSBcn.GulpAndPnp.Package\dist\src\pnp";

            List<string> templatesToApply = new List<string>()
            {
                "ProvisioningTemplate.Fields",
                "ProvisioningTemplate.ContentTypes",
                "ProvisioningTemplate.Files",
                "ProvisioningTemplate.Lists",
                "ProvisioningTemplate.CustomActions",
                "ProvisioningTemplate.ComposedLook",
                "ProvisioningTemplate.Pages",
                "ProvisioningTemplate.Properties"
            };

            //get ClientContext
            AuthenticationManager authManager = new AuthenticationManager();
            ClientContext context =
                authManager.GetSharePointOnlineAuthenticatedContextTenant(
                    url, StringExtensions.GetUser(), StringExtensions.GetPassword());

            //Load template from XMLFileSystemTemplateProvider
            XMLFileSystemTemplateProvider provider = new XMLFileSystemTemplateProvider(distFolder, "");

            //Aplying options ProvisioningTemplateApplyingInformation
            ProvisioningTemplateApplyingInformation ptai = new ProvisioningTemplateApplyingInformation();
            ptai.HandlersToProcess = OfficeDevPnP.Core.Framework.Provisioning.Model.Handlers.All;
            ptai.OverwriteSystemPropertyBagValues = false;

            foreach (var templateName in templatesToApply)
            {
                var template = provider.GetTemplate(string.Format("{0}.xml", templateName));
                template.Connector = new FileSystemConnector(distFolder, "");

                //Apply template
                context.Web.ApplyProvisioningTemplate(template, ptai);
            }            
        }

        [TestMethod]
        public void ExportProvisioningTemplate()
        {
            string url = "https://lmanez.sharepoint.com/sites/spsbcn1";

            //get ClientContext
            AuthenticationManager authManager = new AuthenticationManager();
            ClientContext context =
                authManager.GetSharePointOnlineAuthenticatedContextTenant(
                    url, StringExtensions.GetUser(), StringExtensions.GetPassword());

            //Note: Enable PublishingWeb feature first: 
            //  Enable-SPOFeature -Identity 94c94ca6-b32f-4da9-a9e3-1f3d343d7ecb
            ProvisioningTemplateCreationInformation ptci = new ProvisioningTemplateCreationInformation(context.Web);
            ptci.FileConnector = new FileSystemConnector(@"c:\SPSaturday", "");
            ptci.PersistBrandingFiles = true; //Extract Welcome Page content and Themes
            ptci.HandlersToProcess =
                OfficeDevPnP.Core.Framework.Provisioning.Model.Handlers.All;

            var template = context.Web.GetProvisioningTemplate(ptci);

            XMLFileSystemTemplateProvider provider = new XMLFileSystemTemplateProvider(@"c:\SPSaturday", "");
            string templateName = "SPSBcn.xml";
            provider.SaveAs(template, templateName);
        }
    }
}
