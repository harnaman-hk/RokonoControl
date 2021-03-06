﻿using System;
using System.Collections.Generic;

namespace RCServerCli.Models
{
    public partial class UserAccounts
    {
        public UserAccounts()
        {
            AssociatedProjectMemberRights = new HashSet<AssociatedProjectMemberRights>();
            AssociatedProjectMembers = new HashSet<AssociatedProjectMembers>();
            WorkItem = new HashSet<WorkItem>();
        }

        public int Id { get; set; }
        public string Password { get; set; }
        public DateTime CreationDate { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? ProjectRights { get; set; }
        public string GitUsername { get; set; }
        public string Salt { get; set; }

        public virtual ICollection<AssociatedProjectMemberRights> AssociatedProjectMemberRights { get; set; }
        public virtual ICollection<AssociatedProjectMembers> AssociatedProjectMembers { get; set; }
        public virtual ICollection<WorkItem> WorkItem { get; set; }
    }
}
