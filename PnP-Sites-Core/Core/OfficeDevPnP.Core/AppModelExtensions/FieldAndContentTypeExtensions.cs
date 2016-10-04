﻿using OfficeDevPnP.Core;
using OfficeDevPnP.Core.Diagnostics;
using OfficeDevPnP.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.SharePoint.Client.DocumentSet;

namespace Microsoft.SharePoint.Client
{
    /// <summary>
    /// This class provides extension methods that will help you work with fields and content types.
    /// </summary>
    public static partial class FieldAndContentTypeExtensions
    {
        #region Site Columns

        /// <summary>
        /// Create field to web remotely
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="fieldCreationInformation">Creation Information for the field.</param>
        /// <param name="executeQuery">Optionally skip the executeQuery action</param>
        /// <returns>The newly created field or existing field.</returns>
        public static Field CreateField(this Web web, FieldCreationInformation fieldCreationInformation, bool executeQuery = true)
        {
            return CreateField<Field>(web, fieldCreationInformation, executeQuery);
        }

        /// <summary>
        /// Create field to web remotely
        /// </summary>
        /// <typeparam name="TField">The created field type to return.</typeparam>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="fieldCreationInformation">Field creation information</param>
        /// <param name="executeQuery">Optionally skip the executeQuery action</param>
        /// <returns>The newly created field or existing field.</returns>
        public static TField CreateField<TField>(this Web web, FieldCreationInformation fieldCreationInformation, bool executeQuery = true) where TField : Field
        {
            if (string.IsNullOrEmpty(fieldCreationInformation.InternalName))
            {
                throw new ArgumentNullException("InternalName");
            }

            if (string.IsNullOrEmpty(fieldCreationInformation.DisplayName))
            {
                throw new ArgumentNullException("DisplayName");
            }

            var fields = web.Fields;
            web.Context.Load(fields, fc => fc.Include(f => f.Id, f => f.InternalName));
            web.Context.ExecuteQueryRetry();

            var field = CreateFieldBase<TField>(fields, fieldCreationInformation, executeQuery);
            return field;
        }

        /// <summary>
        /// Create field to web remotely
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="fieldAsXml">The XML declaration of SiteColumn definition</param>
        /// <param name="executeQuery"></param>
        /// <returns>The newly created field or existing field.</returns>
        public static Field CreateField(this Web web, string fieldAsXml, bool executeQuery = true)
        {
            if (string.IsNullOrEmpty(fieldAsXml))
            {
                throw new ArgumentNullException("fieldAsXml");
            }

            var xd = XDocument.Parse(fieldAsXml);
            if (xd.Root != null)
            {
                var ns = xd.Root.Name.Namespace;

                var fieldNode = (from f in xd.Elements(ns + "Field") select f).FirstOrDefault();

                if (fieldNode != null)
                {
                    var id = fieldNode.Attribute("ID").Value;
                    var name = fieldNode.Attribute("Name").Value;

                    Log.Info(Constants.LOGGING_SOURCE, CoreResources.FieldAndContentTypeExtensions_CreateField01, name, id);
                }
            }

            var fields = web.Fields;
            web.Context.Load(fields);
            web.Context.ExecuteQueryRetry();

            var field = fields.AddFieldAsXml(fieldAsXml, false, AddFieldOptions.AddFieldInternalNameHint);
            web.Update();

            if (executeQuery)
            {
                web.Context.ExecuteQueryRetry();
            }

            return field;
        }
        /// <summary>
        /// Removes a field by specifying its internal name
        /// </summary>
        /// <param name="web"></param>
        /// <param name="internalName"></param>
        public static void RemoveFieldByInternalName(this Web web, string internalName)
        {
            var fields = web.Context.LoadQuery(web.Fields.Where(f => f.InternalName == internalName));
            web.Context.ExecuteQueryRetry();

            var enumerable = fields as Field[] ?? fields.ToArray();
            if (!enumerable.Any())
            {
                throw new ArgumentException(string.Format("Could not find field with internalName {0}", internalName));
            }

            enumerable.First().DeleteObject();
        }

        /// <summary>
        /// Removes a field by specifying its ID
        /// </summary>
        /// <param name="web"></param>
        /// <param name="fieldId">The id of the field to remove</param>
        public static void RemoveFieldById(this Web web, string fieldId)
        {
            Guid fieldGuid = new Guid(fieldId);
            var fields = web.Context.LoadQuery(web.Fields.Where(f => f.Id == fieldGuid));
            web.Context.ExecuteQueryRetry();

            var enumerable = fields as Field[] ?? fields.ToArray();
            if (!enumerable.Any())
            {
                throw new ArgumentException(string.Format("Could not find field with id {0}", fieldId));
            }

            enumerable.First().DeleteObject();
        }

        /// <summary>
        /// Creates fields from feature element xml file schema. XML file can contain one or many field definitions created using classic feature framework structure.
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site. Site columns should be created to root site.</param>
        /// <param name="xmlFilePath">Absolute path to the xml location</param>
        public static void CreateFieldsFromXMLFile(this Web web, string xmlFilePath)
        {
            var xd = XDocument.Load(xmlFilePath);

            // Perform the action field creation
            CreateFieldsFromXML(web, xd);
        }

        /// <summary>
        /// Creates fields from feature element xml file schema. XML file can contain one or many field definitions created using classic feature framework structure.
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site. Site columns should be created to root site.</param>
        /// <param name="xmlStructure">XML structure in string format</param>
        public static void CreateFieldsFromXMLString(this Web web, string xmlStructure)
        {
            var xd = XDocument.Parse(xmlStructure);

            // Perform the action field creation
            CreateFieldsFromXML(web, xd);
        }

        /// <summary>
        /// Creates field from xml structure which follows the classic feature framework structure
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site. Site columns should be created to root site.</param>
        /// <param name="xDocument">Actual XML document</param>
        public static void CreateFieldsFromXML(this Web web, XDocument xDocument)
        {
            var ns = xDocument.Root.Name.Namespace;

            var fields = from f in xDocument.Descendants(ns + "Field") select f;

            foreach (var field in fields)
            {
                var id = field.Attribute("ID").Value;
                var name = field.Attribute("Name").Value;

                // If field already existed, let's move on
                if (web.FieldExistsByName(name))
                {
                    Log.Warning(Constants.LOGGING_SOURCE, CoreResources.FieldAndContentTypeExtensions_Field01AlreadyExists, name, id);
                }
                else
                {
                    web.CreateField(field.ToString());
                }
            }
        }

        /// <summary>
        /// Returns if the field is found
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site. Site columns should be created to root site.</param>
        /// <param name="fieldId">Guid for the field ID</param>
        /// <param name="searchInSiteHierarchy">If true, search parent sites and root site</param>         
        /// <returns>True or false depending on the field existence</returns>
        public static bool FieldExistsById(this Web web, Guid fieldId, bool searchInSiteHierarchy = false)
        {
            var field = web.GetFieldById<Field>(fieldId, searchInSiteHierarchy);
            return field != null;
        }

        /// <summary>
        /// Returns the field if it exists. Null if it does not exist.
        /// </summary>
        /// <typeparam name="TField">The selected field type to return.</typeparam>
        /// <param name="web">Site to be processed - can be root web or sub site. Site columns should be created to root site.</param>
        /// <param name="fieldId">Guid for the field ID</param>
        /// <param name="searchInSiteHierarchy">If true, search parent sites and root site</param>         
        /// <returns>Field of type TField</returns>
        public static TField GetFieldById<TField>(this Web web, Guid fieldId, bool searchInSiteHierarchy = false) where TField : Field
        {
            IEnumerable<Field> fields = null;
            if (searchInSiteHierarchy)
            {
                fields = web.Context.LoadQuery(web.AvailableFields.Where(f => f.Id == fieldId));
            }
            else
            {
                fields = web.Context.LoadQuery(web.Fields.Where(f => f.Id == fieldId));
            }

            web.Context.ExecuteQueryRetry();

            var field = fields.FirstOrDefault();
            if (field == null)
            {
                return null;
            }
            else
            {
                return web.Context.CastTo<TField>(field);
            }
        }

