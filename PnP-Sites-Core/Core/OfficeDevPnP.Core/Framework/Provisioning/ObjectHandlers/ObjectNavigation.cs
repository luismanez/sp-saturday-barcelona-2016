﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using OfficeDevPnP.Core.Diagnostics;
using Microsoft.SharePoint.Client.Publishing.Navigation;
using Microsoft.SharePoint.Client.Taxonomy;

namespace OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers
{
    internal class ObjectNavigation : ObjectHandlerBase
    {
        const string NavigationShowSiblings = "__NavigationShowSiblings";

        public override string Name
        {
            get { return "Navigation"; }
        }

        public override ProvisioningTemplate ExtractObjects(Web web, ProvisioningTemplate template, ProvisioningTemplateCreationInformation creationInfo)
        {
            using (var scope = new PnPMonitoredScope(this.Name))
            {
                GlobalNavigationType globalNavigationType;
                CurrentNavigationType currentNavigationType;

                // The Navigation handler works only for sites with Publishing Features enabled
                if (!web.IsPublishingWeb())
                {
                    scope.LogDebug(CoreResources.Provisioning_ObjectHandlers_Navigation_Context_web_is_not_publishing);
                    return template;
                }

                // Retrieve the current web navigation settings
                var navigationSettings = new WebNavigationSettings(web.Context, web);
                web.Context.Load(navigationSettings, ns => ns.CurrentNavigation, ns => ns.GlobalNavigation);
                web.Context.ExecuteQueryRetry();

                switch (navigationSettings.GlobalNavigation.Source)
                {
                    case StandardNavigationSource.InheritFromParentWeb:
                        // Global Navigation is Inherited
                        globalNavigationType = GlobalNavigationType.Inherit;
                        break;
                    case StandardNavigationSource.TaxonomyProvider:
                        // Global Navigation is Managed
                        globalNavigationType = GlobalNavigationType.Managed;
                        break;
                    case StandardNavigationSource.PortalProvider:
                    default:
                        // Global Navigation is Structural
                        globalNavigationType = GlobalNavigationType.Structural;
                        break;
                }

                switch (navigationSettings.CurrentNavigation.Source)
                {
                    case StandardNavigationSource.InheritFromParentWeb:
                        // Current Navigation is Inherited
                        currentNavigationType = CurrentNavigationType.Inherit;
                        break;
                    case StandardNavigationSource.TaxonomyProvider:
                        // Current Navigation is Managed
                        currentNavigationType = CurrentNavigationType.Managed;
                        break;
                    case StandardNavigationSource.PortalProvider:
                    default:
                        // Current Navigation is Structural
                        if (AreSiblingsEnabledForCurrentStructuralNavigation(web))
                        {
                            currentNavigationType = CurrentNavigationType.Structural;
                        }
                        else
                        {
                            currentNavigationType = CurrentNavigationType.StructuralLocal;
                        }
                        break;
                }

                template.Navigation = new Model.Navigation(
                    new GlobalNavigation(globalNavigationType,
                        globalNavigationType == GlobalNavigationType.Structural ? GetGlobalStructuralNavigation(web, navigationSettings) : null,
                        globalNavigationType == GlobalNavigationType.Managed ? GetGlobalManagedNavigation(web, navigationSettings) : null),
                    new CurrentNavigation(currentNavigationType,
                        currentNavigationType == CurrentNavigationType.Structural | currentNavigationType == CurrentNavigationType.StructuralLocal ? GetCurrentStructuralNavigation(web, navigationSettings) : null,
                        currentNavigationType == CurrentNavigationType.Managed ? GetCurrentManagedNavigation(web, navigationSettings) : null)
                    );
            }

            return template;
        }

