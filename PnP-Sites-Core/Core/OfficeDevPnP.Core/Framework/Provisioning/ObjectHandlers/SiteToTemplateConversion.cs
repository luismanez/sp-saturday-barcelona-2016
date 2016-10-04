﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using OfficeDevPnP.Core.Diagnostics;
using OfficeDevPnP.Core.Framework.Provisioning.Extensibility;
using OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers.Extensions;
using System;

namespace OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers
{
    internal class SiteToTemplateConversion
    {
        /// <summary>
        /// Actual implementation of extracting configuration from existing site.
        /// </summary>
        /// <param name="web"></param>
        /// <param name="creationInfo"></param>
        /// <returns></returns>
        internal ProvisioningTemplate GetRemoteTemplate(Web web, ProvisioningTemplateCreationInformation creationInfo)
        {

            using (var scope = new PnPMonitoredScope(CoreResources.Provisioning_ObjectHandlers_Extraction))
            {

                ProvisioningProgressDelegate progressDelegate = null;
                ProvisioningMessagesDelegate messagesDelegate = null;
                if (creationInfo != null)
                {
                    if (creationInfo.BaseTemplate != null)
                    {
                        scope.LogDebug(CoreResources.SiteToTemplateConversion_Base_template_available___0_, creationInfo.BaseTemplate.Id);
                    }
                    progressDelegate = creationInfo.ProgressDelegate;
                    if (creationInfo.ProgressDelegate != null)
                    {
                        scope.LogDebug(CoreResources.SiteToTemplateConversion_ProgressDelegate_registered);
                    }
                    messagesDelegate = creationInfo.MessagesDelegate;
                    if (creationInfo.MessagesDelegate != null)
                    {
                        scope.LogDebug(CoreResources.SiteToTemplateConversion_MessagesDelegate_registered);
                    }
                    if (creationInfo.IncludeAllTermGroups)
                    {
                        scope.LogDebug(CoreResources.SiteToTemplateConversion_IncludeAllTermGroups_is_set_to_true);
                    }
                    if (creationInfo.IncludeSiteCollectionTermGroup)
                    {
                        scope.LogDebug(CoreResources.SiteToTemplateConversion_IncludeSiteCollectionTermGroup_is_set_to_true);
                    }
                    if (creationInfo.PersistBrandingFiles)
                    {
                        scope.LogDebug(CoreResources.SiteToTemplateConversion_PersistBrandingFiles_is_set_to_true);
                    }
                }
                else
                {
                    // When no provisioning info was passed then we want to execute all handlers
                    creationInfo = new ProvisioningTemplateCreationInformation(web);
                    creationInfo.HandlersToProcess = Handlers.All;
                }

                // Create empty object
                ProvisioningTemplate template = new ProvisioningTemplate();

                // Hookup connector, is handy when the generated template object is used to apply to another site
                template.Connector = creationInfo.FileConnector;

                List<ObjectHandlerBase> objectHandlers = new List<ObjectHandlerBase>();

                if (creationInfo.HandlersToProcess.HasFlag(Handlers.RegionalSettings)) objectHandlers.Add(new ObjectRegionalSettings());
                if (creationInfo.HandlersToProcess.HasFlag(Handlers.SupportedUILanguages)) objectHandlers.Add(new ObjectSupportedUILanguages());
                if (creationInfo.HandlersToProcess.HasFlag(Handlers.AuditSettings)) objectHandlers.Add(new ObjectAuditSettings());
                if (creationInfo.HandlersToProcess.HasFlag(Handlers.SitePolicy)) objectHandlers.Add(new ObjectSitePolicy());
                if (creationInfo.HandlersToProcess.HasFlag(Handlers.SiteSecurity)) objectHandlers.Add(new ObjectSiteSecurity());
                if (creationInfo.HandlersToProcess.HasFlag(Handlers.TermGroups)) objectHandlers.Add(new ObjectTermGroups());
                if (creationInfo.HandlersToProcess.HasFlag(Handlers.Fields)) objectHandlers.Add(new ObjectField());
                if (creationInfo.HandlersToProcess.HasFlag(Handlers.ContentTypes)) objectHandlers.Add(new ObjectContentType());
                if (creationInfo.HandlersToProcess.HasFlag(Handlers.Lists)) objectHandlers.Add(new ObjectListInstance());
                if (creationInfo.HandlersToProcess.HasFlag(Handlers.CustomActions)) objectHandlers.Add(new ObjectCustomActions());
                if (creationInfo.HandlersToProcess.HasFlag(Handlers.Features)) objectHandlers.Add(new ObjectFeatures());
                if (creationInfo.HandlersToProcess.HasFlag(Handlers.ComposedLook)) objectHandlers.Add(new ObjectComposedLook());
                if (creationInfo.HandlersToProcess.HasFlag(Handlers.SearchSettings)) objectHandlers.Add(new ObjectSearchSettings());
                if (creationInfo.HandlersToProcess.HasFlag(Handlers.Files)) objectHandlers.Add(new ObjectFiles());
                if (creationInfo.HandlersToProcess.HasFlag(Handlers.Pages)) objectHandlers.Add(new ObjectPages());
                if (creationInfo.HandlersToProcess.HasFlag(Handlers.PageContents)) objectHandlers.Add(new ObjectPageContents());
                if (creationInfo.HandlersToProcess.HasFlag(Handlers.PropertyBagEntries)) objectHandlers.Add(new ObjectPropertyBagEntry());
                if (creationInfo.HandlersToProcess.HasFlag(Handlers.Publishing)) objectHandlers.Add(new ObjectPublishing());
                if (creationInfo.HandlersToProcess.HasFlag(Handlers.Workflows)) objectHandlers.Add(new ObjectWorkflows());
                if (creationInfo.HandlersToProcess.HasFlag(Handlers.WebSettings)) objectHandlers.Add(new ObjectWebSettings());
                if (creationInfo.HandlersToProcess.HasFlag(Handlers.Navigation)) objectHandlers.Add(new ObjectNavigation());
                objectHandlers.Add(new ObjectLocalization()); // Always add this one, check is done in the handler
                if (creationInfo.HandlersToProcess.HasFlag(Handlers.ExtensibilityProviders)) objectHandlers.Add(new ObjectExtensibilityHandlers());
                
                objectHandlers.Add(new ObjectRetrieveTemplateInfo());

                int step = 1;

                var count = objectHandlers.Count(o => o.ReportProgress && o.WillExtract(web, template, creationInfo));

                web.EnsureProperty(w => w.Url);

                foreach (var handler in objectHandlers)
                {
                    if (handler.WillExtract(web, template, creationInfo))
                    {
                        if (messagesDelegate != null)
                        {
                            handler.MessagesDelegate = messagesDelegate;
                        }
                        if (handler.ReportProgress && progressDelegate != null)
                        {
                            progressDelegate(handler.Name, step, count);
                            step++;
                        }

                        using (var handlerContext = web.Context.Clone(web.Url))
                        {
                            template = handler.ExtractObjects(handlerContext.Web, template, creationInfo);
                        }
                    }
                }

                //if (creationInfo.PersistMultiLanguageResources)
                //{
                //    template = UserResourceExtensions.SaveResourceValues(template, creationInfo);
                //}

                return template;
            }
        }

