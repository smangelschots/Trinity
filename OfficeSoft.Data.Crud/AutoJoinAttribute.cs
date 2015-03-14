namespace OfficeSoft.Data.Crud
{
    using System;

    [AttributeUsage(AttributeTargets.Property)]
    public class AutoJoinAttribute : Attribute
    {
        public AutoJoinAttribute() { }
    }
}