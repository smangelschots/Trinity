
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Linq;
using System.Reflection;

namespace Trinity.Ole
{
    public class OleModelCommand<T> : DataCommand<T> where T : class
    {
        public OleModelCommand(IDbConnection connection)
            : base(connection)
        {
        }

        public OleModelCommand(IDbConnection connection,
            List<IDataParameter> parameters, DataCommandType commandType)
            : base(connection, parameters, commandType)
        {
        }

        public OleModelCommand(IDataManager manager)
            : base(manager)
        {
        }

        public override void SetParameter(string parameterName, string column, object value, bool isSelectParameter)
        {


            var newDataParameter = this.Parameters.SingleOrDefault(
                 m => m.Name.Contains(parameterName));

            if (newDataParameter == null)
            {
                newDataParameter = new OleDataParameter()
                {
                    Name = parameterName,
                    ColumnName = column,
                    IsSelectParameter = isSelectParameter,
                };
                this.Columns.Add(column);
                this.Parameters.Add(newDataParameter);
            }
            newDataParameter.Value = value;


        }

        public override string GetTableMap()
        {
            var modelName = this.GetTableMapName();
            IsMapped = false;
            TableMap tableMap = null;
            if (Manager.TableMaps.ContainsKey(modelName))
            {
                tableMap = Manager.TableMaps[modelName];
            }
            else
            {
                tableMap = new TableMap();
                if (string.IsNullOrEmpty(this.TabelName))
                {
                    tableMap.TableName = this.GetTableAttribute();

                    this.TabelName = tableMap.TableName;

                    IsMapped = true;
                }
                else
                {
                    tableMap.TableName = this.TabelName;
                    if (typeof(T).Name == this.TabelName)
                        IsMapped = true;
                }

                tableMap.ColumnMaps = this.GetColumnAttributes();
                this.PrimaryKeys = tableMap.GetPrimaryKeys();
                Manager.TableMaps.Add(modelName, tableMap);
            }
            if (tableMap != null)
            {

                this.TableMap = tableMap;
            }
            return modelName;


        }

        public override List<IColumnMap> GetColumnAttributes(List<IColumnMap> columnMaps = null)
        {

            if (columnMaps == null)
                columnMaps = new List<IColumnMap>();
            var modelType = typeof(T);
            var properties = modelType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var propertyInfo in properties)
            {
                var name = string.Empty;
                if (propertyInfo.CanWrite)
                {
                    var atribute = propertyInfo.GetCustomAttributes(typeof(IgnoreAttribute), false);
                    if (!atribute.Any())
                    {
                        //TODO move to entity support project
                        //var columnEntityFrameworkAttribute =
                        //    propertyInfo.GetCustomAttributes(
                        //        typeof(ColumnAttribute),
                        //        false);
                        //if (columnEntityFrameworkAttribute.Any())
                        //{
                        //    var column = columnEntityFrameworkAttribute.FirstOrDefault() as ColumnAttribute;
                        //    if (column != null)
                        //    {
                        //        name = column.Name;
                        //    }
                        //}
                        //else
                        //{
                            var columAttribute = propertyInfo.GetCustomAttributes(
                                typeof(ColumnConfigurationAttribute),
                                false);

                            if (columAttribute.Any())
                            {
                                var colmn = columAttribute.FirstOrDefault() as ColumnConfigurationAttribute;
                                if (colmn != null)
                                {
                                    name = colmn.Name;
                                }
                            }
                            else
                            {
                                name = propertyInfo.Name;
                            }
                        //}

                        var columnMap = new OleColumnMap();
                        columnMap.ColumnName = name;
                        columnMap.DbType = GetOleDbType(propertyInfo);
                        columnMap.PropertyName = propertyInfo.Name;
                        if (columnMaps.FirstOrDefault(m => m.ColumnName == name) == null)
                            columnMaps.Add(columnMap);
                    }
                }
            }
            return columnMaps;
        }


        private OleDbType GetOleDbType(PropertyInfo propertyInfo)
        {

            var item = propertyInfo.PropertyType.Name.ToLower();


            //http://msdn.microsoft.com/en-us/library/yy6y35y8%28v=vs.110%29.aspx
            switch (item)
            {
                //case "int":
                //    return OleDbType.BigInt;
                //case "binary":
                //    return OleDbType.Binary;
                case "bit":
                    return OleDbType.Boolean;
                    break;
                case "char":
                    return OleDbType.Char;
                    break;
                case "datetime":
                    return OleDbType.DBTimeStamp;
                    break;
                case "decimal":
                    return OleDbType.Decimal;
                    break;
                case "float":
                    return OleDbType.Double;
                    break;
                case "binary":
                    return OleDbType.Variant;
                    break;
                case "int":
                case "int32":
                case "int64":
                    return OleDbType.Integer;
                    break;
                ////case "float":
                //    return OleDbType.Currency;
                //    break;
                //case SqlDbType.NChar:
                //    return OleDbType.WChar;
                //    break;
                //case SqlDbType.NText:
                //    return OleDbType.VarWChar;
                //    break;
                case "string":
                    return OleDbType.VarWChar;
                    break;
                //case SqlDbType.Real:
                //    return OleDbType.Single;
                //    break;
                case "guid":
                    return OleDbType.Guid;
                    break;
                //case SqlDbType.SmallDateTime:
                //    return OleDbType.DBDate;
                //    break;
                //case SqlDbType.SmallInt:
                //    return OleDbType.SmallInt;
                //    break;
                //case SqlDbType.SmallMoney:
                //    return OleDbType.Currency;
                //    break;
                //case SqlDbType.Text:
                //    return OleDbType.VarWChar;
                //    break;
                //case SqlDbType.Timestamp:
                //    return OleDbType.DBTimeStamp;
                //    break;
                //case SqlDbType.TinyInt:
                //    return OleDbType.TinyInt;
                //    break;
                //case SqlDbType.VarBinary:
                //    return OleDbType.VarBinary;
                //    break;
                //case SqlDbType.VarChar:
                //    return OleDbType.VarChar;
                //    break;
                //case SqlDbType.Variant:
                //    return OleDbType.Variant;
                //    break;
                //case SqlDbType.Xml:
                //    return OleDbType.VarWChar;
                //    break;
                //case SqlDbType.Udt:
                //    return OleDbType.Variant;
                //    break;
                //case SqlDbType.Structured:
                //    return OleDbType.Variant;
                //    break;
                //case SqlDbType.Date:
                //    return OleDbType.Date;
                //    break;
                //case SqlDbType.Time:
                //    return OleDbType.DBTime;
                //    break;
                //case SqlDbType.DateTime2:
                //    return OleDbType.DBTimeStamp;
                //    break;
                //case SqlDbType.DateTimeOffset:
                //    return OleDbType.DBTimeStamp;
                //    break;
                default:
                    throw new ArgumentOutOfRangeException("sqlDbType");
            }

        }


