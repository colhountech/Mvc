// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    /// <summary>
    /// <see cref="IModelBinder"/> implementation to bind models of type <see cref="System.Enum"/>.
    /// </summary>
    public class EnumTypeModelBinder : IModelBinder
    {
        /// <inheritdoc />
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult == ValueProviderResult.None)
            {
                // no entry
                bindingContext.Result = ModelBindingResult.Failed();
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);
            var value = valueProviderResult.FirstValue;
            var typeConverter = TypeDescriptor.GetConverter(bindingContext.ModelMetadata.ModelType);

            try
            {
                // Use the built-in type converter for Enum to convert the value from string to enum
                var model = typeConverter.ConvertFrom(
                    context: null,
                    culture: valueProviderResult.Culture,
                    value: value);

                // Check if the converted value is indeed defined on the enum as EnumConverter converts value to the backing type (ex: integer)
                // and does not check if the value is defined on the enum.
                if (model != null)
                {
                    var isFlagsEnum = bindingContext.ModelType.IsDefined(typeof(FlagsAttribute), inherit: false);
                    if (isFlagsEnum)
                    {
                        // convert to the backing type
                        var suppliedValue = Convert.ToInt64(model);

                        // Example:
                        // [Flags]
                        // enum CarOptions
                        // {
                        //      SunRoof = 0x01,
                        //      Spoiler = 0x02,
                        // }
                        //
                        // For the above flags enum, a value like "4" is not valid as the allowed values are 1, 2 and 3,
                        // so check if the supplied value indeed falls within the allowed range

                        // Get the max value of the flags enum
                        //TODO: instead of calculating this everytime for an enum type, cache the result?
                        long maxValue = 0;
                        foreach (var enumValue in Enum.GetValues(bindingContext.ModelType))
                        {
                            maxValue = maxValue | Convert.ToInt64(enumValue);
                        }

                        // Note that just doing an "AND" operation between the value is not sufficient, for example
                        // a supplied value like "4" and a max value of 3, an operation like 4 & 3 sets valid bits but also
                        // has bits which are out of range.

                        // The supplied value should fall within the valid range
                        if (suppliedValue == 0 || suppliedValue > maxValue)
                        {
                            model = null;
                        }
                    }
                    else
                    {
                        if (!Enum.IsDefined(bindingContext.ModelType, model))
                        {
                            model = null;
                        }
                    }
                }

                // When converting newModel a null value may indicate a failed conversion for an otherwise required
                // model (can't set a ValueType to null). This detects if a null model value is acceptable given the
                // current bindingContext. If not, an error is logged.
                if (model == null && !bindingContext.ModelMetadata.IsReferenceOrNullableType)
                {
                    bindingContext.ModelState.TryAddModelError(
                        bindingContext.ModelName,
                        bindingContext.ModelMetadata.ModelBindingMessageProvider.ValueMustNotBeNullAccessor(
                            valueProviderResult.ToString()));

                    return Task.CompletedTask;
                }
                else
                {
                    bindingContext.Result = ModelBindingResult.Success(model);
                    return Task.CompletedTask;
                }
            }
            catch (Exception exception)
            {
                var isFormatException = exception is FormatException;
                if (!isFormatException && exception.InnerException != null)
                {
                    // TypeConverter throws System.Exception wrapping the FormatException,
                    // so we capture the inner exception.
                    exception = ExceptionDispatchInfo.Capture(exception.InnerException).SourceException;
                }

                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName,
                    exception,
                    bindingContext.ModelMetadata);

                // Were able to find a converter for the type but conversion failed.
                return Task.CompletedTask;
            }
        }
    }
}
