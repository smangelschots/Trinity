using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Trinity
{
    public abstract class ModelConfiguration
    {

        public event StartAddDefaultExpressionsEvent StartAddDefaultExpressions;

        public static string NoDateExpression = "NoDate";
        public static string NotNullExpression = "NotNull";
        public static string NotZeroExpression = "NotZero";

        protected virtual void OnStartAddDefaultExpressions(StartAddDefaultExpressionsEventArgs args)
        {
            StartAddDefaultExpressions?.Invoke(this, args);
        }
    }

    public delegate void StartAddDefaultExpressionsEvent(object sender, StartAddDefaultExpressionsEventArgs args);

    public class StartAddDefaultExpressionsEventArgs
    {
        public IModelConfiguration Configuration { get; }

        public StartAddDefaultExpressionsEventArgs(IModelConfiguration modelConfiguration)
        {
            this.Configuration = modelConfiguration;
        }

        public bool Cancel { get; set; }
    }


    [Bindable(false)]
    [Browsable(false)]
    public class ModelConfiguration<T> : ModelConfiguration, IModelConfiguration where T : class
    {

        public event ModelPropertyValidateEvent ModelPropertyValidate;
        public event AfterModelPropertyValidateEvent AfterModelPropertyValidate;

        protected IModelBase Model { get; set; }
        public static List<ModelValidation> Validations { get; set; }
        public static List<RegularExpression> Expressions { get; set; }
        private static List<ColumnConfiguration<T>> Columns { get; set; }
        private List<Type> MergeWithEntities { get; set; }


        public void AddExpression(string name, string expression, string message = "")
        {

            if (string.IsNullOrEmpty(name)) return;
            if (string.IsNullOrEmpty(expression)) return;

            var exp = Expressions.FirstOrDefault(m => m.Name == name);
            if (exp != null) return;

            Expressions.Add(new RegularExpression()
            {
                Name = name,
                Expression = expression,
                Message = message,
            });
        }
        public void RemoveExpression(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            var exp = Expressions.FirstOrDefault(m => m.Name == name);
            if (exp == null) return;

            Expressions.Remove(exp);
        }
        public ModelConfiguration()
        {
            Validations = new List<ModelValidation>();
            Expressions = new List<RegularExpression>();
            Columns = new List<ColumnConfiguration<T>>();
            this.MergeWithEntities = new List<Type>();
            SetDefaultExpressions();
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
            if (Expressions == null)
                Expressions = new List<RegularExpression>();

            if (Validations == null)
                Validations = new List<ModelValidation>();

            foreach (var item in Expressions)
            {
                this.AddExpression(item.Name, item.Expression, item.Message);
            }
            foreach (var item in Validations)
            {
                this.SetValidation(item.Name, item.IsRequired, item.Message, item.RegExpression);
            }
        }

        public virtual void SetDefaultExpressions()
        {
            var arg = new StartAddDefaultExpressionsEventArgs(this);

            OnStartAddDefaultExpressions(arg);
            if (arg.Cancel) return;
            this.AddExpression(NoDateExpression, @"^(((0?[1-9]|[12]\d|3[01])[\.\-\/](0?[13578]|1[02])[\.\-\/]((1[6-9]|[2-9]\d)?\d{2}))|((0?[1-9]|[12]\d|30)[\.\-\/](0?[13456789]|1[012])[\.\-\/]((1[6-9]|[2-9]\d)?\d{2}))|((0?[1-9]|1\d|2[0-8])[\.\-\/]0?2[\.\-\/]((1[6-9]|[2-9]\d)?\d{2}))|(29[\.\-\/]0?2[\.\-\/]((1[6-9]|[2-9]\d)?(0[48]|[2468][048]|[13579][26])|((16|[2468][048]|[3579][2])00)|00)))$", "Geef een geldige datum in.");
            this.AddExpression(NotNullExpression, "^.+$", "Value may not be empty");
            this.AddExpression(NotZeroExpression, "^[0-9]*[1-9]+$|^[1-9]+[0-9]*$", "Select a value");
        }

        public void SetModelConfiguration(IModelBase model)
        {

            if (Validations == null)
                Validations = new List<ModelValidation>();

            if (model == null)
            {
                throw new Exception("model is SetModelConfiguration Is NUll");
            }
            this.Model = model;

            this.Model.PropertyChanged -= this.Model_PropertyChanged;

            foreach (var validation in Validations)
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
            if (Validations == null)
                Validations = new List<ModelValidation>();


            var validation = Validations.FirstOrDefault(m => m.Name == propertyName);
            if (validation == null) return null;
            var value = this.GetValue(propertyName);

            var arg = new ModelValidateEventArgs(validation, value);
            OnModelPropertyValidate(arg);

            if (arg.CancelDefaultValidation == false)
            {

                if (validation.IsRequired)
                {
                    if (value == null)
                    {
                        arg.Valid = false;
                    }
                    else
                    {

                        if (!string.IsNullOrEmpty(validation.RegExpression))
                        {
                            if (Regex.IsMatch(value.ToStringValue(), validation.RegExpression))
                            {
                                arg.Valid = true;
                            }
                            else
                            {
                                arg.Valid = false;
                            }
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(value.ToStringValue()))
                            {
                                arg.Valid = false;
                            }
                            else
                            {
                                arg.Valid = true;
                            }
                        }
                    }
                }
            }
            OnAfterModelPropertyValidate(arg);

            if (arg.Valid)
                this.RemoveError(validation);
            else
            {
                this.AddError(validation);
            }
            return validation;
        }

        private void AddError(ModelValidation validation)
        {
            if (this.Model == null) return;
            if (this.Model.Errors.ContainsKey(validation.Name) == false)
                this.Model.Errors.Add(validation.Name, validation.Message);
        }

        private void RemoveError(ModelValidation validation)
        {
            if (this.Model == null) return;
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

            if (Validations == null)
                Validations = new List<ModelValidation>();

            var validation = Validations.FirstOrDefault(m => m.Name == name);
            if (validation == null)
            {
                validation = new ModelValidation() { Name = name, IsRequired = isRequired, Message = message };

                var expression = Expressions.FirstOrDefault(m => m.Name == regexName);
                if (expression != null)
                {
                    validation.RegExpression = expression.Expression;
                    if (string.IsNullOrEmpty(validation.Message))
                    {
                        validation.Message = expression.Message;
                    }
                }
                Validations.Add(validation);
                AddError(validation);
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

        public ModelConfiguration<T> SetRequiredString<TField>(Expression<Func<T, TField>> field, string message)
        {
            this.SetRequired(field, message, NotNullExpression);
            return this;
        }

        public ModelConfiguration<T> SetRequiredDate<TField>(Expression<Func<T, TField>> field, string message)
        {
            this.SetRequired(field, message, NoDateExpression);
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

        protected virtual void OnModelPropertyValidate(ModelValidateEventArgs args)
        {
            ModelPropertyValidate?.Invoke(this, args);
        }

        protected virtual void OnAfterModelPropertyValidate(ModelValidateEventArgs args)
        {
            AfterModelPropertyValidate?.Invoke(this, args);
        }

        public void Validate()
        {
            var items = Model.GetProperties();
        }
    }

    public delegate void AfterModelPropertyValidateEvent(object sender, ModelValidateEventArgs args);
    public delegate void ModelPropertyValidateEvent(object sender, ModelValidateEventArgs args);

    public class ModelValidateEventArgs
    {
        public ModelValidation Validation { get; }
        public object Value { get; }
        public bool CancelDefaultValidation { get; set; }

        public bool Valid { get; set; }


        public ModelValidateEventArgs(ModelValidation validation, object value)
        {
            Validation = validation;
            Value = value;
        }
    }
}