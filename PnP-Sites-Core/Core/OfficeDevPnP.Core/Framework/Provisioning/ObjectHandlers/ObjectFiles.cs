﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Entities;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using File = Microsoft.SharePoint.Client.File;
using OfficeDevPnP.Core.Diagnostics;
using OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers.Extensions;
using System;
using System.Text.RegularExpressions;
using Microsoft.SharePoint.Client.WebParts;
using System.Xml.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.IO;
using Newtonsoft.Json;
using OfficeDevPnP.Core.Utilities;
using Microsoft.SharePoint.Client.Taxonomy;

namespace OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers
{
    internal class ObjectFiles : ObjectHandlerBase
    {
        private readonly string[] WriteableReadOnlyFields = new string[] { "publishingpagelayout", "contenttypeid" };

        public override string Name
        {
            get { return "Files"; }
        }

        public override TokenParser ProvisionObjects(Web web, ProvisioningTemplate template, TokenParser parser, ProvisioningTemplateApplyingInformation applyingInformation)
        {
            using (var scope = new PnPMonitoredScope(this.Name))
            {
                var context = web.Context as ClientContext;

                web.EnsureProperties(w => w.ServerRelativeUrl, w => w.Url);

                // Build on the fly the list of additional files coming from the Directories
                var directoryFiles = new List<Model.File>();
                foreach (var directory in template.Directories)
                {
                    var metadataProperties = directory.GetMetadataProperties();
                    directoryFiles.AddRange(directory.GetDirectoryFiles(metadataProperties));
                }

                foreach (var file in template.Files.Union(directoryFiles))
                {
                    var folderName = parser.ParseString(file.Folder);

                    if (folderName.ToLower().StartsWith((web.ServerRelativeUrl.ToLower())))
                    {
                        folderName = folderName.Substring(web.ServerRelativeUrl.Length);
                    }

                    var folder = web.EnsureFolderPath(folderName);

                    File targetFile = null;

                    var checkedOut = false;

                    targetFile = folder.GetFile(template.Connector.GetFilenamePart(file.Src));

                    if (targetFile != null)
                    {
                        if (file.Overwrite)
                        {
                            scope.LogDebug(CoreResources.Provisioning_ObjectHandlers_Files_Uploading_and_overwriting_existing_file__0_, file.Src);
                            checkedOut = CheckOutIfNeeded(web, targetFile);

                            using (var stream = GetFileStream(template, file))
                            {
                                targetFile = UploadFile(template, file, folder, stream);
                            }
                        }
                        else
                        {
                            checkedOut = CheckOutIfNeeded(web, targetFile);
                        }
                    }
                    else
                    {
                        using (var stream = GetFileStream(template, file))
                        {
                            scope.LogDebug(CoreResources.Provisioning_ObjectHandlers_Files_Uploading_file__0_, file.Src);
                            targetFile = UploadFile(template, file, folder, stream);
                        }

                        checkedOut = CheckOutIfNeeded(web, targetFile);
                    }

                    if (targetFile != null)
                    {
                        if (file.Properties != null && file.Properties.Any())
                        {
                            Dictionary<string, string> transformedProperties = file.Properties.ToDictionary(property => property.Key, property => parser.ParseString(property.Value));
                            SetFileProperties(targetFile, transformedProperties, false);
                        }

                        if (file.WebParts != null && file.WebParts.Any())
                        {
                            targetFile.EnsureProperties(f => f.ServerRelativeUrl);

                            var existingWebParts = web.GetWebParts(targetFile.ServerRelativeUrl);
                            foreach (var webpart in file.WebParts)
                            {
                                // check if the webpart is already set on the page
                                if (existingWebParts.FirstOrDefault(w => w.WebPart.Title == webpart.Title) == null)
                                {
                                    scope.LogDebug(CoreResources.Provisioning_ObjectHandlers_Files_Adding_webpart___0___to_page, webpart.Title);
                                    var wpEntity = new WebPartEntity();
                                    wpEntity.WebPartTitle = webpart.Title;
                                    wpEntity.WebPartXml = parser.ParseString(webpart.Contents).Trim(new[] { '\n', ' ' });
                                    wpEntity.WebPartZone = webpart.Zone;
                                    wpEntity.WebPartIndex = (int)webpart.Order;
                                    web.AddWebPartToWebPartPage(targetFile.ServerRelativeUrl, wpEntity);
                                }
                            }
                        }

                        if (checkedOut)
                        {
                            targetFile.CheckIn("", CheckinType.MajorCheckIn);
                            web.Context.ExecuteQueryRetry();
                        }

                        // Don't set security when nothing is defined. This otherwise breaks on files set outside of a list
                        if (file.Security != null &&
                            (file.Security.ClearSubscopes == true || file.Security.CopyRoleAssignments == true || file.Security.RoleAssignments.Count > 0))
                        {
                            targetFile.ListItemAllFields.SetSecurity(parser, file.Security);
                        }
                    }

                }
            }
            return parser;
        }

