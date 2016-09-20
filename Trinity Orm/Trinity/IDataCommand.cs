using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Trinity
{
    public interface IDataCommand
    {
        List<string> Changes { get; set; }
        TableMap TableMap { get; set; }
        bool IsMapped { get; set; }
        List<string> PrimaryKeys { get; set; }

        string SqlCommandText { get; set; }

        DataCommandType CommandType { get; set; }
     
        string TabelName { get; set; }

        List<IDataParameter> Parameters { get; set; }

        bool SelectAll { get; set; }

        List<string> Columns { get; set; }

        string BuildSqlCommand();

        bool Validate();

        void ResetCommand();

        string GetTableAttribute();

        List<IColumnMap> GetColumnAttributes(List<IColumnMap> columnMaps = null);

        object GetValue(string name);

        void OnValidating(object manager, ModelCommandPropertyChangedEventArgs e);

    }

    public interface IDataCommand<T> : IDataCommand
        where T : class

    {
        
        IDataCommand<T> Where(string filterString);
        IDataCommand<T> Where(string column, string opperator, object value);
        IDataCommand<T> Where<TField>(Expression<Func<T, TField>> field, string opperator, object value);
        IDataCommand<T> Where(Expression<Func<T, bool>> expression);


        IDataCommand<T> Or(string filterString);
        IDataCommand<T> Or(string column, string opperator, object value);
        IDataCommand<T> Or(Expression<Func<T, bool>> expression);


        IDataCommand<T> And(string filterString);
        IDataCommand<T> And(string column, string op, object value);
        IDataCommand<T> And(Expression<Func<T, bool>> expression);

        IDataCommand<T> From(string tableName = "");


        IDataCommand<T> OrderBy(string column);

        IDataCommand<T> OrderByDesc(string column);

        IList<T> ExecuteToList();
        Task<IList<T>> ExecuteToListAsync();

        T FirstOrDefault();

        IDataCommand<T> All();

        IDataCommand<T> Value(string column, object value);

        IDataCommand<T> Take(int rows);

        IDataCommand<T> Skip(int rows);

        IDataCommand<T> Top(int items);

        IDataCommand<T> InTo(string tableName);

        IDataCommand<T> ForInsert();

        IDataCommand<T> ForUpdate();

        IDataCommand<T> WithKey<TField>(Expression<Func<T, TField>> field);

        IDataCommand<T> WithKeys(string[] keys);

        T Model { get; set; }

        bool Track { get; set; }

        void OnCommandExecuted(ModelCommandExecutedEventArgs<T> modelCommandExecutedEventArgs);


        ResultList SaveChanges();
    }
}