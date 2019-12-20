// <copyright file="FluidityConfigModule.cs" company="Matt Brailsford">
// Copyright (c) 2019 Matt Brailsford and contributors.
// Licensed under the Apache License, Version 2.0.
// </copyright>

using Umbraco.Core.Composing;

namespace Fluidity.Configuration
{
    public abstract class FluidityConfigModule : IComponent
    {
        /// <summary>
        /// The entry point for a Fluidity configuration.
        /// </summary>
        /// <param name="config">The base Fluidity configuration.</param>
        public abstract void Configure(FluidityConfig config);

        public void Initialize()
        {
            FluidityBootManager.FluidityStarting += (sender, args) => {
                Configure(args.Config);
            };
        }

        public void Terminate()
        {
        }
    }
}
