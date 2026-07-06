using System;

namespace HotSync.Models
{
    public class RigDetailsData
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string RigName { get; set; }
        public string RigLoginName { get; set; }
        public int RigId { get; set; }
        public bool Active { get; set; }

        public DateTime Created { get; set; }
        public int CreatedById { get; set; }
        public string CreatedByName { get; set; }
        public string CreatedByEmail { get; set; }

        public DateTime Modified { get; set; }
        public int ModifiedById { get; set; }
        public string ModifiedByName { get; set; }
        public string ModifiedByEmail { get; set; }

        public string Flag { get; set; }
    }
}