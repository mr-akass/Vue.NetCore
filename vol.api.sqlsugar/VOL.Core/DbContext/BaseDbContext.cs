using SqlSugar;
using VOL.Core.DBManager;

namespace VOL.Core.DbContext
{
    public abstract class BaseDbContext : DbContext
    {

        public virtual ISqlSugarClient SqlSugarClient { get; set; }

        public bool QueryTracking
        {
            set { }
        }
        public BaseDbContext() : base() { }

        /// <summary>
        /// 取指定实体应该使用的数据库连接：实体上配了 [Entity(DBServer="连接名")] 就切到那个库，
        /// 否则原样返回默认连接(框架表与绝大多数实体都走这条路，行为与改造前一致)
        ///
        /// 之所以按泛型参数而不是按 DbContext 实例来路由：一个 Repository/Service 的方法里会同时
        /// 操作主表、明细表、Sys_* 框架表，只有按每次访问的实体类型判断才不会把框架表带到业务库去
        /// </summary>
        public virtual ISqlSugarClient GetClient<TEntity>()
        {
            return EntityDbRouter.Route(typeof(TEntity), SqlSugarClient);
        }

        public virtual ISugarQueryable<TEntity> Set<TEntity>(bool filterDeleted = false) where TEntity : class, new()
        {
            return GetClient<TEntity>().Queryable<TEntity>();
        }

        public int SaveChanges()
        {
            return SqlSugarClient.SaveQueues();
        }

        /// <summary>
        /// 提交指定实体所在库的队列：AddQueue 是挂在具体连接上的，
        /// 在业务库上排的队必须由同一个连接提交，否则队列里的数据会被静默丢弃
        /// </summary>
        public int SaveChanges<TEntity>()
        {
            return GetClient<TEntity>().SaveQueues();
        }

    }
}
