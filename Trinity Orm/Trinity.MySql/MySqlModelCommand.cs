using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using MySql.Data.MySqlClient;

namespace Trinity.MySql
{
    public class MySqlModelCommand<T> : DataCommand<T> where T : class
    {
        public MySqlModelCommand(IDbConnection connection)
            : base(connection)
        {
        }

        public MySqlModelCommand(IDbConnection connection,
            List<IDataParameter> parameters, DataCommandType commandType)
            : base(connection, parameters, commandType)
        {
        }

        public MySqlModelCommand(IDataManager manager)
            : base(manager)
        {
        }




        public override void ResetCommand()
        {
            this.Changes.Clear();
            base.ResetCommand();
        }

        public override void SetParameter(string parameterName, string column, object value, bool isSelectParameter)
        {
            {
                var newDataParameter = this.Parameters.SingleOrDefault(
                     m => m.Name.Contains(parameterName));

                if (newDataParameter == null)
                {
                    newDataParameter = Activator.CreateInstance<MySqlDataParameter>();

                    newDataParameter.Name = parameterName;
                    newDataParameter.ColumnName = column;
                    newDataParameter.IsSelectParameter = isSelectParameter;
                };
                this.Columns.Add(column);
                this.Parameters.Add(newDataParameter);
                newDataParameter.Value = value;
            }
        }

