namespace OfficeSoft.Data.Crud
{
    using System.Collections.Generic;

    public interface IModelConfiguration
    {
        void SetModelConfiguration(IModelBase model);
        List<ModelValidation> Validations { get; set; }
        List<RegularExpression> Expressions { get; set; }

        void MergeModelConfiguration(IModelConfiguration configuration);
    }
}