using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;

namespace Red.Wine
{
    public class WineModel
    {
        public virtual string LastModifiedBy { get; protected internal set; }
        [Column(TypeName = "datetime2")]
        public virtual DateTime LastModifiedOn { get; protected internal set; }
        public virtual string Id { get; protected internal set; }
        public virtual string CreatedBy { get; protected internal set; }
        [Column(TypeName = "datetime2")]
        public virtual DateTime CreatedOn { get; protected internal set; }
        public virtual bool IsActive { get; protected internal set; }
        public virtual long KeyId { get; protected internal set; }
        public virtual bool IsDeleted { get; set; }

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

        public void SetDefaults(DbContext context, string userId)
        {
            if(Id == null)
            {
                LastModifiedBy = userId;
                LastModifiedOn = DateTime.Now;
                Id = Guid.NewGuid().ToString();
                CreatedBy = userId;
                CreatedOn = DateTime.Now;
                IsActive = true;
                KeyId = GetIncrementedKeyId(context, this);
            }
        }

        private static long GetIncrementedKeyId(DbContext context, WineModel entity)
        {
            var dbSet = context.Set(entity.GetType());
            var entityList = Enumerable.Cast<WineModel>(dbSet).ToList();
            long currentCount = 0;

            if (entityList.Count > 0)
            {
                var lastInsertedEntity = entityList
                    .OrderByDescending(t => t.KeyId)
                    .First();

                currentCount = lastInsertedEntity.KeyId;
            }

            return ++currentCount;
        }
    }
}
