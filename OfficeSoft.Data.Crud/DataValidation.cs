namespace OfficeSoft.Data.Crud
{
    public class DataValidation
    {

        public string ReplaceExpression { get; set; }
        public string RegExpression { get; set; }
        public string ErrorMessage { get; set; }
        public string Name { get; set; }
        public string Columname { get; set; }

        public DataValidation()
        { }
        public DataValidation(string columname, string name, string errormessage, string regexpression, string replaceexpression)
        {
            this.Columname = columname;
            this.Name = name;
            this.ErrorMessage = errormessage;
            this.RegExpression = regexpression;
        }
    }
}