        /// <summary>
        /// Returns the field if it exists. Null if it does not exist.
        /// </summary>
        /// <typeparam name="TField">The selected field type to return.</typeparam>
        /// <param name="list">List to be processed. Columns assoc in lists are defined on web or rootweb.</param>
        /// <param name="fieldId">Guid for the field ID</param>
        /// <returns>Field of type TField</returns>
        public static TField GetFieldById<TField>(this List list, Guid fieldId) where TField : Field
        {
            var fields = list.Context.LoadQuery(list.Fields.Where(f => f.Id == fieldId));
            list.Context.ExecuteQueryRetry();

            var field = fields.FirstOrDefault();
            return field == null ? null : list.Context.CastTo<TField>(field);
        }

        /// <summary>
        /// Returns the field if it exists. Null if it does not exist.
        /// </summary>
        /// <typeparam name="TField">The selected field type to return.</typeparam>
        /// <param name="fields">FieldCollection to be processed.</param>
        /// <param name="internalName">Guid for the field ID</param>
        /// <returns>Field of type TField</returns>
        public static TField GetFieldByName<TField>(this FieldCollection fields, string internalName) where TField : Field
        {
            if (!fields.ServerObjectIsNull.HasValue ||
                fields.ServerObjectIsNull.Value)
            {
                fields.Context.Load(fields);
                fields.Context.ExecuteQueryRetry();
            }

            var field = fields.FirstOrDefault(f => f.StaticName == internalName);
            return field == null ? null : fields.Context.CastTo<TField>(field);
        }

        /// <summary>
        /// Returns if the field is found
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site. Site columns should be created to root site.</param>
        /// <param name="fieldName">String for the field internal name to be used as query criteria</param>
        /// <param name="searchInSiteHierarchy">If true, search parent sites and root site</param> 
        /// <returns>True or false depending on the field existence</returns>
        public static bool FieldExistsByName(this Web web, string fieldName,bool searchInSiteHierarchy= false)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentNullException("fieldName");
            }

            FieldCollection fields = null;

