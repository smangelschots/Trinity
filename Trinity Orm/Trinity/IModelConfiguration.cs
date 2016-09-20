using System.Collections.Generic;

namespace Trinity
{
    public interface IModelConfiguration
    {
        void SetModelConfiguration(IModelBase model);

        void MergeModelConfiguration(IModelConfiguration configuration);
        void Validate();
    }
}