// <copyright file="FluidityBootManager.cs" company="Matt Brailsford">
// Copyright (c) 2019 Matt Brailsford and contributors.
// Licensed under the Apache License, Version 2.0.
// </copyright>

using System;
using Fluidity.Configuration;
using Fluidity.Data;
using Fluidity.Events;
using Fluidity.Helpers;
using Fluidity.Services;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;
using Umbraco.Web;

namespace Fluidity
{
    internal class FluidityBootManager : IComponent
    {
        private readonly ILogger _logger;
        public static event EventHandler<FluidityStartingEventArgs> FluidityStarting;

        public FluidityBootManager(ILogger logger)
        {
            _logger = logger;
        }

        public void Initialize()
        {
            // Build a configuration
            var config = new FluidityConfig();
            var args = new FluidityStartingEventArgs { Config = config };

            OnFluidityStarting(this, args);

            config = args.Config;
            config.PostProcess();

            // Configure the fluidity context
            FluidityContext.EnsureContext(config,
                new FluidityDataContext(
                    new FluidityRepositoryFactory()
                ),
                new FluidityServiceContext(
                    new FluidityEntityService()
                )
            );

            // Create the sections / trees
            new UmbracoUiHelper().Build(FluidityContext.Current);

            //TODO: Cleanup any orphan sections / trees that may be left over from renames
        }

        public void Terminate()
        {
        }

        protected virtual void OnFluidityStarting(object sender, FluidityStartingEventArgs e)
        {
            if (FluidityStarting != null)
            {
                try
                {
                    FluidityStarting(sender, e);
                }
                catch (Exception ex)
                {
                    _logger.Error<UmbracoApplicationBase>("An error occurred in an FluidityStarting event handler", ex);
                    throw;
                }
            }
        }
    }
}
