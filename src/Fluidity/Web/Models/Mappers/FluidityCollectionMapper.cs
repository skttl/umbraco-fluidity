﻿// <copyright file="FluidityCollectionMapper.cs" company="Matt Brailsford">
// Copyright (c) 2017 Matt Brailsford and contributors.
// Licensed under the Apache License, Version 2.0.
// </copyright>

using System.Linq;
using Fluidity.Configuration;
using Umbraco.Core;
using System.ComponentModel;
using System.Reflection;

namespace Fluidity.Web.Models.Mappers
{
    internal class FluidityCollectionMapper
    {
        public FluidityCollectionDisplayModel ToDisplayModel(FluiditySectionConfig section, FluidityCollectionConfig collection, bool includeListView)
        {
            var m = new FluidityCollectionDisplayModel
            {
                Section = section.Alias,
                Tree = section.Tree.Alias,
                Alias = collection.Alias,
                NameSingular = collection.NameSingular + (!collection.IconColor.IsNullOrWhiteSpace() ? " color-" + collection.IconColor : ""),
                NamePlural = collection.NamePlural + (!collection.IconColor.IsNullOrWhiteSpace() ? " color-" + collection.IconColor : ""),
                IconSingular = collection.IconSingular,
                IconPlural = collection.IconPlural,
                Description = collection.Description,
                IsReadOnly = collection.IsReadOnly,
                IsSearchable = collection.SearchableProperties.Any(),
                HasListView = collection.ViewMode == FluidityViewMode.List,
                Path = collection.Path
            };

            if (includeListView)
            {
                m.ListView = new FluidityListViewDisplayModel
                {
                    PageSize = collection.ListView.PageSize,
                    Properties = collection.ListView.Fields.Select(x =>
                    {
                        // Calculate heading
                        var heading = x.Heading;
                        if (heading.IsNullOrWhiteSpace())
                        {
                            var attr = x.Property.PropertyInfo.GetCustomAttribute<DisplayNameAttribute>(true);
                            if (attr != null)
                            {
                                heading = attr.DisplayName;
                            }
                            else
                            {
                                heading = x.Property.Name.SplitPascalCasing();
                            }
                        }

                        // Build property
                        return new FluidityListViewPropertyDisplayModel // We don't include Name, as it's always automatically included
                        {
                            Alias = x.Property.Name,
                            Header = heading,
                            AllowSorting = true,
                            IsSystem = false
                        };
                    }),
                    Layouts = collection.ListView.Layouts.Select((x, idx) => new FluidityListViewLayoutDisplayModel
                    {
                        Icon = x.Icon,
                        Name = x.Name,
                        Path = x.View,
                        IsSystem = x.IsSystem,
                        Selected = true
                    }),
                    DataViews = collection.ListView.DataViewBuilder.GetDataViews(),
                    BulkActions = collection.ListView.BulkActions.Select(x => new FluidityListViewBulkActionDisplayModel
                    {
                        Icon = x.Icon,
                        Alias = x.Alias,
                        Name = x.Name,
                        AngularServiceName = x.AngularServiceName
                    })
                };
            }

            return m;
        }
    }
}