        public override string GetTableMap()
        {

            if (string.IsNullOrEmpty(this.TabelName))
            {
                this.TabelName = this.GetTableAttribute();
            }
            var modelName = base.GetTableMapName();
            if (Manager.TableMaps.ContainsKey(modelName))
            {
                this.TableMap = Manager.TableMaps[modelName];

                if (this.PrimaryKeys.Count == 0)
                    this.PrimaryKeys = TableMap.GetPrimaryKeys();
            }
            else
            {
                if (this.Manager.TableMapFromDatabase == false)
                    base.GetTableMap();
                else
                {
                    var tableMap = this.TableMap ?? new TableMap();
                    tableMap.TableName = this.TabelName;
                    Manager.TableMaps.Add(modelName, tableMap);
                    this.TableMap = tableMap;


                    string sqlTableConnection =
                        string.Format(
                            "SELECT * FROM INFORMATION_SCHEMA.TABLES where TABLE_TYPE = 'BASE TABLE' and table_schema = '{0}' and table_name = '{1}'",
                            Manager.DatabaseName,this.TabelName);




                    string sqlColumnConnection =
                        string.Format("SELECT * FROM INFORMATION_SCHEMA.COLUMNS where table_name = '{0}'", this.TabelName);


                    using (var conn = new MySqlConnection(Manager.ConnectionString))
                    {
                        conn.Open();
                        using (var command = new MySqlCommand(sqlTableConnection, conn))
                        {
                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    tableMap.TableName = reader["TABLE_NAME"].ToStringValue();
                                    tableMap.Catalog = reader["TABLE_CATALOG"].ToStringValue();
                                    tableMap.Schema = reader["TABLE_SCHEMA"].ToStringValue();
                                    tableMap.TableType = reader["TABLE_TYPE"].ToStringValue();
                                }
                                reader.Close();

                            }
                        }
                        conn.Close();
                    }

                    using (var conn = new MySqlConnection(Manager.ConnectionString))
                    {
                        conn.Open();
                        using (var command = new MySqlCommand(sqlColumnConnection,conn))
                        {

                            using (var reader = command.ExecuteReader())
                            {

                                while (reader.Read())
                                {
                                    var columnName = reader["COLUMN_NAME"].ToStringValue();
                                    var columnMap = tableMap.ColumnMaps.FirstOrDefault(m => m.ColumnName == columnName);
                                    if (columnMap == null)
                                    {
                                        columnMap = new MySqlColumnMap();
                                        columnMap.ColumnName = columnName;
                                        columnMap.IsMapped = false;
                                        tableMap.ColumnMaps.Add(columnMap);
                                    }

                                    var columnKey = reader["COLUMN_KEY"].ToStringValue();
                                    var extra = reader["EXTRA"].ToStringValue();


                                    if (extra.Contains("auto_increment"))
                                        columnMap.IsIdentity = true;


                                    if (columnKey.Contains("PRI"))
                                        columnMap.IsPrimaryKey = true;

                                    
                                    
                                    columnMap.IsNullable = reader["IS_NULLABLE"].ToBool();
                                    columnMap.Size = reader["CHARACTER_MAXIMUM_LENGTH"].ToInt();
                                   // columnMap.SqlDbType = reader["DATA_TYPE"].ToEnumValue<SqlDbType>(SqlDbType.NVarChar);
                                    columnMap.OrdinalPosition = reader["ORDINAL_POSITION"].ToInt();
                                    columnMap.Default = reader["COLUMN_DEFAULT"].ToStringValue();
                                }
                                reader.Close();
                            }
                        
                        }
                        conn.Close();
                    }

                }

            }
            return modelName;
        }


        protected override string GetFrom()
        {
            if (!string.IsNullOrEmpty(this.TabelName))
                return string.Format(" FROM {0} ", this.TabelName);
            else
                throw new ApplicationException("No tablename");
        }

        public override void OnCommandExecuted(ModelCommandExecutedEventArgs<T> e)
        {
            base.OnCommandExecuted(e);
        }





        public override List<IColumnMap> GetColumnAttributes(List<IColumnMap> columnMaps)
        {
            return base.GetColumnAttributes(columnMaps);
        }

        public void BuildSqlParameters(IDbCommand command)
        {

            foreach (var dataParameter in this.Parameters)
            {

                var par = dataParameter as MySqlDataParameter;
                if (par == null)
                    throw new ApplicationException("Parameter is no MySqlDataParameter");


                var columnText = dataParameter.ColumnName.Replace("[", "").Replace("]", "");
                if (this.TableMap != null)
                {
                    var column = this.TableMap.ColumnMaps.FirstOrDefault(m => m.ColumnName == columnText);
                    if (column != null)
                    {
                        var mycolum = column as MySqlColumnMap;
                        par.DbType = mycolum.DbType;
                        par.Size = mycolum.Size;
                        par.IsNullable = mycolum.IsNullable;
                        par.Size = mycolum.Size;
                        par.IsForeinKey = mycolum.IsForeinKey;
                        par.IsPrimaryKey = mycolum.IsPrimaryKey;
                    }
                }
                var value = dataParameter.Value;
                if (dataParameter.Size > 0)
                {
                    var valuestring = value.ToStringValue();
                    if (!string.IsNullOrEmpty(valuestring))
                    {
                        if (valuestring.Length > dataParameter.Size)
                        {
                            throw new ApplicationException(string.Format("Property {0} value is bigger than database size {1} column name in {2} {3} Value {4} length is {5} column length is {6}"
                                , dataParameter.PropertyName, dataParameter.ColumnName, this.TabelName, valuestring, valuestring.Length, dataParameter.Size, valuestring.Length));
                        }
                    }
                }
                if (dataParameter.Value == null)
                {
                    value = DBNull.Value;
                }
                //				else
                //				{
                //					if (dataParameter.SqlDbType == MySql.Data.MySqlClient.MySqlDbType.Guid)
                //					{
                //						if (value.GetType() == typeof(string))
                //						{
                //							value = new Guid(value.ToStringValue());
                //						}
                //
                //					}
                //				}
                if (command is DbCommand)
                {
                    var parameters = command.Parameters as DbParameterCollection;
                    var parameter = parameters.GetParameter(dataParameter.Name);
                    if (parameter != null)
                    {
                        parameter.Value = value;
                    }
                    else
                    {
                        parameter = new MySqlParameter
                        {
                            ParameterName = dataParameter.Name,
                            Value = value,
                            IsNullable = dataParameter.IsNullable,
                            Direction = dataParameter.Direction,
                            SourceColumn = dataParameter.ColumnName,
                            //TODO change
                            //								DbType = dataParameter.SqlDbType,
                            Size = dataParameter.Size
                        };
                        if (parameters != null)
                        {
                            parameters.Add(parameter);
                        }
                    }
                }


                else
                {
                    this.Columns.Remove(dataParameter.ColumnName);
                }
            }

        }

    }
}