        protected internal void BuildSqlParameters(IDbCommand command)
        {
            var splitText = command.CommandText.Split(' ');

            foreach (var text in splitText)
            {
                var columnName = text.Replace(",", "");
                columnName = columnName.Replace(")", "");
                columnName = columnName.Replace("(", "");

                var dataParameter = Parameters.FirstOrDefault(m => m.ColumnName == columnName);
                if (dataParameter != null)
                {
                    var oledbParameter = dataParameter as OleDataParameter;

                    var value = oledbParameter.Value;
                    if (value == null) value = DBNull.Value;

                    var parameters = command.Parameters as DbParameterCollection;
                    if (parameters != null)
                    {
                        var parameter = parameters.GetParameter(string.Format("@{0}", dataParameter.Name));
                        if (parameter != null)
                        {
                            parameter.Value = value;
                        }
                        else
                        {
                            var tempPar = new OleDbParameter()
                            {
                                ParameterName = string.Format("@{0}", oledbParameter.Name),
                                Value = value,
                                IsNullable = oledbParameter.IsNullable,
                                Direction = oledbParameter.Direction,
                                SourceColumn = oledbParameter.ColumnName,
                                OleDbType = oledbParameter.DbType,
                                Size = dataParameter.Size
                            };

                            var map = TableMap.ColumnMaps.FirstOrDefault(m => m.ColumnName == oledbParameter.ColumnName);

                            if (map != null)
                            {
                                var oleMap = map as OleColumnMap;

                                if (oleMap != null)
                                    tempPar.OleDbType = oleMap.DbType;

                            }

                            parameter = tempPar;
                            command.Parameters.Add(parameter);
                        }
                    }
                }
            }
        }

        public override IDataCommand<T> Value(string column, object value)
        {
            this.SetParameter(column, column, value, false);
            return this;
        }

        public override IDataCommand<T> And(string column, string opperator, object value)
        {
            this.WhereText += string.Format(" And {0} {1} {2}", column, opperator, "?");
            this.SetParameter(column, column, value, true);
            return this;
        }

        public override IDataCommand<T> Or(string column, string opperator, object value)
        {
            this.WhereText += string.Format(" Or {0} {1} {2}", column, opperator, "?");
            this.SetParameter(column, column, value, true);
            return this;
        }

        public override IDataCommand<T> Where(string column, string opperator, object value)
        {
            this.SetParameter(column, column, value, true);
            this.WhereText = string.Format("{0} {1} {2}", column, opperator, "?");
            return this;
        }

        protected override string BuildUpdateCommand()
        {
            string commandString = string.Format("UPDATE {0}", this.TabelName) + " SET ";
            foreach (string column in this.Columns)
            {
                var dataParameter =
                    this.Parameters.FirstOrDefault(m => m.ColumnName == column && !m.IsSelectParameter);
                if (dataParameter != null)
                    commandString += string.Format(" {0} = {1},", column, "?");
            }
            this.SqlCommandText = commandString.Remove(commandString.Length - 1) + this.GetWhere();
            return SqlCommandText;

        }

        protected override string BuildInsertCommand()
        {
            string commandString = string.Format("INSERT INTO {0} (", this.TabelName);
            string valueString = string.Empty;
            foreach (string column in this.Columns)
            {
                var dataParameter =
                    this.Parameters.FirstOrDefault(m => m.ColumnName == column && !m.IsSelectParameter);
                if (dataParameter != null)
                {
                    commandString += string.Format(" {0},", column);
                    valueString = valueString + string.Format(" @{0},", dataParameter.Name);
                }
            }
            if (string.IsNullOrEmpty(valueString)) return string.Empty;

            commandString = commandString.Remove(commandString.Length - 1) + string.Format(") VALUES ({0})", valueString.Remove(valueString.Length - 1));
            if (!string.IsNullOrEmpty(this.WhereText))
            {
                commandString += this.WhereText;
            }
            this.SqlCommandText = commandString;
            return commandString;
        }
    }
}
