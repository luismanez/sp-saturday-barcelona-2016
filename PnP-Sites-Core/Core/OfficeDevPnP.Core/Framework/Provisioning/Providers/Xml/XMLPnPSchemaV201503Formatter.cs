﻿using OfficeDevPnP.Core.Framework.Provisioning.Model;
using OfficeDevPnP.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace OfficeDevPnP.Core.Framework.Provisioning.Providers.Xml
{
    [Obsolete("The PnP Provisioning Schema v201503 is obsolete and deprecated, please use the latest version available at https://github.com/OfficeDev/PnP-Provisioning-Schema")]
    internal class XMLPnPSchemaV201503Formatter :
        IXMLSchemaFormatter, ITemplateFormatter
    {
        private TemplateProviderBase _provider;

        public void Initialize(TemplateProviderBase provider)
        {
            this._provider = provider;
        }

        string IXMLSchemaFormatter.NamespaceUri
        {
            get { return (XMLConstants.PROVISIONING_SCHEMA_NAMESPACE_2015_03); }
        }

        string IXMLSchemaFormatter.NamespacePrefix
        {
            get { return (XMLConstants.PROVISIONING_SCHEMA_PREFIX); }
        }

        public bool IsValid(Stream template)
        {
            if (template == null)
            {
                throw new ArgumentNullException("template");
            }

            // Load the template into an XDocument
            XDocument xml = XDocument.Load(template);

            // Load the XSD embedded resource
            Stream stream = typeof(XMLPnPSchemaV201503Formatter)
                .Assembly
                .GetManifestResourceStream("OfficeDevPnP.Core.Framework.Provisioning.Providers.Xml.ProvisioningSchema-2015-03.xsd");

            // Prepare the XML Schema Set
            XmlSchemaSet schemas = new XmlSchemaSet();
            schemas.Add(XMLConstants.PROVISIONING_SCHEMA_NAMESPACE_2015_03,
                new XmlTextReader(stream));

            Boolean result = true;
            xml.Validate(schemas, (o, e) =>
            {
                Diagnostics.Log.Error(e.Exception, "SchemaFormatter", "Template is not valid: {0}", e.Message);
                result = false;
            });

            return (result);
        }

        Stream ITemplateFormatter.ToFormattedTemplate(ProvisioningTemplate template)
        {
            if (template == null)
            {
                throw new ArgumentNullException("template");
            }

            V201503.SharePointProvisioningTemplate result = new V201503.SharePointProvisioningTemplate();

            // Translate basic properties
            result.ID = template.Id;
            result.Version = template.Version.ToString("###0.0", new CultureInfo("en-US"));
            result.SitePolicy = template.SitePolicy;

            // Translate PropertyBagEntries, if any
            if (template.PropertyBagEntries != null && template.PropertyBagEntries.Count > 0)
            {
                result.PropertyBagEntries =
                    (from bag in template.PropertyBagEntries
                     select new V201503.PropertyBagEntry
                     {
                         Key = bag.Key,
                         Value = bag.Value,
                     }).ToArray();
            }
            else
            {
                result.PropertyBagEntries = null;
            }

            // Translate Security configuration, if any
            if (template.Security != null)
            {
                result.Security = new V201503.SharePointProvisioningTemplateSecurity();

                if (template.Security.AdditionalAdministrators != null && template.Security.AdditionalAdministrators.Count > 0)
                {
                    result.Security.AdditionalAdministrators =
                        (from user in template.Security.AdditionalAdministrators
                         select new V201503.User
                         {
                             Name = user.Name,
                         }).ToArray();
                }
                else
                {
                    result.Security.AdditionalAdministrators = null;
                }

                if (template.Security.AdditionalOwners != null && template.Security.AdditionalOwners.Count > 0)
                {
                    result.Security.AdditionalOwners =
                        (from user in template.Security.AdditionalOwners
                         select new V201503.User
                         {
                             Name = user.Name,
                         }).ToArray();
                }
                else
                {
                    result.Security.AdditionalOwners = null;
                }

                if (template.Security.AdditionalMembers != null && template.Security.AdditionalMembers.Count > 0)
                {
                    result.Security.AdditionalMembers =
                        (from user in template.Security.AdditionalMembers
                         select new V201503.User
                         {
                             Name = user.Name,
                         }).ToArray();
                }
                else
                {
                    result.Security.AdditionalMembers = null;
                }

                if (template.Security.AdditionalVisitors != null && template.Security.AdditionalVisitors.Count > 0)
                {
                    result.Security.AdditionalVisitors =
                        (from user in template.Security.AdditionalVisitors
                         select new V201503.User
                         {
                             Name = user.Name,
                         }).ToArray();
                }
                else
                {
                    result.Security.AdditionalVisitors = null;
                }
            }

            // Translate Site Columns (Fields), if any
            if (template.SiteFields != null && template.SiteFields.Count > 0)
            {
                result.SiteFields = new V201503.SharePointProvisioningTemplateSiteFields
                {
                    Any =
                        (from field in template.SiteFields
                         select field.SchemaXml.ToXmlElement()).ToArray(),
                };
            }
            else
            {
                result.SiteFields = null;
            }

            // Translate ContentTypes, if any
            if (template.ContentTypes != null && template.ContentTypes.Count > 0)
            {
                result.ContentTypes = new V201503.SharePointProvisioningTemplateContentTypes();
                List<XmlElement> ctElements = new List<XmlElement>();

                foreach (var ct in template.ContentTypes)
                {
                    XElement xElement = new XElement("ContentType");
                    xElement.Add(new XAttribute("ID", ct.Id));

                    xElement.Add(new XAttribute("Name", ct.Name));
                    if (!string.IsNullOrEmpty(ct.Group))
                    {
                        xElement.Add(new XAttribute("Group", ct.Group));
                    }
                    if (!string.IsNullOrEmpty(ct.Description))
                    {
                        xElement.Add(new XAttribute("Description", ct.Description));
                    }
                    xElement.Add(new XAttribute("Hidden", ct.Hidden.ToString().ToLower()));
                    xElement.Add(new XAttribute("ReadOnly", ct.ReadOnly.ToString().ToLower()));

                    xElement.Add(new XAttribute("Sealed", ct.Sealed.ToString().ToLower()));
                    if (!string.IsNullOrEmpty(ct.DocumentTemplate))
                    {

                        var documentTemplateElement = new XElement("DocumentTemplate");
                        documentTemplateElement.Add(new XAttribute("TargetName", ct.DocumentTemplate));
                        xElement.Add(documentTemplateElement);
                    }
                    var doc = new XmlDocument();
                    using (var elReader = xElement.CreateReader())
                    {
                        doc.Load(elReader);
                        ctElements.Add(doc.DocumentElement);
                    }
                }
                result.ContentTypes.Any = ctElements.ToArray();

            }
            else
            {
                result.ContentTypes = null;
            }

            // Translate Lists Instances, if any
            if (template.Lists != null && template.Lists.Count > 0)
            {
                result.Lists =
                    (from list in template.Lists
                     select new V201503.ListInstance
                     {
                         ContentTypesEnabled = list.ContentTypesEnabled,
                         Description = list.Description,
                         DocumentTemplate = list.DocumentTemplate,
                         EnableVersioning = list.EnableVersioning,
                         Hidden = list.Hidden,
                         MinorVersionLimit = list.MinorVersionLimit,
                         MaxVersionLimit = list.MaxVersionLimit,
                         OnQuickLaunch = list.OnQuickLaunch,
                         RemoveDefaultContentType = list.RemoveExistingContentTypes,
                         TemplateType = list.TemplateType,
                         Title = list.Title,
                         Url = list.Url,
                         ContentTypeBindings = list.ContentTypeBindings.Count > 0 ?
                            (from contentTypeBinding in list.ContentTypeBindings
                             select new V201503.ContentTypeBinding
                             {
                                 ContentTypeID = contentTypeBinding.ContentTypeId,
                                 Default = contentTypeBinding.Default,
                             }).ToArray() : null,
                         Views = list.Views.Count > 0 ?
                         new V201503.ListInstanceViews
                         {
                             Any =
                                (from view in list.Views
                                 select view.SchemaXml.ToXmlElement()).ToArray(),
                         } : null,
                         Fields = list.Fields.Count > 0 ?
                         new V201503.ListInstanceFields
                         {
                             Any =
                             (from field in list.Fields
                              select field.SchemaXml.ToXmlElement()).ToArray(),
                         } : null,
                         FieldRefs = list.FieldRefs.Count > 0 ?
                         (from fieldRef in list.FieldRefs
                          select new V201503.FieldRef
                          {
                              ID = fieldRef.Id.ToString(),
                          }).ToArray() : null,
                     }).ToArray();
            }
            else
            {
                result.Lists = null;
            }

            // Translate Features, if any
            if (template.Features != null)
            {
                result.Features = new V201503.SharePointProvisioningTemplateFeatures();

                // TODO: This nullability check could be useless, because
                // the SiteFeatures property is initialized in the Features
                // constructor
                if (template.Features.SiteFeatures != null && template.Features.SiteFeatures.Count > 0)
                {
                    result.Features.SiteFeatures =
                        (from feature in template.Features.SiteFeatures
                         select new V201503.Feature
                         {
                             ID = feature.Id.ToString(),
                             Deactivate = feature.Deactivate,
                         }).ToArray();
                }
                else
                {
                    result.Features.SiteFeatures = null;
                }

                // TODO: This nullability check could be useless, because
                // the WebFeatures property is initialized in the Features
                // constructor
                if (template.Features.WebFeatures != null && template.Features.WebFeatures.Count > 0)
                {
                    result.Features.WebFeatures =
                        (from feature in template.Features.WebFeatures
                         select new V201503.Feature
                         {
                             ID = feature.Id.ToString(),
                             Deactivate = feature.Deactivate,
                         }).ToArray();
                }
                else
                {
                    result.Features.WebFeatures = null;
                }
            }

            // Translate CustomActions, if any
            if (template.CustomActions != null)
            {
                result.CustomActions = new V201503.SharePointProvisioningTemplateCustomActions();

                if (template.CustomActions.SiteCustomActions != null && template.CustomActions.SiteCustomActions.Count > 0)
                {
                    result.CustomActions.SiteCustomActions =
                        (from customAction in template.CustomActions.SiteCustomActions
                         select new V201503.CustomAction
                         {
                             Description = customAction.Description,
                             Enabled = customAction.Enabled,
                             Group = customAction.Group,
                             ImageUrl = customAction.ImageUrl,
                             Location = customAction.Location,
                             Name = customAction.Name,
                             Rights = customAction.RightsValue,
                             RightsSpecified = true,
                             ScriptBlock = customAction.ScriptBlock,
                             ScriptSrc = customAction.ScriptSrc,
                             Sequence = customAction.Sequence,
                             SequenceSpecified = true,
                             Title = customAction.Title,
                             Url = customAction.Url,
                         }).ToArray();
                }
                else
                {
                    result.CustomActions.SiteCustomActions = null;
                }

                if (template.CustomActions.WebCustomActions != null && template.CustomActions.WebCustomActions.Count > 0)
                {
                    result.CustomActions.WebCustomActions =
                        (from customAction in template.CustomActions.WebCustomActions
                         select new V201503.CustomAction
                         {
                             Description = customAction.Description,
                             Enabled = customAction.Enabled,
                             Group = customAction.Group,
                             ImageUrl = customAction.ImageUrl,
                             Location = customAction.Location,
                             Name = customAction.Name,
                             Rights = customAction.RightsValue,
                             RightsSpecified = true,
                             ScriptBlock = customAction.ScriptBlock,
                             ScriptSrc = customAction.ScriptSrc,
                             Sequence = customAction.Sequence,
                             SequenceSpecified = true,
                             Title = customAction.Title,
                             Url = customAction.Url,
                         }).ToArray();
                }
                else
                {
                    result.CustomActions.WebCustomActions = null;
                }
            }

            // Translate Files, if any
            if (template.Files != null && template.Files.Count > 0)
            {
                result.Files =
                    (from file in template.Files
                     select new V201503.File
                     {
                         Overwrite = file.Overwrite,
                         Src = file.Src,
                         Folder = file.Folder,
                     }).ToArray();
            }
            else
            {
                result.Files = null;
            }

            // Translate ComposedLook, if any
            if (template.ComposedLook != null)
            {
                result.ComposedLook = new V201503.ComposedLook
                {
                    AlternateCSS = template.ComposedLook.AlternateCSS,
                    BackgroundFile = template.ComposedLook.BackgroundFile,
                    ColorFile = template.ComposedLook.ColorFile,
                    FontFile = template.ComposedLook.FontFile,
                    MasterPage = template.ComposedLook.MasterPage,
                    Name = template.ComposedLook.Name,
                    SiteLogo = template.ComposedLook.SiteLogo,
                    Version = template.ComposedLook.Version,
                    VersionSpecified = true,
                };
            }

            // Translate Providers, if any
            if ((template.Providers != null && template.Providers.Count > 0) || (template.ExtensibilityHandlers != null && template.ExtensibilityHandlers.Count > 0))
            {
                var extensibilityHandlers = template.ExtensibilityHandlers.Union(template.Providers);
                result.Providers =
                    (from provider in extensibilityHandlers
                     select new V201503.Provider
                     {
                         Assembly = provider.Assembly,
                         Configuration = provider.Configuration != null ? provider.Configuration.ToXmlNode() : null,
                         Enabled = provider.Enabled,
                         Type = provider.Type,
                     }).ToArray();
            }
            else
            {
                result.Providers = null;
            }

            XmlSerializerNamespaces ns =
                new XmlSerializerNamespaces();
            ns.Add(((IXMLSchemaFormatter)this).NamespacePrefix,
                ((IXMLSchemaFormatter)this).NamespaceUri);

            var output = XMLSerializer.SerializeToStream<V201503.SharePointProvisioningTemplate>(result, ns);
            output.Position = 0;
            return (output);
        }

        public ProvisioningTemplate ToProvisioningTemplate(Stream template)
        {
            return (this.ToProvisioningTemplate(template, null));
        }

        public ProvisioningTemplate ToProvisioningTemplate(Stream template, String identifier)
        {
            if (template == null)
            {
                throw new ArgumentNullException("template");
            }

            // Crate a copy of the source stream
            MemoryStream sourceStream = new MemoryStream();
            template.CopyTo(sourceStream);
            sourceStream.Position = 0;

            // Check the provided template against the XML schema
            if (!this.IsValid(sourceStream))
            {
                // TODO: Use resource file
                throw new ApplicationException("The provided template is not valid!");
            }

            sourceStream.Position = 0;
            XDocument xml = XDocument.Load(sourceStream);
            V201503.SharePointProvisioningTemplate source = XMLSerializer.Deserialize<V201503.SharePointProvisioningTemplate>(xml);

            ProvisioningTemplate result = new ProvisioningTemplate();

            // Translate basic properties
            result.Id = source.ID;
            result.Version = Double.Parse(!String.IsNullOrEmpty(source.Version) ? source.Version : "0", new CultureInfo("en-US"));
            result.SitePolicy = source.SitePolicy;

            // Translate PropertyBagEntries, if any
            if (source.PropertyBagEntries != null)
            {
                result.PropertyBagEntries.AddRange(
                    from bag in source.PropertyBagEntries
                    select new Model.PropertyBagEntry
                    {
                        Key = bag.Key,
                        Value = bag.Value,
                    });
            }

            // Translate Security configuration, if any
            if (source.Security != null)
            {
                if (source.Security.AdditionalAdministrators != null)
                {
                    result.Security.AdditionalAdministrators.AddRange(
                    from user in source.Security.AdditionalAdministrators
                    select new Model.User
                    {
                        Name = user.Name,
                    });
                }
                if (source.Security.AdditionalOwners != null)
                {
                    result.Security.AdditionalOwners.AddRange(
                    from user in source.Security.AdditionalOwners
                    select new Model.User
                    {
                        Name = user.Name,
                    });
                }
                if (source.Security.AdditionalMembers != null)
                {
                    result.Security.AdditionalMembers.AddRange(
                    from user in source.Security.AdditionalMembers
                    select new Model.User
                    {
                        Name = user.Name,
                    });
                }
                if (source.Security.AdditionalVisitors != null)
                {
                    result.Security.AdditionalVisitors.AddRange(
                    from user in source.Security.AdditionalVisitors
                    select new Model.User
                    {
                        Name = user.Name,
                    });
                }
            }

            // Translate Site Columns (Fields), if any
            if ((source.SiteFields != null) && (source.SiteFields.Any != null))
            {
                result.SiteFields.AddRange(
                    from field in source.SiteFields.Any
                    select new Field
                    {
                        SchemaXml = field.OuterXml,
                    });
            }

            // Translate ContentTypes, if any
            if ((source.ContentTypes != null) && (source.ContentTypes.Any != null))
            {
                // Convert the SchemaXml property to the new format
                foreach (var contentType in source.ContentTypes.Any)
                {
                    var xelement = XElement.Parse(contentType.OuterXml);
                    var ID = xelement.Attribute("ID") != null ? xelement.Attribute("ID").Value : null;
                    var Name = xelement.Attribute("Name") != null ? xelement.Attribute("Name").Value : null;
                    var Description = xelement.Attribute("Description") != null ? xelement.Attribute("Description").Value : null;
                    var Group = xelement.Attribute("Group") != null ? xelement.Attribute("Group").Value : null;
                    var Hidden = xelement.Attribute("Hidden") != null ? bool.Parse(xelement.Attribute("Hidden").Value) : false;
                    var ReadOnly = xelement.Attribute("ReadOnly") != null ? bool.Parse(xelement.Attribute("ReadOnly").Value) : false;
                    var Sealed = xelement.Attribute("Sealed") != null ? bool.Parse(xelement.Attribute("Sealed").Value) : false;
                    var Overwrite = xelement.Attribute("Overwrite") != null ? bool.Parse(xelement.Attribute("Overwrite").Value) : false;

                    var ct = new ContentType();
                    ct.Id = ID;
                    ct.Name = Name;
                    ct.Description = Description;
                    ct.Group = Group;
                    ct.Hidden = Hidden;
                    ct.ReadOnly = ReadOnly;
                    ct.Sealed = Sealed;
                    ct.Overwrite = Overwrite;

                    var documentTemplateElement = xelement.Descendants("DocumentTemplate").FirstOrDefault();
                    if (documentTemplateElement != null)
                    {
                        ct.DocumentTemplate = documentTemplateElement.Attribute("TargetName") != null ? documentTemplateElement.Attribute("TargetName").Value : null;
                    }
                    var fieldRefs = xelement.Descendants("FieldRefs").Descendants("FieldRef");
                    foreach (var fieldRef in fieldRefs)
                    {
                        FieldRef fr = new FieldRef();
                        fr.Id = fieldRef.Attribute("ID") != null ? Guid.Parse(fieldRef.Attribute("ID").Value) : Guid.Empty;
                        fr.Required = fieldRef.Attribute("Required") != null ? bool.Parse(fieldRef.Attribute("Required").Value) : false;
                        fr.Hidden = fieldRef.Attribute("Hidden") != null ? bool.Parse(fieldRef.Attribute("Hidden").Value) : false;
                        ct.FieldRefs.Add(fr);
                    }
                    result.ContentTypes.Add(ct);

                }
            }

            // Translate Lists Instances, if any
            if (source.Lists != null)
            {
                result.Lists.AddRange(
                    from list in source.Lists
                    select new Model.ListInstance(
                        (list.ContentTypeBindings != null ?
                                (from contentTypeBinding in list.ContentTypeBindings
                                 select new Model.ContentTypeBinding
                                 {
                                     ContentTypeId = contentTypeBinding.ContentTypeID,
                                     Default = contentTypeBinding.Default,
                                 }) : null),
                        (list.Views != null ?
                                (from view in list.Views.Any
                                 select new View
                                 {
                                     SchemaXml = view.OuterXml,
                                 }) : null),
                        (list.Fields != null ?
                                (from field in list.Fields.Any
                                 select new Field
                                 {
                                     SchemaXml = field.OuterXml,
                                 }) : null),
                        (list.FieldRefs != null ?
                                 (from fieldRef in list.FieldRefs
                                  select new Model.FieldRef
                                  {
                                      Id = Guid.Parse(fieldRef.ID)
                                  }) : null),
                                  null
                         )
                    {
                        ContentTypesEnabled = list.ContentTypesEnabled,
                        Description = list.Description,
                        DocumentTemplate = list.DocumentTemplate,
                        EnableVersioning = list.EnableVersioning,
                        Hidden = list.Hidden,
                        MinorVersionLimit = list.MinorVersionLimit,
                        MaxVersionLimit = list.MaxVersionLimit,
                        OnQuickLaunch = list.OnQuickLaunch,
                        RemoveExistingContentTypes = list.RemoveDefaultContentType,
                        TemplateType = list.TemplateType,
                        Title = list.Title,
                        Url = list.Url,
                    });
            }

            // Translate Features, if any
            if (source.Features != null)
            {
                if (result.Features.SiteFeatures != null && source.Features.SiteFeatures != null)
                {
                    result.Features.SiteFeatures.AddRange(
                        from feature in source.Features.SiteFeatures
                        select new Model.Feature
                        {
                            Id = new Guid(feature.ID),
                            Deactivate = feature.Deactivate,
                        });
                }
                if (result.Features.WebFeatures != null && source.Features.WebFeatures != null)
                {
                    result.Features.WebFeatures.AddRange(
                        from feature in source.Features.WebFeatures
                        select new Model.Feature
                        {
                            Id = new Guid(feature.ID),
                            Deactivate = feature.Deactivate,
                        });
                }
            }

            // Translate CustomActions, if any
            if (source.CustomActions != null)
            {
                if (result.CustomActions.SiteCustomActions != null && source.CustomActions.SiteCustomActions != null)
                {
                    result.CustomActions.SiteCustomActions.AddRange(
                        from customAction in source.CustomActions.SiteCustomActions
                        select new Model.CustomAction
                        {
                            Description = customAction.Description,
                            Enabled = customAction.Enabled,
                            Group = customAction.Group,
                            ImageUrl = customAction.ImageUrl,
                            Location = customAction.Location,
                            Name = customAction.Name,
                            RightsValue = customAction.RightsSpecified ? customAction.Rights : 0,
                            ScriptBlock = customAction.ScriptBlock,
                            ScriptSrc = customAction.ScriptSrc,
                            Sequence = customAction.SequenceSpecified ? customAction.Sequence : 100,
                            Title = customAction.Title,
                            Url = customAction.Url,
                        });
                }
                if (result.CustomActions.WebCustomActions != null && source.CustomActions.WebCustomActions != null)
                {
                    result.CustomActions.WebCustomActions.AddRange(
                        from customAction in source.CustomActions.WebCustomActions
                        select new Model.CustomAction
                        {
                            Description = customAction.Description,
                            Enabled = customAction.Enabled,
                            Group = customAction.Group,
                            ImageUrl = customAction.ImageUrl,
                            Location = customAction.Location,
                            Name = customAction.Name,
                            RightsValue = customAction.RightsSpecified ? customAction.Rights : 0,
                            ScriptBlock = customAction.ScriptBlock,
                            ScriptSrc = customAction.ScriptSrc,
                            Sequence = customAction.SequenceSpecified ? customAction.Sequence : 100,
                            Title = customAction.Title,
                            Url = customAction.Url,
                        });
                }
            }

            // Translate Files, if any
            if (source.Files != null)
            {
                result.Files.AddRange(
                    from file in source.Files
                    select new Model.File
                    {
                        Overwrite = file.Overwrite,
                        Src = file.Src,
                        Folder = file.Folder,
                    });
            }

            // Translate ComposedLook, if any
            if (source.ComposedLook != null)
            {
                result.ComposedLook.AlternateCSS = source.ComposedLook.AlternateCSS;
                result.ComposedLook.BackgroundFile = source.ComposedLook.BackgroundFile;
                result.ComposedLook.ColorFile = source.ComposedLook.ColorFile;
                result.ComposedLook.FontFile = source.ComposedLook.FontFile;
                result.ComposedLook.MasterPage = source.ComposedLook.MasterPage;
                result.ComposedLook.Name = source.ComposedLook.Name;
                result.ComposedLook.SiteLogo = source.ComposedLook.SiteLogo;
                result.ComposedLook.Version = source.ComposedLook.Version;
            }

            // Translate Providers, if any
            if (source.Providers != null)
            {
                result.ExtensibilityHandlers.AddRange(
                    from provider in source.Providers
                    select new Model.ExtensibilityHandler
                    {
                        Assembly = provider.Assembly,
                        Configuration = provider.Configuration != null ? provider.Configuration.ToProviderConfiguration() : null,
                        Enabled = provider.Enabled,
                        Type = provider.Type,
                    });
            }

            return (result);
        }
    }
}

