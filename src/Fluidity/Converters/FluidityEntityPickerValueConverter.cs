// <copyright file="FluidityEntityPickerValueConverter.cs" company="Matt Brailsford">
// Copyright (c) 2019 Matt Brailsford and contributors.
// Licensed under the Apache License, Version 2.0.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Fluidity.Configuration;
using Fluidity.Helpers;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.PropertyEditors;

namespace Fluidity.Converters
{
    public class FluidityEntityPickerConfiguration
    {
        [ConfigurationField("collection", "~/app_plugins/fluidity/prevalues/collectionpicker.html")]
        public string Collection { get; set; }

        [ConfigurationField("minItems", "number")]
        public int MinItems { get; set; }

        [ConfigurationField("maxItems", "number")]
        public int MaxItems { get; set; }

        [ConfigurationField("showOpen", "boolean")]
        public int ShowOpen { get; set; }

        [ConfigurationField("showRemove", "boolean")]
        public int ShowRemove { get; set; }

    }

    //[PropertyValueCache(PropertyCacheValue.All, PropertyCacheLevel.Content)]
    //[PropertyValueType(typeof(IEnumerable<object>))]
    public class FluidityEntityPickerValueConverter : PropertyValueConverterBase
    {
        private readonly ILogger _logger;

        public FluidityEntityPickerValueConverter(ILogger logger)
        {
            _logger = logger;
        }
        public override bool IsConverter(IPublishedPropertyType propertyType)
        {
            return propertyType.EditorAlias.InvariantEquals("Fluidity.EntityPicker");
        }
        public override object ConvertSourceToIntermediate(IPublishedElement owner, IPublishedPropertyType propertyType, object source, bool preview)
        {
            try
            {
                if (source == null || source.ToString().IsNullOrWhiteSpace())
                    return null;

                var ids = source.ToString().Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                if (ids.Length == 0)
                    return null;

                var preValues = propertyType.DataType.ConfigurationAs<FluidityEntityPickerConfiguration>();;
                if (preValues == null || !preValues.Collection.IsNullOrWhiteSpace())
                    throw new ApplicationException($"Fluidity DataType {propertyType.DataType.Id} has no 'collection' pre value.");

                var collectionParts = preValues.Collection.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                if (collectionParts.Length < 2)
                    throw new ApplicationException($"Fluidity DataType {propertyType.DataType.Id} has an invalid 'collection' pre value.");

                var section = FluidityContext.Current.Config.Sections[collectionParts[0]];
                if (section == null)
                    throw new ApplicationException($"Fluidity DataType {propertyType.DataType.Id} has an invalid 'collection' pre value. No section found with the alias {collectionParts[0]}");

                var collection = section.Tree.FlattenedTreeItems[collectionParts[1]] as FluidityCollectionConfig;
                if (collection == null)
                    throw new ApplicationException($"Fluidity DataType {propertyType.DataType.Id} has an invalid 'collection' pre value. No collection found with the alias {collectionParts[1]}");

                return FluidityContext.Current.Services.EntityService.GetEntitiesByIds(section, collection, ids);

            }
            catch (Exception e)
            {
                _logger.Error<FluidityEntityPickerValueConverter>("Error converting value", e);
            }

            return null;
        }
    }
}
