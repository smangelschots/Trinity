using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace Trinity
{
    //TODO Rewrite
    public class CommandResult : ICommandResult
    {

        static ReaderWriterLockSlim RWLock = new ReaderWriterLockSlim();
        Dictionary<string, Delegate> Factories = new Dictionary<string, Delegate>();
        static Dictionary<Type, CommandResult> m_PocoDatas = new Dictionary<Type, CommandResult>();
        static List<Func<object, object>> m_Converters = new List<Func<object, object>>();
        static FieldInfo fldConverters = typeof(DataResult).GetField("m_Converters", BindingFlags.Static | BindingFlags.GetField | BindingFlags.NonPublic);
        static MethodInfo fnListGetItem = typeof(List<Func<object, object>>).GetProperty("Item").GetGetMethod();
        static MethodInfo fnIsDBNull = typeof(IDataRecord).GetMethod("IsDBNull");
        public Type type;
        static MethodInfo fnGetValue = typeof(IDataRecord).GetMethod("GetValue", new Type[] { typeof(int) });
        static MethodInfo fnInvoke = typeof(Func<object, object>).GetMethod("Invoke");
        public Dictionary<string, DataColumn> Columns { get; private set; }

        public bool ForceDateTimesToUtc { get; set; }
        public TableInfo TableInfo { get; private set; }
        public string[] QueryColumns { get; private set; }
        public List<DataError> CommandErrors { get; set; }
        public List<string> Messages { get; set; }

        public string CommandText { get; set; }

        public void AddError(ErrorType errorType, string message, Exception exception = null)
        {
            this.CommandErrors.Add(new DataError()
                                   {
                                       ErrorType = errorType,
                                       Message = message,
                                       Exception = exception,
                                       HasError = true
                                   });


            //TODO change
            var sSource = "SQLdatamanager";
            var sLog = "SQLdatamanager";
            var sEvent = message;

            if (exception != null)
                sEvent += exception.Message + " " + exception.StackTrace;

            LoggingService.SendToLog(sLog, sEvent, errorType);


        }

        public void AddMessage(string message)
        {
            this.Messages.Add(message);
            LoggingService.SendToLog("SQLdatamanager", message, ErrorType.Information);
        }

        public string Name { get; set; }

        public int RecordsAffected { get; set; }

        public bool HasErrors
        {
            get
            {
                return this.CommandErrors.Any(m => m.HasError);
            }
        }

        public DataCommandType CommandType { get; set; }
        public IDataCommand DataCommand { get; set; }


        public CommandResult()
        {
            this.Messages = new List<string>();
            this.CommandErrors = new List<DataError>();
        }

        public CommandResult(Type t)
            : this()
        {
            this.type = t;
            this.TableInfo = new TableInfo();

            if (Database.Mapper != null)
                Database.Mapper.GetTableInfo(t, this.TableInfo);

            this.Columns = new Dictionary<string, DataColumn>(StringComparer.OrdinalIgnoreCase);
            foreach (var pi in t.GetProperties())
            {
                var pc = new DataColumn();
                pc.PropertyInfo = pi;

                if (pc.ColumnName == null)
                {
                    pc.ColumnName = pi.Name;
                    if (Database.Mapper != null && !Database.Mapper.MapPropertyToColumn(pi, ref pc.ColumnName, ref pc.ResultColumn))
                        continue;
                }

                // Store it
                this.Columns.Add(pc.ColumnName, pc);
            }

            // Build column list for automatic select
            this.QueryColumns = (from c in this.Columns where !c.Value.ResultColumn select c.Key).ToArray();

        }


        public static CommandResult ForType(Type t)
        {

            if (t == typeof(System.Dynamic.ExpandoObject))
                throw new InvalidOperationException("Can't use dynamic types with this method");

            // Check cache
            RWLock.EnterReadLock();
            CommandResult pd;
            try
            {
                if (m_PocoDatas.TryGetValue(t, out pd))
                    return pd;
            }
            finally
            {
                RWLock.ExitReadLock();
            }


            // Cache it
            RWLock.EnterWriteLock();
            try
            {
                // Check again
                if (m_PocoDatas.TryGetValue(t, out pd))
                    return pd;

                // Create it
                pd = new CommandResult(t);

                m_PocoDatas.Add(t, pd);
            }
            finally
            {
                RWLock.ExitWriteLock();
            }

            return pd;
        }

        public static CommandResult ForObject(object o, string primaryKeyName)
        {
            var t = o.GetType();
#if !PETAPOCO_NO_DYNAMIC
            if (t == typeof(System.Dynamic.ExpandoObject))
            {
                var pd = new CommandResult();
                pd.TableInfo = new TableInfo();
                pd.Columns = new Dictionary<string, DataColumn>(StringComparer.OrdinalIgnoreCase);
                pd.Columns.Add(primaryKeyName, new ExpandoColumn() { ColumnName = primaryKeyName });
                pd.TableInfo.PrimaryKey = primaryKeyName;
                pd.TableInfo.AutoIncrement = true;
                foreach (var col in (o as IDictionary<string, object>).Keys)
                {
                    if (col != primaryKeyName)
                        pd.Columns.Add(col, new ExpandoColumn() { ColumnName = col });
                }
                return pd;
            }
            else
#endif
                return ForType(t);
        }

        static bool IsIntegralType(Type t)
        {
            var tc = Type.GetTypeCode(t);
            return tc >= TypeCode.SByte && tc <= TypeCode.UInt64;
        }



        private static Func<object, object> GetConverter(bool forceDateTimesToUtc, DataColumn pc, Type srcType, Type dstType)
        {
            Func<object, object> converter = null;

            // Get converter from the mapper
            if (Database.Mapper != null)
            {
                if (pc != null)
                {
                    converter = Database.Mapper.GetFromDbConverter(pc.PropertyInfo, srcType);
                }
                else
                {
                    var m2 = Database.Mapper as IMapper2;
                    if (m2 != null)
                    {
                        converter = m2.GetFromDbConverter(dstType, srcType);
                    }
                }
            }

            // Standard DateTime->Utc mapper
            if (forceDateTimesToUtc && converter == null && srcType == typeof(DateTime) && (dstType == typeof(DateTime) || dstType == typeof(DateTime?)))
            {
                converter = delegate(object src) { return new DateTime(((DateTime)src).Ticks, DateTimeKind.Utc); };
            }

            // Forced type conversion including integral types -> enum
            if (converter == null)
            {
                if (dstType.IsEnum && IsIntegralType(srcType))
                {
                    if (srcType != typeof(int))
                    {
                        converter = delegate(object src) { return Convert.ChangeType(src, typeof(int), null); };
                    }
                }
                else if (!dstType.IsAssignableFrom(srcType))
                {
                    converter = delegate(object src) { return Convert.ChangeType(src, dstType, null); };
                }
            }
            return converter;
        }



        private static void AddConverterToStack(ILGenerator il, Func<object, object> converter)
        {
            if (converter != null)
            {
                // Add the converter
                int converterIndex = m_Converters.Count;
                m_Converters.Add(converter);

                // Generate IL to push the converter onto the stack
                il.Emit(OpCodes.Ldsfld, fldConverters);
                il.Emit(OpCodes.Ldc_I4, converterIndex);
                il.Emit(OpCodes.Callvirt, fnListGetItem);					// Converter
            }
        }
        public Delegate GetFactory(string sql, string connString, bool forceDateTimesToUtc, int firstColumn, int countColumns, IDataReader r)
        {
            // Check cache
            var key = string.Format("{0}:{1}:{2}:{3}:{4}", sql, connString, forceDateTimesToUtc, firstColumn, countColumns);
            RWLock.EnterReadLock();
            try
            {
                // Have we already created it?
                Delegate factory;
                if (this.Factories.TryGetValue(key, out factory))
                    return factory;
            }
            finally
            {
                RWLock.ExitReadLock();
            }

            // Take the writer lock
            RWLock.EnterWriteLock();

            try
            {

                // Check again, just in case
                Delegate factory;
                if (this.Factories.TryGetValue(key, out factory))
                    return factory;

                // Create the method
                var m = new DynamicMethod("petapoco_factory_" + this.Factories.Count.ToString(), this.type, new Type[] { typeof(IDataReader) }, true);
                var il = m.GetILGenerator();

#if !PETAPOCO_NO_DYNAMIC
                if (this.type == typeof(object))
                {
                    // var poco=new T()
                    il.Emit(OpCodes.Newobj, typeof(System.Dynamic.ExpandoObject).GetConstructor(Type.EmptyTypes));			// obj

                    MethodInfo fnAdd = typeof(IDictionary<string, object>).GetMethod("Add");

                    // Enumerate all fields generating a set assignment for the column
                    for (int i = firstColumn; i < firstColumn + countColumns; i++)
                    {
                        var srcType = r.GetFieldType(i);

                        il.Emit(OpCodes.Dup);						// obj, obj
                        il.Emit(OpCodes.Ldstr, r.GetName(i));		// obj, obj, fieldname

                        // Get the converter
                        Func<object, object> converter = null;
                        if (Database.Mapper != null)
                            converter = Database.Mapper.GetFromDbConverter(null, srcType);
                        if (forceDateTimesToUtc && converter == null && srcType == typeof(DateTime))
                            converter = delegate(object src) { return new DateTime(((DateTime)src).Ticks, DateTimeKind.Utc); };

                        // Setup stack for call to converter
                        AddConverterToStack(il, converter);

                        // r[i]
                        il.Emit(OpCodes.Ldarg_0);					// obj, obj, fieldname, converter?,    rdr
                        il.Emit(OpCodes.Ldc_I4, i);					// obj, obj, fieldname, converter?,  rdr,i
                        il.Emit(OpCodes.Callvirt, fnGetValue);		// obj, obj, fieldname, converter?,  value

                        // Convert DBNull to null
                        il.Emit(OpCodes.Dup);						// obj, obj, fieldname, converter?,  value, value
                        il.Emit(OpCodes.Isinst, typeof(DBNull));	// obj, obj, fieldname, converter?,  value, (value or null)
                        var lblNotNull = il.DefineLabel();
                        il.Emit(OpCodes.Brfalse_S, lblNotNull);		// obj, obj, fieldname, converter?,  value
                        il.Emit(OpCodes.Pop);						// obj, obj, fieldname, converter?
                        if (converter != null)
                            il.Emit(OpCodes.Pop);					// obj, obj, fieldname, 
                        il.Emit(OpCodes.Ldnull);					// obj, obj, fieldname, null
                        if (converter != null)
                        {
                            var lblReady = il.DefineLabel();
                            il.Emit(OpCodes.Br_S, lblReady);
                            il.MarkLabel(lblNotNull);
                            il.Emit(OpCodes.Callvirt, fnInvoke);
                            il.MarkLabel(lblReady);
                        }
                        else
                        {
                            il.MarkLabel(lblNotNull);
                        }

                        il.Emit(OpCodes.Callvirt, fnAdd);
                    }
                }
                else
#endif
                    if (this.type.IsValueType || this.type == typeof(string) || this.type == typeof(byte[]))
                    {
                        // Do we need to install a converter?
                        var srcType = r.GetFieldType(0);
                        var converter = GetConverter(forceDateTimesToUtc, null, srcType, this.type);

                        // "if (!rdr.IsDBNull(i))"
                        il.Emit(OpCodes.Ldarg_0);										// rdr
                        il.Emit(OpCodes.Ldc_I4_0);										// rdr,0
                        il.Emit(OpCodes.Callvirt, fnIsDBNull);							// bool
                        var lblCont = il.DefineLabel();
                        il.Emit(OpCodes.Brfalse_S, lblCont);
                        il.Emit(OpCodes.Ldnull);										// null
                        var lblFin = il.DefineLabel();
                        il.Emit(OpCodes.Br_S, lblFin);

                        il.MarkLabel(lblCont);

                        // Setup stack for call to converter
                        AddConverterToStack(il, converter);

                        il.Emit(OpCodes.Ldarg_0);										// rdr
                        il.Emit(OpCodes.Ldc_I4_0);										// rdr,0
                        il.Emit(OpCodes.Callvirt, fnGetValue);							// value

                        // Call the converter
                        if (converter != null)
                            il.Emit(OpCodes.Callvirt, fnInvoke);

                        il.MarkLabel(lblFin);
                        il.Emit(OpCodes.Unbox_Any, this.type);								// value converted
                    }
                    else
                    {
                        // var poco=new T()
                        il.Emit(OpCodes.Newobj, this.type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[0], null));

                        // Enumerate all fields generating a set assignment for the column
                        for (int i = firstColumn; i < firstColumn + countColumns; i++)
                        {
                            // Get the PocoColumn for this db column, ignore if not known
                            DataColumn pc;


                            var name = r.GetName(i);

                            if (!this.Columns.TryGetValue(name, out pc))
                            {
                                continue;
                            }
                                

                            // Get the source type for this column
                            var srcType = r.GetFieldType(i);
                            var dstType = pc.PropertyInfo.PropertyType;

                            // "if (!rdr.IsDBNull(i))"
                            il.Emit(OpCodes.Ldarg_0);										// poco,rdr
                            il.Emit(OpCodes.Ldc_I4, i);										// poco,rdr,i
                            il.Emit(OpCodes.Callvirt, fnIsDBNull);							// poco,bool
                            var lblNext = il.DefineLabel();
                            il.Emit(OpCodes.Brtrue_S, lblNext);								// poco

                            il.Emit(OpCodes.Dup);											// poco,poco

                            // Do we need to install a converter?
                            var converter = GetConverter(forceDateTimesToUtc, pc, srcType, dstType);

                            // Fast
                            bool Handled = false;
                            if (converter == null)
                            {
                                var valuegetter = typeof(IDataRecord).GetMethod("Get" + srcType.Name, new Type[] { typeof(int) });
                                if (valuegetter != null
                                    && valuegetter.ReturnType == srcType
                                    && (valuegetter.ReturnType == dstType || valuegetter.ReturnType == Nullable.GetUnderlyingType(dstType)))
                                {
                                    il.Emit(OpCodes.Ldarg_0);										// *,rdr
                                    il.Emit(OpCodes.Ldc_I4, i);										// *,rdr,i
                                    il.Emit(OpCodes.Callvirt, valuegetter);							// *,value

                                    // Convert to Nullable
                                    if (Nullable.GetUnderlyingType(dstType) != null)
                                    {
                                        il.Emit(OpCodes.Newobj, dstType.GetConstructor(new Type[] { Nullable.GetUnderlyingType(dstType) }));
                                    }

                                    il.Emit(OpCodes.Callvirt, pc.PropertyInfo.GetSetMethod(true));		// poco
                                    Handled = true;
                                }
                            }

                            // Not so fast
                            if (!Handled)
                            {
                                // Setup stack for call to converter
                                AddConverterToStack(il, converter);

                                // "value = rdr.GetValue(i)"
                                il.Emit(OpCodes.Ldarg_0);										// *,rdr
                                il.Emit(OpCodes.Ldc_I4, i);										// *,rdr,i
                                il.Emit(OpCodes.Callvirt, fnGetValue);							// *,value

                                // Call the converter
                                if (converter != null)
                                    il.Emit(OpCodes.Callvirt, fnInvoke);

                                // Assign it
                                il.Emit(OpCodes.Unbox_Any, pc.PropertyInfo.PropertyType);		// poco,poco,value
                                il.Emit(OpCodes.Callvirt, pc.PropertyInfo.GetSetMethod(true));		// poco
                            }

                            il.MarkLabel(lblNext);
                        }

                        var fnOnLoaded = RecurseInheritedTypes<MethodInfo>(this.type, (x) => x.GetMethod("OnLoaded", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[0], null));
                        if (fnOnLoaded != null)
                        {
                            il.Emit(OpCodes.Dup);
                            il.Emit(OpCodes.Callvirt, fnOnLoaded);
                        }
                    }

                il.Emit(OpCodes.Ret);

                // Cache it, return it
                var del = m.CreateDelegate(Expression.GetFuncType(typeof(IDataReader), this.type));
                //if (this.PocoFactories.ContainsKey(key) == false)
                //    this.PocoFactories.Add(key, del);
                return del;
            }
            finally
            {
                RWLock.ExitWriteLock();
            }
        }


        static T RecurseInheritedTypes<T>(Type t, Func<Type, T> cb)
        {
            while (t != null)
            {
                T info = cb(t);
                if (info != null)
                    return info;
                t = t.BaseType;
            }
            return default(T);
        }

    }
}