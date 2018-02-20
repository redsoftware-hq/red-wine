using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Red.Wine
{
    public class BaseModel
    {
        public string LastModifiedBy { get; protected internal set; }
        [Column(TypeName = "datetime2")]
        public DateTime LastModifiedOn { get; protected internal set; }
        public string Id { get; protected internal set; }
        public string CreatedBy { get; protected internal set; }
        [Column(TypeName = "datetime2")]
        public DateTime CreatedOn { get; protected internal set; }
        public bool IsActive { get; protected internal set; }
        public long KeyId { get; protected internal set; }

        public void Set(string lastModifiedBy, DateTime lastModifiedOn, string id, string createdBy, DateTime createdOn, bool isActive, long keyId)
        {
            LastModifiedBy = lastModifiedBy;
            LastModifiedOn = lastModifiedOn;
            Id = id;
            CreatedBy = createdBy;
            CreatedOn = createdOn;
            IsActive = isActive;
            KeyId = keyId;
        }

        public void SetWhenModifying(string lastModifiedBy, DateTime lastModifiedOn)
        {
            LastModifiedBy = lastModifiedBy;
            LastModifiedOn = lastModifiedOn;
        }

        public void SetWhenInserting(string id, string createdBy, DateTime createdOn, bool isActive, long keyId)
        {
            LastModifiedBy = createdBy;
            LastModifiedOn = createdOn;
            Id = id;
            CreatedBy = createdBy;
            CreatedOn = createdOn;
            IsActive = isActive;
            KeyId = keyId;
        }
    }
}
