// <copyright file="UmbracoPublishedPropertyTypeHelper.cs" company="Matt Brailsford">
// Copyright (c) 2019 Matt Brailsford and contributors.
// Licensed under the Apache License, Version 2.0.
// </copyright>

using System.Collections.Generic;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web.Composing;

namespace Fluidity.Helpers
{
    internal static class UmbracoPublishedPropertyTypeHelper
    {
        public static IDictionary<string, PreValue> GetPreValues(this PublishedPropertyType propType)
        {
            //(IDictionary<string, PreValue>)
            return Current.AppCaches.RequestCache.Get($"UmbracoPublishedPropertyTypeHelper.GetPreValues_{propType.DataType.Id}", () => 
                Current.Services.DataTypeService.GetDataType(propType.DataType.Id).PreValuesAsDictionary);
        } 
    }
}
