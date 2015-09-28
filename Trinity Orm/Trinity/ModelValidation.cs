namespace Trinity
{
    public class ModelValidation
    {
        public string Name { get; set; }
        public string RegExpression { get; set; }
        public bool IsRequired { get; set; }
        public string Message { get; set; }
    }
}