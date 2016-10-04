﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OfficeDevPnP.Core.Utilities
{
    public static class PnPCoreUtilities
    {
        /// <summary>
        /// Get's a tag that identifies the PnP Core library
        /// </summary>
        /// <returns>PnP Core library identification tag</returns>
        public static string PnPCoreVersionTag
        {
            get
            {
                return (PnPCoreVersionTagLazy.Value);
            }
        }

        private static Lazy<String> PnPCoreVersionTagLazy = new Lazy<String>(
            () => {
                Assembly coreAssembly = Assembly.GetExecutingAssembly();
                String result = String.Format("PnPCore:{0}", ((AssemblyFileVersionAttribute)coreAssembly.GetCustomAttribute(typeof(AssemblyFileVersionAttribute))).Version.Split('.')[2]);
                return (result);
            }, 
            true);
    }
}
