﻿using Red.Wine.Attributes;
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

        public virtual TEntity CreateNewWineModel(object options)
        {
            TEntity entity = _dbSet.Create();

            Copy(options, entity);
            entity.SetDefaults(_context, _userId);
            _dbSet.Add(entity);

            return entity;
        }

        public virtual TEntity UpdateExistingWineModel(TEntity entity, object options)
        {
            Copy(options, entity);
            entity.SetDefaults(_context, _userId);
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;

            return entity;
        }

        private void Copy(object from, WineModel to)
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
                        var toRepositoryInstance = (dynamic)Activator.CreateInstance(toRepositoryType, _context, _userId);

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
                                var hasId = item.GetType().GetProperty("Id");
                                if (hasId != null && item.Id != null)
                                {
                                    var itemToUpdate = ((System.Collections.IEnumerable)toCollection).Cast<dynamic>().Where(i => i.Id == item.Id).FirstOrDefault();

                                    if (itemToUpdate == null)
                                    {
                                        throw new Exception("Something is wrong with Id - Either mismatch or absence.");
                                    }

                                    toRepositoryInstance.Copy(item, itemToUpdate);
                                }
                                else if (hasId == null || item.Id == null)
                                {
                                    var itemToAdd = (dynamic)Activator.CreateInstance(copyToAttribute.To);
                                    toRepositoryInstance.Copy(item, itemToAdd);
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
                            var fromRepositoryInstance = (dynamic)Activator.CreateInstance(fromRepositoryType, _context, _userId);

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
                                    var repositoryInstance = (dynamic)Activator.CreateInstance(repositoryType, _context, _userId);

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
                            catch
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
    }
}
