﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.SharePoint.Client.Publishing;
using Microsoft.SharePoint.Client.Utilities;
using Microsoft.SharePoint.Client.WebParts;
using OfficeDevPnP.Core;
using OfficeDevPnP.Core.Entities;
using System.Linq;
using System.Net;
using System.IO;
using System.Text;
using OfficeDevPnP.Core.Utilities;
using Microsoft.SharePoint.Client.Publishing.Navigation;
using Microsoft.SharePoint.Client.Taxonomy;
using OfficeDevPnP.Core.Utilities.WebParts;

namespace Microsoft.SharePoint.Client
{
    /// <summary>
    /// Class that handles all page and web part related operations
    /// </summary>
    public static partial class PageExtensions
    {
        private const string WikiPage_OneColumn = @"<div class=""ExternalClassC1FD57BEDB8942DC99A06C02F9A98241""><table id=""layoutsTable"" style=""width&#58;100%;""><tbody><tr style=""vertical-align&#58;top;""><td style=""width&#58;100%;""><div class=""ms-rte-layoutszone-outer"" style=""width&#58;100%;""><div class=""ms-rte-layoutszone-inner"" role=""textbox"" aria-haspopup=""true"" aria-autocomplete=""both"" aria-multiline=""true""></div>&#160;</div></td></tr></tbody></table><span id=""layoutsData"" style=""display&#58;none;"">false,false,1</span></div>";
        private const string WikiPage_OneColumnSideBar = @"<div class=""ExternalClass47565ACDF7974263AA4A556DA974B687""><table id=""layoutsTable"" style=""width&#58;100%;""><tbody><tr style=""vertical-align&#58;top;""><td style=""width&#58;66.6%;""><div class=""ms-rte-layoutszone-outer"" style=""width&#58;100%;""><div class=""ms-rte-layoutszone-inner"" role=""textbox"" aria-haspopup=""true"" aria-autocomplete=""both"" aria-multiline=""true""></div>&#160;</div></td><td class=""ms-wiki-columnSpacing"" style=""width&#58;33.3%;""><div class=""ms-rte-layoutszone-outer"" style=""width&#58;100%;""><div class=""ms-rte-layoutszone-inner"" role=""textbox"" aria-haspopup=""true"" aria-autocomplete=""both"" aria-multiline=""true""></div>&#160;</div></td></tr></tbody></table><span id=""layoutsData"" style=""display&#58;none;"">false,false,2</span></div>";
        private const string WikiPage_TwoColumns = @"<div class=""ExternalClass3811C839E5984CCEA4C8CF738462AFD8""><table id=""layoutsTable"" style=""width&#58;100%;""><tbody><tr style=""vertical-align&#58;top;""><td style=""width&#58;49.95%;""><div class=""ms-rte-layoutszone-outer"" style=""width&#58;100%;""><div class=""ms-rte-layoutszone-inner"" role=""textbox"" aria-haspopup=""true"" aria-autocomplete=""both"" aria-multiline=""true""></div>&#160;</div></td><td class=""ms-wiki-columnSpacing"" style=""width&#58;49.95%;""><div class=""ms-rte-layoutszone-outer"" style=""width&#58;100%;""><div class=""ms-rte-layoutszone-inner"" role=""textbox"" aria-haspopup=""true"" aria-autocomplete=""both"" aria-multiline=""true""></div>&#160;</div></td></tr></tbody></table><span id=""layoutsData"" style=""display&#58;none;"">false,false,2</span></div>";
        private const string WikiPage_TwoColumnsHeader = @"<div class=""ExternalClass850251EB51394304A07A64A05C0BB0F1""><table id=""layoutsTable"" style=""width&#58;100%;""><tbody><tr style=""vertical-align&#58;top;""><td colspan=""2""><div class=""ms-rte-layoutszone-outer"" style=""width&#58;100%;""><div class=""ms-rte-layoutszone-inner"" role=""textbox"" aria-haspopup=""true"" aria-autocomplete=""both"" aria-multiline=""true""></div>&#160;</div></td></tr><tr style=""vertical-align&#58;top;""><td style=""width&#58;49.95%;""><div class=""ms-rte-layoutszone-outer"" style=""width&#58;100%;""><div class=""ms-rte-layoutszone-inner"" role=""textbox"" aria-haspopup=""true"" aria-autocomplete=""both"" aria-multiline=""true""></div>&#160;</div></td><td class=""ms-wiki-columnSpacing"" style=""width&#58;49.95%;""><div class=""ms-rte-layoutszone-outer"" style=""width&#58;100%;""><div class=""ms-rte-layoutszone-inner"" role=""textbox"" aria-haspopup=""true"" aria-autocomplete=""both"" aria-multiline=""true""></div>&#160;</div></td></tr></tbody></table><span id=""layoutsData"" style=""display&#58;none;"">true,false,2</span></div>";
        private const string WikiPage_TwoColumnsHeaderFooter = @"<div class=""ExternalClass71C5527252AD45859FA774445D4909A2""><table id=""layoutsTable"" style=""width&#58;100%;""><tbody><tr style=""vertical-align&#58;top;""><td colspan=""2""><div class=""ms-rte-layoutszone-outer"" style=""width&#58;100%;""><div class=""ms-rte-layoutszone-inner"" role=""textbox"" aria-haspopup=""true"" aria-autocomplete=""both"" aria-multiline=""true""></div>&#160;</div></td></tr><tr style=""vertical-align&#58;top;""><td style=""width&#58;49.95%;""><div class=""ms-rte-layoutszone-outer"" style=""width&#58;100%;""><div class=""ms-rte-layoutszone-inner"" role=""textbox"" aria-haspopup=""true"" aria-autocomplete=""both"" aria-multiline=""true""></div>&#160;</div></td><td class=""ms-wiki-columnSpacing"" style=""width&#58;49.95%;""><div class=""ms-rte-layoutszone-outer"" style=""width&#58;100%;""><div class=""ms-rte-layoutszone-inner"" role=""textbox"" aria-haspopup=""true"" aria-autocomplete=""both"" aria-multiline=""true""></div>&#160;</div></td></tr><tr style=""vertical-align&#58;top;""><td colspan=""2""><div class=""ms-rte-layoutszone-outer"" style=""width&#58;100%;""><div class=""ms-rte-layoutszone-inner"" role=""textbox"" aria-haspopup=""true"" aria-autocomplete=""both"" aria-multiline=""true""></div>&#160;</div></td></tr></tbody></table><span id=""layoutsData"" style=""display&#58;none;"">true,true,2</span></div>";
        private const string WikiPage_ThreeColumns = @"<div class=""ExternalClass833D1FA704C94892A26C4069C3FE5FE9""><table id=""layoutsTable"" style=""width&#58;100%;""><tbody><tr style=""vertical-align&#58;top;""><td style=""width&#58;33.3%;""><div class=""ms-rte-layoutszone-outer"" style=""width&#58;100%;""><div class=""ms-rte-layoutszone-inner"" role=""textbox"" aria-haspopup=""true"" aria-autocomplete=""both"" aria-multiline=""true""></div>&#160;</div></td><td class=""ms-wiki-columnSpacing"" style=""width&#58;33.3%;""><div class=""ms-rte-layoutszone-outer"" style=""width&#58;100%;""><div class=""ms-rte-layoutszone-inner"" role=""textbox"" aria-haspopup=""true"" aria-autocomplete=""both"" aria-multiline=""true""></div>&#160;</div></td><td class=""ms-wiki-columnSpacing"" style=""width&#58;33.3%;""><div class=""ms-rte-layoutszone-outer"" style=""width&#58;100%;""><div class=""ms-rte-layoutszone-inner"" role=""textbox"" aria-haspopup=""true"" aria-autocomplete=""both"" aria-multiline=""true""></div>&#160;</div></td></tr></tbody></table><span id=""layoutsData"" style=""display&#58;none;"">false,false,3</span></div>";
        private const string WikiPage_ThreeColumnsHeader = @"<div class=""ExternalClassD1A150D6187F449B8A6C4BEA2D4913BB""><table id=""layoutsTable"" style=""width&#58;100%;""><tbody><tr style=""vertical-align&#58;top;""><td colspan=""3""><div class=""ms-rte-layoutszone-outer"" style=""width&#58;100%;""><div class=""ms-rte-layoutszone-inner"" role=""textbox"" aria-haspopup=""true"" aria-autocomplete=""both"" aria-multiline=""true""></div>&#160;</div></td></tr><tr style=""vertical-align&#58;top;""><td style=""width&#58;33.3%;""><div class=""ms-rte-layoutszone-outer"" style=""width&#58;100%;""><div class=""ms-rte-layoutszone-inner"" role=""textbox"" aria-haspopup=""true"" aria-autocomplete=""both"" aria-multiline=""true""></div>&#160;</div></td><td class=""ms-wiki-columnSpacing"" style=""width&#58;33.3%;""><div class=""ms-rte-layoutszone-outer"" style=""width&#58;100%;""><div class=""ms-rte-layoutszone-inner"" role=""textbox"" aria-haspopup=""true"" aria-autocomplete=""both"" aria-multiline=""true""></div>&#160;</div></td><td class=""ms-wiki-columnSpacing"" style=""width&#58;33.3%;""><div class=""ms-rte-layoutszone-outer"" style=""width&#58;100%;""><div class=""ms-rte-layoutszone-inner"" role=""textbox"" aria-haspopup=""true"" aria-autocomplete=""both"" aria-multiline=""true""></div>&#160;</div></td></tr></tbody></table><span id=""layoutsData"" style=""display&#58;none;"">true,false,3</span></div>";
        private const string WikiPage_ThreeColumnsHeaderFooter = @"<div class=""ExternalClass5849C2C61FEC44E9B249C60F7B0ACA38""><table id=""layoutsTable"" style=""width&#58;100%;""><tbody><tr style=""vertical-align&#58;top;""><td colspan=""3""><div class=""ms-rte-layoutszone-outer"" style=""width&#58;100%;""><div class=""ms-rte-layoutszone-inner"" role=""textbox"" aria-haspopup=""true"" aria-autocomplete=""both"" aria-multiline=""true""></div>&#160;</div></td></tr><tr style=""vertical-align&#58;top;""><td style=""width&#58;33.3%;""><div class=""ms-rte-layoutszone-outer"" style=""width&#58;100%;""><div class=""ms-rte-layoutszone-inner"" role=""textbox"" aria-haspopup=""true"" aria-autocomplete=""both"" aria-multiline=""true""></div>&#160;</div></td><td class=""ms-wiki-columnSpacing"" style=""width&#58;33.3%;""><div class=""ms-rte-layoutszone-outer"" style=""width&#58;100%;""><div class=""ms-rte-layoutszone-inner"" role=""textbox"" aria-haspopup=""true"" aria-autocomplete=""both"" aria-multiline=""true""></div>&#160;</div></td><td class=""ms-wiki-columnSpacing"" style=""width&#58;33.3%;""><div class=""ms-rte-layoutszone-outer"" style=""width&#58;100%;""><div class=""ms-rte-layoutszone-inner"" role=""textbox"" aria-haspopup=""true"" aria-autocomplete=""both"" aria-multiline=""true""></div>&#160;</div></td></tr><tr style=""vertical-align&#58;top;""><td colspan=""3""><div class=""ms-rte-layoutszone-outer"" style=""width&#58;100%;""><div class=""ms-rte-layoutszone-inner"" role=""textbox"" aria-haspopup=""true"" aria-autocomplete=""both"" aria-multiline=""true""></div>&#160;</div></td></tr></tbody></table><span id=""layoutsData"" style=""display&#58;none;"">true,true,3</span></div>";