        private static bool CheckOutIfNeeded(Web web, File targetFile)
        {
            var checkedOut = false;
            try
            {
                web.Context.Load(targetFile, f => f.CheckOutType, f => f.ListItemAllFields.ParentList.ForceCheckout);
                web.Context.ExecuteQueryRetry();
                if (targetFile.ListItemAllFields.ServerObjectIsNull.HasValue
                    && !targetFile.ListItemAllFields.ServerObjectIsNull.Value
                    && targetFile.ListItemAllFields.ParentList.ForceCheckout)
                {
                    if (targetFile.CheckOutType == CheckOutType.None)
                    {
                        targetFile.CheckOut();
                    }
                    checkedOut = true;
                }
            }
            catch (ServerException ex)
            {
                // Handling the exception stating the "The object specified does not belong to a list."
                if (ex.ServerErrorCode != -2146232832)
                {
                    throw;
                }
            }
            return checkedOut;
        }


        public override ProvisioningTemplate ExtractObjects(Web web, ProvisioningTemplate template, ProvisioningTemplateCreationInformation creationInfo)
        {

            return template;
        }


        public void SetFileProperties(File file, IDictionary<string, string> properties, bool checkoutIfRequired = true)
        {
            var context = file.Context;
            if (properties != null && properties.Count > 0)
            {
                // Get a reference to the target list, if any
                // and load file item properties
                var parentList = file.ListItemAllFields.ParentList;
                context.Load(parentList);
                context.Load(file.ListItemAllFields);
                try
                {
                    context.ExecuteQueryRetry();
                }
                catch (ServerException ex)
                {
                    // If this throws ServerException (does not belong to list), then shouldn't be trying to set properties)
                    if (ex.Message != "The object specified does not belong to a list.")
                    {
                        throw;
                    }
                }

                // Loop through and detect changes first, then, check out if required and apply
                foreach (var kvp in properties)
                {
                    var propertyName = kvp.Key;
                    var propertyValue = kvp.Value;
                    
                    var targetField = parentList.Fields.GetByInternalNameOrTitle(propertyName);
                    targetField.EnsureProperties(f => f.TypeAsString, f => f.ReadOnlyField);

                    // Changed by PaoloPia because there are fields like PublishingPageLayout
                    // which are marked as read-only, but have to be overwritten while uploading
                    // a publishing page file and which in reality can still be written
                    if (!targetField.ReadOnlyField || WriteableReadOnlyFields.Contains(propertyName.ToLower())) 
                    {
                        switch (propertyName.ToUpperInvariant())
                        {
                            case "CONTENTTYPE":
                                {
                                    Microsoft.SharePoint.Client.ContentType targetCT = parentList.GetContentTypeByName(propertyValue);
                                    context.ExecuteQueryRetry();

                                    if (targetCT != null)
                                    {
                                        file.ListItemAllFields["ContentTypeId"] = targetCT.StringId;
                                    }
                                    else
                                    {
                                        Log.Error(Constants.LOGGING_SOURCE, "Content Type {0} does not exist in target list!", propertyValue);
                                    }
                                    break;
                                }
                            default:
                                {
                                    switch (targetField.TypeAsString)
                                    {
                                        case "User":
                                            var user = parentList.ParentWeb.EnsureUser(propertyValue);
                                            context.Load(user);
                                            context.ExecuteQueryRetry();

                                            if (user != null)
                                            {
                                                var userValue = new FieldUserValue
                                                {
                                                    LookupId = user.Id,
                                                };
                                                file.ListItemAllFields[propertyName] = userValue;
                                            }
                                            break;
                                        case "URL":
                                            var urlArray = propertyValue.Split(',');
                                            var linkValue = new FieldUrlValue();
                                            if (urlArray.Length == 2)
                                            {
                                                linkValue.Url = urlArray[0];
                                                linkValue.Description = urlArray[1];
                                            }
                                            else
                                            {
                                                linkValue.Url = urlArray[0];
                                                linkValue.Description = urlArray[0];
                                            }
                                            file.ListItemAllFields[propertyName] = linkValue;
                                            break;
                                        case "MultiChoice":
                                            var multiChoice = JsonUtility.Deserialize<String[]>(propertyValue);
                                            file.ListItemAllFields[propertyName] = multiChoice;
                                            break;
                                        case "LookupMulti":
                                            var lookupMultiValue = JsonUtility.Deserialize<FieldLookupValue[]>(propertyValue);
                                            file.ListItemAllFields[propertyName] = lookupMultiValue;
                                            break;
                                        case "TaxonomyFieldType":
                                            var taxonomyValue = JsonUtility.Deserialize<TaxonomyFieldValue>(propertyValue);
                                            file.ListItemAllFields[propertyName] = taxonomyValue;
                                            break;
                                        case "TaxonomyFieldTypeMulti":
                                            var taxonomyValueArray = JsonUtility.Deserialize<TaxonomyFieldValue[]>(propertyValue);
                                            file.ListItemAllFields[propertyName] = taxonomyValueArray;
                                            break;
                                        default:
                                            file.ListItemAllFields[propertyName] = propertyValue;
                                            break;
                                    }
                                    break;
                                }
                        }
                    }
                    file.ListItemAllFields.Update();
                    context.ExecuteQueryRetry();
                }
            }
        }

