﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using OfficeDevPnP.Core.Diagnostics;

namespace OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers
{
    internal class ObjectAuditSettings : ObjectHandlerBase
    {
        public override string Name
        {
            get { return "Audit Settings"; }
        }

        public override ProvisioningTemplate ExtractObjects(Web web, ProvisioningTemplate template, ProvisioningTemplateCreationInformation creationInfo)
        {
            using (var scope = new PnPMonitoredScope(this.Name))
            {
                var auditSettings = new AuditSettings();

                var site = (web.Context as ClientContext).Site;

                site.EnsureProperties(s => s.Audit, s => s.AuditLogTrimmingRetention, s => s.TrimAuditLog);

                var siteAuditSettings = site.Audit;

                bool include = false;
                if (siteAuditSettings.AuditFlags != auditSettings.AuditFlags)
                {
                    include = true;
                    auditSettings.AuditFlags = siteAuditSettings.AuditFlags;
                }

                if (site.AuditLogTrimmingRetention != auditSettings.AuditLogTrimmingRetention)
                {
                    include = true;
                    auditSettings.AuditLogTrimmingRetention = site.AuditLogTrimmingRetention;
                }

                if (site.TrimAuditLog != auditSettings.TrimAuditLog)
                {
                    include = true;
                    auditSettings.TrimAuditLog = site.TrimAuditLog;
                }

                if (include)
                {
                    template.AuditSettings = auditSettings;
                }
            }
            return template;
        }

        public override TokenParser ProvisionObjects(Web web, ProvisioningTemplate template, TokenParser parser, ProvisioningTemplateApplyingInformation applyingInformation)
        {
            using (var scope = new PnPMonitoredScope(this.Name))
            {
                if (template.AuditSettings != null)
                {
                    var site = (web.Context as ClientContext).Site;

                    site.EnsureProperties(s => s.Audit, s => s.AuditLogTrimmingRetention, s => s.TrimAuditLog);

                    var siteAuditSettings = site.Audit;

                    var isDirty = false;
                    if (template.AuditSettings.AuditFlags != siteAuditSettings.AuditFlags)
                    {
                        site.Audit.AuditFlags = template.AuditSettings.AuditFlags;
                        site.Audit.Update();
                        isDirty = true;
                    }
                    if (template.AuditSettings.AuditLogTrimmingRetention != site.AuditLogTrimmingRetention)
                    {
                        site.AuditLogTrimmingRetention = template.AuditSettings.AuditLogTrimmingRetention;
                        isDirty = true;
                    }
                    if (template.AuditSettings.TrimAuditLog != site.TrimAuditLog)
                    {
                        site.TrimAuditLog = template.AuditSettings.TrimAuditLog;
                        isDirty = true;
                    }
                    if (isDirty)
                    {
                        web.Context.ExecuteQueryRetry();
                    }
                }
            }

            return parser;
        }

        public override bool WillExtract(Web web, ProvisioningTemplate template, ProvisioningTemplateCreationInformation creationInfo)
        {
            return !web.IsSubSite();
        }

        public override bool WillProvision(Web web, ProvisioningTemplate template)
        {
            return !web.IsSubSite() && template.AuditSettings != null;
        }
    }
}
