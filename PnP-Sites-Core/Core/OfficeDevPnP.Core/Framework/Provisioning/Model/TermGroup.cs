﻿using System;
using System.Collections.Generic;
using System.Linq;
using OfficeDevPnP.Core.Extensions;

namespace OfficeDevPnP.Core.Framework.Provisioning.Model
{
    public partial class TermGroup : BaseModel, IEquatable<TermGroup>
    {
        #region Private Members

        private TermSetCollection _termSets;
        private Guid _id;
        private UserCollection _contributors;
        private UserCollection _managers;

        #endregion

        #region Public Members

        /// <summary>
        /// The ID of the TermGroup
        /// </summary>
        public Guid Id { get { return _id; } set { _id = value; } }

        /// <summary>
        /// The Name of the TermGroup
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The Description of the TermGroup
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Is this a site collection term group
        /// </summary>
        public bool SiteCollectionTermGroup { get; set; } = false;

        /// <summary>
        /// List of TermGroup Contributors
        /// </summary>
        public UserCollection Contributors
        {
            get { return (this._contributors); }
            private set { this._contributors = value; }
        }

        /// <summary>
        /// List of TermGroup Managers
        /// </summary>
        public UserCollection Managers
        {
            get { return (this._managers); }
            private set { this._managers = value; }
        }

        /// <summary>
        /// The collection of TermSet items in the TermGroup
        /// </summary>
        public TermSetCollection TermSets
        {
            get { return _termSets; }
            private set { _termSets = value; }
        }

        #endregion

        #region Constructors

        public TermGroup()
        {
            this._termSets = new TermSetCollection(this.ParentTemplate);
            this._contributors = new UserCollection(this.ParentTemplate);
            this._managers = new UserCollection(this.ParentTemplate);
        }

        public TermGroup(Guid id, string name, List<TermSet> termSets,
            bool siteCollectionTermGroup = false,
            IEnumerable<User> contributors = null, 
            IEnumerable<User> managers = null):
            this()
        {
            this.Id = id;
            this.Name = name;
            this.SiteCollectionTermGroup = siteCollectionTermGroup;
            this.TermSets.AddRange(termSets);
            this.Contributors.AddRange(contributors);
            this.Managers.AddRange(managers);
        }
        #endregion

        #region Comparison code

        public override int GetHashCode()
        {
            return (String.Format("{0}|{1}|{2}|{3}",
                (this.Id != null ? this.Id.GetHashCode() : 0),
                (this.Name != null ? this.Name.GetHashCode() : 0),
                (this.Description != null ? this.Description.GetHashCode() : 0),
                this.TermSets.Aggregate(0, (acc, next) => acc += (next != null ? next.GetHashCode() : 0))
            ).GetHashCode());
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TermGroup))
            {
                return (false);
            }
            return (Equals((TermGroup)obj));
        }

        public bool Equals(TermGroup other)
        {
            if (other == null)
            {
                return (false);
            }

            return (this.Id == other.Id &&
                this.Name == other.Name &&
                this.Description == other.Description &&
                this.TermSets.DeepEquals(other.TermSets));
        }

        #endregion
    }
}
