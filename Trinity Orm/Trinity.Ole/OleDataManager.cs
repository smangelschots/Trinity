
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Trinity.Ole
{
    public class OleDataManager<T> : DataManagerBase<OleModelCommand<T>>, IModelCommand<T>
        where T : class
    {
        public OleDataManager(string connectionString)
            : this(connectionString, "System.Data.OleDb")
        {
        }

        public ModelConfiguration<T> ModelConfiguration { get; set; }

        public OleDataManager(string connectionString, string providerName)
            : base(connectionString, providerName)
        {
        }



        protected override ICommandResult ExecuteDeleteCommand(OleModelCommand<T> dataCommand, IDbCommand command)
        {
            var result = new ModelCommandResult<T>();
            dataCommand.BuildKeys();
            dataCommand.BuildSqlParameters(command);
            dataCommand.BuildSqlCommand();
            command.CommandText = dataCommand.SqlCommandText;
            int records = command.ExecuteNonQuery();
            result.RecordsAffected = records;
            result.AddMessage(string.Format("{0} executed with {1} rows affected", dataCommand.SqlCommandText, records));
            dataCommand.ResetCommand();

            return result;

        }
        protected override ICommandResult ExecuteUpdateCommand(OleModelCommand<T> dataCommand, IDbCommand command)
        {
            var result = new ModelCommandResult<T>();
            var item = dataCommand.Model as IModelBase;

            if (item != null)
            {
                if (item.HasErrors())
                {
                    return result;
                }
            }

            if (TableMapFromDatabase)
                dataCommand.GetTableMap();


            if (dataCommand.Track == false)
            {
                dataCommand.AddChanges();
            }
            dataCommand.BuildKeys();
            foreach (var change in dataCommand.Changes)
            {
                if (dataCommand.TableMap != null)
                {
                    var column = dataCommand.TableMap.ColumnMaps.FirstOrDefault(m => m.PropertyName == change);
                    if (column != null)
                    {
                        dataCommand.Value(column.ColumnName, dataCommand.GetValue(change));
                    }
                }
                else
                {
                    dataCommand.Value(change, dataCommand.GetValue(change));
                }
            }
            if (dataCommand.Columns.Count > 0 && dataCommand.Changes.Count > 0)
            {
                command.CommandText = dataCommand.BuildSqlCommand();
                dataCommand.BuildSqlParameters(command);
                if (command.CommandText.Contains("WHERE"))
                {
                    int resultIndex = command.ExecuteNonQuery();
                    result.RecordsAffected = resultIndex;
                    if (resultIndex == 0)
                    {
                        result.AddError(LogType.Information, "No rows affected");
                    }
                    else
                    {
                        dataCommand.ResetCommand();
                    }
                }
                else
                {
                    result.AddError(LogType.Information, "No where in update");
                }
                result.AddMessage(string.Format("{0} executed with {1} rows affected", dataCommand.SqlCommandText, result.RecordsAffected));
            }
            return result;
        }
        protected override ICommandResult ExecuteInsertCommand(OleModelCommand<T> dataCommand, IDbCommand command)
        {
            var result = new ModelCommandResult<T>();
            var item = dataCommand.Model as IModelBase;

            if (item != null)
            {
                if (item.HasErrors())
                {
                    return result;
                }
            }

            dataCommand.GetTableMap();

            var select = string.Empty;
            var where = string.Empty;
            bool identity = false;
            var retunColums = new Dictionary<string, string>();
            dataCommand.AddChanges();
            foreach (var key in dataCommand.PrimaryKeys)
            {
                var column = dataCommand.TableMap.ColumnMaps.FirstOrDefault(m => m.PropertyName == key);
                if (column != null)
                {
                    if (column.IsIdentity)
                    {
                        select += string.Format(" {0},", column.ColumnName);
                        where = string.Format("{0} = @@IDENTITY", column.ColumnName);
                        identity = true;
                        retunColums.Add(column.ColumnName, column.PropertyName);

                        if (dataCommand.Changes.Contains(key))
                        {
                            dataCommand.Changes.Remove(key);
                        }
                    }
                }
            }
            if (identity)
            {
                select = select.Remove(select.Length - 1);
                dataCommand.SetWhereText(string.Format(
                    " SELECT {0} FROM {1} WHERE {2}",
                    select,
                    dataCommand.TabelName,
                    where));
            }

            foreach (var change in dataCommand.Changes)
            {
                var column = dataCommand.TableMap.ColumnMaps.FirstOrDefault(m => m.PropertyName == change);
                if (column != null)
                {
                    var value = dataCommand.GetValue(change);
                    if (value != null)
                        if (!string.IsNullOrEmpty(value.ToStringValue()))
                            dataCommand.Value(column.ColumnName, value);
                }
            }


            dataCommand.BuildSqlCommand();
            command.CommandText = dataCommand.SqlCommandText;
            dataCommand.BuildSqlParameters(command);
            using (var dataReader = command.ExecuteReader())
            {
                result.RecordsAffected = dataReader.RecordsAffected;
                if (dataReader.RecordsAffected == 0)
                {
                    result.AddError(LogType.Information, "No rows affected");
                }
                else
                {
                    dataCommand.CommandType = DataCommandType.Update;

                }
                while (dataReader.Read())
                {

                    for (int i = 0; i < 1; ++i)
                    {
                        var name = dataReader.GetName(i);
                        var property = retunColums[name];
                        dataCommand.SetValue(property, dataReader.GetValue(i));
                    }
                }

            }
            result.AddMessage(string.Format("{0} executed with {1} rows affected", dataCommand.SqlCommandText, result.RecordsAffected));
            dataCommand.ResetCommand();
            return result;
        }
        protected override ICommandResult ExecuteSelectCommand(OleModelCommand<T> dataCommand, IDbCommand command)
        {
            var result = new ModelCommandResult<T>();

            dataCommand.BuildSqlCommand();
            dataCommand.BuildSqlParameters(command);
            command.CommandText = dataCommand.SqlCommandText;
            var items = new List<T>();
            var pd = CommandResult.ForType(typeof(T));
            try
            {
                int rowsIndex = 0;
                using (var r = command.ExecuteReader())
                {

                    this.OnExecutedCommand(command);
                    var factory = pd.GetFactory(command.CommandText, this.Connection.ConnectionString, false, 0, r.FieldCount, r) as Func<IDataReader, T>;
                    while (true)
                    {
                        T poco;
                        try
                        {
                            if (!r.Read())
                                break;
                            poco = factory(r);
                        }
                        catch (Exception x)
                        {
                            this.OnException(x);
                            throw;
                        }
                        items.Add(poco);
                        rowsIndex++;
                    }
                }
                result.RecordsAffected = rowsIndex;
            }
            catch (Exception exception)
            {
                result.AddError(LogType.Error, "No rows affected", exception);
            }

            result.Data = items;
            result.AddMessage(string.Format("{0} executed with {1} rows affected", dataCommand.SqlCommandText, result.RecordsAffected));
            dataCommand.OnCommandExecuted(new ModelCommandExecutedEventArgs<T> { Result = result });
            dataCommand.ResetCommand();
            return result;
        }

        public IDataCommand<T> Track(T model)
        {
            var dataCommand = this.Update();

            dataCommand.Model = model;
            dataCommand.Track = true;
            var errorInfo = model as IDataErrorInfo;
            if (errorInfo != null)
            {
                dataCommand.OnSetValidation(new ModelCommandValidationEventArgs<T> { ModelCommand = dataCommand });
            }

            var item = model as INotifyPropertyChanged;
            if (item != null)
            {
                item.PropertyChanged += (e, r) =>
                {
                    var property = item.GetType().GetProperty(r.PropertyName);
                    dataCommand.AddPropertyChange(property);
                };
            }
            return dataCommand;
        }
        public IDataCommand<T> Insert(T model)
        {
            var dataCommand = this.Insert();
            dataCommand.Model = model;
            var errorInfo = model;
            if (errorInfo != null)
            {
                dataCommand.OnSetValidation(new ModelCommandValidationEventArgs<T> { ModelCommand = dataCommand });
            }
            return dataCommand;
        }
        public IDataCommand<T> Update(T model)
        {
            var dataCommand = this.Update();
            dataCommand.Model = model;
            var errorInfo = model;
            if (errorInfo != null)
            {
                dataCommand.OnSetValidation(new ModelCommandValidationEventArgs<T> { ModelCommand = dataCommand });
            }
            return dataCommand;
        }

        protected override Task<ICommandResult> ExecuteDeleteCommandAsync(OleModelCommand<T> dataCommand, IDbCommand command)
        {
            throw new NotImplementedException();
        }

        protected override Task<ICommandResult> ExecuteUpdateCommandAsync(OleModelCommand<T> dataCommand, IDbCommand command)
        {
            throw new NotImplementedException();
        }

        protected override Task<ICommandResult> ExecuteInsertCommandAsync(OleModelCommand<T> dataCommand, IDbCommand command)
        {
            throw new NotImplementedException();
        }

        protected override Task<ICommandResult> ExecuteSelectCommandAsync(OleModelCommand<T> dataCommand, IDbCommand command)
        {
            throw new NotImplementedException();
        }
    }
}
