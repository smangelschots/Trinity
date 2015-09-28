using System.Collections.Generic;

namespace Trinity
{
    public interface IModelConfiguration
    {
        void SetModelConfiguration(IModelBase model);
        List<ModelValidation> Validations { get; set; }
        List<RegularExpression> Expressions { get; set; }

        void MergeModelConfiguration(IModelConfiguration configuration);
    }
}