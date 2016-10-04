﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficeDevPnP.Core.Framework.Provisioning.Model
{
    /// <summary>
    /// Collection of RoleAssignment objects
    /// </summary>
    public partial class RoleAssignmentCollection : ProvisioningTemplateCollection<RoleAssignment>
    {
        public RoleAssignmentCollection(ProvisioningTemplate parentTemplate) : base(parentTemplate)
        {

        }
    }
}
