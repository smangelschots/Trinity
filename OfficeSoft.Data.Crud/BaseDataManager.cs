namespace OfficeSoft.Data.Crud
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Reflection;

    public abstract class BaseDataManager<T> : IDataManager<T> where T : IDataCommand
    {
        private readonly DbProviderFactory factory;
    
        public event EventHandler<ModelCommandPropertyChangedEventArgs> Validating;
     
        public bool TableMapFromDatabase { get; set; }

        public Dictionary<string, TableMap> TableMaps { get; set; }

        private DbConnection connection;
        private DbTransaction Transaction { get; set; }
        private List<DataError> Errors { get; set; }

        protected List<IDataCommand> Commands { get; set; }
        public IDbConnection Connection
        {
            get
            {
                return this.connection;
            }
        }

        internal void OnExecutedCommand(IDbCommand cmd)
        {
            //TODO Make better of delete

        }

        public string ConnectionString { get; set; }

        protected void OnException(Exception x)
        {
            System.Diagnostics.Debug.WriteLine(x.ToString());
        }

        protected BaseDataManager(string connectionString, string providerName): this()
        {
            this.ConnectionString = connectionString;
            this.factory = DbProviderFactories.GetFactory(providerName);
        }

        private void OnValidating(ModelCommandPropertyChangedEventArgs e)
        {
            EventHandler<ModelCommandPropertyChangedEventArgs> handler = this.Validating;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private BaseDataManager()
        {
            this.Commands = new List<IDataCommand>();
            this.Errors = new List<DataError>();
            this.TableMaps = new Dictionary<string, TableMap>();
        }

        //TODO Check if this works
        protected BaseDataManager(DbConnection connection)
            : this()
        {
            this.connection = connection;
        }

        private void OpenSharedConnection()
        {
            if (this.connection == null)
            {
                this.connection = this.factory.CreateConnection();
            }
            if (this.connection != null)
            {
                if (this.connection.State == ConnectionState.Closed)
                {
                    this.connection.ConnectionString = this.ConnectionString;
                    this.connection.Open();
                }
            }
        }

        //TODO Implement transaction
        protected void CreateTransaction()
        {
            if (this.Connection.State != ConnectionState.Open)
                this.Connection.Open();

            if (this.Transaction == null)
            {
                this.Transaction = this.connection.BeginTransaction();
            }
        }

        private T AddNew()
        {
            var command = (T)Activator.CreateInstance(typeof(T), this);
            this.Commands.Add(command);
            return command;
        }

        public string DatabaseName
        {
            get
            {

                if (string.IsNullOrEmpty(ConnectionString))
                {
                    return string.Empty;
                }

                return GetDatabaseName(ConnectionString);

            }
        }


        public IDataCommand GetCommand(int index)
        {
            return (T)this.Commands[index];
        }

        //TODO rename to removeCommand
        public void Remove(int index)
        {
             this.Commands.RemoveAt(index);
        }
        public void ClearCommands()
        {
            this.Commands.Clear();
            
        }
        //TODO rename to removeCommand
        public void Remove(IDataCommand command)
        {
            this.Commands.Remove(command);
        }

        private IEnumerable<ICommandResult> InsertCommands()
        {
            var inserts = this.Commands.Where(m => m.CommandType == DataCommandType.Insert).ToArray();
            return this.ExecuteCommands(inserts);
        }
        private IEnumerable<ICommandResult> UpdateCommands()
        {
            var updates = this.Commands.Where(m => m.CommandType == DataCommandType.Update).ToArray();
            return this.ExecuteCommands(updates); ;
        }
        private IEnumerable<ICommandResult> DeleteCommands()
        {
            var deletes = this.Commands.Where(m => m.CommandType == DataCommandType.Delete).ToArray();
            this.OnBeforeDelete(deletes);
            var restult = this.ExecuteCommands(deletes);
            for (int i = deletes.Count() - 1; i >= 0; i--)
            {
                this.Commands.Remove(deletes[i]);
            }
            return restult;
        }
        private IEnumerable<ICommandResult> SelectCommands()
        {
            var inserts = this.Commands.Where(m => m.CommandType == DataCommandType.Select).ToArray();
            this.OnBeforeSelect(inserts);
            return this.ExecuteCommands(inserts);
        }

        public virtual string GetDatabaseName(string connectionString)
        {
            return string.Empty;
        }
        
        public virtual ResultList SaveChanges()
        {
            var results = new ResultList();
            results.AddRange(this.DeleteCommands());
            results.AddRange(this.UpdateCommands());
            results.AddRange(this.InsertCommands());
            results.AddRange(this.SelectCommands());
            return results;
        }

        protected abstract ICommandResult ExecuteDeleteCommand(T dataCommand, IDbCommand command);
        protected abstract ICommandResult ExecuteUpdateCommand(T dataCommand, IDbCommand command);
        protected abstract ICommandResult ExecuteInsertCommand(T dataCommand, IDbCommand command);
        protected abstract ICommandResult ExecuteSelectCommand(T dataCommand, IDbCommand command);

        private void OnBeforeDelete(IDataCommand[] deleteCommands)
        {
            //TODO implement event handler
        }
        private void OnBeforeSelect(IDataCommand[] insertCommands)
        {
            //TODO implement event handler
        }

        public ICommandResult ExecuteCommand(IDataCommand dataCommand)
        {
            var result = new CommandResult();

            try
            {
                if (dataCommand.Validate() == false) return null;
                this.OpenSharedConnection();
                try
                {
                    using (var command = this.Connection.CreateCommand())
                    {
                        command.Connection = this.Connection;
                        if (this.Transaction != null) command.Transaction = this.Transaction;
                        switch (dataCommand.CommandType)
                        {
                            case DataCommandType.Select:
                                return this.ExecuteSelectCommand((T)dataCommand, command);
                            case DataCommandType.Insert:
                                return ExecuteInsertCommand((T)dataCommand, command);
                            case DataCommandType.Update:
                                return ExecuteUpdateCommand((T)dataCommand, command);
                            case DataCommandType.Delete:
                                return ExecuteDeleteCommand((T)dataCommand, command);
                        }
                    }
                }
                catch (SqlException exception)
                {
                    result.AddError(ErrorType.Error, "Sql exeption", exception);
                }
                catch (Exception ex)
                {
                    result.AddError(ErrorType.Error, "exeption", ex);
                }
            }
            catch (Exception ex1)
            {
                this.Errors.Add(new DataError
                {
                    StackTrace = ex1.StackTrace,
                    Exception = ex1,
                    HasError = true
                });
            }
            finally
            {
                if (this.Transaction == null)
                    this.Connection.Close();
            }

            return result;
        }
        private IEnumerable<ICommandResult> ExecuteCommands(IDataCommand[] commandsList)
        {

            var results = new List<ICommandResult>();

            try
            {
                if (!commandsList.Any()) return results;
                this.OpenSharedConnection();
                // this.CreateTransaction();
                results.AddRange(commandsList.Select(this.ExecuteCommand));
                try
                {
                    //   this.Transaction.Commit();
                }
                catch (Exception exception)
                {
                    this.Errors.Add(item: new DataError { Exception = exception, HasError = true });
                    this.Transaction.Rollback();
                }
            }
            catch (Exception ex1)
            {
                this.Errors.Add(new DataError { Exception = ex1, HasError = true });
                Console.WriteLine("Commit Exception Type: {0}", ex1.GetType());
                Console.WriteLine("  Message: {0}", ex1.Message);
            }
            finally
            {
                if (this.Connection != null)
                    this.Connection.Close();
            }
            return results;
        }

        public T Select()
        {
            var dataCommand = AddNew();
            dataCommand.SelectAll = true;
            dataCommand.CommandType = DataCommandType.Select;
           
            return dataCommand;
        }
        public T Delete()
        {
            var dataCommand = AddNew();
            dataCommand.CommandType = DataCommandType.Delete;
            return dataCommand;
        }
        public T Insert()
        {
            var dataCommand = AddNew();
            dataCommand.CommandType = DataCommandType.Insert;
            return dataCommand;
        }
        public T Update()
        {
            var dataCommand = AddNew();
            dataCommand.CommandType = DataCommandType.Update;
            return dataCommand;
        }

        public void Dispose()
        {
            if (this.connection == null)
                return;
            this.connection.Dispose();
        }

    }
}