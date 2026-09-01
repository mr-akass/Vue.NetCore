using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VOL.Core.BaseProvider;
using VOL.Core.Configuration;
using VOL.Core.DbContext;
using VOL.Core.Enums;
using VOL.Core.Extensions;
using VOL.Core.UserManager;


namespace VOL.Core.DBManager
{
    public static class SqlSugarExtension
    {

        public static int Add<T>(this BaseDbContext dbContext, T table, bool saveChange = false) where T : class, new()
        {
            return dbContext.GetClient<T>().Add(table, saveChange);
        }

        public static int Add<T>(this ISqlSugarClient sqlSugarClient, T table, bool saveChange = false) where T : class, new()
        {
            //所有写入入口统一按实体路由：AddQueue排的队和提交队列必须是同一个连接，
            //否则分库的表会出现"接口成功但数据没进库"
            sqlSugarClient = EntityDbRouter.Route<T>(sqlSugarClient);
            sqlSugarClient.Insertable(table).AddQueue();
            if (saveChange)
            {
                return sqlSugarClient.SaveQueues();
            }
            return 1;
        }
        public static int AddWithSetIdentity<T>(this ISqlSugarClient sqlSugarClient, T entity, bool saveChange = false) where T : class, new()
        {
            sqlSugarClient = EntityDbRouter.Route<T>(sqlSugarClient);
            if (typeof(T).GetSugarSplitTable() != null)
            {
                sqlSugarClient.Insertable(entity).SplitTable().ExecuteCommand();
            }
            else
            {
                sqlSugarClient.Insertable(entity).ExecuteReturnEntity();
            }
            return 1;
        }
        public static async Task<int> AddWithSetIdentityAsync<T>(this ISqlSugarClient sqlSugarClient, T entity, bool saveChange = false) where T : class, new()
        {
            sqlSugarClient = EntityDbRouter.Route<T>(sqlSugarClient);
            if (typeof(T).GetSugarSplitTable() != null)
            {
                await sqlSugarClient.Insertable(entity).SplitTable().ExecuteCommandAsync();
            }
            else
            {
                await sqlSugarClient.Insertable(entity).ExecuteReturnEntityAsync();
            }
            return 1;
        }

        public static async Task<int> AddAsync<T>(this BaseDbContext dbContext, T list, bool saveChange = false) where T : class, new()
        {
            var client = dbContext.GetClient<T>();
            client.Insertable(list).AddQueue();
            if (saveChange)
            {
                return await client.SaveQueuesAsync();
            }
            return 1;
        }

        public static int AddRange<T>(this BaseDbContext dbContext, List<T> list, bool saveChange = false) where T : class, new()
        {
            var client = dbContext.GetClient<T>();
            if (typeof(T).GetSugarSplitTable() != null)
            {
                client.Insertable(list).SplitTable().ExecuteCommand();
                return list.Count;
            }
            client.Insertable(list).AddQueue();
            if (saveChange)
            {
                return client.SaveQueues();
            }
            return list.Count;
        }

        public static async Task<int> AddRangeAsync<T>(this BaseDbContext dbContext, List<T> list, bool saveChange = false) where T : class, new()
        {
            var client = dbContext.GetClient<T>();
            if (typeof(T).GetSugarSplitTable() != null)
            {
                await client.Insertable(list).SplitTable().ExecuteCommandAsync();
                return list.Count;
            }
            client.Insertable(list).AddQueue();
            if (saveChange)
            {
                return await client.SaveQueuesAsync();
            }
            return list.Count;
        }
        public static int Update<TSource>(this BaseDbContext dbContext, TSource entity, bool saveChanges = false) where TSource : class, new()
        {
            return UpdateRange<TSource>(dbContext, new List<TSource>() { entity }, new string[] { }, saveChanges);
        }
        public static int Update<TSource>(this BaseDbContext dbContext, TSource entity, Expression<Func<TSource, object>> updateMainFields, bool saveChanges = false) where TSource : class, new()
        {
            return UpdateRange<TSource>(dbContext, new List<TSource>() { entity }, updateMainFields.GetExpressionProperty(), saveChanges);
        }