        /// <summary>
        /// Actual implementation of the apply templates
        /// </summary>
        /// <param name="web"></param>
        /// <param name="template"></param>
        /// <param name="provisioningInfo"></param>
        internal void ApplyRemoteTemplate(Web web, ProvisioningTemplate template, ProvisioningTemplateApplyingInformation provisioningInfo)
        {
            using (var scope = new PnPMonitoredScope(CoreResources.Provisioning_ObjectHandlers_Provisioning))
            {
                ProvisioningProgressDelegate progressDelegate = null;
                ProvisioningMessagesDelegate messagesDelegate = null;
                if (provisioningInfo != null)
                {
                    if (provisioningInfo.OverwriteSystemPropertyBagValues == true)
                    {
                        scope.LogInfo(CoreResources.SiteToTemplateConversion_ApplyRemoteTemplate_OverwriteSystemPropertyBagValues_is_to_true);
                    }
                    progressDelegate = provisioningInfo.ProgressDelegate;
                    if (provisioningInfo.ProgressDelegate != null)
                    {
                        scope.LogInfo(CoreResources.SiteToTemplateConversion_ProgressDelegate_registered);
                    }
                    messagesDelegate = provisioningInfo.MessagesDelegate;
                    if (provisioningInfo.MessagesDelegate != null)
                    {
                        scope.LogInfo(CoreResources.SiteToTemplateConversion_MessagesDelegate_registered);
                    }
                }
                else
                {
                    // When no provisioning info was passed then we want to execute all handlers
                    provisioningInfo = new ProvisioningTemplateApplyingInformation();
                    provisioningInfo.HandlersToProcess = Handlers.All;
                }

                // Check if the target site shares the same base template with the template's source site
                var targetSiteTemplateId = web.GetBaseTemplateId();
                if (!String.IsNullOrEmpty(targetSiteTemplateId) && !String.IsNullOrEmpty(template.BaseSiteTemplate))
                {
                    if (!targetSiteTemplateId.Equals(template.BaseSiteTemplate, StringComparison.InvariantCultureIgnoreCase))
                    {
                        var templatesNotMatchingWarning = String.Format(CoreResources.Provisioning_Asymmetric_Base_Templates, template.BaseSiteTemplate, targetSiteTemplateId);
                        scope.LogWarning(templatesNotMatchingWarning);
                        if (provisioningInfo.MessagesDelegate!= null)
                        {
                            provisioningInfo.MessagesDelegate(templatesNotMatchingWarning, ProvisioningMessageType.Warning);
                        }
                    }
                }

                // Always ensure the Url property is loaded. In the tokens we need this and we don't want to call ExecuteQuery as this can 
                // impact delta scenarions (calling ExecuteQuery before the planned update is called)
                web.EnsureProperty(w => w.Url);

                List<ObjectHandlerBase> objectHandlers = new List<ObjectHandlerBase>();

                if (provisioningInfo.HandlersToProcess.HasFlag(Handlers.RegionalSettings)) objectHandlers.Add(new ObjectRegionalSettings());
                if (provisioningInfo.HandlersToProcess.HasFlag(Handlers.SupportedUILanguages)) objectHandlers.Add(new ObjectSupportedUILanguages());
                if (provisioningInfo.HandlersToProcess.HasFlag(Handlers.AuditSettings)) objectHandlers.Add(new ObjectAuditSettings());
                if (provisioningInfo.HandlersToProcess.HasFlag(Handlers.SitePolicy)) objectHandlers.Add(new ObjectSitePolicy());
                if (provisioningInfo.HandlersToProcess.HasFlag(Handlers.SiteSecurity)) objectHandlers.Add(new ObjectSiteSecurity());
                if (provisioningInfo.HandlersToProcess.HasFlag(Handlers.Features)) objectHandlers.Add(new ObjectFeatures());
                if (provisioningInfo.HandlersToProcess.HasFlag(Handlers.TermGroups)) objectHandlers.Add(new ObjectTermGroups());
                if (provisioningInfo.HandlersToProcess.HasFlag(Handlers.Fields) || provisioningInfo.HandlersToProcess.HasFlag(Handlers.Lists)) objectHandlers.Add(new ObjectField());
                if (provisioningInfo.HandlersToProcess.HasFlag(Handlers.ContentTypes)) objectHandlers.Add(new ObjectContentType());
                if (provisioningInfo.HandlersToProcess.HasFlag(Handlers.Lists)) objectHandlers.Add(new ObjectListInstance());
                if (provisioningInfo.HandlersToProcess.HasFlag(Handlers.Fields) || provisioningInfo.HandlersToProcess.HasFlag(Handlers.Lists)) objectHandlers.Add(new ObjectLookupFields());
                if (provisioningInfo.HandlersToProcess.HasFlag(Handlers.Fields) || provisioningInfo.HandlersToProcess.HasFlag(Handlers.Lists)) objectHandlers.Add(new ObjectListInstanceDataRows());
                if (provisioningInfo.HandlersToProcess.HasFlag(Handlers.Workflows)) objectHandlers.Add(new ObjectWorkflows());
                if (provisioningInfo.HandlersToProcess.HasFlag(Handlers.Files)) objectHandlers.Add(new ObjectFiles());
                if (provisioningInfo.HandlersToProcess.HasFlag(Handlers.Pages)) objectHandlers.Add(new ObjectPages());
                if (provisioningInfo.HandlersToProcess.HasFlag(Handlers.PageContents)) objectHandlers.Add(new ObjectPageContents());
                if (provisioningInfo.HandlersToProcess.HasFlag(Handlers.CustomActions)) objectHandlers.Add(new ObjectCustomActions());
                if (provisioningInfo.HandlersToProcess.HasFlag(Handlers.Publishing)) objectHandlers.Add(new ObjectPublishing());
                if (provisioningInfo.HandlersToProcess.HasFlag(Handlers.ComposedLook)) objectHandlers.Add(new ObjectComposedLook());
                if (provisioningInfo.HandlersToProcess.HasFlag(Handlers.SearchSettings)) objectHandlers.Add(new ObjectSearchSettings());
                if (provisioningInfo.HandlersToProcess.HasFlag(Handlers.PropertyBagEntries)) objectHandlers.Add(new ObjectPropertyBagEntry());
                if (provisioningInfo.HandlersToProcess.HasFlag(Handlers.WebSettings)) objectHandlers.Add(new ObjectWebSettings());
                if (provisioningInfo.HandlersToProcess.HasFlag(Handlers.Navigation)) objectHandlers.Add(new ObjectNavigation());
                objectHandlers.Add(new ObjectLocalization()); // Always add this one, check is done in the handler
                if (provisioningInfo.HandlersToProcess.HasFlag(Handlers.ExtensibilityProviders)) objectHandlers.Add(new ObjectExtensibilityHandlers());

                // Only persist template information in case this flag is set: this will allow the engine to 
                // work with lesser permissions
                if (provisioningInfo.PersistTemplateInfo)
                {
                    objectHandlers.Add(new ObjectPersistTemplateInfo());
                }

                var tokenParser = new TokenParser(web, template);
                if (provisioningInfo.HandlersToProcess.HasFlag(Handlers.ExtensibilityProviders))
                {
                    var extensibilityHandler = objectHandlers.OfType<ObjectExtensibilityHandlers>().First();
                    extensibilityHandler.AddExtendedTokens(web, template, tokenParser, provisioningInfo);
                }

                int step = 1;

                var count = objectHandlers.Count(o => o.ReportProgress && o.WillProvision(web, template));

                foreach (var handler in objectHandlers)
                {
                    if (handler.WillProvision(web, template))
                    {
                        if (messagesDelegate != null)
                        {
                            handler.MessagesDelegate = messagesDelegate;
                        }
                        if (handler.ReportProgress && progressDelegate != null)
                        {
                            progressDelegate(handler.Name, step, count);
                            step++;
                        }
                        tokenParser = handler.ProvisionObjects(web, template, tokenParser, provisioningInfo);
                    }
                }
            }
        }
    }
}
