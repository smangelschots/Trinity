using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Trinity
{
    [Bindable(false)]
    [Browsable(false)]
    public class ModelConfiguration<T> : IModelConfiguration where T : class
    {
        protected IModelBase Model { get; set; }
        public List<ModelValidation> Validations { get; set; }
        public List<RegularExpression> Expressions { get; set; }
        private List<ColumnConfiguration<T>> Columns { get; set; }
        private List<Type> MergeWithEntities { get; set; } 


        public static string NoDateExpression = "NoDate";
        public static string NotNullExpression = "NotNull";
        public static string NotZeroExpression = "NotZero";

        public void AddExpression(string name, string expression, string message = "")
        {

            if (string.IsNullOrEmpty(name)) return;
            if (string.IsNullOrEmpty(expression)) return;

            var exp = this.Expressions.FirstOrDefault(m => m.Name == name);
            if (exp != null) return;

            this.Expressions.Add(new RegularExpression()
                                 {
                                     Name = name,
                                     Expression = expression,
                                     Message = message,
                                 });
        }
        public void RemoveExpression(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            var exp = this.Expressions.FirstOrDefault(m => m.Name == name);
            if (exp == null) return;

            this.Expressions.Remove(exp);
        }
        public ModelConfiguration()
        {
            this.Validations = new List<ModelValidation>();
            this.Expressions = new List<RegularExpression>();
            this.Columns  = new List<ColumnConfiguration<T>>();
            this.MergeWithEntities = new List<Type>();
        }

        public ColumnConfiguration<T> Column<TField>(Expression<Func<T, TField>> field)
        {
            var name = GetName(field);
            var config = new ColumnConfiguration<T>() { Name = name };
            Columns.Add(config);
            return config;
        }

        public ModelConfiguration<T> MergeModel<TClass>()
        {
            this.MergeWithEntities.Add(typeof(TClass));
            return this;
        }  


        public void MergeModelConfiguration(IModelConfiguration configuration)
        {
            foreach (var item in configuration.Expressions)
            {
                this.AddExpression(item.Name, item.Expression, item.Message);
            }
            foreach (var item in configuration.Validations)
            {
                this.SetValidation(item.Name, item.IsRequired, item.Message, item.RegExpression);
            }
        }

        public virtual void SetDefaultExpressions()
        {

            this.AddExpression(NoDateExpression, @"^(((0?[1-9]|[12]\d|3[01])[\.\-\/](0?[13578]|1[02])[\.\-\/]((1[6-9]|[2-9]\d)?\d{2}))|((0?[1-9]|[12]\d|30)[\.\-\/](0?[13456789]|1[012])[\.\-\/]((1[6-9]|[2-9]\d)?\d{2}))|((0?[1-9]|1\d|2[0-8])[\.\-\/]0?2[\.\-\/]((1[6-9]|[2-9]\d)?\d{2}))|(29[\.\-\/]0?2[\.\-\/]((1[6-9]|[2-9]\d)?(0[48]|[2468][048]|[13579][26])|((16|[2468][048]|[3579][2])00)|00)))$", "Geef een geldige datum in.");
            this.AddExpression(NotNullExpression, "^.+$", "Value may not be empty");
            this.AddExpression(NotZeroExpression, "^[0-9]*[1-9]+$|^[1-9]+[0-9]*$", "Select a value");
        }

        public void SetModelConfiguration(IModelBase model)
        {

            if (model == null)
            {
                throw new Exception("model is SetModelConfiguration Is NUll");
            }
            this.Model = model;

            this.Model.PropertyChanged -= this.Model_PropertyChanged;

            foreach (var validation in this.Validations)
            {
                this.Model.SetColumnError(validation.Name, validation.Message);
            }

            this.Model.PropertyChanged += this.Model_PropertyChanged;

        }

        void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.OnValidateProperty(e.PropertyName);
        }

        public virtual ModelValidation OnValidateProperty(string propertyName)
        {
            var validation = this.Validations.FirstOrDefault(m => m.Name == propertyName);
            if (validation == null) return null;
            var value = this.GetValue(propertyName);

            if (validation.IsRequired)
            {
                if (value == null)
                {
                    this.AddError(validation);
                    return validation;
                }
                if (!string.IsNullOrEmpty(validation.RegExpression))
                {
                    if (Regex.IsMatch(value.ToStringValue(), validation.Message))
                    {
                        this.RemoveError(validation);
                        return validation;
                    }
                    this.AddError(validation);
                    return validation;
                }
                 if (!string.IsNullOrEmpty(value.ToStringValue()))
                {
                    this.RemoveError(validation);
                    return validation;
                }
                 if (value.ToInt() != 0)
                {
                    this.RemoveError(validation);
                    return validation;
                }
                this.AddError(validation);
                return validation;
            }
            if (!string.IsNullOrEmpty(validation.RegExpression))
            {
                if (Regex.IsMatch(value.ToStringValue(), validation.Message))
                {
                    this.RemoveError(validation);
                    return validation;
                }
                this.AddError(validation);
                return validation;
            }
            return validation;
        }

        private void AddError(ModelValidation validation)
        {
            if (this.Model.Errors.ContainsKey(validation.Name))
                this.Model.Errors.Add(validation.Name, validation.Message);
        }

        private void RemoveError(ModelValidation validation)
        {
            if (this.Model.Errors.ContainsKey(validation.Name))
            {
                this.Model.Errors.Remove(validation.Name);
            }
        }

        public virtual object GetValue(string propertyName)
        {
            var model = this.Model as T;

            if (model != null)
            {
                var property = model.GetType().GetProperty(propertyName);
                var value = property.GetValue(model, null);
                return value;
            }
            return null;
        }

        public ModelConfiguration<T> SetValidation(string name, bool isRequired, string message, string regexName)
        {

            var validation = this.Validations.FirstOrDefault(m => m.Name == name);
            if (validation == null)
            {
                validation = new ModelValidation() { Name = name, IsRequired = isRequired, Message = message };

                var expression = this.Expressions.FirstOrDefault(m => m.Name == regexName);
                if (expression != null)
                {
                    validation.RegExpression = expression.Expression;
                    if (string.IsNullOrEmpty(validation.Message))
                    {
                        validation.Message = expression.Message;
                    }
                }
                this.Validations.Add(validation);
            }
            return this;
        }
        public ModelConfiguration<T> SetValidation<TField>(
            Expression<Func<T, TField>> field,
            bool isRequired,
            string message,
            string regexName)
        {
            var name = this.GetName(field);
            return SetValidation(name, isRequired, message, regexName);
        }

        public ModelConfiguration<T> SetRequired<TField>(Expression<Func<T, TField>> field, string message, string regexName)
        {
            this.SetValidation(field, true, message, regexName);
            return this;
        }
        public ModelConfiguration<T> SetRequired<TField>(Expression<Func<T, TField>> field, string message)
        {
            this.SetRequired(field, message, string.Empty);
            return this;
        }
        public ModelConfiguration<T> SetRequiredDate<TField>(Expression<Func<T, TField>> field, string message)
        {
            this.SetRequired(field, NoDateExpression, message);
            return this;
        }

        private string GetName<TField>(Expression<Func<T, TField>> field)
        {
            if (field == null)
                throw new ArgumentNullException("propertyExpression");

            var memberExpression = field.Body as MemberExpression;
            if (memberExpression == null)
                throw new ArgumentException("memberExpression");

            var property = memberExpression.Member as PropertyInfo;
            if (property == null)
                throw new ArgumentException("property");

            var getMethod = property.GetGetMethod(true);
            if (getMethod.IsStatic)
                throw new ArgumentException("static method");

            return memberExpression.Member.Name;
        }
    }
}