        /// <summary>
        /// Returns the HTML contents of a wiki page
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="serverRelativePageUrl">Server relative url of the page, e.g. /sites/demo/SitePages/Test.aspx</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown when serverRelativePageUrl is a zero-length string or contains only white space</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when serverRelativePageUrl is null</exception>
        public static string GetWikiPageContent(this Web web, string serverRelativePageUrl)
        {
            if (string.IsNullOrEmpty(serverRelativePageUrl))
            {
                throw (serverRelativePageUrl == null)
                  ? new ArgumentNullException("serverRelativePageUrl")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "serverRelativePageUrl");
            }

            File file = web.GetFileByServerRelativeUrl(serverRelativePageUrl);

            web.Context.Load(file, f => f.ListItemAllFields);

            web.Context.ExecuteQueryRetry();

            return file.ListItemAllFields["WikiField"] as string;
        }

        /// <summary>
        /// List the web parts on a page
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="serverRelativePageUrl">Server relative url of the page containing the webparts</param>
        /// <exception cref="System.ArgumentException">Thrown when serverRelativePageUrl is a zero-length string or contains only white space</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when serverRelativePageUrl is null</exception>
        public static IEnumerable<WebPartDefinition> GetWebParts(this Web web, string serverRelativePageUrl)
        {
            if (string.IsNullOrEmpty(serverRelativePageUrl))
            {
                throw (serverRelativePageUrl == null)
                  ? new ArgumentNullException("serverRelativePageUrl")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "serverRelativePageUrl");
            }

            File file = web.GetFileByServerRelativeUrl(serverRelativePageUrl);
            LimitedWebPartManager limitedWebPartManager = file.GetLimitedWebPartManager(PersonalizationScope.Shared);

            IEnumerable<WebPartDefinition> query = null;

#if ONPREMISES
            // As long as we've no CSOM library that has the ZoneID we can't use the version check as things don't compile...
            query = web.Context.LoadQuery(limitedWebPartManager.WebParts.IncludeWithDefaultProperties(wp => wp.Id, wp => wp.WebPart, wp => wp.WebPart.Title, wp => wp.WebPart.Properties, wp => wp.WebPart.Hidden));
#else
            if (web.Context.HasMinimalServerLibraryVersion(Constants.MINIMUMZONEIDREQUIREDSERVERVERSION))
            {
                query = web.Context.LoadQuery(limitedWebPartManager.WebParts.IncludeWithDefaultProperties(wp => wp.Id, wp => wp.ZoneId, wp => wp.WebPart, wp => wp.WebPart.Title, wp => wp.WebPart.Properties, wp => wp.WebPart.Hidden));
            }
            else
            {
                query = web.Context.LoadQuery(limitedWebPartManager.WebParts.IncludeWithDefaultProperties(wp => wp.Id, wp => wp.WebPart, wp => wp.WebPart.Title, wp => wp.WebPart.Properties, wp => wp.WebPart.Hidden));
            }
#endif
            web.Context.ExecuteQueryRetry();