        private string Tokenize(Web web, string xml)
        {
            var lists = web.Lists;
            web.Context.Load(web, w => w.ServerRelativeUrl, w => w.Id);
            web.Context.Load(lists, ls => ls.Include(l => l.Id, l => l.Title));
            web.Context.ExecuteQueryRetry();

            foreach (var list in lists)
            {
                xml = Regex.Replace(xml, list.Id.ToString(), string.Format("{{listid:{0}}}", list.Title), RegexOptions.IgnoreCase);
            }
            xml = Regex.Replace(xml, web.Id.ToString(), "{siteid}", RegexOptions.IgnoreCase);
            if (web.ServerRelativeUrl != "/")
            {
                xml = Regex.Replace(xml, web.ServerRelativeUrl, "{site}", RegexOptions.IgnoreCase);
            }

            return xml;
        }

        private ProvisioningTemplate CleanupEntities(ProvisioningTemplate template, ProvisioningTemplate baseTemplate)
        {

            return template;
        }

        public override bool WillProvision(Web web, ProvisioningTemplate template)
        {
            if (!_willProvision.HasValue)
            {
                _willProvision = template.Files.Any() | template.Directories.Any();
            }
            return _willProvision.Value;
        }

        public override bool WillExtract(Web web, ProvisioningTemplate template, ProvisioningTemplateCreationInformation creationInfo)
        {
            if (!_willExtract.HasValue)
            {
                _willExtract = false;
            }
            return _willExtract.Value;
        }