        public override TokenParser ProvisionObjects(Web web, ProvisioningTemplate template, TokenParser parser, ProvisioningTemplateApplyingInformation applyingInformation)
        {
            using (var scope = new PnPMonitoredScope(this.Name))
            {
                if (template.Navigation != null)
                {
                    // The Navigation handler works only for sites with Publishing Features enabled
                    if (!web.IsPublishingWeb())
                    {
                        scope.LogDebug(CoreResources.Provisioning_ObjectHandlers_Navigation_Context_web_is_not_publishing);
                        return parser;
                    }

                    // Retrieve the current web navigation settings
                    var navigationSettings = new WebNavigationSettings(web.Context, web);
                    web.Context.Load(navigationSettings, ns => ns.CurrentNavigation, ns => ns.GlobalNavigation);
                    web.Context.ExecuteQueryRetry();

                    if (template.Navigation.GlobalNavigation != null)
                    {
                        switch (template.Navigation.GlobalNavigation.NavigationType)
                        {
                            case GlobalNavigationType.Inherit:
                                navigationSettings.GlobalNavigation.Source = StandardNavigationSource.InheritFromParentWeb;
                                break;
                            case GlobalNavigationType.Managed:
                                if (template.Navigation.GlobalNavigation.ManagedNavigation == null)
                                {
                                    throw new ApplicationException(CoreResources.Provisioning_ObjectHandlers_Navigation_missing_global_managed_navigation);
                                }
                                navigationSettings.GlobalNavigation.Source = StandardNavigationSource.TaxonomyProvider;
                                navigationSettings.GlobalNavigation.TermStoreId = Guid.Parse(parser.ParseString(template.Navigation.GlobalNavigation.ManagedNavigation.TermStoreId));
                                navigationSettings.GlobalNavigation.TermSetId = Guid.Parse(parser.ParseString(template.Navigation.GlobalNavigation.ManagedNavigation.TermSetId));
                                break;
                            case GlobalNavigationType.Structural:
                            default:
                                if (template.Navigation.GlobalNavigation.StructuralNavigation == null)
                                {
                                    throw new ApplicationException(CoreResources.Provisioning_ObjectHandlers_Navigation_missing_global_structural_navigation);
                                }
                                ProvisionGlobalStructuralNavigation(web,
                                    template.Navigation.GlobalNavigation.StructuralNavigation, parser);
                                break;
                        }
                        web.Context.ExecuteQueryRetry();
                    }

                    if (template.Navigation.CurrentNavigation != null)
                    {
                        switch (template.Navigation.CurrentNavigation.NavigationType)
                        {
                            case CurrentNavigationType.Inherit:
                                navigationSettings.CurrentNavigation.Source = StandardNavigationSource.InheritFromParentWeb;
                                break;
                            case CurrentNavigationType.Managed:
                                if (template.Navigation.CurrentNavigation.ManagedNavigation == null)
                                {
                                    throw new ApplicationException(CoreResources.Provisioning_ObjectHandlers_Navigation_missing_current_managed_navigation);
                                }
                                navigationSettings.CurrentNavigation.Source = StandardNavigationSource.TaxonomyProvider;
                                navigationSettings.CurrentNavigation.TermStoreId = Guid.Parse(parser.ParseString(template.Navigation.CurrentNavigation.ManagedNavigation.TermStoreId));
                                navigationSettings.CurrentNavigation.TermSetId = Guid.Parse(parser.ParseString(template.Navigation.CurrentNavigation.ManagedNavigation.TermSetId));
                                break;
                            case CurrentNavigationType.StructuralLocal:
                                web.SetPropertyBagValue(NavigationShowSiblings, "false");
                                if (template.Navigation.CurrentNavigation.StructuralNavigation == null)
                                {
                                    throw new ApplicationException(CoreResources.Provisioning_ObjectHandlers_Navigation_missing_current_structural_navigation);
                                }
                                ProvisionCurrentStructuralNavigation(web,
                                    template.Navigation.CurrentNavigation.StructuralNavigation, parser);
                                break;
                            case CurrentNavigationType.Structural:
                            default:
                                if (template.Navigation.CurrentNavigation.StructuralNavigation == null)
                                {
                                    throw new ApplicationException(CoreResources.Provisioning_ObjectHandlers_Navigation_missing_current_structural_navigation);
                                }
                                ProvisionCurrentStructuralNavigation(web,
                                    template.Navigation.CurrentNavigation.StructuralNavigation, parser);
                                break;
                        }
                        web.Context.ExecuteQueryRetry();
                    }
                }
            }

            return parser;
        }

