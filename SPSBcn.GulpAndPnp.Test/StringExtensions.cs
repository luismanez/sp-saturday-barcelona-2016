using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace SPSBcn.GulpAndPnp.Test
{
    public static class StringExtensions
    {
        public static SecureString ToSecureString(this string Source)
        {
            if (string.IsNullOrWhiteSpace(Source)) return null;

            SecureString Result = new SecureString();
            foreach (char c in Source.ToCharArray())
            {
                Result.AppendChar(c);
            }
            return Result;
        }

        #region Safe
        public static string GetPassword()
        {
            #region Do not show! :)
            return "12345.aa";
            #endregion
        }

        public static string GetUser()
        {
            #region Do not show! :)
            return "lmanez@lmanez.onmicrosoft.com";
            #endregion
        }
        #endregion
    }
}