        public static int Update<TSource>(this BaseDbContext dbContext, TSource entity, string[] properties, bool saveChanges = false) where TSource : class, new()
        {
            return UpdateRange<TSource>(dbContext, new List<TSource>() { entity }, properties, saveChanges);
        }
        public static int UpdateRange<TSource>(this BaseDbContext dbContext, IEnumerable<TSource> entities, bool saveChanges = false) where TSource : class, new()
        {
            return UpdateRange<TSource>(dbContext, entities, new string[] { }, saveChanges);
        }
        public static int UpdateRange<TSource>(this BaseDbContext dbContext, IEnumerable<TSource> entities, Expression<Func<TSource, object>> updateMainFields, bool saveChanges = false) where TSource : class, new()
        {
            return UpdateRange<TSource>(dbContext, entities, updateMainFields.GetExpressionProperty(), saveChanges);
        }
        public static int UpdateRange<TSource>(this BaseDbContext dbContext, IEnumerable<TSource> entities, string[] properties, bool saveChanges = false) where TSource : class, new()
        {
            return dbContext.GetClient<TSource>().UpdateRange<TSource>(entities, properties, saveChanges);
        }
        public static int Update<TSource>(this ISqlSugarClient sqlSugarClient, TSource entity, string[] properties, bool saveChanges = false) where TSource : class, new()
        {
            return sqlSugarClient.UpdateRange<TSource>(new List<TSource>() { entity }, properties, saveChanges);
        }

        //public static int Update<TSource>(this SqlSugarScope sqlSugarScope, TSource entity, string[] properties, bool saveChanges = false) where TSource : class, new()
        //{
        //    return sqlSugarScope.UpdateRange<TSource>(new List<TSource>() { entity }, properties, saveChanges);
        //}

        public static int UpdateRange<TSource>(this ISqlSugarClient sqlSugarClient, IEnumerable<TSource> entities, string[] properties, bool saveChanges = false) where TSource : class, new()
        {
            if (entities.Count() == 0)
            {
                return 0;
            }
            if (properties != null && properties.Length > 0)
            {
                PropertyInfo[] entityProperty = typeof(TSource).GetProperties();
                string keyName = entityProperty.GetKeyName();
                if (properties.Contains(keyName))
                {
                    properties = properties.Where(x => x != keyName).ToArray();
                }
                properties = properties.Where(x => entityProperty.Select(s => s.Name).Contains(x)).ToArray();
            }
            bool splitTable = typeof(TSource).GetSugarSplitTable() != null;
            IUpdateable<TSource> updateable = null;
            //按实体路由到它所在的库(AddQueue与SaveQueues必须是同一个连接)
            sqlSugarClient = EntityDbRouter.Route<TSource>(sqlSugarClient);
            if (properties == null || properties.Length == 0)
            {
                updateable = sqlSugarClient.Updateable<TSource>(entities.ToList());//.AddQueue();
            }
            else
            {
                updateable = sqlSugarClient.Updateable<TSource>(entities.ToList()).UpdateColumns(properties);//.AddQueue();
            }
            if (splitTable)
            {
                updateable.SplitTable().ExecuteCommand();
                return entities.Count();
            }
            updateable.AddQueue();
            if (!saveChanges)
            {
                return 0;
            }
            return sqlSugarClient.SaveQueues();
        }


        public static Task<T> FirstOrDefaultAsync<T>(this ISugarQueryable<T> queryable)
        {
            return queryable.FirstAsync();
        }