        #region Utility methods

        private Boolean AreSiblingsEnabledForCurrentStructuralNavigation(Web web)
        {
            bool siblingsEnabled = false;

            if (bool.TryParse(web.GetPropertyBagValueString(NavigationShowSiblings, "false"), out siblingsEnabled))
            {
            }

            return siblingsEnabled;
        }

        private void ProvisionGlobalStructuralNavigation(Web web, StructuralNavigation structuralNavigation, TokenParser parser)
        {
            ProvisionStructuralNavigation(web, structuralNavigation, parser, false);
        }

        private void ProvisionCurrentStructuralNavigation(Web web, StructuralNavigation structuralNavigation, TokenParser parser)
        {
            ProvisionStructuralNavigation(web, structuralNavigation, parser, true);
        }

        private void ProvisionStructuralNavigation(Web web, StructuralNavigation structuralNavigation, TokenParser parser, bool currentNavigation)
        {
            // Determine the target structural navigation
            var navigationType = currentNavigation ?
                Enums.NavigationType.QuickLaunch :
                Enums.NavigationType.TopNavigationBar;

            // Remove existing nodes, if requested
            if (structuralNavigation.RemoveExistingNodes)
            {
                web.DeleteAllNavigationNodes(navigationType);
            }

            // Provision root level nodes, and children recursively
            ProvisionStructuralNavigationNodes(
                web,
                parser,
                navigationType, 
                structuralNavigation.NavigationNodes
                );
        }

        private void ProvisionStructuralNavigationNodes(Web web, TokenParser parser, Enums.NavigationType navigationType, Model.NavigationNodeCollection nodes, string parentNodeTitle = null)
        {
            foreach (var node in nodes)
            {
                web.AddNavigationNode(
                    parser.ParseString(node.Title),
                    new Uri(parser.ParseString(node.Url), UriKind.RelativeOrAbsolute),
                    parser.ParseString(parentNodeTitle),
                    navigationType,
                    node.IsExternal);

                ProvisionStructuralNavigationNodes(
                    web, 
                    parser,
                    navigationType, 
                    node.NavigationNodes, 
                    parser.ParseString(node.Title));
            }
        }

        private ManagedNavigation GetGlobalManagedNavigation(Web web, WebNavigationSettings navigationSettings)
        {
            return GetManagedNavigation(web, navigationSettings, false);
        }

        private StructuralNavigation GetGlobalStructuralNavigation(Web web, WebNavigationSettings navigationSettings)
        {
            return GetStructuralNavigation(web, navigationSettings, false);
        }

        private ManagedNavigation GetCurrentManagedNavigation(Web web, WebNavigationSettings navigationSettings)
        {
            return GetManagedNavigation(web, navigationSettings, true);
        }

        private StructuralNavigation GetCurrentStructuralNavigation(Web web, WebNavigationSettings navigationSettings)
        {
            return GetStructuralNavigation(web, navigationSettings, true);
        }

        private ManagedNavigation GetManagedNavigation(Web web, WebNavigationSettings navigationSettings, Boolean currentNavigation)
        {
            var result = new ManagedNavigation
            {
                TermStoreId = currentNavigation ? navigationSettings.CurrentNavigation.TermStoreId.ToString() : navigationSettings.GlobalNavigation.TermStoreId.ToString(),
                TermSetId = currentNavigation ? navigationSettings.CurrentNavigation.TermSetId.ToString() : navigationSettings.GlobalNavigation.TermSetId.ToString(),
            };

            // Apply any token replacement for taxonomy IDs
            TokenizeManagedNavigationTaxonomyIds(web, result);

            return (result);
        }