            if (searchInSiteHierarchy)
            {
                fields = web.AvailableFields;
            }
            else
            {
                fields = web.Fields;
            }
            var results = web.Context.LoadQuery(fields.Where(item => item.InternalName == fieldName));
            web.Context.ExecuteQueryRetry();
            return results.FirstOrDefault() != null;
        }

        /// <summary>
        /// Does field exist in web
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site. Site columns should be created to root site.</param>
        /// <param name="fieldId">String representation of the field ID (=guid)</param>
        /// <param name="searchInSiteHierarchy">If true, search parent sites and root site</param> 
        /// <returns>True if exists, false otherwise</returns>
        public static bool FieldExistsById(this Web web, string fieldId, bool searchInSiteHierarchy=false)
        {
            if (string.IsNullOrEmpty(fieldId))
            {
                throw new ArgumentNullException("fieldId");
            }

            return FieldExistsById(web, new Guid(fieldId), searchInSiteHierarchy);
        }

        /// <summary>
        /// Field exists in content type
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site. Site columns should be created to root site.</param>
        /// <param name="contentTypeName">Name of the content type</param>
        /// <param name="fieldName">Name of the field</param>
        /// <returns>True if exists, false otherwise</returns>
        public static bool FieldExistsByNameInContentType(this Web web, string contentTypeName, string fieldName)
        {
            if (string.IsNullOrEmpty(contentTypeName))
            {
                throw new ArgumentNullException("contentTypeName");
            }

            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentNullException("fieldName");
            }

            var ct = GetContentTypeByName(web, contentTypeName);
            var fields = ct.Fields;
            var results = ct.Context.LoadQuery(fields.Where(item => item.InternalName == fieldName));
            ct.Context.ExecuteQueryRetry();
            return results.FirstOrDefault() != null;
        }
        
        /// <summary>
        /// Checks if a field exists in a content type by id
        /// </summary>
        /// <param name="contentType">The content type to check</param>
        /// <param name="fieldName">The name of the field to look for</param>
        /// <returns>True if field exists in content type, otherwise false</returns>
        public static bool FieldExistsByNameInContentType(this ContentType contentType, string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentNullException("fieldName");
            }

            var fields = contentType.Fields;
            var results = contentType.Context.LoadQuery(fields.Where(item => item.InternalName == fieldName));
            contentType.Context.ExecuteQueryRetry();
            return results.FirstOrDefault() != null;
        }

        /// <summary>
        /// Adds jsLink to a field.
        /// </summary>
        /// <param name="field">The field to add jsLink to</param>
        /// <param name="jsLink">JSLink to set to the form. Set to empty string to remove the set JSLink customization.
        /// Specify multiple values separated by pipe symbol. For e.g.: ~sitecollection/_catalogs/masterpage/jquery-2.1.0.min.js|~sitecollection/_catalogs/masterpage/custom.js
        /// </param>
        public static void SetJsLinkCustomizations(this Field field, string jsLink)
        {
            field.JSLink = jsLink;
            field.UpdateAndPushChanges(true);
            field.Context.ExecuteQueryRetry();
        }


        #endregion

        #region List Fields

        /// <summary>
        /// Adds field to a list
        /// </summary>
        /// <param name="list">List to process</param>
        /// <param name="fieldCreationInformation">Creation information for the field</param>
        /// <param name="executeQuery"></param>
        /// <returns>The newly created field or existing field.</returns>
        public static Field CreateField(this List list, FieldCreationInformation fieldCreationInformation, bool executeQuery = true)
        {
            return CreateField<Field>(list, fieldCreationInformation, executeQuery);
        }

        /// <summary>
        /// Adds field to a list
        /// </summary>
        /// <typeparam name="TField">The selected field type to return.</typeparam>
        /// <param name="list">List to process</param>
        /// <param name="fieldCreationInformation">Field creation information</param>
        /// <param name="executeQuery">Optionally skip the executeQuery action</param>
        /// <returns>The newly created field or existing field.</returns>
        public static TField CreateField<TField>(this List list, FieldCreationInformation fieldCreationInformation, bool executeQuery = true) where TField : Field
        {
            if (string.IsNullOrEmpty(fieldCreationInformation.InternalName))
            {
                throw new ArgumentNullException("InternalName");
            }

            if (string.IsNullOrEmpty(fieldCreationInformation.DisplayName))
            {
                throw new ArgumentNullException("DisplayName");
            }

            var fields = list.Fields;
            list.Context.Load(fields, fc => fc.Include(f => f.Id, f => f.InternalName));
            list.Context.ExecuteQueryRetry();

            var field = CreateFieldBase<TField>(fields, fieldCreationInformation, executeQuery);
            return field;
        }

        /// <summary>
        /// Base implementation for creating fields
        /// </summary>
        /// <typeparam name="TField">The selected field type to return.</typeparam>
        /// <param name="fields">Field collection to which the created field will be added</param>
        /// <param name="fieldCreationInformation">The information about the field to be created</param>
        /// <param name="executeQuery">Optionally skip the executeQuery action</param>
        /// <returns></returns>
        static TField CreateFieldBase<TField>(FieldCollection fields, FieldCreationInformation fieldCreationInformation, bool executeQuery = true) where TField : Field
        {
            Field field = fields.FirstOrDefault(f => f.Id == fieldCreationInformation.Id || f.InternalName == fieldCreationInformation.InternalName) as TField;

            if (field != null)
            {
                throw new ArgumentException("id", "Field already exists");
            }

            var newFieldCAML = FormatFieldXml(fieldCreationInformation);

            Log.Info(Constants.LOGGING_SOURCE, CoreResources.FieldAndContentTypeExtensions_CreateField01, fieldCreationInformation.InternalName, fieldCreationInformation.Id);
            field = fields.AddFieldAsXml(newFieldCAML, fieldCreationInformation.AddToDefaultView, AddFieldOptions.AddFieldInternalNameHint);
            fields.Context.Load(field);

            if (executeQuery)
            {
                fields.Context.ExecuteQueryRetry();
            }

            return fields.Context.CastTo<TField>(field);
        }

        /// <summary>
        /// Formats a fieldcreationinformation object into Field CAML xml.
        /// </summary>
        /// <param name="fieldCreationInformation"></param>
        /// <returns></returns>
        public static string FormatFieldXml(FieldCreationInformation fieldCreationInformation)
        {
            List<string> additionalAttributesList = new List<string>();

            if (fieldCreationInformation.AdditionalAttributes != null)
            {
                foreach (var keyvaluepair in fieldCreationInformation.AdditionalAttributes)
                {
                    additionalAttributesList.Add(string.Format(Constants.FIELD_XML_PARAMETER_FORMAT, keyvaluepair.Key, keyvaluepair.Value));
                }
            }

            string newFieldCAML = string.Format(Constants.FIELD_XML_FORMAT,
                fieldCreationInformation.FieldType,
                fieldCreationInformation.InternalName,
                fieldCreationInformation.DisplayName,
                fieldCreationInformation.Id,
                fieldCreationInformation.Group,
                fieldCreationInformation.Required ? "TRUE" : "FALSE",
                additionalAttributesList.Any() ? string.Join(" ", additionalAttributesList) : "");

            return newFieldCAML;
        }

        /// <summary>
        /// Adds a field to a list
        /// </summary>
        /// <param name="list">List to process</param>
        /// <param name="fieldAsXml">The XML declaration of SiteColumn definition</param>
        /// <param name="executeQuery">Optionally skip the executeQuery action</param>
        /// <returns>The newly created field or existing field.</returns>
        public static Field CreateField(this List list, string fieldAsXml, bool executeQuery = true)
        {
            var fields = list.Fields;
            list.Context.Load(fields);
            list.Context.ExecuteQueryRetry();

            var xd = XDocument.Parse(fieldAsXml);
            if (xd.Root != null)
            {
                var ns = xd.Root.Name.Namespace;

                var fieldNode = (from f in xd.Elements(ns + "Field") select f).FirstOrDefault();

                if (fieldNode != null)
                {
                    string id = string.Empty;
                    if (fieldNode.Attribute("ID") != null)
                    {
                        id = fieldNode.Attribute("ID").Value;
                    }
                    else
                    {
                        id = "<No ID specified in XML>";
                    }
                    var name = fieldNode.Attribute("Name").Value;

                    Log.Info(Constants.LOGGING_SOURCE, CoreResources.FieldAndContentTypeExtensions_CreateField01, name, id);
                }
            }
            var field = fields.AddFieldAsXml(fieldAsXml, false, AddFieldOptions.AddFieldInternalNameHint);
            list.Update();

            if (executeQuery)
            {
                list.Context.ExecuteQueryRetry();
            }

            return field;
        }

        /// <summary>
        /// Returns if the field is found
        /// </summary>
        /// <param name="list">List to process</param>
        /// <param name="fieldId">Guid of the field ID</param>
        /// <returns>True if the fields exists, false otherwise</returns>
        public static bool FieldExistsById(this List list, Guid fieldId)
        {
            var fields = list.Fields;
            var results = list.Context.LoadQuery(fields.Where(item => item.Id == fieldId));
            list.Context.ExecuteQueryRetry();

            return results.FirstOrDefault() != null;
        }

        /// <summary>
        /// Returns if the field is found, query based on the ID
        /// </summary>
        /// <param name="list">List to process</param>
        /// <param name="fieldId">String representation of the field ID (=guid)</param>
        /// <returns>True if the fields exists, false otherwise</returns>
        public static bool FieldExistsById(this List list, string fieldId)
        {
            if (string.IsNullOrEmpty(fieldId))
            {
                throw new ArgumentNullException("fieldId");
            }

            return FieldExistsById(list, new Guid(fieldId));
        }

        /// <summary>
        /// Field exists in list by name
        /// </summary>
        /// <param name="list">List to process</param>
        /// <param name="fieldName">Internal name of the field</param>
        /// <returns>True if the fields exists, false otherwise</returns>
        public static bool FieldExistsByName(this List list, string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentNullException("fieldName");
            }

            var fields = list.Fields;
            var results = list.Context.LoadQuery(fields.Where(item => item.InternalName == fieldName));
            list.Context.ExecuteQueryRetry();

            return results.FirstOrDefault() != null;
        }

        /// <summary>
        /// Gets a list of fields from a list by names.
        /// </summary>
        /// <param name="list">The target list containing the fields.</param>
        /// <param name="fieldInternalNames">List of field names to retreieve.</param>
        /// <returns>List of fields requested.</returns>
        public static IEnumerable<Field> GetFields(this List list, params string[] fieldInternalNames)
        {
            var fields = new List<Field>();

            if (fieldInternalNames == null || fieldInternalNames.Length == 0)
            {
                return fields;
            }

            foreach (var fieldName in fieldInternalNames)
            {
                var field = list.Fields.GetByInternalNameOrTitle(fieldName);
                list.Context.Load(field);
                fields.Add(field);
            }

            list.Context.ExecuteQueryRetry();
            return fields;
        }

        /// <summary>
        /// Adds jsLink to a list field.
        /// </summary>
        /// <param name="list">The list where the field exists.</param>
        /// <param name="fieldName">The field to add jsLink to.</param>
        /// <param name="jsLink">JSLink to set to the form. Set to empty string to remove the set JSLink customization.
        /// Specify multiple values separated by pipe symbol. For e.g.: ~sitecollection/_catalogs/masterpage/jquery-2.1.0.min.js|~sitecollection/_catalogs/masterpage/custom.js
        /// </param>
        public static void SetJsLinkCustomizations(this List list, string fieldName, string jsLink)
        {
            Field field = list.Fields.GetByInternalNameOrTitle(fieldName);
            field.SetJsLinkCustomizations(jsLink);
        }

        #endregion

        #region Helper methods

        /// <summary>
        /// Helper method to parse Key="Value" strings into a keyvaluepair
        /// </summary>
        /// <param name="xmlAttributes"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Xml.Linq.XElement.Parse(System.String)")]
        private static List<KeyValuePair<string, string>> ParseAdditionalAttributes(string xmlAttributes)
        {
            List<KeyValuePair<string, string>> attributes = null;

            // The XmlAttributes should be presented in the Key="Value" AnotherKey="Value" format.
            if (!string.IsNullOrEmpty(xmlAttributes))
            {
                attributes = new List<KeyValuePair<string, string>>();
                string parameterXml = string.Format(Constants.FIELD_XML_PARAMETER_WRAPPER_FORMAT, xmlAttributes); // Temporary xml structure
                XElement xe = XElement.Parse(parameterXml);

                foreach (var attribute in xe.Attributes())
                {
                    attributes.Add(new KeyValuePair<string, string>(attribute.Name.LocalName, attribute.Value));
                }
            }

            return attributes;
        }

        #endregion

        #region Content Types

        /// <summary>
        /// Adds content type to list
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="listTitle">Title of the list</param>
        /// <param name="contentTypeId">Complete ID for the content type</param>
        /// <param name="defaultContent">Optionally make this the default content type</param>
        /// <param name="searchContentTypeInSiteHierarchy">search for content type in site hierarchy</param>
        public static void AddContentTypeToListById(this Web web, string listTitle, string contentTypeId, bool defaultContent = false, bool searchContentTypeInSiteHierarchy = false)
        {
            // Get content type instance
            var contentType = GetContentTypeById(web, contentTypeId, searchContentTypeInSiteHierarchy);

            // Add content type to list
            AddContentTypeToList(web, listTitle, contentType, defaultContent);
        }

        /// <summary>
        /// Adds content type to list
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="listTitle">Title of the list</param>
        /// <param name="contentTypeName">Name of the content type</param>
        /// <param name="defaultContent">Optionally make this the default content type</param>
        /// <param name="searchContentTypeInSiteHierarchy">search for content type in site hierarchy</param>
        public static void AddContentTypeToListByName(this Web web, string listTitle, string contentTypeName, bool defaultContent = false, bool searchContentTypeInSiteHierarchy = false)
        {
            // Get content type instance
            var contentType = GetContentTypeByName(web, contentTypeName, searchContentTypeInSiteHierarchy);

            // Add content type to list
            AddContentTypeToList(web, listTitle, contentType, defaultContent);
        }

        /// <summary>
        /// Adds content type to list
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="listTitle">Title of the list</param>
        /// <param name="contentType">Content type to be added to the list</param>
        /// <param name="defaultContent">If set true, content type is updated to be default content type for the list</param>
        public static void AddContentTypeToList(this Web web, string listTitle, ContentType contentType, bool defaultContent = false)
        {
            // Get list instances
            var list = web.GetListByTitle(listTitle);

            // Add content type to list
            AddContentTypeToList(list, contentType, defaultContent);
        }

        /// <summary>
        /// Add content type to list
        /// </summary>
        /// <param name="list">List to add content type to</param>
        /// <param name="contentTypeID">Complete ID for the content type</param>
        /// <param name="defaultContent">If set true, content type is updated to be default content type for the list</param>
        /// <param name="searchContentTypeInSiteHierarchy">search for content type in site hierarchy</param>
        public static bool AddContentTypeToListById(this List list, string contentTypeID, bool defaultContent = false, bool searchContentTypeInSiteHierarchy = false)
        {
            var web = list.ParentWeb;
            var contentType = GetContentTypeById(web, contentTypeID, searchContentTypeInSiteHierarchy);
            return AddContentTypeToList(list, contentType, defaultContent);
        }

        /// <summary>
        /// Add content type to list
        /// </summary>
        /// <param name="list">List to add content type to</param>
        /// <param name="contentTypeName">Name of the content type</param>
        /// <param name="defaultContent">If set true, content type is updated to be default content type for the list</param>
        /// <param name="searchContentTypeInSiteHierarchy">search for content type in site hierarchy</param>
        public static bool AddContentTypeToListByName(this List list, string contentTypeName, bool defaultContent = false, bool searchContentTypeInSiteHierarchy = false)
        {
            var web = list.ParentWeb;
            var contentType = GetContentTypeByName(web, contentTypeName, searchContentTypeInSiteHierarchy);
            return AddContentTypeToList(list, contentType, defaultContent);
        }

        /// <summary>
        /// Add content type to list
        /// </summary>
        /// <param name="list">List to add content type to</param>
        /// <param name="contentType">Content type to add to the list</param>
        /// <param name="defaultContent">If set true, content type is updated to be default content type for the list</param>
        public static bool AddContentTypeToList(this List list, ContentType contentType, bool defaultContent = false)
        {
            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            if (list.ContentTypeExistsById(contentType.Id.StringValue))
            {
                return false;
            }

            list.EnsureProperty(l => l.ContentTypesEnabled);

            if (list.ContentTypesEnabled == false)
            {
                list.ContentTypesEnabled = true;
                list.Update();
                list.Context.ExecuteQueryRetry();
            }

            list.ContentTypes.AddExistingContentType(contentType);
            list.Context.ExecuteQueryRetry();

            // Set the default content type
            if (defaultContent)
            {
                SetDefaultContentTypeToList(list, contentType);
            }
            return true;
        }



        /// <summary>
        /// Associates field to content type
        /// </summary>
        /// <param name="contentType">Content Type to add the field to</param>
        /// <param name="fieldId">String representation of the id of the field (=Guid)</param>
        /// <param name="required">True if the field is required</param>
        /// <param name="hidden">True if the field is hidden</param>
        public static void AddFieldById(this ContentType contentType, string fieldId, bool required = false, bool hidden = false)
        {
            AddFieldById(contentType, Guid.Parse(fieldId), required, hidden);
        }

        /// <summary>
        /// Associates field to content type
        /// </summary>
        /// <param name="contentType">Content Type to add the field to</param>
        /// <param name="fieldId">The Id of the field</param>
        /// <param name="required">True if the field is required</param>
        /// <param name="hidden">True if the field is hidden</param>
        public static void AddFieldById(this ContentType contentType, Guid fieldId, bool required = false, bool hidden = false)
        {
            var ctx = contentType.Context as ClientContext;
            var field = ctx.Web.Fields.GetById(fieldId);
            ctx.Load(field);
            ctx.ExecuteQueryRetry();
            AddFieldToContentType(ctx.Web, contentType, field, required, hidden);
        }

        /// <summary>
        /// Associates field to content type
        /// </summary>
        /// <param name="contentType">Content Type to add the field to</param>
        /// <param name="fieldName">The title or internal name of the field</param>
        /// <param name="required">True if the field is required</param>
        /// <param name="hidden">True if the field is hidden</param>
        public static void AddFieldByName(this ContentType contentType, string fieldName, bool required = false, bool hidden = false)
        {
            var ctx = contentType.Context as ClientContext;
            var field = ctx.Web.Fields.GetByInternalNameOrTitle(fieldName);
            ctx.Load(field);
            ctx.ExecuteQueryRetry();

            AddFieldToContentType(ctx.Web, contentType, field, required, hidden);
        }

        /// <summary>
        /// Associates field to content type
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="contentTypeID">String representation of the id of the content type to add the field to</param>
        /// <param name="fieldId">String representation of the field ID (=guid)</param>
        /// <param name="required">True if the field is required</param>
        /// <param name="hidden">True if the field is hidden</param>
        public static void AddFieldToContentTypeById(this Web web, string contentTypeID, string fieldId, bool required = false, bool hidden = false)
        {
            // Get content type
            var ct = web.GetContentTypeById(contentTypeID);
            web.Context.Load(ct);
            web.Context.Load(ct.FieldLinks);
            web.Context.ExecuteQueryRetry();

            // Get field
            var fld = web.Fields.GetById(new Guid(fieldId));

            // Add field association to content type
            AddFieldToContentType(web, ct, fld, required, hidden);
        }

        /// <summary>
        /// Associates field to content type
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="contentTypeName">Name of the content type</param>
        /// <param name="fieldID">Guid representation of the field ID</param>
        /// <param name="required">True if the field is required</param>
        /// <param name="hidden">True if the field is hidden</param>
        public static void AddFieldToContentTypeByName(this Web web, string contentTypeName, Guid fieldID, bool required = false, bool hidden = false)
        {
            // Get content type
            var ct = web.GetContentTypeByName(contentTypeName);
            web.Context.Load(ct);
            web.Context.Load(ct.FieldLinks);
            web.Context.ExecuteQueryRetry();

            // Get field
            var fld = web.Fields.GetById(fieldID);

            // Add field association to content type
            AddFieldToContentType(web, ct, fld, required, hidden);
        }

        /// <summary>
        /// Associates field to content type
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="contentType">Content type to associate field to</param>
        /// <param name="field">Field to associate to the content type</param>
        /// <param name="required">Optionally make this a required field</param>
        /// <param name="hidden">Optionally make this a hidden field</param>
        public static void AddFieldToContentType(this Web web, ContentType contentType, Field field, bool required = false, bool hidden = false)
        {
            //// Forcibly include Ids of FieldLinks
            //web.Context.Load(contentType, c => c.FieldLinks.Include(fl => fl.Id, fl => fl.Required, fl => fl.Hidden));
            //web.Context.ExecuteQueryRetry();

            // Ensure other content-type properties
            contentType.EnsureProperties(c => c.Id, c => c.SchemaXml, c => c.FieldLinks.Include(fl => fl.Id, fl => fl.Required, fl => fl.Hidden));
            field.EnsureProperties(f => f.Id, f => f.SchemaXmlWithResourceTokens);

            Log.Info(Constants.LOGGING_SOURCE, CoreResources.FieldAndContentTypeExtensions_AddField0ToContentType1, field.Id, contentType.Id);

            // Get the field if already exists in content type, else add field to content type
            // This will help to customize (required or hidden) any pre-existing field, also to handle existing field of Parent Content type
            var flink = contentType.FieldLinks.FirstOrDefault(fld => fld.Id == field.Id);
            if (flink == null)
            {
                XElement fieldElement = XElement.Parse(field.SchemaXmlWithResourceTokens);
                fieldElement.SetAttributeValue("AllowDeletion", "TRUE"); // Default behavior when adding a field to a CT from the UI.
                field.SchemaXml = fieldElement.ToString();
                var fldInfo = new FieldLinkCreationInformation();
                fldInfo.Field = field;
                contentType.FieldLinks.Add(fldInfo);
                contentType.Update(true);
                web.Context.ExecuteQueryRetry();

                flink = contentType.FieldLinks.GetById(field.Id);
            }

            //update field link required and hidden properties
            flink.EnsureProperties(f => f.Required, f => f.Hidden);

            if ((required != flink.Required) || (hidden != flink.Hidden))
            {
                // Update FieldLink
                flink.Required = required;
                flink.Hidden = hidden;
                contentType.Update(true);
                web.Context.ExecuteQueryRetry();
            }
        }

        /// <summary>
        /// Searches the list content types and returns the content type identifier (ID) that is the 
        /// nearest match to the specified content type ID.
        /// </summary>
        /// <param name="list">The list to check for content types</param>
        /// <param name="baseContentTypeId">A string with the base content type ID to match.</param>
        /// <returns>The value of the Id property for the content type with the closest match to the value 
        /// of the specified content type ID. </returns>
        /// <remarks>
        /// <para>
        /// If the search finds multiple matches, the shorter ID is returned. For example, if 0x0101 is the 
        /// argument, and the collection contains both 0x010109 and 0x01010901, the method returns 0x010109.
        /// </para>
        /// </remarks>
        public static ContentTypeId BestMatchContentTypeId(this List list, string baseContentTypeId)
        {
            if (baseContentTypeId == null)
            {
                throw new ArgumentNullException("contentTypeId");
            }

            if (string.IsNullOrWhiteSpace(baseContentTypeId))
            {
                throw new ArgumentException("Content type must be provided and cannot be empty.", "contentTypeId");
            }

            return BestMatchContentTypeIdImplementation(list, baseContentTypeId);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "OfficeDevPnP.Core.Diagnostics.Log.Debug(System.String,System.String,System.Object[])")]
        private static ContentTypeId BestMatchContentTypeIdImplementation(this List list, string baseContentTypeId)
        {
            var contentTypes = list.ContentTypes;
            list.Context.Load(contentTypes);
            list.Context.ExecuteQueryRetry();

            Log.Debug(Constants.LOGGING_SOURCE, "Checking {0} content types in list for best match", contentTypes.Count);

            var shortestMatchLength = int.MaxValue;
            ContentTypeId bestMatchId = null;

            foreach (var contentType in contentTypes)
            {
                if (contentType.StringId.StartsWith(baseContentTypeId, StringComparison.InvariantCultureIgnoreCase))
                {
                    Log.Debug(Constants.LOGGING_SOURCE, "Found match {0}", contentType.StringId);
                    if (contentType.StringId.Length < shortestMatchLength)
                    {
                        bestMatchId = contentType.Id;
                        shortestMatchLength = contentType.StringId.Length;
                        Log.Debug(Constants.LOGGING_SOURCE, " - Is best match. Best match length now {0}", shortestMatchLength);
                    }
                }
            }

            return bestMatchId;
        }

        /// <summary>
        /// Does content type exists in the web
        /// </summary>
        /// <param name="web">Web to be processed</param>
        /// <param name="contentTypeId">Complete ID for the content type</param>
        /// <param name="searchInSiteHierarchy">Searches accross all content types in the site up to the root site</param>
        /// <returns>True if the content type exists, false otherwise</returns>
        public static bool ContentTypeExistsById(this Web web, string contentTypeId, bool searchInSiteHierarchy = false)
        {
            if (string.IsNullOrEmpty(contentTypeId))
            {
                throw new ArgumentNullException("contentTypeId");
            }

            ContentTypeCollection ctCol;
            if (searchInSiteHierarchy)
            {
                ctCol = web.AvailableContentTypes;
            }
            else
            {
                ctCol = web.ContentTypes;
            }

            web.Context.Load(ctCol);
            web.Context.ExecuteQueryRetry();
            foreach (var item in ctCol)
            {
                if (item.Id.StringValue.StartsWith(contentTypeId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Does content type exists in the web
        /// </summary>
        /// <param name="web">Web to be processed</param>
        /// <param name="contentTypeName">Name of the content type</param>
        /// <param name="searchInSiteHierarchy">Searches accross all content types in the site up to the root site</param>
        /// <returns>True if the content type exists, false otherwise</returns>
        public static bool ContentTypeExistsByName(this Web web, string contentTypeName, bool searchInSiteHierarchy = false)
        {
            if (string.IsNullOrEmpty(contentTypeName))
            {
                throw new ArgumentNullException("contentTypeName");
            }

            var ctCol = searchInSiteHierarchy ? web.AvailableContentTypes : web.ContentTypes;

            var results = web.Context.LoadQuery(ctCol.Where(item => item.Name == contentTypeName));
            web.Context.ExecuteQueryRetry();

            var ct = results.FirstOrDefault();
            return ct != null;
        }

        /// <summary>
        /// Does content type exist in web
        /// </summary>
        /// <param name="web">Web to be processed</param>
        /// <param name="listTitle">Title of the list to be updated</param>
        /// <param name="contentTypeId">Complete ID for the content type</param>
        /// <returns>True if the content type exists, false otherwise</returns>
        public static bool ContentTypeExistsById(this Web web, string listTitle, string contentTypeId)
        {
            if (string.IsNullOrEmpty(listTitle))
            {
                throw new ArgumentNullException("listTitle");
            }

            if (string.IsNullOrEmpty(contentTypeId))
            {
                throw new ArgumentNullException("contentTypeId");
            }

            var list = web.GetListByTitle(listTitle);
            return ContentTypeExistsById(list, contentTypeId);
        }

        /// <summary>
        /// Does content type exist in list
        /// </summary>
        /// <param name="list">List to update</param>
        /// <param name="contentTypeId">Complete ID for the content type</param>
        /// <returns>True if the content type exists, false otherwise</returns>
        public static bool ContentTypeExistsById(this List list, string contentTypeId)
        {
            if (string.IsNullOrEmpty(contentTypeId))
            {
                throw new ArgumentNullException("contentTypeId");
            }

            list.EnsureProperty(l => l.ContentTypesEnabled);

            if (!list.ContentTypesEnabled)
            {
                return false;
            }

            var ctCol = list.ContentTypes;
            list.Context.Load(ctCol, col => col.Include(ct => ct.Id, ct => ct.Parent));
            list.Context.ExecuteQueryRetry();

            return (ctCol.Any(item => item.Id.StringValue.Equals(contentTypeId, StringComparison.OrdinalIgnoreCase)
             || item.Parent.Id.StringValue.Equals(contentTypeId, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Does content type exist in web
        /// </summary>
        /// <param name="web">Web to be processed</param>
        /// <param name="listTitle">Title of the list to be updated</param>
        /// <param name="contentTypeName">Name of the content type</param>
        /// <returns>True if the content type exists, false otherwise</returns>
        public static bool ContentTypeExistsByName(this Web web, string listTitle, string contentTypeName)
        {
            if (string.IsNullOrEmpty(listTitle))
            {
                throw new ArgumentNullException("listTitle");
            }

            if (string.IsNullOrEmpty(contentTypeName))
            {
                throw new ArgumentNullException("contentTypeName");
            }

            var list = web.GetListByTitle(listTitle);
            return ContentTypeExistsByName(list, contentTypeName);
        }

        /// <summary>
        /// Does content type exist in list
        /// </summary>
        /// <param name="list">List to update</param>
        /// <param name="contentTypeName">Name of the content type</param>
        /// <returns>True if the content type exists, false otherwise</returns>
        public static bool ContentTypeExistsByName(this List list, string contentTypeName)
        {
            if (string.IsNullOrEmpty(contentTypeName))
            {
                throw new ArgumentNullException("contentTypeName");
            }

            var ctCol = list.ContentTypes;
            var results = list.Context.LoadQuery(ctCol.Where(item => item.Name == contentTypeName));
            list.Context.ExecuteQueryRetry();

            return results.Any();
        }

        /// <summary>
        /// Create a content type based on the classic feature framework structure.
        /// </summary>
        /// <param name="web">Web to operate against</param>
        /// <param name="absolutePathToFile">Absolute path to the xml location</param>
        public static ContentType CreateContentTypeFromXMLFile(this Web web, string absolutePathToFile)
        {
            var xd = XDocument.Load(absolutePathToFile);
            return CreateContentTypeFromXML(web, xd);
        }

        /// <summary>
        /// Create a content type based on the classic feature framework structure.
        /// </summary>
        /// <param name="web">Web to operate against</param>
        /// <param name="xmlStructure">XML structure in string format</param>
        public static ContentType CreateContentTypeFromXMLString(this Web web, string xmlStructure)
        {
            var xd = XDocument.Parse(xmlStructure);
            return CreateContentTypeFromXML(web, xd);
        }

        /// <summary>
        /// Create a content type based on the classic feature framework structure.
        /// </summary>
        /// <param name="web">Web to operate against</param>
        /// <param name="xDocument">Actual XML document</param>
        public static ContentType CreateContentTypeFromXML(this Web web, XDocument xDocument)
        {
            ContentType returnCT = null;
            var ns = xDocument.Root.Name.Namespace;

            var contentTypes = from cType in xDocument.Descendants(ns + "ContentType") select cType;

            foreach (var ct in contentTypes)
            {
                string ctid = ct.Attribute("ID").Value;
                string name = ct.Attribute("Name").Value;

                if (web.ContentTypeExistsByName(name))
                {
                    // Skip
                    Log.Warning(Constants.LOGGING_SOURCE, CoreResources.FieldAndContentTypeExtensions_ContentType01AlreadyExists, name, ctid);
                }
                else
                {
                    var description = ct.Attribute("Description") != null ? ct.Attribute("Description").Value : string.Empty;
                    var group = ct.Attribute("Group") != null ? ct.Attribute("Group").Value : string.Empty;

                    // Create CT
                    var newct = web.CreateContentType(name, description, ctid, group);

                    // Add fields to content type 
                    var fieldRefs = from fr in ct.Descendants(ns + "FieldRefs").Elements(ns + "FieldRef") select fr;
                    foreach (var fieldRef in fieldRefs)
                    {
                        var frid = fieldRef.Attribute("ID").Value;
                        var required = fieldRef.Attribute("Required") != null ? bool.Parse(fieldRef.Attribute("Required").Value) : false;
                        var hidden = fieldRef.Attribute("Hidden") != null ? bool.Parse(fieldRef.Attribute("Hidden").Value) : false;
                        web.AddFieldToContentTypeById(ctid, frid, required, hidden);
                    }

                    // Add AllowedContentTypes/SharedFields/WelcomePageFields
                    // Only for Document Sets of course
                    if (ctid.StartsWith(BuiltInContentTypeId.DocumentSet)) //DocumentSetTemplate.DocumentSetTemplate.IsChildOfDocumentSetContentType() appears not to be working
                    {
                        // Load Docset Template
                        var template = DocumentSetTemplate.GetDocumentSetTemplate(web.Context, newct);
                        web.Context.Load(template, t => t.AllowedContentTypes, t => t.SharedFields, t => t.WelcomePageFields);
                        web.Context.ExecuteQueryRetry();

                        // Add allowed content types
                        var allowedContentTypes = from ac in ct.Descendants(ns + "AllowedContentTypes").Elements(ns + "AllowedContentType") select ac;
                        foreach (var allowedContentType in allowedContentTypes)
                        {
                            var id = allowedContentType.Attribute("ID").Value;
                            var act = web.GetContentTypeById(id);
                            if (act != null &&
                                !template.AllowedContentTypes.Any(a => a.StringValue == id))
                            {
                                template.AllowedContentTypes.Add(act.Id);
                                template.Update(true);
                            }
                        }

                        // Add shared fields
                        var sharedFields = from sf in ct.Descendants(ns + "SharedFields").Elements(ns + "SharedField") select sf;
                        foreach (var sharedField in sharedFields)
                        {
                            var id = sharedField.Attribute("ID").Value;
                            var field = web.GetFieldById<Field>(new Guid(id));
                            if (field != null &&
                                !template.SharedFields.Any(a => a.Id == field.Id))
                            {
                                template.SharedFields.Add(field);
                                template.Update(true);
                            }
                        }

                        // Add WelcomePageFields fields
                        var welcomePageFields = from wpf in ct.Descendants(ns + "WelcomePageFields").Elements(ns + "WelcomePageField") select wpf;
                        foreach (var welcomePageField in welcomePageFields)
                        {
                            var id = welcomePageField.Attribute("ID").Value;
                            var field = web.GetFieldById<Field>(new Guid(id));
                            if (field != null &&
                                !template.WelcomePageFields.Any(a => a.Id == field.Id))
                            {
                                template.WelcomePageFields.Add(field);
                                template.Update(true);
                            }
                        }
                        
                        web.Context.ExecuteQueryRetry();
                    }

                    returnCT = web.GetContentTypeById(ctid);
                }
            }

            return returnCT;
        }

        /// <summary>
        /// Create new content type to web
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="name">Name of the content type</param>
        /// <param name="id">Complete ID for the content type</param>
        /// <param name="group">Group for the content type</param>
        /// <returns></returns>
        public static ContentType CreateContentType(this Web web, string name, string id, string group)
        {
            // Load the current collection of content types
            return CreateContentType(web, name, string.Empty, id, group);
        }

        /// <summary>
        /// Create new content type to web
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="name">Name of the content type</param>
        /// <param name="description">Description for the content type</param>
        /// <param name="id">Complete ID for the content type</param>
        /// <param name="group">Group for the content type</param>
        /// <param name="parentContentType">Parent Content Type</param>
        /// <returns>The created content type</returns>
        public static ContentType CreateContentType(this Web web, string name, string description, string id, string group, ContentType parentContentType = null)
        {
            Log.Info(Constants.LOGGING_SOURCE, CoreResources.FieldAndContentTypeExtensions_CreateContentType01, name, id);

            var contentTypes = web.ContentTypes;

            var newCt = new ContentTypeCreationInformation();


            // Set the properties for the content type
            newCt.Name = name;
            newCt.Id = id;
            newCt.Description = description;
            newCt.Group = group;
            newCt.ParentContentType = parentContentType;
            var myContentType = contentTypes.Add(newCt);
            web.Context.ExecuteQueryRetry();

            // Return the content type object
            return myContentType;
        }

        /// <summary>
        /// Deletes a content type from the web by name
        /// </summary>
        /// <param name="web">Web to delete the content type from</param>
        /// <param name="contentTypeName">Name of the content type to delete</param>
        public static void DeleteContentTypeByName(this Web web, string contentTypeName)
        {
            var contentTypes = web.Context.LoadQuery(web.ContentTypes.Where(c => c.Name == contentTypeName));
            web.Context.ExecuteQueryRetry();

            var enumerable = contentTypes as ContentType[] ?? contentTypes.ToArray();
            if (!enumerable.Any())
            {
                Log.Warning(Constants.LOGGING_SOURCE, CoreResources.FieldAndContentTypeExtensions_DeleteContentTypeByName, contentTypeName);
            }
            else
            {
                enumerable.First().DeleteObject();
                web.Context.ExecuteQueryRetry();
            }
        }

        /// <summary>
        /// Deletes a content type from the web by id
        /// </summary>
        /// <param name="web">Web to delete the content type from</param>
        /// <param name="contentTypeId">Id of the content type to delete</param>
        public static void DeleteContentTypeById(this Web web, string contentTypeId)
        {
            var contentType = GetContentTypeById(web, contentTypeId);
            if (contentType == null)
            {
                Log.Warning(Constants.LOGGING_SOURCE, CoreResources.FieldAndContentTypeExtensions_DeleteContentTypeById, contentTypeId);
            }
            else
            {
                contentType.DeleteObject();
                web.Context.ExecuteQueryRetry();
            }
        }

        /// <summary>
        /// Return content type by name
        /// </summary>
        /// <param name="web">Web to be processed</param>
        /// <param name="contentTypeName">Name of the content type</param>
        /// <param name="searchInSiteHierarchy">Searches accross all content types in the site up to the root site</param>
        /// <returns>Content type object or null if was not found</returns>
        public static ContentType GetContentTypeByName(this Web web, string contentTypeName, bool searchInSiteHierarchy = false)
        {
            if (string.IsNullOrEmpty(contentTypeName))
            {
                throw new ArgumentNullException("contentTypeName");
            }

            var ctCol = searchInSiteHierarchy ? web.AvailableContentTypes : web.ContentTypes;

            var results = web.Context.LoadQuery(ctCol.Where(item => item.Name == contentTypeName));
            web.Context.ExecuteQueryRetry();
            return results.FirstOrDefault();
        }

        /// <summary>
        /// Return content type by Id
        /// </summary>
        /// <param name="web">Web to be processed</param>
        /// <param name="contentTypeId">Complete ID for the content type</param>
        /// <param name="searchInSiteHierarchy">Searches accross all content types in the site up to the root site</param>
        /// <returns>Content type object or null if was not found</returns>
        public static ContentType GetContentTypeById(this Web web, string contentTypeId, bool searchInSiteHierarchy = false)
        {
            if (string.IsNullOrEmpty(contentTypeId))
            {
                throw new ArgumentNullException("contentTypeId");
            }

            var ctCol = searchInSiteHierarchy ? web.AvailableContentTypes : web.ContentTypes;

            web.Context.Load(ctCol);
            web.Context.ExecuteQueryRetry();
            foreach (var item in ctCol)
            {
                if (item.Id.StringValue.Equals(contentTypeId, StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }

            return null;
        }

        /// <summary>
        /// Return content type by name
        /// </summary>
        /// <param name="list">List to update</param>
        /// <param name="contentTypeName">Name of the content type</param>
        /// <returns>Content type object or null if was not found</returns>
        public static ContentType GetContentTypeByName(this List list, string contentTypeName)
        {
            if (string.IsNullOrEmpty(contentTypeName))
            {
                throw new ArgumentNullException("contentTypeName");
            }

            var ctCol = list.ContentTypes;
            var results = list.Context.LoadQuery(ctCol.Where(item => item.Name == contentTypeName));
            list.Context.ExecuteQueryRetry();

            return results.FirstOrDefault();
        }

        /// <summary>
        /// Return content type by Id
        /// </summary>
        /// <param name="list">List to update</param>
        /// <param name="contentTypeId">Complete ID for the content type</param>
        /// <returns>Content type object or null if was not found</returns>
        public static ContentType GetContentTypeById(this List list, string contentTypeId)
        {
            if (string.IsNullOrEmpty(contentTypeId))
            {
                throw new ArgumentNullException("contentTypeId");
            }

            var ctCol = list.ContentTypes;
            list.Context.Load(ctCol);
            list.Context.ExecuteQueryRetry();

            foreach (var item in ctCol)
            {
                if (item.Id.StringValue.Equals(contentTypeId, StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }

            return null;
        }

        /// <summary>
        /// Searches for the content type with the closest match to the value of the specified content type ID. 
        /// If the search finds two matches, the shorter ID is returned. 
        /// </summary>
        /// <param name="contentTypes">Content type collection to search</param>
        /// <param name="contentTypeId">Complete ID for the content type to search</param>
        /// <returns>Content type Id object or null if was not found</returns>
        public static ContentTypeId BestMatch(this ContentTypeCollection contentTypes, string contentTypeId)
        {
            if (string.IsNullOrEmpty(contentTypeId))
            {
                throw new ArgumentNullException("contentTypeId");
            }
            var ctx = contentTypes.Context;
            contentTypes.EnsureProperties(c => c.Include(ct => ct.Id));

            var res = contentTypes.Where(c => c.Id.StringValue.StartsWith(contentTypeId)).OrderBy(c => c.Id.StringValue.Length).FirstOrDefault();
            if (res != null)
            {
                return res.Id;
            }
            return null;
        }

        /// <summary>
        /// Removes content type from list
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="listTitle">Title of the list</param>
        /// <param name="contentTypeName">The name of the content type</param>
        public static void RemoveContentTypeFromListByName(this Web web, string listTitle, string contentTypeName)
        {

            // Get list instances
            var list = web.GetListByTitle(listTitle);
            // Get content type instance
            var contentType = GetContentTypeByName(web, contentTypeName,true);
            // Remove content type from list
            RemoveContentTypeFromList(web, list, contentType);

        }

        /// <summary>
        /// Removes content type from list
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="list">The List</param>
        /// <param name="contentTypeName">The name of the content type</param>
        public static void RemoveContentTypeFromListByName(this Web web, List list, string contentTypeName)
        {
            if (string.IsNullOrEmpty(contentTypeName))
                throw new ArgumentNullException("contentTypeName");
            // Get content type instance
            var contentType = GetContentTypeByName(web, contentTypeName,true);
            // Remove content type from list
            RemoveContentTypeFromList(web, list, contentType);

        }

        /// <summary>
        /// Removes content type from a list
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="listTitle">Title of the list</param>
        /// <param name="contentTypeId">Complete ID for the content type</param>
        public static void RemoveContentTypeFromListById(this Web web, string listTitle, string contentTypeId)
        {
            // Get list instances
            var list = web.GetListByTitle(listTitle);
            var contentType = GetContentTypeById(web, contentTypeId,true);
            // Remove content type from list
            RemoveContentTypeFromList(web, list, contentType);
        }

        /// <summary>
        /// Removes content type from a list
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="list">The List</param>
        /// <param name="contentTypeId">Complete ID for the content type</param>
        public static void RemoveContentTypeFromListById(this Web web, List list, string contentTypeId)
        {
            if (string.IsNullOrEmpty(contentTypeId))
                throw new ArgumentNullException("contentTypeId");
            var contentType = GetContentTypeById(web, contentTypeId,true);
            // Remove content type from list
            RemoveContentTypeFromList(web, list, contentType);
        }

        /// <summary>
        /// Removes content type from a list
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="list">The List</param>
        /// <param name="contentType">The Content Type</param>
        public static void RemoveContentTypeFromList(this Web web, List list, ContentType contentType)
        {
            if (contentType == null)
                throw new ArgumentNullException("contentType");

            if (!list.ContentTypeExistsByName(contentType.Name))
                return;
            list.RemoveContentTypeByName(contentType.Name);
            list.Context.ExecuteQueryRetry();
        }

        /// <summary>
        /// Set default content type to list
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="list">List to update</param>
        /// <param name="contentTypeId">Complete ID for the content type</param>
        public static void SetDefaultContentTypeToList(this Web web, List list, string contentTypeId)
        {
            SetDefaultContentTypeToList(list, contentTypeId);
        }

        /// <summary>
        /// Set default content type to list
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="list">List to update</param>
        /// <param name="contentType">Content type to make default</param>
        public static void SetDefaultContentTypeToList(this Web web, List list, ContentType contentType)
        {
            SetDefaultContentTypeToList(list, contentType.Id.ToString());
        }

        /// <summary>
        /// Set default content type to list
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="listTitle">Title of the list to be updated</param>
        /// <param name="contentTypeId">Complete ID for the content type</param>
        public static void SetDefaultContentTypeToList(this Web web, string listTitle, string contentTypeId)
        {
            // Get list instances
            var list = web.GetListByTitle(listTitle);
            web.Context.Load(list);
            web.Context.ExecuteQueryRetry();

            // Add content type to list
            SetDefaultContentTypeToList(list, contentTypeId);
        }

        /// <summary>
        /// Set's default content type list. 
        /// </summary>
        /// <remarks>Notice. Currently removes other content types from the list. Known issue</remarks>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="listTitle">Title of the list to be updated</param>
        /// <param name="contentType">Content type to make default</param>
        public static void SetDefaultContentTypeToList(this Web web, string listTitle, ContentType contentType)
        {
            SetDefaultContentTypeToList(web, listTitle, contentType.Id.ToString());
        }

        /// <summary>
        /// Set's default content type list. 
        /// </summary>
        /// <remarks>Notice. Currently removes other content types from the list. Known issue</remarks>
        /// <param name="list">List to update</param>
        /// <param name="contentTypeId">Complete ID for the content type</param>
        public static void SetDefaultContentTypeToList(this List list, string contentTypeId)
        {
            var ctCol = list.ContentTypes;
            list.Context.Load(ctCol);
            list.Context.ExecuteQueryRetry();

            var ctIds = ctCol.AsEnumerable().Select(ct => ct.Id).ToList();

            // remove the folder content type
            var newOrder = ctIds.Except(ctIds.Where(id => id.StringValue.StartsWith("0x012000")))
                                 .OrderBy(x => !x.StringValue.StartsWith(contentTypeId, StringComparison.OrdinalIgnoreCase))
                                 .ToArray();
            list.RootFolder.UniqueContentTypeOrder = newOrder;

            list.RootFolder.Update();
            list.Update();
            list.Context.ExecuteQueryRetry();
        }

        /// <summary>
        /// Set default content type to list
        /// </summary>
        /// <param name="list">List to update</param>
        /// <param name="contentType">Content type to make default</param>
        public static void SetDefaultContentTypeToList(this List list, ContentType contentType)
        {
            SetDefaultContentTypeToList(list, contentType.Id.ToString());
        }

        /// <summary>
        /// Reorders content types on the list. The first one in the list is the default item.
        /// Any items left out from the list will still be on the content type, but will not be visible on the new button.
        /// </summary>
        /// <param name="list">Target list containing the content types</param>
        /// <param name="contentTypeNamesOrIds">Content type names or ids to sort.</param>
        public static void ReorderContentTypes(this List list, IEnumerable<string> contentTypeNamesOrIds)
        {
            var listContentTypes = list.ContentTypes;
            list.Context.Load(listContentTypes);
            list.Context.ExecuteQueryRetry();
            IList<ContentTypeId> newOrder = new List<ContentTypeId>();

            // Casting throws "Specified method is not supported" when using in v15
            // var ctCol = listContentTypes.Cast<ContentType>().ToList();
            List<ContentType> ctCol = new List<ContentType>();
            foreach (ContentType ct in listContentTypes)
            {
                ctCol.Add(ct);
            }

            foreach (var ctypeName in contentTypeNamesOrIds)
            {
                var ctype = ctCol.Find(ct => ctypeName.Equals(ct.Name, StringComparison.OrdinalIgnoreCase) || ct.StringId.StartsWith(ctypeName));
                if (ctype != null)
                    newOrder.Add(ctype.Id);
            }

            list.RootFolder.UniqueContentTypeOrder = newOrder;
            list.RootFolder.Update();
            list.Update();
            list.Context.ExecuteQueryRetry();
        }

        #endregion

#if !ONPREMISES

        #region Localization

        /// <summary>
        /// Set localized labels for content type
        /// </summary>
        /// <param name="web">Web to operate on</param>
        /// <param name="contentTypeName">Name of the content type</param>
        /// <param name="cultureName">Culture for the localization (en-es, nl-be, fi-fi,...)</param>
        /// <param name="nameResource">Localized value for the Name property</param>
        /// <param name="descriptionResource">Localized value for the Description property</param>
        public static void SetLocalizationForContentType(this Web web, string contentTypeName, string cultureName, string nameResource, string descriptionResource)
        {
            var contentType = web.GetContentTypeByName(contentTypeName);
            contentType.SetLocalizationForContentType(cultureName, nameResource, descriptionResource);
        }

        /// <summary>
        /// Set localized labels for content type
        /// </summary>
        /// <param name="list">List to update</param>
        /// <param name="contentTypeId">Complete ID for the content type</param>
        /// <param name="cultureName">Culture for the localization (en-es, nl-be, fi-fi,...)</param>
        /// <param name="nameResource">Localized value for the Name property</param>
        /// <param name="descriptionResource">Localized value for the Description property</param>
        public static void SetLocalizationForContentType(this List list, string contentTypeId, string cultureName, string nameResource, string descriptionResource)
        {
            var contentTypes = list.ContentTypes;
            list.Context.Load(contentTypes);
            list.Context.ExecuteQueryRetry();

            var contentType = contentTypes.GetById(contentTypeId);
            list.Context.ExecuteQueryRetry();

            contentType.SetLocalizationForContentType(cultureName, nameResource, descriptionResource);
        }

        /// <summary>
        /// Set localized labels for content type
        /// </summary>
        /// <param name="contentType">Name of the content type</param>
        /// <param name="cultureName">Culture for the localization (en-es, nl-be, fi-fi,...)</param>
        /// <param name="nameResource">Localized value for the Name property</param>
        /// <param name="descriptionResource">Localized value for the Description property</param>
        public static void SetLocalizationForContentType(this ContentType contentType, string cultureName, string nameResource, string descriptionResource)
        {
            if (!contentType.IsObjectPropertyInstantiated("NameResource"))
            {
                contentType.Context.Load(contentType);
                contentType.Context.ExecuteQueryRetry();
            }

            // Set translations for the culture
            contentType.NameResource.SetValueForUICulture(cultureName, nameResource);
            contentType.DescriptionResource.SetValueForUICulture(cultureName, descriptionResource);
            contentType.Update(true);
            contentType.Context.ExecuteQueryRetry();
        }

        /// <summary>
        /// Set localized labels for field
        /// </summary>
        /// <param name="web">Web to operate on</param>
        /// <param name="siteColumnId">Guid with the site column ID</param>
        /// <param name="cultureName">Culture for the localization (en-es, nl-be, fi-fi,...)</param>
        /// <param name="titleResource">Localized value for the Title property</param>
        /// <param name="descriptionResource">Localized value for the Description property</param>
        public static void SetLocalizationForField(this Web web, Guid siteColumnId, string cultureName, string titleResource, string descriptionResource)
        {
            var fields = web.Fields;
            var fld = fields.GetById(siteColumnId);
            SetLocalizationForField(fld, cultureName, titleResource, descriptionResource);
        }

        /// <summary>
        /// Set localized labels for field
        /// </summary>
        /// <param name="web">Web to operate on</param>
        /// <param name="siteColumnName">Name of the site column</param>
        /// <param name="cultureName">Culture for the localization (en-es, nl-be, fi-fi,...)</param>
        /// <param name="titleResource">Localized value for the Title property</param>
        /// <param name="descriptionResource">Localized value for the Description property</param>
        public static void SetLocalizationForField(this Web web, string siteColumnName, string cultureName, string titleResource, string descriptionResource)
        {
            var fields = web.Fields;
            var fld = fields.GetByInternalNameOrTitle(siteColumnName);
            SetLocalizationForField(fld, cultureName, titleResource, descriptionResource);
        }

        /// <summary>
        /// Set localized labels for field
        /// </summary>
        /// <param name="web">Web to operate on</param>
        /// <param name="siteColumn">Site column to localize</param>
        /// <param name="cultureName">Culture for the localization (en-es, nl-be, fi-fi,...)</param>
        /// <param name="titleResource">Localized value for the Title property</param>
        /// <param name="descriptionResource">Localized value for the Description property</param>
        public static void SetLocalizationForField(this Web web, Field siteColumn, string cultureName, string titleResource, string descriptionResource)
        {
            SetLocalizationForField(siteColumn, cultureName, titleResource, descriptionResource);
        }

        /// <summary>
        /// Set localized labels for field
        /// </summary>
        /// <param name="list">List to update</param>
        /// <param name="siteColumnId">Guid of the site column ID</param>
        /// <param name="cultureName">Culture for the localization (en-es, nl-be, fi-fi,...)</param>
        /// <param name="titleResource">Localized value for the Title property</param>
        /// <param name="descriptionResource">Localized value for the Description property</param>
        public static void SetLocalizationForField(this List list, Guid siteColumnId, string cultureName, string titleResource, string descriptionResource)
        {
            var fields = list.Fields;
            var fld = fields.GetById(siteColumnId);
            SetLocalizationForField(fld, cultureName, titleResource, descriptionResource);
        }

        /// <summary>
        /// Set localized labels for field
        /// </summary>
        /// <param name="list">List to update</param>
        /// <param name="siteColumnName">Name of the site column</param>
        /// <param name="cultureName">Culture for the localization (en-es, nl-be, fi-fi,...)</param>
        /// <param name="titleResource">Localized value for the Title property</param>
        /// <param name="descriptionResource">Localized value for the Description property</param>
        public static void SetLocalizationForField(this List list, string siteColumnName, string cultureName, string titleResource, string descriptionResource)
        {
            var fields = list.Fields;
            var fld = fields.GetByInternalNameOrTitle(siteColumnName);
            SetLocalizationForField(fld, cultureName, titleResource, descriptionResource);
        }

        /// <summary>
        /// Set localized labels for field
        /// </summary>
        /// <param name="list">List to update</param>
        /// <param name="siteColumn">Site column to update</param>
        /// <param name="cultureName">Culture for the localization (en-es, nl-be, fi-fi,...)</param>
        /// <param name="titleResource">Localized value for the Title property</param>
        /// <param name="descriptionResource">Localized value for the Description property</param>
        public static void SetLocalizationForField(this List list, Field siteColumn, string cultureName, string titleResource, string descriptionResource)
        {
            SetLocalizationForField(siteColumn, cultureName, titleResource, descriptionResource);
        }

        /// <summary>
        /// Set localized labels for field
        /// </summary>
        /// <param name="field">Field to update</param>
        /// <param name="cultureName">Culture for the localization (en-es, nl-be, fi-fi,...)</param>
        /// <param name="titleResource">Localized value for the Title property</param>
        /// <param name="descriptionResource">Localized value for the Description property</param>
        public static void SetLocalizationForField(this Field field, string cultureName, string titleResource, string descriptionResource)
        {
            if (string.IsNullOrEmpty(cultureName))
            {
                throw new ArgumentNullException("cultureName");
            }

            if (string.IsNullOrEmpty(titleResource))
            {
                throw new ArgumentNullException("titleResource");
            }

            if (field.IsObjectPropertyInstantiated("TitleResource"))
            {
                field.Context.Load(field);
                field.Context.ExecuteQueryRetry();
            }

            // Set translations for the culture
            field.TitleResource.SetValueForUICulture(cultureName, titleResource);
            field.DescriptionResource.SetValueForUICulture(cultureName, descriptionResource);
            field.UpdateAndPushChanges(true);
            field.Context.ExecuteQueryRetry();
        }

        #endregion

#endif
    }
}