        public static T FirstOrDefault<T>(this ISugarQueryable<T> queryable)
        {
            return queryable.First();
        }
        public static T FindById<T>(this BaseDbContext dbContext, object id) where T : class, new()
        {
            Type type = typeof(T);
            var keyPro = type.GetKeyProperty();
            var methods = typeof(SqlSugarExtension)
                        .GetMethods(BindingFlags.NonPublic | BindingFlags.Static);
            return (methods.First(m => m.Name == nameof(FindQueryableId))
                          .MakeGenericMethod(type, keyPro.PropertyType)
                          .Invoke(null, [dbContext, id, keyPro.Name]) as ISugarQueryable<T>).First();
        }

        public static async Task<T> FindByIdAsync<T>(this BaseDbContext dbContext, object id) where T : class, new()
        {
            Type type = typeof(T);
            var keyPro = type.GetKeyProperty();
            var methods = typeof(SqlSugarExtension)
                        .GetMethods(BindingFlags.NonPublic | BindingFlags.Static);
            return await (methods.First(m => m.Name == nameof(FindQueryableId))
                          .MakeGenericMethod(type, keyPro.PropertyType)
                          .Invoke(null, [dbContext, id, keyPro.Name]) as ISugarQueryable<T>).FirstAsync();
        }

        private static ISugarQueryable<Entity> FindQueryableId<Entity, KeyType>(this BaseDbContext dbContext, object key, string keyName = null) where Entity : class, new()
        {
            var keyValue = (KeyType)(key.ChangeType(typeof(KeyType)));
            keyName ??= typeof(Entity).GetKeyName();
            var expression = keyName.CreateExpression<Entity>(keyValue, LinqExpressionType.Equal);
            return dbContext.SqlSugarClient.Set<Entity>().Where(expression);
        }
        public static ISugarQueryable<T> Include<T, TProperty>(this ISugarQueryable<T> queryable, Expression<Func<T, TProperty>> incluedProperty) where T : new() where TProperty : new()
        {
            return queryable.Includes(incluedProperty);
        }

        public static T First<T>(this ISugarQueryable<T> queryable)
        {
            return queryable.First();
        }

        public static ISugarQueryable<T> ThenByDescending<T>(this ISugarQueryable<T> queryable, Expression<Func<T, object>> expression)
        {
            return queryable.OrderByDescending(expression);
        }


        public static int SaveChanges(this ISqlSugarClient sqlSugarClient)
        {
            return sqlSugarClient.SaveQueues();
        }

        public static async Task<int> SaveChangesAsync(this ISqlSugarClient sqlSugarClient)
        {
            return await sqlSugarClient.SaveQueuesAsync();
        }

        public static ISugarQueryable<TEntity> Set<TEntity>(this ISqlSugarClient sqlSugarClient, bool filterDeleted = false) where TEntity : class, new()
        {
            //按实体上的[Entity(DBServer)]路由：传进来的是总入口(SqlSugarScope)时才切库，
            //已经是具体连接的(调用方明确指定过库)保持原样。
            //这样 DbManger.Db.Set<T>()、WorkFlowManager 里的动态分库查询都会自动走对的库
            return EntityDbRouter.Route<TEntity>(sqlSugarClient).Queryable<TEntity>();
        }

        public static List<T> QueryList<T>(this ISqlSugarClient sqlSugarClient, string sql, object parameters)
        {
            return sqlSugarClient.Ado.SqlQuery<T>(sql, parameters);
        }
        public static object ExecuteScalar(this ISqlSugarClient sqlSugarClient, string sql, object parameters)
        {
            return sqlSugarClient.Ado.GetScalar(sql, parameters);
        }
        public static int ExcuteNonQuery(this ISqlSugarClient sqlSugarClient, string sql, object parameters)
        {
            return sqlSugarClient.Ado.ExecuteCommand(sql, parameters);
        }
        public static ISqlSugarClient SetTimout(this ISqlSugarClient sqlSugarClient, int time)
        {
            return sqlSugarClient;
        }

        public static ISugarQueryable<T> FilterLogicDel<T>(this ISugarQueryable<T> query) where T : class, new()
        {
            return query;
        }
    }
}