            return query;
        }

        /// <summary>
        /// Inserts a web part on a web part page
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="webPart">Information about the web part to insert</param>
        /// <param name="page">Page to add the web part on</param>
        /// <exception cref="System.ArgumentException">Thrown when page is a zero-length string or contains only white space</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when webPart or page is null</exception>
        public static void AddWebPartToWebPartPage(this Web web, WebPartEntity webPart, string page)
        {
            if (webPart == null)
            {
                throw new ArgumentNullException("webPart");
            }

            if (string.IsNullOrEmpty(page))
            {
                throw (page == null)
                  ? new ArgumentNullException("page")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "page");
            }

            if (!web.IsObjectPropertyInstantiated("ServerRelativeUrl"))
            {
                web.Context.Load(web, w => w.ServerRelativeUrl);
                web.Context.ExecuteQueryRetry();
            }
            var serverRelativeUrl = UrlUtility.Combine(web.ServerRelativeUrl, page);

            AddWebPartToWebPartPage(web, serverRelativeUrl, webPart);
        }

        /// <summary>
        /// Inserts a web part on a web part page
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="serverRelativePageUrl">Page to add the web part on</param>
        /// <param name="webPart">Information about the web part to insert</param>
        /// <exception cref="System.ArgumentException">Thrown when serverRelativePageUrl is a zero-length string or contains only white space</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when serverRelativePageUrl or webPart is null</exception>
        public static void AddWebPartToWebPartPage(this Web web, string serverRelativePageUrl, WebPartEntity webPart)
        {
            if (string.IsNullOrEmpty(serverRelativePageUrl))
            {
                throw (serverRelativePageUrl == null)
                  ? new ArgumentNullException("serverRelativePageUrl")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "serverRelativePageUrl");
            }

            if (webPart == null)
            {
                throw new ArgumentNullException("webPart");
            }

            var webPartPage = web.GetFileByServerRelativeUrl(serverRelativePageUrl);

            if (webPartPage == null)
            {
                return;
            }

            web.Context.Load(webPartPage);
            web.Context.ExecuteQueryRetry();

            AddWebPart(webPartPage, webPart, webPart.WebPartZone, webPart.WebPartIndex);
        }

        /// <summary>
        /// Add web part to a wiki style page
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="folder">System name of the wiki page library - typically sitepages</param>
        /// <param name="webPart">Information about the web part to insert</param>
        /// <param name="page">Page to add the web part on</param>
        /// <param name="row">Row of the wiki table that should hold the inserted web part</param>
        /// <param name="col">Column of the wiki table that should hold the inserted web part</param>
        /// <param name="addSpace">Does a blank line need to be added after the web part (to space web parts)</param>
        /// <exception cref="System.ArgumentException">Thrown when folder or page is a zero-length string or contains only white space</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when folder, webPart or page is null</exception>
        public static void AddWebPartToWikiPage(this Web web, string folder, WebPartEntity webPart, string page, int row, int col, bool addSpace)
        {
            if (string.IsNullOrEmpty(folder))
            {
                throw (folder == null)
                  ? new ArgumentNullException("folder")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "folder");
            }

            if (webPart == null)
            {
                throw new ArgumentNullException("webPart");
            }

            if (string.IsNullOrEmpty(page))
            {
                throw (page == null)
                  ? new ArgumentNullException("page")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "page");
            }

            if (!web.IsObjectPropertyInstantiated("ServerRelativeUrl"))
            {
                web.Context.Load(web, w => w.ServerRelativeUrl);
                web.Context.ExecuteQueryRetry();
            }

            var webServerRelativeUrl = UrlUtility.EnsureTrailingSlash(web.ServerRelativeUrl);
            var serverRelativeUrl = UrlUtility.Combine(folder, page);
            AddWebPartToWikiPage(web, webServerRelativeUrl + serverRelativeUrl, webPart, row, col, addSpace);
        }

        /// <summary>
        /// Add web part to a wiki style page
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="serverRelativePageUrl">Server relative url of the page to add the webpart to</param>
        /// <param name="webPart">Information about the web part to insert</param>
        /// <param name="row">Row of the wiki table that should hold the inserted web part</param>
        /// <param name="col">Column of the wiki table that should hold the inserted web part</param>
        /// <param name="addSpace">Does a blank line need to be added after the web part (to space web parts)</param>
        /// <exception cref="System.ArgumentException">Thrown when serverRelativePageUrl is a zero-length string or contains only white space</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when serverRelativePageUrl or webPart is null</exception>
        [SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Xml.XmlDocument.CreateTextNode(System.String)")]
        public static void AddWebPartToWikiPage(this Web web, string serverRelativePageUrl, WebPartEntity webPart, int row, int col, bool addSpace)
        {
            if (string.IsNullOrEmpty(serverRelativePageUrl))
            {
                throw (serverRelativePageUrl == null)
                  ? new ArgumentNullException("serverRelativePageUrl")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "serverRelativePageUrl");
            }

            if (webPart == null)
            {
                throw new ArgumentNullException("webPart");
            }

            File webPartPage = web.GetFileByServerRelativeUrl(serverRelativePageUrl);

            if (webPartPage == null)
            {
                return;
            }

            web.Context.Load(webPartPage, wp => wp.ListItemAllFields);
            web.Context.ExecuteQueryRetry();

            string wikiField = (string)webPartPage.ListItemAllFields["WikiField"];

            var wpdNew = AddWebPart(webPartPage, webPart, "wpz", 0);

            //HTML structure in default team site home page (W16)
            //<div class="ExternalClass284FC748CB4242F6808DE69314A7C981">
            //  <div class="ExternalClass5B1565E02FCA4F22A89640AC10DB16F3">
            //    <table id="layoutsTable" style="width&#58;100%;">
            //      <tbody>
            //        <tr style="vertical-align&#58;top;">
            //          <td colspan="2">
            //            <div class="ms-rte-layoutszone-outer" style="width&#58;100%;">
            //              <div class="ms-rte-layoutszone-inner" style="word-wrap&#58;break-word;margin&#58;0px;border&#58;0px;">
            //                <div><span><span><div class="ms-rtestate-read ms-rte-wpbox"><div class="ms-rtestate-read 9ed0c0ac-54d0-4460-9f1c-7e98655b0847" id="div_9ed0c0ac-54d0-4460-9f1c-7e98655b0847"></div><div class="ms-rtestate-read" id="vid_9ed0c0ac-54d0-4460-9f1c-7e98655b0847" style="display&#58;none;"></div></div></span></span><p> </p></div>
            //                <div class="ms-rtestate-read ms-rte-wpbox">
            //                  <div class="ms-rtestate-read c7a1f9a9-4e27-4aa3-878b-c8c6c87961c0" id="div_c7a1f9a9-4e27-4aa3-878b-c8c6c87961c0"></div>
            //                  <div class="ms-rtestate-read" id="vid_c7a1f9a9-4e27-4aa3-878b-c8c6c87961c0" style="display&#58;none;"></div>
            //                </div>
            //              </div>
            //            </div>
            //          </td>
            //        </tr>
            //        <tr style="vertical-align&#58;top;">
            //          <td style="width&#58;49.95%;">
            //            <div class="ms-rte-layoutszone-outer" style="width&#58;100%;">
            //              <div class="ms-rte-layoutszone-inner" style="word-wrap&#58;break-word;margin&#58;0px;border&#58;0px;">
            //                <div class="ms-rtestate-read ms-rte-wpbox">
            //                  <div class="ms-rtestate-read b55b18a3-8a3b-453f-a714-7e8d803f4d30" id="div_b55b18a3-8a3b-453f-a714-7e8d803f4d30"></div>
            //                  <div class="ms-rtestate-read" id="vid_b55b18a3-8a3b-453f-a714-7e8d803f4d30" style="display&#58;none;"></div>
            //                </div>
            //              </div>
            //            </div>
            //          </td>
            //          <td class="ms-wiki-columnSpacing" style="width&#58;49.95%;">
            //            <div class="ms-rte-layoutszone-outer" style="width&#58;100%;">
            //              <div class="ms-rte-layoutszone-inner" style="word-wrap&#58;break-word;margin&#58;0px;border&#58;0px;">
            //                <div class="ms-rtestate-read ms-rte-wpbox">
            //                  <div class="ms-rtestate-read 0b2f12a4-3ab5-4a59-b2eb-275bbc617f95" id="div_0b2f12a4-3ab5-4a59-b2eb-275bbc617f95"></div>
            //                  <div class="ms-rtestate-read" id="vid_0b2f12a4-3ab5-4a59-b2eb-275bbc617f95" style="display&#58;none;"></div>
            //                </div>
            //              </div>
            //            </div>
            //          </td>
            //        </tr>
            //      </tbody>
            //    </table>
            //    <span id="layoutsData" style="display&#58;none;">true,false,2</span>
            //  </div>
            //</div>

            // Close all BR tags
            Regex brRegex = new Regex("<br>", RegexOptions.IgnoreCase);

            wikiField = brRegex.Replace(wikiField, "<br/>");

            XmlDocument xd = new XmlDocument();
            xd.PreserveWhitespace = true;
            xd.LoadXml(wikiField);

            // Sometimes the wikifield content seems to be surrounded by an additional div? 
            XmlElement layoutsTable = xd.SelectSingleNode("div/div/table") as XmlElement;
            if (layoutsTable == null)
            {
                layoutsTable = xd.SelectSingleNode("div/table") as XmlElement;
            }

            XmlElement layoutsZoneInner = layoutsTable.SelectSingleNode(string.Format("tbody/tr[{0}]/td[{1}]/div/div", row, col)) as XmlElement;
            // - space element
            XmlElement space = xd.CreateElement("p");
            XmlText text = xd.CreateTextNode(" ");
            space.AppendChild(text);

            // - wpBoxDiv
            XmlElement wpBoxDiv = xd.CreateElement("div");
            layoutsZoneInner.AppendChild(wpBoxDiv);

            if (addSpace)
            {
                layoutsZoneInner.AppendChild(space);
            }

            XmlAttribute attribute = xd.CreateAttribute("class");
            wpBoxDiv.Attributes.Append(attribute);
            attribute.Value = "ms-rtestate-read ms-rte-wpbox";
            attribute = xd.CreateAttribute("contentEditable");
            wpBoxDiv.Attributes.Append(attribute);
            attribute.Value = "false";
            // - div1
            XmlElement div1 = xd.CreateElement("div");
            wpBoxDiv.AppendChild(div1);
            div1.IsEmpty = false;
            attribute = xd.CreateAttribute("class");
            div1.Attributes.Append(attribute);
            attribute.Value = "ms-rtestate-read " + wpdNew.Id.ToString("D");
            attribute = xd.CreateAttribute("id");
            div1.Attributes.Append(attribute);
            attribute.Value = "div_" + wpdNew.Id.ToString("D");
            // - div2
            XmlElement div2 = xd.CreateElement("div");
            wpBoxDiv.AppendChild(div2);
            div2.IsEmpty = false;
            attribute = xd.CreateAttribute("style");
            div2.Attributes.Append(attribute);
            attribute.Value = "display:none";
            attribute = xd.CreateAttribute("id");
            div2.Attributes.Append(attribute);
            attribute.Value = "vid_" + wpdNew.Id.ToString("D");

            ListItem listItem = webPartPage.ListItemAllFields;
            listItem["WikiField"] = xd.OuterXml;
            listItem.Update();
            web.Context.ExecuteQueryRetry();

        }

        public static string GetWebPartXml(this Web web, Guid webPartId, string serverRelativePageUrl)
        {

            string webPartXml = null;

            Guid id = Guid.Empty;

            var wp = web.GetWebParts(serverRelativePageUrl).FirstOrDefault(wps => wps.Id == webPartId);
            if (wp != null)
            {
                id = wp.Id;
            }
            else
            {
                return null;
            }


            if (id != Guid.Empty)
            {
                var uri = new Uri(web.Context.Url);
                var serverRelativeUrl = web.EnsureProperty(w => w.ServerRelativeUrl);
                var hostUri = uri.Host;
                var webUrl = string.Format("{0}://{1}{2}", uri.Scheme, uri.Host, serverRelativeUrl);
                var pageUrl = string.Format("{0}://{1}{2}", uri.Scheme, uri.Host, serverRelativePageUrl);
                var request = (HttpWebRequest)WebRequest.Create(string.Format("{0}/_vti_bin/exportwp.aspx?pageurl={1}&guidstring={2}", webUrl, pageUrl, id.ToString()));

#if ONPREMISES
                request.Credentials = web.Context.Credentials;
#else
                var credentials = web.Context.Credentials as SharePointOnlineCredentials;
                var authCookieValue = credentials.GetAuthenticationCookie(uri);

                Cookie fedAuth = new Cookie()
                {
                    Name = "SPOIDCRL",
                    Value = authCookieValue.TrimStart("SPOIDCRL=".ToCharArray()),
                    Path = "/",
                    Secure = true,
                    HttpOnly = true,
                    Domain = uri.Host
                };
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(fedAuth);
#endif

                var response = request.GetResponse();
                using (Stream stream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                    webPartXml = reader.ReadToEnd();
                }
            }

            return webPartXml;
        }

                /// <summary>
                /// Applies a layout to a wiki page
                /// </summary>
                /// <param name="web">Site to be processed - can be root web or sub site</param>
                /// <param name="layout">Wiki page layout to be applied</param>
                /// <param name="serverRelativePageUrl"></param>
                /// <exception cref="System.ArgumentException">Thrown when serverRelativePageUrl is a zero-length string or contains only white space</exception>
                /// <exception cref="System.ArgumentNullException">Thrown when serverRelativePageUrl is null</exception>
        public static void AddLayoutToWikiPage(this Web web, WikiPageLayout layout, string serverRelativePageUrl)
        {
            if (string.IsNullOrEmpty(serverRelativePageUrl))
            {
                throw (serverRelativePageUrl == null)
                  ? new ArgumentNullException("serverRelativePageUrl")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "serverRelativePageUrl");
            }

            string html = "";
            switch (layout)
            {
                case WikiPageLayout.OneColumn:
                    html = WikiPage_OneColumn;
                    break;
                case WikiPageLayout.OneColumnSideBar:
                    html = WikiPage_OneColumnSideBar;
                    break;
                case WikiPageLayout.TwoColumns:
                    html = WikiPage_TwoColumns;
                    break;
                case WikiPageLayout.TwoColumnsHeader:
                    html = WikiPage_TwoColumnsHeader;
                    break;
                case WikiPageLayout.TwoColumnsHeaderFooter:
                    html = WikiPage_TwoColumnsHeaderFooter;
                    break;
                case WikiPageLayout.ThreeColumns:
                    html = WikiPage_ThreeColumns;
                    break;
                case WikiPageLayout.ThreeColumnsHeader:
                    html = WikiPage_ThreeColumnsHeader;
                    break;
                case WikiPageLayout.ThreeColumnsHeaderFooter:
                    html = WikiPage_ThreeColumnsHeaderFooter;
                    break;
                default:
                    break;
            }

            web.AddHtmlToWikiPage(serverRelativePageUrl, html);
        }

        /// <summary>
        /// Applies a layout to a wiki page
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="folder">System name of the wiki page library - typically sitepages</param>
        /// <param name="layout">Wiki page layout to be applied</param>
        /// <param name="page">Name of the page that will get a new wiki page layout</param>
        /// <exception cref="System.ArgumentException">Thrown when folder or page is a zero-length string or contains only white space</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when folder or page is null</exception>
        public static void AddLayoutToWikiPage(this Web web, string folder, WikiPageLayout layout, string page)
        {
            if (string.IsNullOrEmpty(folder))
            {
                throw (folder == null)
                  ? new ArgumentNullException("folder")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "folder");
            }

            if (string.IsNullOrEmpty(page))
            {
                throw (page == null)
                  ? new ArgumentNullException("page")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "page");
            }

            string html = "";
            switch (layout)
            {
                case WikiPageLayout.OneColumn:
                    html = WikiPage_OneColumn;
                    break;
                case WikiPageLayout.OneColumnSideBar:
                    html = WikiPage_OneColumnSideBar;
                    break;
                case WikiPageLayout.TwoColumns:
                    html = WikiPage_TwoColumns;
                    break;
                case WikiPageLayout.TwoColumnsHeader:
                    html = WikiPage_TwoColumnsHeader;
                    break;
                case WikiPageLayout.TwoColumnsHeaderFooter:
                    html = WikiPage_TwoColumnsHeaderFooter;
                    break;
                case WikiPageLayout.ThreeColumns:
                    html = WikiPage_ThreeColumns;
                    break;
                case WikiPageLayout.ThreeColumnsHeader:
                    html = WikiPage_ThreeColumnsHeader;
                    break;
                case WikiPageLayout.ThreeColumnsHeaderFooter:
                    html = WikiPage_ThreeColumnsHeaderFooter;
                    break;
                default:
                    break;
            }

            web.AddHtmlToWikiPage(folder, html, page);
        }

        /// <summary>
        /// Add html to a wiki style page
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="folder">System name of the wiki page library - typically sitepages</param>
        /// <param name="html">The html to insert</param>
        /// <param name="page">Page to add the html on</param>
        /// <exception cref="System.ArgumentException">Thrown when folder, html or page is a zero-length string or contains only white space</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when folder, html or page is null</exception>
        public static void AddHtmlToWikiPage(this Web web, string folder, string html, string page)
        {
            if (string.IsNullOrEmpty(folder))
            {
                throw (folder == null)
                  ? new ArgumentNullException("folder")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "folder");
            }

            if (string.IsNullOrEmpty(html))
            {
                throw (html == null)
                  ? new ArgumentNullException("html")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "html");
            }

            if (string.IsNullOrEmpty(page))
            {
                throw (page == null)
                  ? new ArgumentNullException("page")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "page");
            }

            if (!web.IsObjectPropertyInstantiated("ServerRelativeUrl"))
            {
                web.Context.Load(web, w => w.ServerRelativeUrl);
                web.Context.ExecuteQueryRetry();
            }

            var webServerRelativeUrl = UrlUtility.EnsureTrailingSlash(web.ServerRelativeUrl);

            var serverRelativeUrl = UrlUtility.Combine(webServerRelativeUrl, folder, page);

            AddHtmlToWikiPage(web, serverRelativeUrl, html);
        }

        /// <summary>
        /// Add HTML to a wiki page
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="serverRelativePageUrl"></param>
        /// <param name="html"></param>
        /// <exception cref="System.ArgumentException">Thrown when serverRelativePageUrl or html is a zero-length string or contains only white space</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when serverRelativePageUrl or html is null</exception>
        public static void AddHtmlToWikiPage(this Web web, string serverRelativePageUrl, string html)
        {
            if (string.IsNullOrEmpty(serverRelativePageUrl))
            {
                throw (serverRelativePageUrl == null)
                  ? new ArgumentNullException("serverRelativePageUrl")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "serverRelativePageUrl");
            }

            if (string.IsNullOrEmpty(html))
            {
                throw (html == null)
                  ? new ArgumentNullException("html")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "html");
            }

            File file = web.GetFileByServerRelativeUrl(serverRelativePageUrl);

            web.Context.Load(file, f => f.ListItemAllFields);
            web.Context.ExecuteQueryRetry();

            ListItem item = file.ListItemAllFields;

            item["WikiField"] = html;

            item.Update();

            web.Context.ExecuteQueryRetry();
        }

        /// <summary>
        /// Add a HTML fragment to a location on a wiki style page
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="folder">System name of the wiki page library - typically sitepages</param>
        /// <param name="html">html to be inserted</param>
        /// <param name="page">Page to add the web part on</param>
        /// <param name="row">Row of the wiki table that should hold the inserted web part</param>
        /// <param name="col">Column of the wiki table that should hold the inserted web part</param>
        /// <exception cref="System.ArgumentException">Thrown when folder, html or page is a zero-length string or contains only white space</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when folder, html or page is null</exception>
        public static void AddHtmlToWikiPage(this Web web, string folder, string html, string page, int row, int col)
        {
            if (string.IsNullOrEmpty(folder))
            {
                throw (folder == null)
                  ? new ArgumentNullException("folder")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "folder");
            }

            if (string.IsNullOrEmpty(html))
            {
                throw (html == null)
                  ? new ArgumentNullException("html")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "html");
            }

            if (string.IsNullOrEmpty(page))
            {
                throw (page == null)
                  ? new ArgumentNullException("page")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "page");
            }

            if (!web.IsObjectPropertyInstantiated("ServerRelativeUrl"))
            {
                web.Context.Load(web, w => w.ServerRelativeUrl);
                web.Context.ExecuteQueryRetry();
            }

            var webServerRelativeUrl = UrlUtility.EnsureTrailingSlash(web.ServerRelativeUrl);

            var serverRelativeUrl = UrlUtility.Combine(webServerRelativeUrl, folder, page);

            AddHtmlToWikiPage(web, serverRelativeUrl, html, row, col);
        }

        /// <summary>
        /// Add a HTML fragment to a location on a wiki style page
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="serverRelativePageUrl">server relative Url of the page to add the fragment to</param>
        /// <param name="html">html to be inserted</param>
        /// <param name="row">Row of the wiki table that should hold the inserted web part</param>
        /// <param name="col">Column of the wiki table that should hold the inserted web part</param>
        /// <exception cref="System.ArgumentException">Thrown when serverRelativePageUrl or html is a zero-length string or contains only white space</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when serverRelativePageUrl or html is null</exception>
        [SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Xml.XmlDocument.CreateTextNode(System.String)")]
        public static void AddHtmlToWikiPage(this Web web, string serverRelativePageUrl, string html, int row, int col)
        {
            if (string.IsNullOrEmpty(serverRelativePageUrl))
            {
                throw (serverRelativePageUrl == null)
                  ? new ArgumentNullException("serverRelativePageUrl")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "serverRelativePageUrl");
            }

            if (string.IsNullOrEmpty(html))
            {
                throw (html == null)
                  ? new ArgumentNullException("html")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "html");
            }

            File file = web.GetFileByServerRelativeUrl(serverRelativePageUrl);

            web.Context.Load(file, f => f.ListItemAllFields);
            web.Context.ExecuteQueryRetry();

            ListItem item = file.ListItemAllFields;

            string wikiField = (string)item["WikiField"];

            XmlDocument xd = new XmlDocument();
            xd.PreserveWhitespace = true;
            xd.LoadXml(wikiField);

            // Sometimes the wikifield content seems to be surrounded by an additional div? 
            XmlElement layoutsTable = xd.SelectSingleNode("div/div/table") as XmlElement;
            if (layoutsTable == null)
            {
                layoutsTable = xd.SelectSingleNode("div/table") as XmlElement;
            }

            // Add the html content
            XmlElement layoutsZoneInner = layoutsTable.SelectSingleNode(string.Format("tbody/tr[{0}]/td[{1}]/div/div", row, col)) as XmlElement;
            XmlText text = xd.CreateTextNode("!!123456789!!");
            layoutsZoneInner.AppendChild(text);

            item["WikiField"] = xd.OuterXml.Replace("!!123456789!!", html);
            item.Update();
            web.Context.ExecuteQueryRetry();
        }

        /// <summary>
        /// Deletes a web part from a page
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="folder">System name of the wiki page library - typically sitepages</param>
        /// <param name="title">Title of the web part that needs to be deleted</param>
        /// <param name="page">Page to remove the web part from</param>
        /// <exception cref="System.ArgumentException">Thrown when folder, title or page is a zero-length string or contains only white space</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when folder, title or page is null</exception>
        public static void DeleteWebPart(this Web web, string folder, string title, string page)
        {
            if (string.IsNullOrEmpty(folder))
            {
                throw (folder == null)
                  ? new ArgumentNullException("folder")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "folder");
            }

            if (string.IsNullOrEmpty(title))
            {
                throw (title == null)
                  ? new ArgumentNullException("title")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "title");
            }

            if (string.IsNullOrEmpty(page))
            {
                throw (page == null)
                  ? new ArgumentNullException("page")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "page");
            }

            if (!web.IsObjectPropertyInstantiated("ServerRelativeUrl"))
            {
                web.Context.Load(web, w => w.ServerRelativeUrl);
                web.Context.ExecuteQueryRetry();
            }

            var webServerRelativeUrl = UrlUtility.EnsureTrailingSlash(web.ServerRelativeUrl);

            var serverRelativeUrl = UrlUtility.Combine(folder, page);

            DeleteWebPart(web, webServerRelativeUrl + serverRelativeUrl, title);
        }

        /// <summary>
        /// Deletes a web part from a page
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="serverRelativePageUrl">Server relative URL of the page to remove</param>
        /// <param name="title">Title of the web part that needs to be deleted</param>
        /// <exception cref="System.ArgumentException">Thrown when serverRelativePageUrl or title is a zero-length string or contains only white space</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when serverRelativePageUrl or title is null</exception>
        public static void DeleteWebPart(this Web web, string serverRelativePageUrl, string title)
        {
            if (string.IsNullOrEmpty(serverRelativePageUrl))
            {
                throw (serverRelativePageUrl == null)
                  ? new ArgumentNullException("serverRelativePageUrl")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "serverRelativePageUrl");
            }

            if (string.IsNullOrEmpty(title))
            {
                throw (title == null)
                  ? new ArgumentNullException("title")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "title");
            }

            var webPartPage = web.GetFileByServerRelativeUrl(serverRelativePageUrl);

            if (webPartPage == null)
            {
                return;
            }

            web.Context.Load(webPartPage);
            web.Context.ExecuteQueryRetry();

            LimitedWebPartManager limitedWebPartManager = webPartPage.GetLimitedWebPartManager(PersonalizationScope.Shared);
            web.Context.Load(limitedWebPartManager.WebParts, wps => wps.Include(wp => wp.WebPart.Title));
            web.Context.ExecuteQueryRetry();

            if (limitedWebPartManager.WebParts.Count >= 0)
            {
                for (int i = 0; i < limitedWebPartManager.WebParts.Count; i++)
                {
                    WebPart oWebPart = limitedWebPartManager.WebParts[i].WebPart;
                    if (oWebPart.Title.Equals(title, StringComparison.InvariantCultureIgnoreCase))
                    {
                        limitedWebPartManager.WebParts[i].DeleteWebPart();
                        web.Context.ExecuteQueryRetry();
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Adds a blank Wiki page to the site pages library
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="wikiPageLibraryName">Name of the wiki page library</param>
        /// <param name="wikiPageName">Wiki page to operate on</param>
        /// <returns>The relative URL of the added wiki page</returns>
        /// <exception cref="System.ArgumentException">Thrown when wikiPageLibraryName or wikiPageName is a zero-length string or contains only white space</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when wikiPageLibraryName or wikiPageName is null</exception>
        public static string AddWikiPage(this Web web, string wikiPageLibraryName, string wikiPageName)
        {
            string wikiPageUrl, newWikiPageUrl, pathAndQuery;
            List pageLibrary;
            File currentPageFile;

            WikiPageImplementation(web, wikiPageLibraryName, wikiPageName, out wikiPageUrl, out pageLibrary, out newWikiPageUrl, out currentPageFile, out pathAndQuery);

            if (!currentPageFile.Exists)
            {
                var newpage = pageLibrary.RootFolder.Files.AddTemplateFile(newWikiPageUrl, TemplateFileType.WikiPage);
                web.Context.Load(newpage);
                web.Context.ExecuteQueryRetry();
                wikiPageUrl = newpage.ServerRelativeUrl.Replace(pathAndQuery, "");
            }

            return wikiPageUrl;
        }

        /// <summary>
        /// Returns the Url for the requested wiki page, creates it if the pageis not yet available
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="wikiPageLibraryName">Name of the wiki page library</param>
        /// <param name="wikiPageName">Wiki page to operate on</param>
        /// <returns>The relative URL of the added wiki page</returns>
        /// <exception cref="System.ArgumentException">Thrown when wikiPageLibraryName or wikiPageName is a zero-length string or contains only white space</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when wikiPageLibraryName or wikiPageName is null</exception>
        public static string EnsureWikiPage(this Web web, string wikiPageLibraryName, string wikiPageName)
        {
            string wikiPageUrl, newWikiPageUrl, pathAndQuery;
            List pageLibrary;
            File currentPageFile;

            WikiPageImplementation(web, wikiPageLibraryName, wikiPageName, out wikiPageUrl, out pageLibrary, out newWikiPageUrl, out currentPageFile, out pathAndQuery);

            if (!currentPageFile.Exists)
            {
                var newpage = pageLibrary.RootFolder.Files.AddTemplateFile(newWikiPageUrl, TemplateFileType.WikiPage);
                web.Context.Load(newpage);
                web.Context.ExecuteQueryRetry();
                wikiPageUrl = newpage.ServerRelativeUrl.Replace(pathAndQuery, "");
            }
            else
            {
                web.Context.Load(currentPageFile, s => s.ServerRelativeUrl);
                web.Context.ExecuteQueryRetry();
                wikiPageUrl = currentPageFile.ServerRelativeUrl.Replace(pathAndQuery, "");
            }

            return wikiPageUrl;
        }

        private static void WikiPageImplementation(Web web, string wikiPageLibraryName, string wikiPageName, out string wikiPageUrl, out List pageLibrary, out string newWikiPageUrl, out File currentPageFile, out string pathAndQuery)
        {
            if (string.IsNullOrEmpty(wikiPageLibraryName))
            {
                throw (wikiPageLibraryName == null)
                  ? new ArgumentNullException("wikiPageLibraryName")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "wikiPageLibraryName");
            }

            if (string.IsNullOrEmpty(wikiPageName))
            {
                throw (wikiPageName == null)
                  ? new ArgumentNullException("wikiPageName")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "wikiPageName");
            }

            wikiPageUrl = "";
            pageLibrary = web.Lists.GetByTitle(wikiPageLibraryName);
            web.Context.Load(web, w => w.Url);
            web.Context.Load(pageLibrary.RootFolder, f => f.ServerRelativeUrl);
            web.Context.ExecuteQueryRetry();

            var pageLibraryUrl = pageLibrary.RootFolder.ServerRelativeUrl;
            newWikiPageUrl = pageLibraryUrl + "/" + wikiPageName;
            currentPageFile = web.GetFileByServerRelativeUrl(newWikiPageUrl);
            web.Context.Load(currentPageFile, f => f.Exists);
            web.Context.ExecuteQueryRetry();

            pathAndQuery = new Uri(web.Url).PathAndQuery;
            if (!pathAndQuery.EndsWith("/"))
            {
                pathAndQuery = pathAndQuery + "/";
            }
        }

        /// <summary>
        /// Adds a wiki page by Url
        /// </summary>
        /// <param name="web">The web to process</param>
        /// <param name="serverRelativePageUrl">Server relative URL of the wiki page to process</param>
        /// <param name="html">HTML to add to wiki page</param>
        /// <exception cref="System.ArgumentException">Thrown when serverRelativePageUrl is a zero-length string or contains only white space</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when serverRelativePageUrl is null</exception>
        public static void AddWikiPageByUrl(this Web web, string serverRelativePageUrl, string html = null)
        {
            if (string.IsNullOrEmpty(serverRelativePageUrl))
            {
                throw (serverRelativePageUrl == null)
                  ? new ArgumentNullException("serverRelativePageUrl")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "serverRelativePageUrl");
            }

            string folderName = serverRelativePageUrl.Substring(0, serverRelativePageUrl.LastIndexOf("/"));
            Folder folder = web.GetFolderByServerRelativeUrl(folderName);
            File file = folder.Files.AddTemplateFile(serverRelativePageUrl, TemplateFileType.WikiPage);

            web.Context.ExecuteQueryRetry();
            if (html != null)
            {
                web.AddHtmlToWikiPage(serverRelativePageUrl, html);
            }
        }

        /// <summary>
        /// Sets a web part property
        /// </summary>
        /// <param name="web">The web to process</param>
        /// <param name="key">The key to update</param>
        /// <param name="value">The value to set</param>
        /// <param name="id">The id of the webpart</param>
        /// <param name="serverRelativePageUrl"></param>
        /// <exception cref="System.ArgumentException">Thrown when key or serverRelativePageUrl is a zero-length string or contains only white space</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when key or serverRelativePageUrl is null</exception>
        public static void SetWebPartProperty(this Web web, string key, string value, Guid id, string serverRelativePageUrl)
        {
            SetWebPartPropertyInternal(web, key, value, id, serverRelativePageUrl);
        }

        /// <summary>
        /// Sets a web part property
        /// </summary>
        /// <param name="web">The web to process</param>
        /// <param name="key">The key to update</param>
        /// <param name="value">The value to set</param>
        /// <param name="id">The id of the webpart</param>
        /// <param name="serverRelativePageUrl"></param>
        /// <exception cref="System.ArgumentException">Thrown when key or serverRelativePageUrl is a zero-length string or contains only white space</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when key or serverRelativePageUrl is null</exception>
        public static void SetWebPartProperty(this Web web, string key, int value, Guid id, string serverRelativePageUrl)
        {
            SetWebPartPropertyInternal(web, key, value, id, serverRelativePageUrl);
        }

        /// <summary>
        /// Sets a web part property
        /// </summary>
        /// <param name="web">The web to process</param>
        /// <param name="key">The key to update</param>
        /// <param name="value">The value to set</param>
        /// <param name="id">The id of the webpart</param>
        /// <param name="serverRelativePageUrl"></param>
        /// <exception cref="System.ArgumentException">Thrown when key or serverRelativePageUrl is a zero-length string or contains only white space</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when key or serverRelativePageUrl is null</exception>
        public static void SetWebPartProperty(this Web web, string key, bool value, Guid id, string serverRelativePageUrl)
        {
            SetWebPartPropertyInternal(web,key,value,id,serverRelativePageUrl);
        }

	    private static WebPartDefinition AddWebPart(File webPartPage, WebPartEntity webPart, string zoneId, int zoneIndex)
	    {
            var limitedWebPartManager = webPartPage.GetLimitedWebPartManager(PersonalizationScope.Shared);
            var oWebPartDefinition = limitedWebPartManager.ImportWebPart(webPart.WebPartXml);

            var wpdNew = limitedWebPartManager.AddWebPart(oWebPartDefinition.WebPart, zoneId, zoneIndex);
            webPartPage.Context.Load(wpdNew);
            webPartPage.Context.ExecuteQueryRetry();

	        WebPartPostProcessorFactory.Resolve(webPart.WebPartXml).Process(wpdNew, webPartPage);

            return wpdNew;
	    }

        private static void SetWebPartPropertyInternal(this Web web, string key, object value, Guid id, string serverRelativePageUrl)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw (key == null)
                  ? new ArgumentNullException("key")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "key");
            }

            if (string.IsNullOrEmpty(serverRelativePageUrl))
            {
                throw (serverRelativePageUrl == null)
                  ? new ArgumentNullException("serverRelativePageUrl")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "serverRelativePageUrl");
            }

            ClientContext context = web.Context as ClientContext;

            File file = web.GetFileByServerRelativeUrl(serverRelativePageUrl);

            context.Load(file);
            context.ExecuteQueryRetry();

            LimitedWebPartManager wpm = file.GetLimitedWebPartManager(PersonalizationScope.Shared);

            context.Load(wpm.WebParts);

            context.ExecuteQueryRetry();

            WebPartDefinition def = wpm.WebParts.GetById(id);

            context.Load(def);
            context.ExecuteQueryRetry();

            switch (key.ToLower())
            {
                case "title":
                    {
                        def.WebPart.Title = value as string;
                        break;
                    }
                case "titleurl":
                    {
                        def.WebPart.TitleUrl = value as string;
                        break;
                    }
                default:
                    {
                        def.WebPart.Properties[key] = value;
                        break;
                    }
            }


            def.SaveWebPartChanges();

            context.ExecuteQueryRetry();
        }

        /// <summary>
        /// Returns web part properties
        /// </summary>
        /// <param name="web">The web to process</param>
        /// <param name="id">The id of the webpart</param>
        /// <param name="serverRelativePageUrl"></param>
        /// <exception cref="System.ArgumentException">Thrown when key or serverRelativePageUrl is a zero-length string or contains only white space</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when key or serverRelativePageUrl is null</exception>
        public static PropertyValues GetWebPartProperties(this Web web, Guid id, string serverRelativePageUrl)
        {
            if (string.IsNullOrEmpty(serverRelativePageUrl))
            {
                throw (serverRelativePageUrl == null)
                  ? new ArgumentNullException("serverRelativePageUrl")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "serverRelativePageUrl");
            }

            ClientContext context = web.Context as ClientContext;

            File file = web.GetFileByServerRelativeUrl(serverRelativePageUrl);

            context.Load(file);
            context.ExecuteQueryRetry();

            LimitedWebPartManager wpm = file.GetLimitedWebPartManager(PersonalizationScope.Shared);

            WebPartDefinition def = wpm.WebParts.GetById(id);

            context.Load(def.WebPart.Properties);
            context.ExecuteQueryRetry();

            return def.WebPart.Properties;
        }

        /// <summary>
        /// Adds the publishing page.
        /// </summary>
        /// <param name="web">The web.</param>
        /// <param name="pageName">Name of the page.</param>
        /// <param name="pageTemplateName">Name of the page template/layout excluded the .aspx file extension.</param>
        /// <param name="title">The title of the target publishing page.</param>
        /// <param name="publish">Should the page be published or not?</param>
        /// <param name="folder">The target folder for the page, within the Pages library.</param>
        /// <param name="startDate">Start date for scheduled publishing.</param>
        /// <param name="endDate">End date for scheduled publishing.</param>
        /// <param name="schedule">Defines whether to define a schedule or not.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when key or pageName is a zero-length string or contains only white space</exception>
        /// <exception cref="System.ArgumentException">Thrown when key or pageName is null</exception>
        public static void AddPublishingPage(this Web web, string pageName, string pageTemplateName, string title = null, 
            bool publish = false, Folder folder = null,
            DateTime? startDate = null, DateTime? endDate = null, Boolean schedule = false)
        {
            if (string.IsNullOrEmpty(pageName))
            {
                throw (title == null)
                  ? new ArgumentNullException("pageName")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "pageName");
            }
            if (string.IsNullOrEmpty(pageTemplateName))
            {
                throw (title == null)
                  ? new ArgumentNullException("pageTemplateName")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "pageTemplateName");
            }
            if (string.IsNullOrEmpty(title))
            {
                title = pageName;
            }

            // Fix page name, if needed
            pageName = pageName.ReplaceInvalidUrlChars("-");

            ClientContext context = web.Context as ClientContext;
            Site site = context.Site;
            context.Load(site, s => s.ServerRelativeUrl);
            context.ExecuteQueryRetry();

            // Load reference Page Layout
            File pageFromPageLayout = context.Site.RootWeb.GetFileByServerRelativeUrl(String.Format("{0}_catalogs/masterpage/{1}.aspx",
                UrlUtility.EnsureTrailingSlash(site.ServerRelativeUrl),
                pageTemplateName));
            ListItem pageLayoutItem = pageFromPageLayout.ListItemAllFields;
            context.Load(pageLayoutItem);
            context.ExecuteQueryRetry();

            // Create the publishing page
            PublishingWeb publishingWeb = PublishingWeb.GetPublishingWeb(context, web);
            context.Load(publishingWeb);

            // Configure the publishing page
            var pageInformation = new PublishingPageInformation
            {
                Name = !pageName.EndsWith(".aspx", StringComparison.InvariantCultureIgnoreCase) ? 
                    string.Format("{0}.aspx", pageName) : pageName,
                PageLayoutListItem = pageLayoutItem,
            };

            // Handle target folder, if any
            if (folder != null)
            {
                pageInformation.Folder = folder;
            }
            PublishingPage page = publishingWeb.AddPublishingPage(pageInformation);

            // Get parent list of item, this way we can handle all languages
            List pagesLibrary = page.ListItem.ParentList;
            context.Load(pagesLibrary);
            context.ExecuteQueryRetry();
            ListItem pageItem = page.ListItem;
            pageItem["Title"] = title;
            pageItem.Update();

            // Checkin the page file, if needed
            web.Context.Load(pageItem, p => p.File.CheckOutType);
            web.Context.ExecuteQueryRetry();
            if (pageItem.File.CheckOutType != CheckOutType.None)
            {
                pageItem.File.CheckIn(String.Empty, CheckinType.MajorCheckIn);
            }

            // Publish the page, if required
            if (publish)
            {
                pageItem.File.Publish(String.Empty);
                if (pagesLibrary.EnableModeration)
                {
                    pageItem.File.Approve(String.Empty);

                    // Setup scheduling, if required
                    if (schedule && startDate.HasValue)
                    {
                        page.StartDate = startDate.Value;
                        page.EndDate = endDate.HasValue ? endDate.Value : new DateTime(2050, 01, 01);
                        page.Schedule(String.Empty);
                    }
                }
            }
            context.ExecuteQueryRetry();
        }

        /// <summary>
        /// Adds a user-friendly URL for a PublishingPage object.
        /// </summary>
        /// <param name="page">The target page to add to managed navigation.</param>
        /// <param name="web">The target web.</param>
        /// <param name="navigationTitle">The title for the navigation item.</param>
        /// <param name="friendlyUrlSegment">The user-friendly text to use as the URL segment.</param>
        /// <param name="editableParent">The parent NavigationTermSetItem object below which this new friendly URL should be created.</param>
        /// <param name="showInGlobalNavigation">Defines whether the navigation item has to be shown in the Global Navigation, optional and default to true.</param>
        /// <param name="showInCurrentNavigation">Defines whether the navigation item has to be shown in the Current Navigation, optional and default to true.</param>
        /// <returns>The simple link URL just created.</returns>
        public static String AddNavigationFriendlyUrl(this PublishingPage page, Web web, 
            String navigationTitle, String friendlyUrlSegment, NavigationTermSetItem editableParent, 
            Boolean showInGlobalNavigation = true, Boolean showInCurrentNavigation = true)
        {
            // Add the Friendly URL
            var friendlyUrl = page.AddFriendlyUrl(friendlyUrlSegment, editableParent, true);

            // Retrieve terms for searching parent
            web.Context.Load(editableParent.Terms, ts => ts.Include(t => t.FriendlyUrlSegment, t => t.Title));
            web.Context.ExecuteQueryRetry();

            // Configure the friendly URL Title
            NavigationTerm friendlyUrlTerm = editableParent.Terms
                .FirstOrDefault(t => t.FriendlyUrlSegment.Value == friendlyUrlSegment);
            if (friendlyUrlTerm != null)
            {
                // Assign term label for taxonomy equal to navigation item title
                Term friendlyUrlBackingTerm = friendlyUrlTerm.GetTaxonomyTerm();
                web.Context.Load(friendlyUrlBackingTerm, t => t.Labels);
                web.Context.ExecuteQueryRetry();

                Label defaultLabel = friendlyUrlBackingTerm.Labels.FirstOrDefault(l => l.IsDefaultForLanguage);
                if (defaultLabel != null)
                {
                    defaultLabel.Value = navigationTitle;
                }

                // Configure the navigation settings
                if (!showInGlobalNavigation || !showInCurrentNavigation)
                {
                    if (!showInGlobalNavigation && !showInCurrentNavigation)
                    {
                        friendlyUrlBackingTerm.SetLocalCustomProperty("_Sys_Nav_ExcludedProviders", "\"GlobalNavigationTaxonomyProvider\",\"CurrentNavigationTaxonomyProvider\"");
                    }
                    if (!showInGlobalNavigation)
                    {
                        friendlyUrlBackingTerm.SetLocalCustomProperty("_Sys_Nav_ExcludedProviders", "\"GlobalNavigationTaxonomyProvider\"");
                    }
                    else
                    {
                        friendlyUrlBackingTerm.SetLocalCustomProperty("_Sys_Nav_ExcludedProviders", "\"CurrentNavigationTaxonomyProvider\"");
                    }
                }

                // Assign term title for site navigation
                friendlyUrlTerm.Title.Value = navigationTitle;
                friendlyUrlTerm.GetTaxonomyTermStore().CommitAll();
                web.Context.ExecuteQueryRetry();
            }

            return (friendlyUrl.Value);
        }

        /// <summary>
        /// Gets a Publishing Page from the root folder of the Pages library.
        /// </summary>
        /// <param name="web">The web.</param>
        /// <param name="fileLeafRef">The file leaf reference.</param>
        /// <returns>The PublishingPage object, if any. Otherwise null.</returns>
        /// <exception cref="System.ArgumentNullException">fileLeafRef</exception>
        /// <exception cref="System.ArgumentException">fileLeafRef</exception>
        public static PublishingPage GetPublishingPage(this Web web, string fileLeafRef)
        {
            return (web.GetPublishingPage(fileLeafRef, null));
        }

        /// <summary>
        /// Gets a Publishing Page from any folder in the Pages library.
        /// </summary>
        /// <param name="web">The web.</param>
        /// <param name="fileLeafRef">The file leaf reference.</param>
        /// <param name="folder">The folder where to search the page.</param>
        /// <returns>The PublishingPage object, if any. Otherwise null.</returns>
        /// <exception cref="System.ArgumentNullException">fileLeafRef</exception>
        /// <exception cref="System.ArgumentException">fileLeafRef</exception>
        public static PublishingPage GetPublishingPage(this Web web, string fileLeafRef, Folder folder)
        {
            if (string.IsNullOrEmpty(fileLeafRef))
            {
                throw (fileLeafRef == null)
                  ? new ArgumentNullException("fileLeafRef")
                  : new ArgumentException(CoreResources.Exception_Message_EmptyString_Arg, "fileLeafRef");
            }

            var context = web.Context as ClientContext;
            List pages = web.GetPagesLibrary();
            // Get the language agnostic "Pages" library name         
            context.Load(pages, p => p.RootFolder, p => p.ItemCount);
            context.ExecuteQueryRetry();

            if (pages != null && pages.ItemCount > 0)
            {
                CamlQuery camlQuery = new CamlQuery();
                camlQuery.FolderServerRelativeUrl = folder != null ? folder.ServerRelativeUrl : pages.RootFolder.ServerRelativeUrl;
                camlQuery.ViewXml = string.Format(@"<View Scope='RecursiveAll'>  
                                                        <Query> 
                                                           <Where><Eq><FieldRef Name='FileLeafRef' /><Value Type='Text'>{0}</Value></Eq></Where> 
                                                        </Query> 
                                                    </View>", fileLeafRef);

                ListItemCollection listItems = pages.GetItems(camlQuery);
                context.Load(listItems);
                context.ExecuteQueryRetry();

                if (listItems.Count > 0)
                {
                    PublishingPage page = PublishingPage.GetPublishingPage(context, listItems[0]);
                    context.Load(page);
                    context.ExecuteQueryRetry();
                    return page;
                }
            }

            return null;
        }
    }
}
