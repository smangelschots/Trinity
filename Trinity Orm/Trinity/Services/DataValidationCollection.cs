using System;
using System.Collections;

namespace Trinity
{
    public class DataValidationCollection : CollectionBase
    {

        public void Add(DataValidation validation)
        {
            this.List.Add(validation);
        }

        public void Remove(DataValidation validation)
        {
            this.List.Remove(validation);
        }

        public DataValidation this[int index]
        {
            get
            {
                return (DataValidation)this.List[index];
            }
        }

        public bool ContainsColumnName(string columnname)
        {
            foreach (DataValidation item in this)
            {
                if (item.Columname.Equals(columnname))
                {
                    return true;
                }
            }
            return false;
        }


        private string IsValidDate(int day, int month, int year)
        {
            if (month < 1 || month > 12) return "Month";
            if (day < 1 || day > DateTime.DaysInMonth(year, month)) return "Day";
            if (year < 1980 || year > 2010) return "Year";
            return "";
        }
    }
}