        private static File UploadFile(ProvisioningTemplate template, Model.File file, Microsoft.SharePoint.Client.Folder folder, Stream stream)
        {
            File targetFile = null;
            var fileName = template.Connector.GetFilenamePart(file.Src);
            try
            {
                targetFile = folder.UploadFile(fileName, stream, file.Overwrite);
            }
            catch (Exception)
            {
                //The file name might contain encoded characters that prevent upload. Decode it and try again.
                fileName = WebUtility.UrlDecode(fileName);
                targetFile = folder.UploadFile(fileName, stream, file.Overwrite);
            }
            return targetFile;
        }

        /// <summary>
        /// Retrieves <see cref="Stream"/> from connector. If the file name contains special characters (e.g. "%20") and cannot be retrieved, a workaround will be performed
        /// </summary>
        private static Stream GetFileStream(ProvisioningTemplate template, Model.File file)
        {
            var fileName = file.Src;
            var container = String.Empty;
            if (fileName.Contains(@"\") || fileName.Contains(@"/"))
            {
                var tempFileName = fileName.Replace(@"/", @"\");
                container = fileName.Substring(0, tempFileName.LastIndexOf(@"\"));
                fileName = fileName.Substring(tempFileName.LastIndexOf(@"\") + 1);
            }
            
            // add the default provided container (if any)
            if (!String.IsNullOrEmpty(container))
            {
                if (!String.IsNullOrEmpty(template.Connector.GetContainer()))
                {
                    container = String.Format(@"{0}\{1}", template.Connector.GetContainer(), container);
                }               
            }
            else
            {
                container = template.Connector.GetContainer();
            }

            var stream = template.Connector.GetFileStream(fileName, container);
            if (stream == null)
            {
                //Decode the URL and try again
                fileName = WebUtility.UrlDecode(fileName);
                stream = template.Connector.GetFileStream(fileName, container);
            }

            return stream;
        }
    }

    internal static class DirectoryExtensions
    {
        internal static Dictionary<String, Dictionary<String, String>> GetMetadataProperties(this Model.Directory directory)
        {
            Dictionary<String, Dictionary<String, String>> result = null;

            if (!string.IsNullOrEmpty(directory.MetadataMappingFile))
            {
                var metadataPropertiesStream = directory.ParentTemplate.Connector.GetFileStream(directory.MetadataMappingFile);
                if (metadataPropertiesStream != null)
                {
                    using (var sr = new StreamReader(metadataPropertiesStream))
                    {
                        var metadataPropertiesString = sr.ReadToEnd();
                        result = JsonConvert.DeserializeObject<Dictionary<String, Dictionary<String, String>>>(metadataPropertiesString);
                    }
                }
            }

            return (result);
        }

        internal static List<Model.File> GetDirectoryFiles(this Model.Directory directory, 
            Dictionary<String, Dictionary<String, String>> metadataProperties = null)
        {
            var result = new List<Model.File>();

            var files = directory.ParentTemplate.Connector.GetFiles(directory.Src);

            if (!String.IsNullOrEmpty(directory.IncludedExtensions) && directory.IncludedExtensions != "*.*")
            {
                var includedExtensions = directory.IncludedExtensions.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                files = files.Where(f => includedExtensions.Contains($"*{Path.GetExtension(f).ToLower()}")).ToList();
            }

            if (!String.IsNullOrEmpty(directory.ExcludedExtensions))
            {
                var excludedExtensions = directory.ExcludedExtensions.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                files = files.Where(f => !excludedExtensions.Contains($"*{Path.GetExtension(f).ToLower()}")).ToList();
            }

            result.AddRange(from file in files
                            select new Model.File(
                                directory.Src + @"\" + file,
                                directory.Folder,
                                directory.Overwrite,
                                null, // No WebPartPages are supported with this technique
                                metadataProperties != null ? metadataProperties[directory.Src + @"\" + file] : null,
                                directory.Security,
                                directory.Level
                                ));

            if (directory.Recursive)
            {
                var subFolders = directory.ParentTemplate.Connector.GetFolders(directory.Src);
                foreach (var folder in subFolders)
                {
                    directory.Src += @"\" + folder;
                    directory.Folder += @"\" + folder;
                    result.AddRange(directory.GetDirectoryFiles(metadataProperties));
                }
            }

            return (result);
        }
    }
}
