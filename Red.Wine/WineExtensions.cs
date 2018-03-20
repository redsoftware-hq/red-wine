using System;
using System.Data.Entity;
using System.Linq;
using System.Reflection;

namespace Red.Wine
{
    public static class WineExtensions
    {
        public static T ToEnum<T>(this string value)
        {
            return (T)Enum.Parse(typeof(T), value);
        }

        public static bool IsNormalProperty(this PropertyInfo propertyInfo)
        {
            if (propertyInfo.PropertyType.IsValueType || propertyInfo.PropertyType.Equals(typeof(string)))
            {
                return true;
            }

            return false;
        }

        public static bool IsEnumProperty(this PropertyInfo propertyInfo)
        {
            if (propertyInfo.PropertyType.IsEnum)
            {
                return true;
            }

            return false;
        }

        public static bool IsObjectProperty(this PropertyInfo propertyInfo)
        {
            if (propertyInfo.PropertyType.IsClass && !propertyInfo.PropertyType.IsGenericType)
            {
                return true;
            }

            return false;
        }

        public static bool IsCollectionProperty(this PropertyInfo propertyInfo)
        {
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(propertyInfo.PropertyType) && propertyInfo.PropertyType.IsGenericType)
            {
                return true;
            }

            return false;
        }

        public static void UpdateContextWithDefaultValues(this DbContext context, string userId)
        {
            var entries = context.ChangeTracker.Entries<WineModel>().Where(x => x.State == EntityState.Modified || x.State == EntityState.Added);

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.SetWhenModifying(userId, DateTime.Now);
                }
                else if (entry.State == EntityState.Added && entry.Entity.Id == null)
                {
                    entry.Entity.SetWhenInserting(
                        Guid.NewGuid().ToString(),
                        userId,
                        DateTime.Now,
                        true,
                        GetIncrementedKeyId(context, entry.Entity));
                }
            }
        }

        public static long GetIncrementedKeyId(DbContext context, WineModel entity)
        {
            var dbSet = context.Set(entity.GetType());
            var dbEntitiesList = Enumerable.Cast<WineModel>(dbSet).ToList();
            var localDbEntitiesList = Enumerable.Cast<WineModel>(dbSet.Local).ToList();
            long currentCount = 0;

            if (dbEntitiesList.Count > 0)
            {
                var lastInsertedEntity = dbEntitiesList
                    .OrderByDescending(t => t.KeyId)
                    .First();

                currentCount = lastInsertedEntity.KeyId;
            }

            if (localDbEntitiesList.Count > 0)
            {
                var lastLocalEntity = localDbEntitiesList
                    .OrderByDescending(t => t.KeyId)
                    .First();

                if (lastLocalEntity.KeyId > currentCount)
                {
                    currentCount = lastLocalEntity.KeyId;
                }
            }

            return ++currentCount;
        }
    }
}
