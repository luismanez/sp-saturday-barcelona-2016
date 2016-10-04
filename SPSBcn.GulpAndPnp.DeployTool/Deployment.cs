using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core;
using OfficeDevPnP.Core.Framework.Provisioning.Connectors;
using OfficeDevPnP.Core.Framework.Provisioning.Providers.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPSBcn.GulpAndPnp.DeployTool
{
    public static class Deployment
    {
        public static void ApplyProvisioningTemplate(string url, string user, string password, string templateFile)
        {
            Console.WriteLine("ApplyProvisioningTemplate. {0} - {1} - {2} - {3}", 
                url, user, password, templateFile);

            string distFolder = Path.GetDirectoryName(templateFile);
            string templateName = Path.GetFileName(templateFile);

            Console.WriteLine("Provisioning Template from folder: {0}", distFolder);
            Console.WriteLine("Provisioning Template name: {0}", templateName);

            //get ClientContext
            AuthenticationManager authManager = new AuthenticationManager();
            ClientContext context =
                authManager.GetSharePointOnlineAuthenticatedContextTenant(url, user, password);

            //Load template from XMLFileSystemTemplateProvider
            XMLFileSystemTemplateProvider provider = new XMLFileSystemTemplateProvider(distFolder, "");

            var template = provider.GetTemplate(templateName);
            template.Connector = new FileSystemConnector(distFolder, "");

            //Apply template
            context.Web.ApplyProvisioningTemplate(template);
        }
    }
}
