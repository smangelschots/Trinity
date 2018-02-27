using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Trinity.MsSql
{
    public class SqlModelCommand<T> : DataCommand<T> where T : class
    {


        public SqlModelCommand(IDbConnection connection)
            : base(connection)
        {

        }

        public SqlModelCommand(IDbConnection connection, List<IDataParameter> parameters, DataCommandType commandType)
            : base(connection, parameters, commandType)
        {
        }


        public SqlModelCommand(IDataManager manager) : base(manager)
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
                    newDataParameter = Activator.CreateInstance<SqlDataParameter>();
                    newDataParameter.Name = parameterName;

                    newDataParameter.ColumnName = column;
                    newDataParameter.IsSelectParameter = isSelectParameter;

                    Column(column);
                    this.Parameters.Add(newDataParameter);
                }
                ;

                newDataParameter.Value = value;
            }
        }

        public override string GetTableMap()
        {
            if (string.IsNullOrEmpty(this.TabelName))
            {
                this.TabelName = this.GetTableAttribute();
                IsMapped = true;
            }
            else
            {
                if (typeof(T).Name == this.TabelName)
                    IsMapped = true;
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
                    if (Manager.TableMaps.ContainsKey(modelName) == false)
                    {
                        try
                        {
                            Manager.TableMaps.Add(modelName, tableMap);
                        }
                        catch (Exception)
                        {

                        }
                        
                    }
                    this.TableMap = tableMap;
                    string sqlTableMap =
                            string.Format(
                                @"SELECT INFORMATION_SCHEMA.TABLES.TABLE_TYPE, INFORMATION_SCHEMA.COLUMNS.TABLE_CATALOG, 
                      INFORMATION_SCHEMA.COLUMNS.TABLE_SCHEMA, INFORMATION_SCHEMA.COLUMNS.TABLE_NAME, INFORMATION_SCHEMA.COLUMNS.COLUMN_NAME, 
                      INFORMATION_SCHEMA.COLUMNS.ORDINAL_POSITION, INFORMATION_SCHEMA.COLUMNS.COLUMN_DEFAULT, INFORMATION_SCHEMA.COLUMNS.IS_NULLABLE, 
                      INFORMATION_SCHEMA.COLUMNS.DATA_TYPE, INFORMATION_SCHEMA.COLUMNS.CHARACTER_MAXIMUM_LENGTH, 
                      INFORMATION_SCHEMA.COLUMNS.CHARACTER_OCTET_LENGTH, INFORMATION_SCHEMA.COLUMNS.NUMERIC_PRECISION, 
                      INFORMATION_SCHEMA.COLUMNS.NUMERIC_PRECISION_RADIX, INFORMATION_SCHEMA.COLUMNS.NUMERIC_SCALE, 
                      INFORMATION_SCHEMA.COLUMNS.DATETIME_PRECISION, INFORMATION_SCHEMA.COLUMNS.CHARACTER_SET_CATALOG, 
                      INFORMATION_SCHEMA.COLUMNS.CHARACTER_SET_SCHEMA, INFORMATION_SCHEMA.COLUMNS.CHARACTER_SET_NAME, 
                      INFORMATION_SCHEMA.COLUMNS.COLLATION_CATALOG, INFORMATION_SCHEMA.COLUMNS.COLLATION_SCHEMA, 
                      INFORMATION_SCHEMA.COLUMNS.COLLATION_NAME, INFORMATION_SCHEMA.COLUMNS.DOMAIN_CATALOG, 
                      INFORMATION_SCHEMA.COLUMNS.DOMAIN_SCHEMA, INFORMATION_SCHEMA.COLUMNS.DOMAIN_NAME,
					  COLUMNPROPERTY(object_id(INFORMATION_SCHEMA.COLUMNS.TABLE_NAME), INFORMATION_SCHEMA.COLUMNS.COLUMN_NAME, 'IsIdentity') as IsIdentity,
					  COLUMNPROPERTY(object_id(INFORMATION_SCHEMA.COLUMNS.TABLE_NAME), INFORMATION_SCHEMA.COLUMNS.COLUMN_NAME, 'ColumnId') as ColumnId,
					  COLUMNPROPERTY(object_id(INFORMATION_SCHEMA.COLUMNS.TABLE_NAME), INFORMATION_SCHEMA.COLUMNS.COLUMN_NAME, 'IsComputed ') as IsComputed,
                      OBJECT_ID(INFORMATION_SCHEMA.COLUMNS.TABLE_NAME) as TableId
                      FROM INFORMATION_SCHEMA.TABLES INNER JOIN
                      INFORMATION_SCHEMA.COLUMNS ON INFORMATION_SCHEMA.TABLES.TABLE_NAME = INFORMATION_SCHEMA.COLUMNS.TABLE_NAME
                      WHERE (NOT (INFORMATION_SCHEMA.COLUMNS.TABLE_NAME LIKE N'sys%')) AND 
                      (OBJECTPROPERTY(OBJECT_ID(QUOTENAME(INFORMATION_SCHEMA.TABLES.TABLE_SCHEMA) + '.' + QUOTENAME(INFORMATION_SCHEMA.TABLES.TABLE_NAME)), 'IsMSShipped') = 0)
                      AND INFORMATION_SCHEMA.TABLES.TABLE_NAME = '{0}'
                      ORDER BY INFORMATION_SCHEMA.TABLES.TABLE_TYPE, INFORMATION_SCHEMA.COLUMNS.TABLE_SCHEMA, INFORMATION_SCHEMA.COLUMNS.TABLE_NAME,  INFORMATION_SCHEMA.COLUMNS.COLUMN_NAME",
                                tableMap.TableName);



                    using (var conn = new SqlConnection(Manager.ConnectionString))
                    {



                        conn.Open();
                        using (var command = new SqlCommand(sqlTableMap, conn))
                        {
                            using (var reader = command.ExecuteReader())
                            {
                                int index = 0;



                                while (reader.Read())
                                {
                                    if (index == 0)
                                    {
                                        tableMap.Id = reader["TableId"].ToInt();
                                        tableMap.TableName = reader["TABLE_NAME"].ToStringValue();
                                        tableMap.Catalog = reader["TABLE_CATALOG"].ToStringValue();
                                        tableMap.Schema = reader["TABLE_SCHEMA"].ToStringValue();
                                        tableMap.TableType = reader["TABLE_TYPE"].ToStringValue();

                                    }
                                    index++;

                                    var columnName = reader["COLUMN_NAME"].ToStringValue();
                                    var columnMap = tableMap.ColumnMaps.FirstOrDefault(m => m.ColumnName == columnName);

                                    if (columnMap == null)
                                    {
                                        columnMap = new SqlColumnMap();

                                        columnMap.ColumnName = columnName;
                                        columnMap.IsMapped = false;
                                        tableMap.ColumnMaps.Add(columnMap);
                                    }

                                    var map = columnMap as SqlColumnMap;
                                    if (map == null)
                                        throw new ApplicationException("ColumnMap is no SqlColumnMap");

                                    map.Id = reader["ColumnId"].ToInt();
                                    map.IsIdentity = reader["IsIdentity"].ToBool();
                                    map.IsComputed = reader["IsComputed"].ToBool();
                                    map.IsNullable = reader["IS_NULLABLE"].ToBool();
                                    map.Size = reader["CHARACTER_MAXIMUM_LENGTH"].ToInt();
                                    map.DbType = reader["DATA_TYPE"].ToEnumValue<SqlDbType>(SqlDbType.NVarChar);
                                    map.OrdinalPosition = reader["ORDINAL_POSITION"].ToInt();
                                    map.Default = reader["COLUMN_DEFAULT"].ToStringValue();

                                }
                                reader.Close();

                            }
                            var propertyMap = this.GetColumnAttributes();
                            foreach (var column in tableMap.ColumnMaps)
                            {
                                var item =
                                 propertyMap.FirstOrDefault(m => m.ColumnName == column.ColumnName);

                                if (item != null)
                                {
                                    column.IsMapped = true;
                                    column.PropertyName = item.PropertyName;
                                }
                                else
                                {
                                    column.IsMapped = false;
                                }
                            }

                            tableMap.KeyMaps = GetKeyMap(tableMap.TableName, tableMap.ColumnMaps);

                            foreach (var item in tableMap.KeyMaps)
                            {
                                var column = tableMap.ColumnMaps.FirstOrDefault(m => m.ColumnName == item.ParentColumnName);
                                if (column != null)
                                    switch (item.KeyType)
                                    {
                                        case KeyMapType.PrimaryKey:
                                            column.IsPrimaryKey = true;
                                            break;
                                        case KeyMapType.ForeignKey:
                                            column.IsForeinKey = true;
                                            break;
                                    }



                            }
                        }
                    }
                    this.PrimaryKeys = tableMap.GetPrimaryKeys();
                }
            }
            return modelName;
        }
        private List<KeyMap> GetKeyMap(string tablename, List<IColumnMap> columnMaps)
        {

            var keys = new List<KeyMap>();


            string sqlKeysMap =
                string.Format(
                    @"WITH   ALL_KEYS_IN_TABLE (CONSTRAINT_NAME,CONSTRAINT_TYPE,PARENT_TABLE_NAME,PARENT_COL_NAME,PARENT_COL_NAME_DATA_TYPE,REFERENCE_TABLE_NAME,REFERENCE_COL_NAME) AS(SELECT CONSTRAINT_NAME= CAST (PKnUKEY.name AS VARCHAR(150)) ,CONSTRAINT_TYPE=CAST (PKnUKEY.type_desc AS VARCHAR(150)) , PARENT_TABLE_NAME=CAST (PKnUTable.name AS VARCHAR(150)) , PARENT_COL_NAME=CAST ( PKnUKEYCol.name AS VARCHAR(150)) ,PARENT_COL_NAME_DATA_TYPE=  oParentColDtl.DATA_TYPE,REFERENCE_TABLE_NAME='' ,REFERENCE_COL_NAME='' 
                                FROM sys.key_constraints as PKnUKEY INNER JOIN sys.tables as PKnUTable ON PKnUTable.object_id = PKnUKEY.parent_object_id INNER JOIN sys.index_columns as PKnUColIdx ON PKnUColIdx.object_id = PKnUTable.object_id AND PKnUColIdx.index_id = PKnUKEY.unique_index_id INNER JOIN sys.columns as PKnUKEYCol ON PKnUKEYCol.object_id = PKnUTable.object_id AND PKnUKEYCol.column_id = PKnUColIdx.column_id INNER JOIN INFORMATION_SCHEMA.COLUMNS oParentColDtl ON oParentColDtl.TABLE_NAME=PKnUTable.name AND oParentColDtl.COLUMN_NAME=PKnUKEYCol.name
                                UNION ALL
                                SELECT  CONSTRAINT_NAME= CAST (oConstraint.name AS VARCHAR(150)) , CONSTRAINT_TYPE='FK', PARENT_TABLE_NAME=CAST (oParent.name AS VARCHAR(150)) , PARENT_COL_NAME=CAST ( oParentCol.name AS VARCHAR(150)) , PARENT_COL_NAME_DATA_TYPE= oParentColDtl.DATA_TYPE, REFERENCE_TABLE_NAME=CAST ( oReference.name AS VARCHAR(150)) , REFERENCE_COL_NAME=CAST (oReferenceCol.name AS VARCHAR(150)) 
                                FROM sys.foreign_key_columns FKC INNER JOIN sys.sysobjects oConstraint ON FKC.constraint_object_id=oConstraint.id INNER JOIN sys.sysobjects oParent ON FKC.parent_object_id=oParent.id INNER JOIN sys.all_columns oParentCol ON FKC.parent_object_id=oParentCol.object_id AND FKC.parent_column_id=oParentCol.column_id INNER JOIN sys.sysobjects oReference ON FKC.referenced_object_id=oReference.id INNER JOIN INFORMATION_SCHEMA.COLUMNS oParentColDtl ON oParentColDtl.TABLE_NAME=oParent.name AND oParentColDtl.COLUMN_NAME=oParentCol.name INNER JOIN sys.all_columns oReferenceCol ON FKC.referenced_object_id=oReferenceCol.object_id AND FKC.referenced_column_id=oReferenceCol.column_id)
                                SELECT * FROM ALL_KEYS_IN_TABLE WHERE PARENT_TABLE_NAME  in ('{0}') ORDER BY PARENT_TABLE_NAME,CONSTRAINT_NAME;",
                    tablename);

            using (var conn = new SqlConnection(Manager.ConnectionString))
            {
                conn.Open();
                using (var keyCommand = new SqlCommand(sqlKeysMap, conn))
                {

                    using (var keyReader = keyCommand.ExecuteReader())
                    {
                        while (keyReader.Read())
                        {
                            var keyMap = new KeyMap();
                            keyMap.KeyType =
                                keyMap.GetSqlKeyType(keyReader["CONSTRAINT_TYPE"].ToStringValue());
                            keyMap.ParentColumnName = keyReader["PARENT_COL_NAME"].ToStringValue();
                            keyMap.ReferenceTableName =
                                keyReader["REFERENCE_TABLE_NAME"].ToStringValue();
                            keyMap.ReferenceColumnName = keyReader["REFERENCE_COL_NAME"].ToStringValue();

                            if (columnMaps != null)
                            {
                                var columnmap = columnMaps.FirstOrDefault(m => m.ColumnName == keyMap.ParentColumnName);
                                if (columnmap != null)
                                {
                                    keyMap.PropertyName = columnmap.PropertyName;
                                    keyMap.IsMapped = true;
                                }
                            }

                            if (
                                keys.FirstOrDefault(
                                    m =>
                                        m.ParentColumnName == keyMap.ParentColumnName
                                        && m.KeyType == keyMap.KeyType) == null) keys.Add(keyMap);
                        }
                        keyReader.Close();
                    }
                }
            }
            return keys;
        }
        public override void BuildKeys()
        {
            var index = 0;

            if (this.TableMap == null)
            {
                base.BuildKeys();
            }
            else
            {
                foreach (var key in this.PrimaryKeys)
                {
                    var column = this.TableMap.ColumnMaps.FirstOrDefault(m => m.PropertyName == key);
                    if (column != null)
                    {
                        if (index == 0)
                        {
                            this.Where(column.ColumnName, "=", this.GetValue(column.PropertyName));
                        }
                        else
                        {
                            this.And(column.ColumnName, "=", this.GetValue(column.PropertyName));
                        }
                    }
                    else
                    {
                        column = this.TableMap.ColumnMaps.FirstOrDefault(m => m.ColumnName == key);

                        bool buildNormal = true;
                        if (column != null)
                        {
                            if (Model != null)
                            {
                                var item = Model as IModelBase;
                                if (item != null)
                                {
                                    if (item.OldValues.ContainsKey(column.PropertyName))
                                    {
                                        if (index == 0)
                                        {
                                            this.Where(column.ColumnName, "=", item.OldValues[column.PropertyName]);
                                        }
                                        else
                                        {
                                            this.And(column.ColumnName, "=", item.OldValues[column.PropertyName]);
                                        }
                                        buildNormal = false;
                                    }
                                }
                            }
                            if (buildNormal)
                            {
                                if (index == 0)
                                {
                                    this.Where(column.ColumnName, "=", this.GetValue(column.PropertyName));
                                }
                                else
                                {
                                    this.And(column.ColumnName, "=", this.GetValue(column.PropertyName));
                                }
                            }
                        }
                    }
                    index++;
                }
            }
        }

        //TODO TEST
        public override IDataCommand<T> Count<TField>(Expression<Func<T, TField>> field)
        {
            this.SelectAll = false;
            this.Columns.Clear();
            string property = GetField(field);

            this.Columns.Add(string.Format("COUNT(*) as {0}", property));
            return this;
        }

        public override IDataCommand<T> Or(string column, string opperator, object value)
        {
            string columnText = column;

            var columnObject = GetColumn(columnText);
            if (columnObject != null)
            {
                columnText = columnObject.ColumnName;
            }


            return base.Or(columnText, opperator, value);
        }

        public override IDataCommand<T> And(string column, string opperator, object value)
        {
            string columnText = column;

            var columnObject = GetColumn(columnText);
            if (columnObject != null)
            {
                columnText = columnObject.ColumnName;
            }


            return base.And(columnText, opperator, value);
        }


        public override IDataCommand<T> Where(string column, string opperator, object value)
        {

            string columnText = column;

            var columnObject = GetColumn(columnText);
            if (columnObject != null)
            {
                columnText = columnObject.ColumnName;
            }

            return base.Where(columnText, opperator, value);
        }

        private IColumnMap GetColumn(string name)
        {
            GetTableMap();

            if (this.TableMap != null)
            {
                var columnObject = TableMap.ColumnMaps.FirstOrDefault(m => m.ColumnName == name);
                if (columnObject == null)
                    columnObject = TableMap.ColumnMaps.FirstOrDefault(m => m.PropertyName == name);


                if (columnObject != null)
                {
                    return columnObject;
                }

            }

            return null;
        }

        public IDataCommand<T> FromWhereColumnsMap2Model(string tableName = "",  bool onlymapped = true)
        {

            this.From(tableName);
            
            //TODO test
            GetTableMap();



            var columns = GetColumnAttributes();
            foreach (var columnMap in columns)
            {
                bool addColumn = true;



                if (onlymapped)
                {
                    if (this.TableMap == null)
                        addColumn = false;

                    var column = this.TableMap.ColumnMaps.FirstOrDefault(m => m.ColumnName == columnMap.ColumnName);
                    if (column == null)
                        addColumn = false;
                }

                if (addColumn)
                    Column(  columnMap.ColumnName);
            }

            this.SelectAll = false;
            return this;

        }


        protected internal void BuildSqlParameters(IDbCommand command)
        {

            foreach (var dataParameter in this.Parameters)
            {

                var sqlparameter = dataParameter as SqlDataParameter;

                if (sqlparameter == null)
                    throw new ApplicationException("Parameter is no SqlDataParameter");

                var columnText = dataParameter.ColumnName.Replace("[", "").Replace("]", "");
                if (this.TableMap != null)
                {
                    var column = this.TableMap.ColumnMaps.FirstOrDefault(m => m.ColumnName == columnText);

                    if (column == null)
                    {
                        column = this.TableMap.ColumnMaps.FirstOrDefault(m => m.PropertyName == columnText);
                        if (column == null)
                        {

                        
                        }

                    }

                    if (column != null)
                    {

                        var col = column as SqlColumnMap;

                        if (col != null)
                        {
                            sqlparameter.DbType = col.DbType;
                            sqlparameter.Size = col.Size;
                            sqlparameter.IsNullable = col.IsNullable;
                            sqlparameter.Size = col.Size;
                            sqlparameter.IsForeinKey = col.IsForeinKey;
                            sqlparameter.IsPrimaryKey = col.IsPrimaryKey;
                        }
                    }
                }
                if (sqlparameter.DbType != SqlDbType.Timestamp)
                {
                    var value = sqlparameter.Value;

                    if (sqlparameter.Size > 0)
                    {
                        var valuestring = value.ToStringValue();
                        if (!string.IsNullOrEmpty(valuestring))
                        {
                            if (valuestring.Length > sqlparameter.Size)
                            {
                                throw new ApplicationException(string.Format("Property {0} value is bigger than database size {1} column name in {2} {3} Value {4} length is {5} column length is {6}"
                                    , sqlparameter.PropertyName, sqlparameter.ColumnName, this.TabelName, valuestring, valuestring.Length, sqlparameter.Size, valuestring.Length));
                            }
                        }
                    }
                    if (sqlparameter.Value == null)
                    {
                       value = DBNull.Value;
                    }
                    else
                    {
                        switch (sqlparameter.DbType)
                        {
                            case SqlDbType.BigInt:
                                break;
                            case SqlDbType.Binary:
                                break;
                            case SqlDbType.Bit:
                                value = value
                                    .ToStringValue()
                                    .ToLower();
                                break;
                            case SqlDbType.Char:
                                
                                break;
                            case SqlDbType.DateTime:
                                
                                break;
                            case SqlDbType.Decimal:
                                break;
                            case SqlDbType.Float:
                                break;
                            case SqlDbType.Image:
                                break;
                            case SqlDbType.Int:
                                value = value.ToInt();
                                break;
                            case SqlDbType.Money:
                                break;
                            case SqlDbType.NChar:
                                break;
                            case SqlDbType.NText:
                                break;
                            case SqlDbType.NVarChar:
                                break;
                            case SqlDbType.Real:
                                break;
                            case SqlDbType.UniqueIdentifier:
                                if (value is string)
                                {
                                    value = new Guid(value.ToStringValue());
                                }
                                break;
                            case SqlDbType.SmallDateTime:
                                break;
                            case SqlDbType.SmallInt:
                                break;
                            case SqlDbType.SmallMoney:
                                break;
                            case SqlDbType.Text:
                                break;
                            case SqlDbType.Timestamp:
                                break;
                            case SqlDbType.TinyInt:
                                break;
                            case SqlDbType.VarBinary:
                                break;
                            case SqlDbType.VarChar:
                                break;
                            case SqlDbType.Variant:
                                break;
                            case SqlDbType.Xml:
                                break;
                            case SqlDbType.Udt:
                                break;
                            case SqlDbType.Structured:
                                break;
                            case SqlDbType.Date:
                                break;
                            case SqlDbType.Time:
                                break;
                            case SqlDbType.DateTime2:
                                break;
                            case SqlDbType.DateTimeOffset:
                                break;
                            default:
                                break;
                        }
                    }

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
                            parameter = new SqlParameter
                            {
                                ParameterName = sqlparameter.Name,
                                Value = value,
                                IsNullable = sqlparameter.IsNullable,
                                Direction = sqlparameter.Direction,
                                SourceColumn = sqlparameter.ColumnName,
                                SqlDbType = sqlparameter.DbType,
                                Size = dataParameter.Size
                            };
                            if (parameters != null)
                            {
                                parameters.Add(parameter);
                            }
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
