using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using FastMember;


namespace Trinity.MsSql
{
    public class SqlServerDataManager<T> : DataManagerBase<SqlModelCommand<T>>, IModelCommand<T>
        where T : class
    {
        public ModelConfiguration<T> ModelConfiguration { get; set; }

        public SqlServerDataManager(string connectionString)
            : this(connectionString, "System.Data.SqlClient")
        {
        }

        public SqlServerDataManager(string connectionString, string providerName)
            : base(connectionString, providerName)
        {
            if (DataContextBase.TableMaps == null)
            {
                this.TableMaps = new Dictionary<string, TableMap>();
                DataContextBase.TableMaps = this.TableMaps;
            }
            else
            {
                this.TableMaps = DataContextBase.TableMaps;
            }
            TableMapFromDatabase = true;
        }


        public override void CreateTransaction()
        {
            var conn = Connection as SqlConnection;


            if (conn != null)
                this.Transaction = conn.BeginTransaction(IsolationLevel.ReadCommitted);
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


            if (model is IModelBase)
            {
                var item = model as IModelBase;
                item.PropertyChanging += (sender, args) =>
                {

                    if (model is IObjectDataManager)
                    {
                        var manager = model as IObjectDataManager;
                        var value = manager.GetData(args.PropertyName);

                        if (item.OldValues.ContainsKey(args.PropertyName))
                            item.OldValues[args.PropertyName] = value;
                        else
                        {
                            item.OldValues.Add(args.PropertyName, value);
                        }
                    }

                    //TODO imp 

                };


            }

            if (model is INotifyPropertyChanged)
            {
                var item = model as INotifyPropertyChanged;
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

        protected override ICommandResult ExecuteDeleteCommand(SqlModelCommand<T> dataCommand, IDbCommand command)
        {
            var result = new ModelCommandResult<T>();
            if (TableMapFromDatabase)
                dataCommand.GetTableMap();

            dataCommand.BuildSqlParameters(command);
            dataCommand.BuildSqlCommand();
            command.CommandText = dataCommand.SqlCommandText;



            if (command.CommandText.ToUpper().Contains("WHERE"))
            {
                int records = command.ExecuteNonQuery();
                result.RecordsAffected = records;
                result.AddMessage(string.Format("{0} executed with {1} rows affected", dataCommand.SqlCommandText,
                    records));
                dataCommand.ResetCommand();
            }
            else
            {
                result.AddError(LogType.Information, "No where in delete " + command.CommandText);
            }
            result.DataCommand = dataCommand;

            return result;
        }



        protected override ICommandResult ExecuteUpdateCommand(SqlModelCommand<T> dataCommand, IDbCommand command)
        {

            var result = new ModelCommandResult<T>();
            var item = dataCommand.Model as IModelBase;
            //TODO beforeExectute
            if (item != null)
            {
                if (item.Error == null)
                    if (item.HasErrors())
                    {
                        result.AddError(LogType.Error, "Model has validation error");
                        return result;
                    }
            }
            if (TableMapFromDatabase)
                dataCommand.GetTableMap();


            if (dataCommand.TableMap.TableType == "VIEW")
            {

                if (item.Configuration == null)
                {
                    result.AddMessage(string.Format("The command is type of View and has no merge configuration"));
                    return result;
                }
            }
            else
            {
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

                        if (column == null)
                            column = dataCommand.TableMap.ColumnMaps.FirstOrDefault(m => m.ColumnName == change);

                        if (column?.IsIdentity == false)
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
                    dataCommand.BuildSqlParameters(command);
                    dataCommand.BuildSqlCommand();
                    command.CommandText = dataCommand.SqlCommandText;
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

                    result.CommandText = command.CommandText;
                    result.AddMessage(string.Format("{0} executed with {1} rows affected", command.CommandText,
                        result.RecordsAffected));
                }
                result.DataCommand = dataCommand;

            }
            return result;

        }



        protected override ICommandResult ExecuteInsertCommand(SqlModelCommand<T> dataCommand, IDbCommand command)
        {
            var result = new ModelCommandResult<T>();
            var item = dataCommand.Model as IModelBase;

            if (item != null)
            {
                if (item.Errors != null)
                    if (item.HasErrors())
                    {
                        result.AddError(LogType.Error, "Model has validation error");
                        return result;
                    }
            }
            if (TableMapFromDatabase)
                dataCommand.GetTableMap();


            var select = string.Empty;
            bool identity = false;
            var retunColums = new Dictionary<string, string>();
            dataCommand.AddChanges();
            foreach (var key in dataCommand.PrimaryKeys)
            {
                var column = dataCommand.TableMap.ColumnMaps.FirstOrDefault(m => m.PropertyName == key);

                if (column == null)
                    column = dataCommand.TableMap.ColumnMaps.FirstOrDefault(m => m.ColumnName == key);

                if (column != null)
                {
                    if (column.IsIdentity)
                    {
                        select += string.Format(" {0},", column.ColumnName);
                        identity = true;
                        retunColums.Add(column.ColumnName, column.PropertyName);
                        if (dataCommand.Changes.Contains(column.PropertyName))
                        {
                            dataCommand.Changes.Remove(column.PropertyName);
                        }
                    }
                }
            }

            var guidIdColumns =
                dataCommand.TableMap.ColumnMaps.Where(map => !string.IsNullOrEmpty(map.Default))
                    .Where(
                        map =>
                            map.Default.ToUpper().Contains("NEWSEQUENTIALID")
                            || map.Default.ToUpper().ToUpper().Contains("NEWID"))
                    .ToList();
            foreach (var guidIdColumn in guidIdColumns)
            {
                select += string.Format(" {0},", guidIdColumn.ColumnName);
                retunColums.Add(guidIdColumn.ColumnName, guidIdColumn.PropertyName);
            }

            if (identity)
            {
                dataCommand.SetWhereText(string.Format(
                    " SELECT SCOPE_IDENTITY() as [{0}]", retunColums.FirstOrDefault().Key
                ));
            }


            foreach (var change in dataCommand.Changes)
            {
                var column = dataCommand.TableMap.ColumnMaps.FirstOrDefault(m => m.PropertyName == change);
                if (column != null)
                {
                    dataCommand.Value(column.ColumnName, dataCommand.GetValue(change));
                }
            }

            dataCommand.BuildSqlParameters(command);
            dataCommand.BuildSqlCommand();
            command.CommandText = dataCommand.SqlCommandText;

            using (var dataReader = command.ExecuteReader())
            {
                

                result.RecordsAffected = dataReader.RecordsAffected;
                if (dataReader.RecordsAffected == 0)
                {
                    result.AddError(LogType.Information, "No rows affected");
                }
                else
                {

                    dataCommand.SetWhereText(string.Empty);
                    dataCommand.CommandType = DataCommandType.Update;


                }
                while (dataReader.Read())
                {

                    for (int i = 0; i < 1; ++i)
                    {
                        var name = dataReader.GetName(i);
                        var property = retunColums[name];
                        dataCommand.SetValue(property, dataReader.GetValue(i).ToInt());
                    }
                }

            }
            //TODO error when transaction is full 
            result.AddMessage(string.Format("{0} executed with {1} rows affected", dataCommand.SqlCommandText,
                result.RecordsAffected));
            dataCommand.ResetCommand();
            result.DataCommand = dataCommand;
            return result;
        }

        protected override async Task<ICommandResult> ExecuteSelectCommandAsync(SqlModelCommand<T> dataCommand,
            IDbCommand command)
        {

            if (TableMapFromDatabase)
                dataCommand.GetTableMap();

            dataCommand.BuildSqlCommand();
            dataCommand.BuildSqlParameters(command);
            command.CommandText = dataCommand.SqlCommandText;
            var items = new List<T>();
            ICommandResult result = null;

            try
            {
                int rowsIndex = 0;
                var sqlCommand = command as SqlCommand;

                using (SqlDataReader r = await sqlCommand.ExecuteReaderAsync())
                {

                    this.OnExecutedCommand(command);
                    Type objectType = typeof(T);

                    if (objectType == typeof(DataTable))
                    {

                        result = new DataTabelCommandResult();
                        var dt = new DataTable(dataCommand.TabelName);
                        dt.Load(r);
                        ((DataTabelCommandResult)result).Data = dt;

                        return result;
                    }
                    result = new ModelCommandResult<T>();

                    var tablename = dataCommand.GetTableAttribute();
                    while (await r.ReadAsync())
                    {
                        bool userDataManager = false;
                        var newObject = (T)Activator.CreateInstance(objectType);
                        var dataManager = newObject as IObjectDataManager;

                        if (dataManager != null)
                            userDataManager = true;

                        if (dataCommand.TableMap != null)
                            if (dataCommand.TabelName != objectType.Name)
                            {
                                if (tablename == dataCommand.TabelName)
                                    if (dataManager != null)
                                        userDataManager = true;
                                    else
                                        userDataManager = false;
                            }

                        if (userDataManager)
                        {
                            dataManager.SetData(r);
                        }
                        else
                        {

                            //TODO Change code to use Reflection.Emit 
                            int counter = r.FieldCount;
                            for (int i = 0; i < counter; i++)
                            {
                                var name = r.GetName(i);
                                try
                                {
                                    var fieldType = r.GetFieldType(i);
                                    var value = r.GetValue(i);
                                    if (dataCommand.TableMap != null)
                                    {
                                        var column =
                                            dataCommand.TableMap.ColumnMaps.FirstOrDefault(m => m.ColumnName == name);

                                        if (column != null)
                                        {
                                            if (!string.IsNullOrEmpty(column.PropertyName))
                                            {
                                                var prop = objectType.GetProperty(column.PropertyName);
                                                if (prop != null)
                                                {
                                                    if (value != DBNull.Value)
                                                    {
                                                        prop.SetValue(newObject, value, null);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                TrySetValue(dataCommand, newObject, objectType, name, value);
                                            }

                                        }
                                        else
                                        {
                                            TrySetValue(dataCommand, newObject, objectType, name, value);
                                        }
                                    }
                                    else
                                    {
                                        var prop = objectType.GetProperty(name);
                                        if (prop != null)
                                        {
                                            if (value != DBNull.Value)
                                            {
                                                prop.SetValue(newObject, value, null);
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    result.AddError(LogType.Error,
                                        string.Format("{1} {0} ", name, dataCommand.TabelName), ex);
                                }
                            }
                        }
                        items.Add(newObject);
                        rowsIndex++;
                    }
                }
                result.RecordsAffected = rowsIndex;
            }
            catch (Exception exception)
            {
                result.AddError(LogType.Error, dataCommand.TabelName + " " + typeof(T).FullName, exception);
            }

            ((ModelCommandResult<T>)result).Data = items;
            result.AddMessage(string.Format("{0} executed with {1} rows affected", dataCommand.SqlCommandText,
                result.RecordsAffected));
            //TODO change class to use base type
            dataCommand.OnCommandExecuted(new ModelCommandExecutedEventArgs<T> { Result = (ModelCommandResult<T>)result });
            dataCommand.ResetCommand();
            result.DataCommand = dataCommand;
            return result;
        }



        protected override ICommandResult ExecuteSelectCommand(SqlModelCommand<T> dataCommand, IDbCommand command)
        {

            if (TableMapFromDatabase)
            {

                var map = dataCommand.GetTableMap();
                if (dataCommand.SelectAll == false)
                {
                   
                    if (!string.IsNullOrEmpty(map))
                    {
                        var columns = dataCommand.GetColumnAttributes();
                        foreach (var columnMap in columns)
                        {
                            bool addColumn = true;
                            if (dataCommand.TableMap == null)
                                addColumn = false;

                            var column = dataCommand.TableMap.ColumnMaps.FirstOrDefault(m => m.ColumnName == columnMap.ColumnName);
                            if (column == null)
                                addColumn = false;


                            if (addColumn)
                                dataCommand.Column(columnMap.ColumnName);
                        }
                        dataCommand.SelectAll = false;
                    }

                }

            }


            dataCommand.BuildSqlCommand();
            dataCommand.BuildSqlParameters(command);
            command.CommandText = dataCommand.SqlCommandText;
            var items = new List<T>();
            ICommandResult result = null;
            try
            {
                int rowsIndex = 0;
                using (SqlDataReader r = command.ExecuteReader() as SqlDataReader)
                {

                    this.OnExecutedCommand(command);
                    Type objectType = typeof(T);

                    if (objectType == typeof(DataTable))
                    {

                        result = new DataTabelCommandResult();
                        var dt = new DataTable(dataCommand.TabelName);
                        dt.Load(r);
                        ((DataTabelCommandResult)result).Data = dt;

                        return result;
                    }
                    result = new ModelCommandResult<T>();

                    var tablename = dataCommand.GetTableAttribute();

                    var accessor = TypeAccessor.Create(objectType);

                    ConstructorInfo ctor = objectType.GetConstructor(Type.EmptyTypes);
                    TrinityActivator.ObjectActivator<T> createdActivator = TrinityActivator.GetActivator<T>(ctor);

                  

                    while (r.Read())
                    {
                        bool userDataManager = false;

                        //https://vagifabilov.wordpress.com/2010/04/02/dont-use-activator-createinstance-or-constructorinfo-invoke-use-compiled-lambda-expressions/

                        var newObject = createdActivator();
                        //var newObject = (T)Activator.CreateInstance(objectType);
                        var dataManager = newObject as IObjectDataManager;


                        if (dataCommand.TableMap != null)
                            if (dataCommand.TabelName != objectType.Name)
                            {
                                if (tablename == dataCommand.TabelName)
                                    if (dataManager != null)
                                        userDataManager = true;
                                    else
                                        userDataManager = false;
                            }
                            else
                            {

                                if (dataManager != null)
                                    userDataManager = true;
                            }

                        if (userDataManager)
                        {
                            dataManager.SetData(r);

                        }
                        else
                        {

                            //TODO Change code to use Reflection.Emit  
                            int counter = r.FieldCount;
                            for (int i = 0; i < counter; i++)
                            {
                                var name = r.GetName(i);
                                try
                                {
                                    var fieldType = r.GetFieldType(i);
                                    var value = r.GetValue(i);
                                    if (dataCommand.TableMap != null)
                                    {
                                        var column =
                                            dataCommand.TableMap.ColumnMaps.FirstOrDefault(m => m.ColumnName == name);

                                        if (column != null)
                                        {
                                            if (!string.IsNullOrEmpty(column.PropertyName))
                                            {
                                                try
                                                {
                                                    if (value != DBNull.Value)
                                                        accessor[newObject, column.PropertyName] = value;
                                                }
                                                catch (Exception e)
                                                {
                                                    try
                                                    {
                                                        if (value != DBNull.Value)
                                                        {
                                                            var prop = objectType.GetProperty(column.PropertyName);
                                                            accessor[newObject, column.PropertyName] = value.ConvertValue(prop);

                                                        }
                                                    }
                                                    catch (Exception exception)
                                                    {

                                                        var prop = objectType.GetProperty(column.PropertyName);
                                                        if (prop != null)
                                                        {
                                                            if (value != DBNull.Value)
                                                            {
                                                                try
                                                                {
                                                                    prop.SetValue(newObject, value, null);
                                                                }
                                                                catch (Exception)
                                                                {
                                                                    prop.SetValue(newObject, value.ConvertValue(prop), null);
                                                                }
                                                            }
                                                        }
                                                    }


                                                }


                                            }
                                            else
                                            {
                                                try
                                                {
                                                    if (value != DBNull.Value)
                                                        accessor[newObject, name] = value;
                                                }
                                                catch (Exception e)
                                                {
                                                    TrySetValue(dataCommand, newObject, objectType, name, value);
                                                }



                                            }

                                        }
                                        else
                                        {
                                            try
                                            {
                                                if (value != DBNull.Value)
                                                    accessor[newObject, name] = value;
                                            }
                                            catch (Exception e)
                                            {
                                                TrySetValue(dataCommand, newObject, objectType, name, value);
                                            }


                                        }
                                    }
                                    else
                                    {
                                        var prop = objectType.GetProperty(name);
                                        if (prop != null)
                                        {
                                            if (value != DBNull.Value)
                                            {
                                                prop.SetValue(newObject, value, null);
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    result.AddError(LogType.Error, string.Format("{1} {0} ", name, dataCommand.TabelName), ex);
                                }
                            }
                        }
                        items.Add(newObject);
                        rowsIndex++;
                    }
                }
                result.RecordsAffected = rowsIndex;
            }
            catch (Exception exception)
            {
                result.AddError(LogType.Error, $"{dataCommand.TabelName} {dataCommand.SqlCommandText} {dataCommand.Connection.ConnectionString} {typeof(T).FullName}", exception);
            }




                ((ModelCommandResult<T>)result).Data = items;
            result.AddMessage(string.Format("{0} executed with {1} rows affected", dataCommand.SqlCommandText, result.RecordsAffected));
            //TODO change class to use base type


            dataCommand.OnCommandExecuted(new ModelCommandExecutedEventArgs<T> { Result = (ModelCommandResult<T>)result });

            dataCommand.ResetCommand();
            result.DataCommand = dataCommand;
            return result;
        }




        private void TrySetValue(IDataCommand dataCommand, object newObject, Type objectType, string name, object value)
        {
            var prop = objectType.GetProperty(name);
            if (prop != null)
            {
                if (value != DBNull.Value)
                {
                    prop.SetValue(newObject, value, null);
                }
            }
            else
            {
                PropertyInfo[] properties = objectType.GetProperties();
                var index = properties.Count();
                for (int j = 0; j < index; j++)
                {
                    var props = properties[j];
                    if (props.CanWrite)
                    {
                        var propName = props.Name.ToLower();
                        var columnName = name.ToLower();
                        if (propName == columnName)
                        {

                            if (value != DBNull.Value)
                                props.SetValue(newObject, value, null);

                            var column = dataCommand.TableMap.ColumnMaps.FirstOrDefault(m => m.ColumnName == name);
                            if (column == null)
                            {
                                dataCommand.TableMap.ColumnMaps.Add(new SqlColumnMap()
                                {
                                    ColumnName = name,
                                    PropertyName = props.Name
                                });
                            }

                            else
                            {
                                column.PropertyName = props.Name;
                            }
                            break;
                        }
                    }
                }
            }
        }

        protected override Task<ICommandResult> ExecuteDeleteCommandAsync(SqlModelCommand<T> dataCommand, IDbCommand command)
        {
            throw new NotImplementedException();
        }

        protected override Task<ICommandResult> ExecuteUpdateCommandAsync(SqlModelCommand<T> dataCommand, IDbCommand command)
        {
            throw new NotImplementedException();
        }

        protected override Task<ICommandResult> ExecuteInsertCommandAsync(SqlModelCommand<T> dataCommand, IDbCommand command)
        {
            throw new NotImplementedException();
        }


    }
}