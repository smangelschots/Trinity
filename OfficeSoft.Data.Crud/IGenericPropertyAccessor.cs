using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OfficeSoft.Data.Crud
{
    public interface IGenericPropertyAccessor<T, V>
    {
        /// <summary>
        /// Gets the value stored in the property for 
        /// the specified target.
        /// </summary>
        /// <param name="target">Object to retrieve
        /// the property from.</param>
        /// <returns>Property value.</returns>
        V Get(T target);

        /// <summary>
        /// Sets the value for the property of
        /// the specified target.
        /// </summary>
        /// <param name="target">Object to set the
        /// property on.</param>
        /// <param name="value">Property value.</param>
        void Set(T target, V value);
    }
}
