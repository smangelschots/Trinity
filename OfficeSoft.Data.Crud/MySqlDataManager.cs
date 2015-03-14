using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Reflection;


namespace OfficeSoft.Data.Crud
{

    public class MySqlDataManager <T> : BaseDataManager<MySqlModelCommand<T>>, IModelCommand<T>
		where T : class
	{
		public ModelConfiguration<T> ModelConfiguration {
			get {
				throw new NotImplementedException ();
			}
			set {
				throw new NotImplementedException ();
			}
		}

		public MySqlDataManager (string connectionString)
			: this(connectionString, "MySql.Data.MySqlClient")
		{
		   
		}



		public MySqlDataManager(string connectionString, string providerName)
			: base(connectionString,providerName)
		{
            if (BaseDataContext.TableMaps == null)
            {
                this.TableMaps = new Dictionary<string, TableMap>();
                BaseDataContext.TableMaps = this.TableMaps;
            }
            else
            {
                this.TableMaps = BaseDataContext.TableMaps;
            }



            //this.TableMaps = new Dictionary<string, TableMap>();
            this.TableMapFromDatabase = true;
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

		protected override ICommandResult ExecuteDeleteCommand(MySqlModelCommand<T> dataCommand, IDbCommand command)
		{
			var result = new ModelCommandResult<T>();
			if (TableMapFromDatabase)
				dataCommand.GetTableMap();

			//TODO change the where has to be set to parameters before execute
			if (string.IsNullOrEmpty(dataCommand.WhereText))
				//if (dataCommand.WhereText.Contains("@"))
				dataCommand.BuildKeys();



			dataCommand.BuildSqlParameters(command);
			dataCommand.BuildSqlCommand();
			command.CommandText = dataCommand.SqlCommandText;






			if (command.CommandText.ToUpper().Contains("WHERE"))
			{
				int records = command.ExecuteNonQuery();
				result.RecordsAffected = records;
				result.AddMessage(string.Format("{0} executed with {1} rows affected", dataCommand.SqlCommandText, records));
				dataCommand.ResetCommand();
			}
			else
			{
				result.AddError(ErrorType.Information, "No where in delete " + command.CommandText);
			}

			return result;
		}
		protected override ICommandResult ExecuteUpdateCommand(MySqlModelCommand<T> dataCommand, IDbCommand command)
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
						if (column.IsIdentity == false)
						{
							dataCommand.Value(column.ColumnName, dataCommand.GetValue(change));
						}
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
						result.AddError(ErrorType.Information, "No rows affected");
					}
					else
					{
						dataCommand.ResetCommand();
					}
				}
				else
				{
					result.AddError(ErrorType.Information, "No where in update");
				}
				result.AddMessage(string.Format("{0} executed with {1} rows affected", dataCommand.SqlCommandText, result.RecordsAffected));
			}
			return result;
		}
		protected override ICommandResult ExecuteInsertCommand(MySqlModelCommand<T> dataCommand, IDbCommand command)
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

			var select = string.Empty;
			var where = string.Empty;
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
							where = string.Format("{0} = @@IDENTITY", column.ColumnName);
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
				select = select.Remove(select.Length - 1);
				dataCommand.WhereText = string.Format(
					" SELECT {0} FROM {1} WHERE {2}",
					select,
					dataCommand.TabelName,
					where);
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
					result.AddError(ErrorType.Information, "No rows affected");
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
		protected override ICommandResult ExecuteSelectCommand(MySqlModelCommand<T> dataCommand, IDbCommand command)
		{
			var result = new ModelCommandResult<T>();
			if (TableMapFromDatabase)
				dataCommand.GetTableMap();

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
				result.AddError(ErrorType.Error, "No rows affected", exception);
			}

			result.Data = items;
			result.AddMessage(string.Format("{0} executed with {1} rows affected", dataCommand.SqlCommandText, result.RecordsAffected));
			dataCommand.OnCommandExecuted(new ModelCommandExecutedEventArgs<T> { Result = result });
			dataCommand.ResetCommand();
			return result;
		}

	    public override string GetDatabaseName(string connectionString)
	    {

	        var items = connectionString.Split(';');
	        foreach (var item in items)
	        {

	            if (item.Contains("Database"))
	            {
	                var values = item.Split('=');

	                return values[1];

	            }

	        }

	        return string.Empty;
	    }
	}
}

