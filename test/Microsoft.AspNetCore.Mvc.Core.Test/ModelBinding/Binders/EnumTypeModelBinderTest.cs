// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public class EnumTypeModelBinderTest
    {
        [Fact]
        public async Task BindModel_BindsEnumModels_IfArrayElementIsStringKey()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(IntEnum));
            bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", new object[] { "Value1" } }
            };

            var binder = new EnumTypeModelBinder();

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);
            var boundModel = Assert.IsType<IntEnum>(bindingContext.Result.Model);
            Assert.Equal(IntEnum.Value1, boundModel);
        }

        [Fact]
        public async Task BindModel_BindsEnumModels_IfArrayElementIsStringValue()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(IntEnum));
            bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", new object[] { "1" } }
            };

            var binder = new EnumTypeModelBinder();

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);
            var boundModel = Assert.IsType<IntEnum>(bindingContext.Result.Model);
            Assert.Equal(IntEnum.Value1, boundModel);
        }

        [Fact]
        public async Task BindModel_BindsIntEnumModels()
        {
            // Arrange
            var modelType = typeof(IntEnum);
            var bindingContext = GetBindingContext(modelType);
            bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", "2" }
            };

            var binder = new EnumTypeModelBinder();

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);
            Assert.IsType(modelType, bindingContext.Result.Model);
            Assert.True(Enum.IsDefined(modelType, bindingContext.Result.Model));
        }

        [Theory]
        [InlineData("1")]
        [InlineData("8, 1")]
        [InlineData("Value2, Value8")]
        [InlineData("value8,value4,value2,value1")]
        public async Task BindModel_BindsFlagsEnumModels(string flagsEnumValue)
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(FlagsEnum));
            bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", flagsEnumValue }
            };

            var binder = new EnumTypeModelBinder();

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);
            var boundModel = Assert.IsType<FlagsEnum>(bindingContext.Result.Model);
            var boundModelAsInt = Convert.ToInt64(boundModel);
            var maxValueOfEnum = GetMaxValueOfEnum(typeof(FlagsEnum));
            Assert.True(boundModelAsInt != 0 && boundModelAsInt <= maxValueOfEnum);
        }

        [Theory]
        [InlineData(typeof(IntEnum), "3")]
        [InlineData(typeof(FlagsEnum), "19")]
        [InlineData(typeof(FlagsEnum), "0")]
        [InlineData(typeof(FlagsEnum), "Value1, Value10")]
        [InlineData(typeof(FlagsEnum), "value10, value1")]
        [InlineData(typeof(FlagsEnum), "Value10")]
        [InlineData(typeof(FlagsEnum), "1, 16")]
        // These two values look like big integers but are treated as two separate enum values that are
        // or'd together.
        [InlineData(typeof(FlagsEnum), "32,015")]
        [InlineData(typeof(FlagsEnum), "32,128")]
        public async Task BindModel_AddsErrorToModelState_ForInvalidEnumValues(Type modelType, string suppliedValue)
        {
            // Arrange
            var message = $"The value '{suppliedValue}' is not valid.";
            var bindingContext = GetBindingContext(modelType);
            bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", suppliedValue }
            };

            var binder = new EnumTypeModelBinder();

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(bindingContext.Result.IsModelSet);
            Assert.Null(bindingContext.Result.Model);
            Assert.False(bindingContext.ModelState.IsValid);
            var error = Assert.Single(bindingContext.ModelState["theModelName"].Errors);
            Assert.Equal(message, error.ErrorMessage);
        }

        private static DefaultModelBindingContext GetBindingContext(Type modelType)
        {
            return new DefaultModelBindingContext
            {
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(modelType),
                ModelName = "theModelName",
                ModelState = new ModelStateDictionary(),
                ValueProvider = new SimpleValueProvider() // empty
            };
        }

        private long GetMaxValueOfEnum(Type enumType)
        {
            long maxValue = 0;
            foreach (var enumValue in Enum.GetValues(enumType))
            {
                maxValue = maxValue | Convert.ToInt64(enumValue);
            }
            return maxValue;
        }

        private sealed class TestClass
        {
        }

        [Flags]
        private enum FlagsEnum
        {
            Value1 = 1,
            Value2 = 2,
            Value4 = 4,
            Value8 = 8,
        }

        private enum IntEnum
        {
            Value0 = 0,
            Value1 = 1,
            Value2 = 2,
            MaxValue = int.MaxValue
        }
    }
}
