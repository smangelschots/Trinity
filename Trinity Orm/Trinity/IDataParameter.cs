﻿using System.Data;

namespace Trinity
{
    public interface IDataParameter : IColumnMap
    {
        string Name { get; set; }
        DataRowVersion SourceVersion { get; set; }
        object Value { get; set; }
        string SourceColumn { get; set; }
        ParameterDirection Direction { get; set; }
        bool IsSelectParameter { get; set; }
    }
}