        private StructuralNavigation GetStructuralNavigation(Web web, WebNavigationSettings navigationSettings, Boolean currentNavigation)
        {
            // By default avoid removing existing nodes
            var result = new StructuralNavigation { RemoveExistingNodes = false };
            Microsoft.SharePoint.Client.NavigationNodeCollection sourceNodes = currentNavigation ?
                web.Navigation.QuickLaunch : web.Navigation.TopNavigationBar;

            web.Context.Load(web, w => w.ServerRelativeUrl);
            web.Context.Load(sourceNodes);
            web.Context.ExecuteQueryRetry();

            result.NavigationNodes.AddRange(from n in sourceNodes.AsEnumerable()
                                            select n.ToDomainModelNavigationNode(web));

            return (result);
        }

        protected void TokenizeManagedNavigationTaxonomyIds(Web web, ManagedNavigation managedNavigation)
        {
            // Replace Taxonomy field references to SspId, TermSetId with tokens
            TaxonomySession session = TaxonomySession.GetTaxonomySession(web.Context);
            TermStore defaultStore = session.GetDefaultSiteCollectionTermStore();
            web.Context.Load(defaultStore, ts => ts.Name, ts => ts.Id);
            web.Context.ExecuteQueryRetry();

            Guid navigationTermStoreId = Guid.Parse(managedNavigation.TermStoreId);
            if (navigationTermStoreId != Guid.Empty)
            {
                TermStore navigationTermStore = session.TermStores.GetById(navigationTermStoreId);
                web.Context.Load(navigationTermStore, ts => ts.Name, ts => ts.Id);
                web.Context.ExecuteQueryRetry();

                if (!navigationTermStore.ServerObjectIsNull())
                {
                    if (navigationTermStore.Id == defaultStore.Id)
                    {
                        managedNavigation.TermStoreId = "{sitecollectiontermstoreid}";
                    }
                    else
                    {
                        managedNavigation.TermStoreId = $"{{termstoreid:{navigationTermStore.Name}}}";
                    }

                    Guid navigationTermSetId = Guid.Parse(managedNavigation.TermSetId);
                    if (navigationTermSetId != Guid.Empty)
                    {
                        var navigationTermSet = navigationTermStore.GetTermSet(navigationTermSetId);
                        web.Context.Load(navigationTermSet, ts => ts.Name, ts => ts.Id, ts => ts.Group);
                        web.Context.ExecuteQueryRetry();

                        if (!navigationTermSet.ServerObjectIsNull())
                        {
                            managedNavigation.TermSetId = $"{{termsetid:{navigationTermSet.Group.Name}:{navigationTermSet.Name}}}";
                        }
                    }
                }
            }
        }

        #endregion

        public override bool WillExtract(Web web, ProvisioningTemplate template, ProvisioningTemplateCreationInformation creationInfo)
        {
            return web.IsPublishingWeb();
        }

        public override bool WillProvision(Web web, ProvisioningTemplate template)
        {
            return web.IsPublishingWeb() && template.Navigation != null;
        }
    }

    internal static class NavigationNodeExtensions
    {
        internal static Model.NavigationNode ToDomainModelNavigationNode(this Microsoft.SharePoint.Client.NavigationNode node, Web web)
        {
            var result = new Model.NavigationNode
            {
                Title = node.Title,
                IsExternal = node.IsExternal,
                Url = node.Url.Replace(web.ServerRelativeUrl, "{site}"),
            };

            node.Context.Load(node.Children);
            node.Context.ExecuteQueryRetry();

            result.NavigationNodes.AddRange(from n in node.Children.AsEnumerable()
                                            select n.ToDomainModelNavigationNode(web));

            return (result);
        }
    }
}
