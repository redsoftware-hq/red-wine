using Red.Wine.Attributes;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Red.Wine
{
    public class WineRepository<TEntity> where TEntity : WineModel
    {
        private DbContext _context;
        private DbSet<TEntity> _dbSet;
        private readonly string _userId;

        public WineRepository(DbContext context, string userId)
        {
            _context = context;
            _dbSet = _context.Set<TEntity>();
            _userId = userId;
        }

        public virtual IEnumerable<TEntity> GetWithRawSql(string query, params object[] parameters)
        {
            return _dbSet.SqlQuery(query, parameters).ToList();
        }

        public virtual IEnumerable<TEntity> Get(
            Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            string includeProperties = "")
        {
            IQueryable<TEntity> query = _dbSet;

            var param = Expression.Parameter(typeof(TEntity));
            var isDeletedCondition = Expression.Lambda<Func<TEntity, bool>>(
                            Expression.Equal(
                                Expression.Property(param, "IsDeleted"),
                                Expression.Constant(false, typeof(bool))
                            ),
                            param
                        );

            query = query.Where(isDeletedCondition);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            foreach (var includeProperty in includeProperties.Split
                (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty);
            }

            if (orderBy != null)
            {
                return orderBy(query).ToList();
            }
            else
            {
                return query.ToList();
            }
        }

        public virtual TEntity GetByID(object id)
        {
            return _dbSet.Find(id);
        }

        public virtual TEntity Insert(object entitySubset)
        {
            TEntity entity = Activator.CreateInstance<TEntity>();

            Copy(entity, entitySubset);
            _dbSet.Add(entity);
            UpdateContextWithDefaultValues();

            return entity;
        }

        public virtual TEntity Update(object entitySubset)
        {
            string id = entitySubset.GetType().GetProperty("Id").GetValue(entitySubset, null).ToString();
            TEntity entity = GetByID(id);

            _dbSet.Attach(entity);
            Copy(entity, entitySubset);
            UpdateContextWithDefaultValues();

            return entity;
        }

        public virtual TEntity Delete(object id)
        {
            TEntity entity = _dbSet.Find(id);

            if (_context.Entry(entity).State == EntityState.Detached)
            {
                _dbSet.Attach(entity);
            }

            _dbSet.Remove(entity);

            return entity;
        }

        private void Copy(TEntity to, object from)
        {
            var propertyInfoArray = from.GetType().GetProperties();

            foreach (var propertyInfo in propertyInfoArray)
            {
                try
                {
                    var copyToAttribute = propertyInfo.GetCustomAttribute<CopyToAttribute>();
                    var ignoreWhileCopyAttribute = propertyInfo.GetCustomAttribute<IgnoreWhileCopyAttribute>();

                    if (ignoreWhileCopyAttribute != null)
                    {
                        continue;
                    }

                    if (propertyInfo.IsCollectionProperty())
                    {
                        if (copyToAttribute == null)
                        {
                            throw new Exception("No CopyTo attribute applied on property " + propertyInfo.PropertyType.FullName);
                        }

                        var toCollection = copyToAttribute.Name != null ? (dynamic)to.GetType().GetProperty(copyToAttribute.Name).GetValue(to) : (dynamic)to.GetType().GetProperty(propertyInfo.Name).GetValue(to);
                        var toCollectionItemType = copyToAttribute.To;
                        var fromCollection = (System.Collections.IEnumerable)propertyInfo.GetValue(from);
                        var fromCollectionItemType = fromCollection.GetType().GetGenericArguments().Single();
                        var toRepositoryType = typeof(WineRepository<>).MakeGenericType(toCollectionItemType);
                        var toRepositoryInstance = (dynamic)Activator.CreateInstance(toRepositoryType, _context);

                        if (copyToAttribute.Relationship == Relationship.Dependency)
                        {
                            if (fromCollectionItemType == typeof(string) && copyToAttribute.IsForeignKey)
                            {
                                foreach (string item in fromCollection)
                                {
                                    toCollection.Add(toRepositoryInstance.GetByID(item));
                                }
                            }
                            else if (fromCollectionItemType == typeof(WineModel))
                            {
                                foreach (WineModel item in fromCollection)
                                {
                                    toCollection.Add(toRepositoryInstance.GetByID(item.Id));
                                }
                            }
                        }
                        else if (copyToAttribute.Relationship == Relationship.Dependent)
                        {
                            foreach (dynamic item in fromCollection)
                            {
                                if (item.Id != null)
                                {
                                    var itemToUpdate = ((System.Collections.IEnumerable)toCollection).Cast<dynamic>().Where(i => i.Id == item.Id).FirstOrDefault();

                                    if (itemToUpdate == null)
                                    {
                                        throw new Exception("Something is wrong with Id - Either mismatch or absence.");
                                    }

                                    toRepositoryInstance.Copy(itemToUpdate, item);
                                }
                                else if (item.Id == null)
                                {
                                    var itemToAdd = (dynamic)Activator.CreateInstance(copyToAttribute.To);
                                    toRepositoryInstance.Copy(itemToAdd, item);
                                    toCollection.Add(itemToAdd);
                                }
                            }
                        }
                        else if (copyToAttribute.Relationship == Relationship.Seed)
                        {
                            Mapper mapper = new Mapper();
                            var methodInfo = mapper.GetType().GetMethod("Map");
                            var genericMethodInfo = methodInfo.MakeGenericMethod(copyToAttribute.From, copyToAttribute.To);
                            var fromRepositoryType = typeof(WineRepository<>).MakeGenericType(copyToAttribute.From);
                            var fromRepositoryInstance = (dynamic)Activator.CreateInstance(fromRepositoryType, _context);

                            if (fromCollectionItemType == typeof(string))
                            {
                                foreach (string item in fromCollection)
                                {
                                    var fromItem = fromRepositoryInstance.GetByID(item);
                                    var toItem = genericMethodInfo.Invoke(mapper, new object[] { fromItem });
                                    toCollection.GetType().GetMethod("Add").Invoke(toCollection, new object[] { toItem });
                                }
                            }
                        }
                    }
                    else if (propertyInfo.IsNormalProperty())
                    {
                        if (copyToAttribute != null)
                        {
                            if (copyToAttribute.IsForeignKey)
                            {
                                if (propertyInfo.GetValue(from) == null)
                                {
                                    // Do nothing
                                }
                                else
                                {
                                    var repositoryType = typeof(WineRepository<>).MakeGenericType(copyToAttribute.To);
                                    var repositoryInstance = (dynamic)Activator.CreateInstance(repositoryType, _context);

                                    string entityName = null;

                                    if (string.IsNullOrEmpty(copyToAttribute.Name))
                                    {
                                        // Foreign key is conventionally named as TEntityNameId
                                        entityName = propertyInfo.Name.Remove(propertyInfo.Name.Length - 2);
                                    }
                                    else
                                    {
                                        entityName = copyToAttribute.Name;
                                    }

                                    var entityPropertyInfo = to.GetType().GetProperty(entityName);

                                    if (propertyInfo.GetValue(from) != null)
                                    {
                                        entityPropertyInfo.SetValue(to, repositoryInstance.GetByID(propertyInfo.GetValue(from)));
                                    }
                                }
                            }
                            else
                            {
                                throw new Exception("Not marked as foreign key");
                            }
                        }

                        if (to.GetType().GetProperty(propertyInfo.Name).PropertyType.IsEnum)
                        {
                            var enumType = Type.GetType(to.GetType().GetProperty(propertyInfo.Name).PropertyType.FullName);
                            MethodInfo methodInfo = typeof(WineExtensions).GetMethod("ToEnum");
                            dynamic enumValue = new System.Dynamic.ExpandoObject();

                            try
                            {
                                enumValue = methodInfo.MakeGenericMethod(new[] { enumType }).Invoke(propertyInfo.GetValue(from), new[] { propertyInfo.GetValue(from) });
                            }
                            catch (Exception e)
                            {
                                enumValue = -1;
                            }

                            _context.Entry(to).Property(propertyInfo.Name).CurrentValue = enumValue;
                        }
                        else
                        {
                            _context.Entry(to).Property(propertyInfo.Name).CurrentValue = propertyInfo.GetValue(from);
                        }
                    }
                }
                catch (Exception e)
                {
                    var message = "Something is wrong with " + propertyInfo.Name;
                    throw new Exception(message, e);
                }
            }
        }

        private void UpdateContextWithDefaultValues()
        {
            var entries = _context.ChangeTracker.Entries<WineModel>().Where(x => x.State == EntityState.Modified || x.State == EntityState.Added);

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.LastModifiedBy = _userId;
                    entry.Entity.LastModifiedOn = DateTime.Now;
                }
                else if (entry.State == EntityState.Added)
                {
                    entry.Entity.Id = Guid.NewGuid().ToString();
                    entry.Entity.CreatedBy = _userId;
                    entry.Entity.CreatedOn = DateTime.Now;
                    entry.Entity.IsActive = true;
                    entry.Entity.KeyId = GetIncrementedKeyId(); //Bug: entry.Entity type needs to be passed
                }
            }
        }

        // Needs to operate on the type passed. _dbSet is useless.
        private long GetIncrementedKeyId()
        {
            var entity = _dbSet.OrderByDescending(t => t.KeyId).First();
            return ++entity.KeyId;
        }
    }
}
