﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OfficeDevPnP.Core.Framework.Provisioning.Model
{
    public partial class TermLabel : BaseModel, IEquatable<TermLabel>
    {

        #region Public Members
        public int Language { get; set; }
        public bool IsDefaultForLanguage { get; set; }
        public string Value { get; set; }

        #endregion

        #region Constructors
        #endregion

        #region Comparison code

        public override int GetHashCode()
        {
            return (String.Format("{0}|{1}",
                this.Language.GetHashCode(),
                (this.Value != null ? this.Value.GetHashCode() : 0)
            ).GetHashCode());
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TermLabel))
            {
                return (false);
            }
            return (Equals((TermLabel)obj));
        }

        public bool Equals(TermLabel other)
        {
            if (other == null)
            {
                return (false);
            }

            return (this.Language == other.Language &&
                this.Value == other.Value);
        }

        #endregion
    }
}
