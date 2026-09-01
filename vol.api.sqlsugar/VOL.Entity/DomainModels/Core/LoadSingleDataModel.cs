using System.Collections.Generic;

namespace VOL.Entity.DomainModels
{
    public class PageDataOptions
    {
        public int Page { get; set; }
        public int Rows { get; set; }
        public int Total { get; set; }
        public string TableName { get; set; }
        public string DetailTable { get; set; }
        public string Sort { get; set; }
        /// <summary>
        /// 排序方式
        /// </summary>
        public string Order { get; set; }
        public string Wheres { get; set; }
        public bool Export { get; set; }
        public object Value { get; set; }


        /// <summary>
        /// 查询条件
        /// </summary>
        public List<SearchParameters> Filter { get; set; }

        public string[] Columns { get; set; }

        public Dictionary<string, string> Summary { get; set; }
    }
    public class SearchParameters
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string DisplayType { get; set; }

        /// <summary>
        /// in/notIn的多个值(2026.08.24表头筛选)：Value是逗号拼接的，值本身含逗号时会被拆坏
        /// (如地区名"北京市,新疆")。前端勾选多个值时同时传这个字段，后端优先用它；
        /// 没传时仍按Value拆逗号，兼容所有老页面与自定义查询
        /// </summary>
        public List<string> Values { get; set; }

        public List<string> Fields { get; set; }

        public string Group { get; set; }

        public bool Or { get; set; }
    }

    /// <summary>
    /// 表头筛选获取列去重值的请求参数
    /// </summary>
    public class ColumnDistinctValueOptions
    {
        public string ColumnName { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
