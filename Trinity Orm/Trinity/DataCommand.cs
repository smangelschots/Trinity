
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Trinity
{
    public abstract class DataCommand<T> : DataCommand, IDataCommand<T>
        where T : class
    {
        private DataValidationCollection ValidationCollection { get; set; }
        public bool IsMapped { get; set; }
        public bool Track { get; set; }

        public event EventHandler<ModelCommandExecutedEventArgs<T>> CommandExecuted;
        public event EventHandler<ModelCommandValidationEventArgs<T>> SetValidation;


        //TODO Implement Validation make user of baseclass 
        public void OnSetValidation(ModelCommandValidationEventArgs<T> e)
        {
            EventHandler<ModelCommandValidationEventArgs<T>> handler = this.SetValidation;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public virtual void OnCommandExecuted(ModelCommandExecutedEventArgs<T> e)
        {
            EventHandler<ModelCommandExecutedEventArgs<T>> handler = this.CommandExecuted;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public ResultList SaveChanges()
        {
            return Manager.SaveChanges();
        }


        public event EventHandler<ModelCommandPropertyChangedEventArgs> Validating;

        public TableMap TableMap { get; set; }
        public virtual void BuildKeys()
        {

            var index = 0;
            foreach (var key in this.PrimaryKeys)
            {

                if (index == 0)
                {
                    this.Where(key, "=", this.GetValue(key));
                }
                else
                {
                    this.And(key, "=", this.GetValue(key));
                }
                index++;
            }
        }

        public virtual List<IColumnMap> GetColumnAttributes(List<IColumnMap> columnMaps = null)
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
                        var columnMap = new ColumnMap();
                        columnMap.ColumnName = name;
                        columnMap.PropertyName = propertyInfo.Name;
                        if (columnMaps.FirstOrDefault(m => m.ColumnName == name) == null)
                            columnMaps.Add(columnMap);
                    }
                }
            }
            return columnMaps;
        }
        public virtual string GetTableAttribute()
        {

            var modelType = typeof(T);
            var tableAttribute = modelType.GetCustomAttributes(typeof(TableConfigurationAttribute), false);
            if (tableAttribute.Any())
            {
                var table = tableAttribute.FirstOrDefault() as TableConfigurationAttribute;
                if (table != null)
                {
                    return table.TableName;
                }

            }
            return modelType.Name;
        }

        public object GetValue(string name)
        {
            var propInfo = typeof(T).GetProperty(name);
            try
            {
                var value = propInfo.GetValue(this.Model, null);
                return value;
            }
            catch (Exception exception)
            {
                var message =
                    $"Can't GetValue from property {name} try adding the [IgnoreAttribute] to the property {exception.Message} {exception.StackTrace}";
                LoggingService.SendToLog("DataCommand", message, LogType.Error);
                //  throw new NotImplementedException("Error in get value ", exception);
            }
            return DBNull.Value;
        }

        public void OnValidating(object manager, ModelCommandPropertyChangedEventArgs e)
        {
            EventHandler<ModelCommandPropertyChangedEventArgs> handler = this.Validating;
            if (handler != null)
            {
                handler(manager, e);
            }
        }

        public void SetValue(string name, object value)
        {
            var propInfo = typeof(T).GetProperty(name);
            propInfo.SetValue(this.Model, value, null);
        }

        public override bool Validate()
        {
            var errors = this.Model as IDataErrorInfo;

            if (errors == null) return true;

            return true;
        }



        public T Model { get; set; }
        public List<string> Changes { get; set; }
        public List<string> PrimaryKeys { get; set; }

        public DataCommand(IDbConnection connection)
            : base(connection)
        {
        }

        public DataCommand(IDbConnection connection, List<IDataParameter> parameters, DataCommandType commandType)
            : base(connection, parameters, commandType)
        {
        }

        protected DataCommand(IDataManager manager)
            : base(manager)
        {
            this.Changes = new List<string>();
            this.PrimaryKeys = new List<string>();
            this.ValidationCollection = new DataValidationCollection();
        }

        public void AddChanges()
        {
            var model = this.Model;
            if (string.IsNullOrEmpty(this.TabelName))
            {

            }
            if (this.TableMap == null)
            {
                //TODO Check for bugs
                if (Manager.TableMapFromDatabase)
                    this.GetTableMap();
            }
            foreach (var propertyInfo in model.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                this.AddPropertyChange(propertyInfo);
            }
        }

        public string GetTableMapName()
        {
            return string.Format("{0}_{1}", typeof(T).Name, this.TabelName);

        }
        public virtual string GetTableMap()
        {
            var modelName = this.GetTableMapName();
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
                }
                else
                {
                    tableMap.TableName = this.TabelName;
                }

                tableMap.ColumnMaps = this.GetColumnAttributes();
                this.PrimaryKeys = tableMap.GetPrimaryKeys();
                Manager.TableMaps.Add(modelName, tableMap);
            }
            if (tableMap != null)
            {
                this.IsMapped = true;
                this.TableMap = tableMap;
            }
            return modelName;
        }

        public void AddPropertyChange(PropertyInfo property)
        {
            if (property.CanWrite)
            {
                var atribute = property.GetCustomAttributes(typeof(IgnoreAttribute), false);
                if (!atribute.Any())
                {
                    var value = this.GetValue(property.Name);
                    this.OnValidating(
                        this,
                        new ModelCommandPropertyChangedEventArgs(property.Name)
                        {
                            Value = value,
                            ModelCommand = this
                        });

                    var name = property.Name;
                    if (this.Changes.Contains(name) == false)
                    {
                        this.Changes.Add(property.Name);
                    }
                }
            }
        }

        protected internal string GetField<TField>(Expression<Func<T, TField>> field)
        {
            if (field == null)
                throw new ArgumentNullException("propertyExpression");

            var memberExpression = field.Body as MemberExpression;
            if (memberExpression == null)
                throw new ArgumentException("memberExpression");

            var property = memberExpression.Member as PropertyInfo;
            if (property == null)
                throw new ArgumentException("property");

            var getMethod = property.GetGetMethod(true);
            if (getMethod.IsStatic)
                throw new ArgumentException("static method");

            return memberExpression.Member.Name; ;
        }

        public IDataCommand<T> WithKey<TField>(Expression<Func<T, TField>> field)
        {
            var key = GetField(field);

            if (!string.IsNullOrEmpty(key))
            {
                if (this.PrimaryKeys.FirstOrDefault(m => m == key) == null)
                {
                    this.PrimaryKeys.Add(key);
                }
            }

            return this;
        }
        public IDataCommand<T> WithKeys(string[] keys)
        {

            if (keys != null)
                foreach (var key in keys)
                {
                    var tempKey = key;
                    if (this.PrimaryKeys.FirstOrDefault(m => m == tempKey) == null)
                    {
                        this.PrimaryKeys.Add(tempKey);
                    }
                }
            return this;
        }

        public IDataCommand<T> Where(Expression<Func<T, bool>> expression)
        {
            var sql = this.Convert(expression);
            if (sql != null)
                Where(sql.Columnname, sql.Expression, sql.Value);

            return this;
        }



        public CommandExpression Convert<T1>(Expression<Func<T1, object>> expression)
        {
            if (expression.Body is UnaryExpression)
                return Convert(expression, (UnaryExpression)expression.Body);
            throw new InvalidOperationException("Unable to convert expression to SQL");
        }

        public string ConvertToString<T1>(Expression<Func<T1, object>> expression)
        {
            if (expression.Body is MemberExpression)
                return Convert(expression, (MemberExpression)expression.Body);
            if (expression.Body is MethodCallExpression)
                return Convert((MethodCallExpression)expression.Body);
            if (expression.Body is UnaryExpression)
                return ConvertToString(expression, (UnaryExpression)expression.Body);
            if (expression.Body is ConstantExpression)
                return Convert((ConstantExpression)expression.Body);


            throw new InvalidOperationException("Unable to convert expression to SQL");
        }


        public object ConvertToObject<T1>(Expression<Func<T1, object>> expression)
        {
            if (expression.Body is MemberExpression)
                return ConvertToObject(expression, (MemberExpression)expression.Body);
            if (expression.Body is MethodCallExpression)
                return ConvertToObject((MethodCallExpression)expression.Body);
            if (expression.Body is UnaryExpression)
                return ConvertToObject(expression, (UnaryExpression)expression.Body);
            if (expression.Body is ConstantExpression)
                return Convert((ConstantExpression)expression.Body);

            throw new InvalidOperationException("Unable to convert expression to SQL");
        }
        public CommandExpression Convert<T1>(Expression<Func<T1, bool>> expression)
        {
            if (expression.Body is BinaryExpression)
                return Convert<T>((BinaryExpression)expression.Body);
            var memberExpression = expression.Body as MemberExpression;
            throw new InvalidOperationException("Unable to convert expression to SQL");
        }

        private object ConvertToObject<T1>(Expression<Func<T1, object>> expression, MemberExpression body)
        {
            // TODO: should really do something about conventions and overridden names here
            var member = body.Member;


            if (member.DeclaringType.IsAssignableFrom(typeof(T)))
                return member.Name;

            var compiledExpression = expression.Compile();
            var value = compiledExpression(default(T1));


            return ConvertToObject(value);
        }


        private string Convert<T1>(Expression<Func<T1, object>> expression, MemberExpression body)
        {
            // TODO: should really do something about conventions and overridden names here
            var member = body.Member;


            if (member.DeclaringType.IsAssignableFrom(typeof(T)))
                return member.Name;


            var compiledExpression = expression.Compile();
            var value = compiledExpression(default(T1));


            return Convert(value);
        }

        private object ConvertToObject<T1>(Expression<Func<T1, object>> expression, UnaryExpression body)
        {
            var constant = body.Operand as ConstantExpression;


            if (constant != null)
                return Convert(constant);


            var member = body.Operand as MemberExpression;


            if (member != null)
                return Convert(expression, member);


            var unaryExpression = body.Operand as UnaryExpression;


            if (unaryExpression != null && unaryExpression.NodeType == ExpressionType.Convert)
                return ConvertToObject(expression, unaryExpression);


            throw new InvalidOperationException("Unable to convert expression to SQL");
        }


        private string ConvertToString<T1>(Expression<Func<T1, object>> expression, UnaryExpression body)
        {
            var constant = body.Operand as ConstantExpression;


            if (constant != null)
                return Convert(constant);


            var member = body.Operand as MemberExpression;


            if (member != null)
                return Convert(expression, member);


            var unaryExpression = body.Operand as UnaryExpression;


            if (unaryExpression != null && unaryExpression.NodeType == ExpressionType.Convert)
                return ConvertToString(expression, unaryExpression);


            throw new InvalidOperationException("Unable to convert expression to SQL");
        }



        private CommandExpression Convert<T>(Expression<Func<T, object>> expression, UnaryExpression body)
        {
            var constant = body.Operand as ConstantExpression;
            var member = body.Operand as MemberExpression;

            var unaryExpression = body.Operand as UnaryExpression;


            if (unaryExpression != null && unaryExpression.NodeType == ExpressionType.Convert)
                return Convert(expression, unaryExpression);


            throw new InvalidOperationException("Unable to convert expression to SQL");
        }


        private Expression<Func<T, object>> CreateExpression<T>(Expression body)
        {
            var expression = body;
            var parameter = Expression.Parameter(typeof(T), "x");


            if (expression.Type.IsValueType)
                expression = Expression.Convert(expression, typeof(object));
            return (Expression<Func<T, object>>)Expression.Lambda(typeof(Func<T, object>), expression, parameter);
        }


        private CommandExpression Convert<T1>(BinaryExpression expression)
        {

            //TODO make paramters values.
            var left = ConvertToString(CreateExpression<T>(expression.Left));
            var right = ConvertToObject(CreateExpression<T>(expression.Right));
            string op;


            switch (expression.NodeType)
            {
                default:
                case ExpressionType.Equal:
                    op = "=";
                    break;
                case ExpressionType.GreaterThan:
                    op = ">";
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    op = ">=";
                    break;
                case ExpressionType.LessThan:
                    op = "<";
                    break;
                case ExpressionType.LessThanOrEqual:
                    op = "<=";
                    break;
                case ExpressionType.NotEqual:
                    op = "!=";
                    break;
            }






            return new CommandExpression()
            {
                Columnname = left,
                Expression = op,
                Value = right
            };



        }

        private string Convert(object value)
        {
            if (value is string)
            {
                if (value.ToStringValue().EndsWith("'") == false)
                    return "'" + value + "'";

                return value.ToStringValue();
            }

            if (value is bool)
            {
                return (bool)value ? "1" : "0";
            }

            if (value is ConstantExpression)
            {
                var item = value as ConstantExpression;

                if (!string.IsNullOrEmpty(item.Value.ToStringValue()))
                {
                    return item.Value.ToStringValue();
                }
                return null;
            }

            return value.ToStringValue();
        }


        private object ConvertToObject(object value)
        {
            if (value is string)
            {
                return value.ToStringValue();
            }
            if (value is bool)
            {
                return value.ToBool();
            }
            if (value is ConstantExpression)
            {
                var item = value as ConstantExpression;
                return item.Value.ToStringValue();
            }

            return value;
        }



        public T FirstOrDefault()
        {
            this.TopIndex = 1;
            return this.ExecuteToList().FirstOrDefault();
        }

        public IDataCommand<T> All()
        {
            this.SelectAll = true;
            return this;
        }

        public virtual IDataCommand<T> Value(string column, object value)
        {
            var column1 = string.Format("[{0}]", column);
            string parameterName = string.Format("@{0}_{1}", column.Replace(" ", ""), 1);
            this.SetParameter(parameterName, column1, value, false);
            return this;
        }

        //TODO TEST
        public IDataCommand<T> Column(string name)
        {
            this.SelectAll = false;
            this.Columns.Add(name);
            return this;
        }

        public virtual IDataCommand<T> Count<TField>(Expression<Func<T, TField>> field)
        {
            return this;
        }

        public IDataCommand<T> Take(int rows)
        {
            this.TakeIndex = rows;
            return this;
        }
        public IDataCommand<T> Skip(int rows)
        {
            this.SkipIndex = rows;
            return this;
        }

        public IDataCommand<T> Top(int items)
        {
            this.TopIndex = items;
            return this;
        }

        public IDataCommand<T> InTo(string tableName)
        {
            this.TabelName = tableName;
            return this;
        }
        public IDataCommand<T> ForInsert()
        {
            this.CommandType = DataCommandType.Insert;
            return this;
        }
        public IDataCommand<T> ForUpdate()
        {
            this.CommandType = DataCommandType.Update;
            return this;
        }


        public virtual IDataCommand<T> And(string column, string opperator, object value)
        {
            if (value == null)
            {
                if (opperator == "=")
                {
                    this.WhereText = string.Format(" And [{0}] is null", column);
                }
                else
                {
                    this.WhereText = string.Format(" And [{0}] is not null", column);
                }
            }
            else
            {
                var parameterName = string.Format("{0}_{1}", column.Replace(" ", ""), 2);
                this.WhereText += string.Format(" And [{0}] {1} {2}", column, opperator, string.Format("@{0}", parameterName));
                SetParameter(parameterName, column, value, true);
            }
            return this;
        }
        public IDataCommand<T> From(string tableName = "")
        {
            TabelName = tableName;
            return this;
        }

        public virtual IDataCommand<T> WhereBetween<TField>(Expression<Func<T, TField>> field, object begin, object end)
        {
            string property = GetField(field);

            var parameterBegin = $"{property.Replace(" ", "")}_Begin";
            var parameterEnd = $"{property.Replace(" ", "")}_End";


            this.WhereText = string.Format(" WHERE [{0}] BETWEEN {1} AND {2}", property,
                $"@{parameterBegin}",
                $"@{parameterEnd}");
            SetParameter(parameterBegin, property, begin, true);
            SetParameter(parameterEnd, property, end, true);

            return this;

        }

        public virtual IDataCommand<T> WhereNotBetween<TField>(Expression<Func<T, TField>> field, object begin, object end)
        {
            string property = GetField(field);

            var parameterBegin = $"{property.Replace(" ", "")}_Begin";
            var parameterEnd = $"{property.Replace(" ", "")}_End";


            this.WhereText = string.Format(" WHERE [{0}] NOT BETWEEN {1} AND {2}", property,
                $"@{parameterBegin}",
                $"@{parameterEnd}");
            SetParameter(parameterBegin, property, begin, true);
            SetParameter(parameterEnd, property, end, true);

            return this;

        }

        public virtual IDataCommand<T> And(string filterString)
        {
            this.WhereText += string.Format(" And {0}", filterString);
            return this;
        }


        public virtual IDataCommand<T> And(Expression<Func<T, bool>> expression)
        {

            var filter = Convert(expression);
            And(filter.Columnname, filter.Expression, filter.Value);
            return this;
        }

        public virtual IDataCommand<T> Or(string filterString)
        {
            this.WhereText += string.Format(" Or {0}", filterString); ;
            return this;
        }

        public virtual IDataCommand<T> Or(string column, string opperator, object value)
        {
            if (value == null)
            {
                if (opperator == "=")
                {
                    this.WhereText = string.Format(" Or [{0}] is null", column);
                }
                else
                {
                    this.WhereText = string.Format(" Or [{0}] is not null", column);
                }
            }
            else
            {
                string parameterName = string.Format("{0}_{1}", column.Replace(" ", ""), 2);
                this.WhereText += string.Format(" Or [{0}] {1} {2}", column, opperator, string.Format("@{0}", parameterName));
                this.SetParameter(parameterName, column, value, true);
            }


            return this;
        }

        public virtual IDataCommand<T> OrderBy(string column)
        {
            if (string.IsNullOrEmpty(this.OrderByString))
            {
                this.OrderByString = string.Format(" ORDER BY [{0}]", column);
            }
            else
            {
                this.OrderByString += string.Format(", [{0}]", column);
            }
            return this;
        }

        public virtual IDataCommand<T> OrderByDesc(string column)
        {
            if (string.IsNullOrEmpty(this.OrderByString))
            {
                this.OrderByString = string.Format(" ORDER BY [{0}] DESC", column); ;
            }
            else
            {
                this.OrderByString += string.Format(", [{0}] DESC", column);
            }
            return this;
        }


        public virtual IDataCommand<T> Or(Expression<Func<T, bool>> expression)
        {
            var filter = Convert(expression);
            Or(filter.Columnname, filter.Expression, filter.Value);
            return this;
        }

        public IDataCommand<T> Where(string filterString)
        {
            this.WhereText = filterString;
            return this;
        }




        public IDataCommand<T> Where<TField>(Expression<Func<T, TField>> field, string opperator, object value)
        {
            string property = GetField(field);

            if (opperator.ToLower() == "like")
            {
                value = string.Format("%{0}%", value);
            }

            return Where(property, opperator, value);
        }


        public virtual IDataCommand<T> Where(string column, string opperator, object value)
        {

            if (value == null)
            {
                if (opperator == "=")
                {
                    this.WhereText = string.Format("[{0}] is null", column);
                }
                else
                {
                    this.WhereText = string.Format("[{0}] is not null", column);
                }
            }
            else
            {
                string parameterName = string.Format("{0}_{1}", column.Replace(" ", ""), 2);
                this.WhereText = string.Format("[{0}] {1} {2}", column, opperator, string.Format("@{0}", parameterName));
                this.SetParameter(parameterName, column, value, true);
            }
            return this;
        }

        public IList<T> ExecuteToList()
        {

            var list = new List<T>();
            
            var result = this.Manager.ExecuteCommand(this) as ModelCommandResult<T>;
            this.Manager.Remove(this);
            if (result != null)
            {
                if (result.HasErrors == false)
                {
                    list = result.Data.ToList();
                }
            }
            Manager.Connection.Close();
            Manager.Connection.Dispose();


            return list;
        }

        public async Task<IList<T>> ExecuteToListAsync()
        {
            var list = new List<T>();
            var result = await this.Manager.ExecuteCommandAsync(this) as ModelCommandResult<T>;
            this.Manager.Remove(this);
            if (result != null)
            {
                if (result.HasErrors == false)
                {
                    list = result.Data.ToList();
                }
            }
            return list;
        }

        public async void ExecuteToList(Action<IList<T>> callback)
        {

            var list = new List<T>();
            var result = await this.Manager.ExecuteCommandAsync(this) as ModelCommandResult<T>;
            this.Manager.Remove(this);
            if (result != null)
            {
                if (result.HasErrors == false)
                {
                    list = result.Data.ToList();
                }
            }
            callback.Invoke(list);

        }

    }

    public class CommandExpression
    {

        public string Columnname { get; set; }
        public string Expression { get; set; }
        public object Value { get; set; }
    }


    public abstract class DataCommand
    {
        private string _tabelName;
        private int _top;

        protected internal IDataManager Manager { get; set; }

        public IDbConnection Connection { get; set; }

        protected string WhereText { get; set; }

        public string SqlCommandText { get; set; }

        public DataCommandType CommandType { get; set; }

        public int TakeIndex { get; set; }

        public int SkipIndex { get; set; }

        public int TopIndex { get; set; }

        public string OrderByString { get; set; }

        public string TabelName
        {
            get { return _tabelName; }
            set
            {
                _tabelName = value;
            }
        }

        public List<IDataParameter> Parameters { get; set; }

        public bool SelectAll { get; set; }

        public List<string> Columns { get; set; }

        public DataCommand(IDbConnection connection)
            : this(connection, new List<IDataParameter>(), DataCommandType.Update)
        {
            this.WhereText = string.Empty;
        }

        public DataCommand(IDbConnection connection, List<IDataParameter> parameters, DataCommandType commandType)
        {
            this.Connection = connection;
            this.Columns = new List<string>();
            this.CommandType = commandType;


            if (parameters == null)
            {
                parameters = new List<IDataParameter>();
            }

            this.Parameters = parameters;

        }

        public void SetWhereText(string text)
        {
            this.WhereText = text;
        }

        public string GetWhereText()
        {
            return this.WhereText;
        }

        public DataCommand(IDataManager manager)
            : this(manager.Connection)
        {
            this.Manager = manager;
        }

        public string BuildSqlCommand()
        {
            if (!string.IsNullOrEmpty(this.SqlCommandText)) return string.Empty;
            switch (this.CommandType)
            {
                case DataCommandType.Select:
                    if (this.Columns.Count == 0) this.SelectAll = true;
                    return this.BuildSelectCommand();
                case DataCommandType.Insert:
                    return this.BuildInsertCommand();
                case DataCommandType.Update:
                    return this.BuildUpdateCommand();
                case DataCommandType.Delete:
                    return this.BuildDeleteCommand();
            }
            return this.SqlCommandText;
        }

        protected virtual string BuildDeleteCommand()
        {
            this.SqlCommandText = "DELETE" + this.GetFrom() + this.GetWhere();
            return SqlCommandText;
        }

        protected virtual string BuildUpdateCommand()
        {
            string commandString = string.Format("UPDATE [{0}]", this.TabelName) + " SET ";
            foreach (string column in this.Columns)
            {
                var dataParameter =
                    this.Parameters.FirstOrDefault(m => m.ColumnName == column && !m.IsSelectParameter);
                if (dataParameter != null)
                    commandString += string.Format(" {0} = {1},", column, dataParameter.Name);
            }
            this.SqlCommandText = commandString.Remove(commandString.Length - 1) + this.GetWhere();
            return SqlCommandText;
        }

        protected virtual string BuildInsertCommand()
        {
            string commandString = string.Format("INSERT INTO [{0}] (", this.TabelName);
            string valueString = string.Empty;
            foreach (string column in this.Columns)
            {
                var dataParameter =
                    this.Parameters.FirstOrDefault(m => m.ColumnName == column && !m.IsSelectParameter);
                if (dataParameter != null)
                {
                    commandString += string.Format(" {0},", column);
                    valueString = valueString + string.Format(" {0},", dataParameter.Name);
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

        protected virtual string BuildSelectCommand()
        {
            string commandString = "SELECT ";

            if (TopIndex > 0)
            {
                commandString += string.Format(" TOP {0} ", TopIndex);
            }

            commandString = (!this.SelectAll ? commandString + this.GetColumns() : commandString + " * ") + this.GetFrom() + this.GetWhere();
            if (!string.IsNullOrEmpty(this.OrderByString))
            {
                commandString += this.OrderByString;
                if (this.TakeIndex > 0)
                {
                    commandString += string.Format("OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY", this.SkipIndex, this.TakeIndex);
                }
            }
            this.SqlCommandText = commandString;
            return commandString;
        }

        private string GetColumns()
        {
            string columnString = this.Columns.Aggregate(string.Empty, (current, column) => current + string.Format("{0},", column));
            if (!string.IsNullOrEmpty(columnString))
                columnString = columnString.Remove(columnString.Length - 1);
            return columnString;
        }

        protected virtual string GetFrom()
        {
            if (!string.IsNullOrEmpty(this.TabelName))
                if (this.TabelName.Contains("JOIN"))
                    return string.Format(" FROM {0} ", this.TabelName);
                else
                    return string.Format(" FROM [{0}] ", this.TabelName);
            else
                throw new ApplicationException("No tablename");
        }


        public virtual void ResetCommand()
        {
            this.Columns.Clear();
            this.Parameters.Clear();
            this.SqlCommandText = string.Empty;
            this.WhereText = string.Empty;
            this.OrderByString = string.Empty;
            this.TabelName = null;
            this.SelectAll = false;
            this.TakeIndex = 0;
            this.SkipIndex = 0;
            this.TopIndex = 0;
        }

        public abstract void SetParameter(string parameterName, string column, object value, bool isSelectParameter);


        protected virtual string GetWhere()
        {

            if (!string.IsNullOrEmpty(this.WhereText))
            {
                if (this.WhereText.ToLower().Contains("where"))
                {
                    return this.WhereText;
                }
                return string.Format(" WHERE {0}", this.WhereText);
            }
            return this.WhereText;
        }

        public override string ToString()
        {
            return this.SqlCommandText;
        }

        public object[] GetValues()
        {
            var values = new List<object>();
            foreach (var item in this.Parameters)
            {
                values.Add(item.Value);
            }
            return values.ToArray();
        }

        public DataCommand AddColumns(string[] columns)
        {

            for (int i = 0; i < columns.Count(); i++)
            {
                this.AddColumn(columns[i]);
            }
            return this;
        }

        private DataCommand AddColumn(string column)
        {
            var newColumn = string.Format("[{0}]", column);
            var oldColumn = this.Columns.FirstOrDefault(m => m == newColumn);
            if (oldColumn == null)
            {
                this.Columns.Add(newColumn);
            }
            return this;
        }

        public virtual bool Validate()
        {
            return true;
        }
    }
}
