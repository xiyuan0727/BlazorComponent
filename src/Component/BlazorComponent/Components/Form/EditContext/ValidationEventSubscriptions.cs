﻿using FluentValidation;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using Util.Reflection.Expressions;
using FluentValidationResult = FluentValidation.Results.ValidationResult;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace BlazorComponent;

internal sealed class ValidationEventSubscriptions : IDisposable
{
    private static readonly ConcurrentDictionary<Type, IValidator> ModelFluentValidatorMap = new();
    private static readonly ConcurrentDictionary<Type, Func<object, Dictionary<string, object>>> ModelPropertiesMap = new();
    private static readonly Dictionary<Type, Type> FluentValidationTypeMap = new();

    static ValidationEventSubscriptions()
    {
        try
        {
            var referenceAssembles = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var referenceAssembly in referenceAssembles)
            {
                if (referenceAssembly.FullName.StartsWith("Microsoft.") || referenceAssembly.FullName.StartsWith("System."))
                    continue;

                var types = referenceAssembly
                            .GetTypes()
                            .Where(t => t.IsClass)
                            .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
                            .Where(t => typeof(IValidator).IsAssignableFrom(t))
                            .ToArray();

                foreach (var type in types)
                {
                    var modelType = type.BaseType.GenericTypeArguments[0];
                    var validatorType = typeof(IValidator<>).MakeGenericType(modelType);
                    FluentValidationTypeMap.Add(modelType, validatorType);
                }
            }
        }
        catch
        {
            // ignored
        }
    }

    private readonly EditContext _editContext;
    private readonly ValidationMessageStore _messageStore;
    private readonly IServiceProvider _serviceProvider;
    private I18n.I18n? _i18n;

    [MemberNotNullWhen(true, nameof(_i18n))]
    private bool EnableI18n { get; set; }

    public ValidationEventSubscriptions(EditContext editContext, ValidationMessageStore messageStore, IServiceProvider serviceProvider, bool enableI18n)
    {
        _serviceProvider = serviceProvider;
        _editContext = editContext ?? throw new ArgumentNullException(nameof(editContext));
        _messageStore = messageStore;

        _editContext.OnFieldChanged += OnFieldChanged;
        _editContext.OnValidationRequested += OnValidationRequested;
        EnableI18n = enableI18n;
        if (EnableI18n)
        {
            _i18n = _serviceProvider.GetService<I18n.I18n>();
        }
    }

    private void OnFieldChanged(object? sender, FieldChangedEventArgs eventArgs)
    {
        Validate(eventArgs.FieldIdentifier);
    }

    private void OnValidationRequested(object? sender, ValidationRequestedEventArgs e)
    {
        Validate(new FieldIdentifier(new(), ""));
    }

    private void Validate(FieldIdentifier field)
    {
        if (FluentValidationTypeMap.ContainsKey(_editContext.Model.GetType()))
        {
            FluentValidate(_editContext.Model, _messageStore, field);
        }
        else
        {
            DataAnnotationsValidate(_editContext.Model, _messageStore, field);
        }

        _editContext.NotifyValidationStateChanged();
    }

    private void DataAnnotationsValidate(object model, ValidationMessageStore messageStore, FieldIdentifier field)
    {
        var validationResults = new List<ValidationResult>();
        if (field.FieldName == "")
        {
            var validationContext = new ValidationContext(model);
            Validator.TryValidateObject(model, validationContext, validationResults, true);
            messageStore.Clear();

            foreach (var validationResult in validationResults)
            {
                if (validationResult is EnumerableValidationResult enumerableValidationResult)
                {
                    foreach (var descriptor in enumerableValidationResult.Descriptors)
                    {
                        foreach (var result in descriptor.Results)
                        {
                            foreach (var memberName in result.MemberNames)
                            {
                                AddValidationMessage(new FieldIdentifier(descriptor.ObjectInstance, memberName), result.ErrorMessage!);
                            }
                        }
                    }
                }
                else
                {
                    foreach (var memberName in validationResult.MemberNames)
                    {
                        AddValidationMessage(new FieldIdentifier(model, memberName), validationResult.ErrorMessage);
                    }
                }
            }
        }
        else
        {
            var validationContext = new ValidationContext(field.Model);
            Validator.TryValidateObject(field.Model, validationContext, validationResults, true);
            messageStore.Clear(field);
            foreach (var validationResult in validationResults)
            {
                if (validationResult.MemberNames.Contains(field.FieldName))
                {
                    AddValidationMessage(field, validationResult.ErrorMessage);
                    return;
                }
            }
        }
    }

    private void FluentValidate(object model, ValidationMessageStore messageStore, FieldIdentifier field)
    {
        var validationResult = GetValidationResult(model);
        if (field.FieldName == "")
        {
            messageStore.Clear();
            var propertyMap = GetPropertyMap(model);
            foreach (var error in validationResult.Errors)
            {
                if (error.PropertyName.Contains("."))
                {
                    var propertyName = error.PropertyName.Substring(0, error.PropertyName.IndexOf('.'));
                    if (propertyMap.ContainsKey(propertyName))
                    {
                        var modelItem = propertyMap[propertyName];
                        var modelItemPropertyName = error.FormattedMessagePlaceholderValues["PropertyName"].ToString().Replace(" ", "");
                        AddValidationMessage(new FieldIdentifier(modelItem, modelItemPropertyName), error.ErrorMessage);
                    }
                }
                else
                {
                    AddValidationMessage(new FieldIdentifier(model, error.PropertyName), error.ErrorMessage);
                }
            }
        }
        else
        {
            messageStore.Clear(field);
            if (field.Model == model)
            {
                var error = validationResult.Errors.FirstOrDefault(e => e.PropertyName == field.FieldName);
                if (error is not null)
                {
                    AddValidationMessage(field, error.ErrorMessage);
                }
            }
            else
            {
                var propertyMap = GetPropertyMap(model);
                var key = propertyMap.FirstOrDefault(pm => pm.Value == field.Model).Key;
                var errorMessage = validationResult.Errors.FirstOrDefault(e => e.PropertyName == ($"{key}.{field.FieldName}"))?.ErrorMessage;
                if (errorMessage is not null)
                {
                    AddValidationMessage(field, errorMessage);
                }
            }
        }
    }

    private FluentValidationResult GetValidationResult(object model)
    {
        var type = model.GetType();
        var validationContext = new ValidationContext<object>(model);

        if (ModelFluentValidatorMap.TryGetValue(type, out var validator))
        {
            return validator.Validate(validationContext);
        }

        var genericType = typeof(IValidator<>).MakeGenericType(type);
        validator = (IValidator)_serviceProvider.GetService(genericType);

        if (validator is not null)
        {
            ModelFluentValidatorMap[type] = validator;
            return validator.Validate(validationContext);
        }

        throw new NotImplementedException($"Validator for {type} does not exists.");
    }

    private Dictionary<string, object> GetPropertyMap(object model)
    {
        var type = model.GetType();
        if (ModelPropertiesMap.TryGetValue(type, out var func) is false)
        {
            var modelParameter = Expr.BlockParam<object>().Convert(type);
            Var map = Expr.New<Dictionary<string, object>>();
            var properties = type.GetProperties();
            foreach (var property in properties)
            {
                if (property.PropertyType.IsValueType || property.PropertyType == typeof(string))
                    continue;
                else
                {
                    if (property.PropertyType.GetInterfaces().Any(gt => gt == typeof(System.Collections.IEnumerable)))
                    {
                        Var index = -1;
                        Expr.Foreach(modelParameter[property.Name], (item, @continue, @return) =>
                        {
                            index++;
                            map[property.Name + "[" + index + "]"] = item.Convert<object>();
                        });
                    }
                    else
                    {
                        map[$"[{property.Name}]"] = modelParameter[property.Name].Convert<object>();
                    }
                }
            }

            func = map.BuildDelegate<Func<object, Dictionary<string, object>>>();
            ModelPropertiesMap[type] = func;
        }

        return func(model);
    }

    private void AddValidationMessage(in FieldIdentifier fieldIdentifier, string message)
    {
        if (EnableI18n)
        {
            message = _i18n.T(message, true);
        }

        _messageStore.Add(fieldIdentifier, message);
    }

    public void Dispose()
    {
        _messageStore.Clear();
        _editContext.OnFieldChanged -= OnFieldChanged;
        _editContext.OnValidationRequested -= OnValidationRequested;
        _editContext.NotifyValidationStateChanged();
    }
}
