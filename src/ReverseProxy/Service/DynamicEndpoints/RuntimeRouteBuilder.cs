// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.ReverseProxy.ConfigModel;
using Microsoft.ReverseProxy.RuntimeModel;
using Microsoft.ReverseProxy.Service.Config;
using Microsoft.ReverseProxy.Utilities;

namespace Microsoft.ReverseProxy.Service
{
    /// <summary>
    /// Default implementation of the <see cref="IRuntimeRouteBuilder"/> interface.
    /// </summary>
    internal class RuntimeRouteBuilder : IRuntimeRouteBuilder
    {
        private readonly ITransformBuilder _transformBuilder;
        private RequestDelegate _pipeline;

        public RuntimeRouteBuilder(ITransformBuilder transformBuilder)
        {
            _transformBuilder = transformBuilder ?? throw new ArgumentNullException(nameof(transformBuilder));
        }

        public void SetProxyPipeline(RequestDelegate pipeline)
        {
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        }

        /// <inheritdoc/>
        public RouteConfig Build(ParsedRoute source, BackendInfo backendOrNull, RouteInfo runtimeRoute)
        {
            Contracts.CheckValue(source, nameof(source));
            Contracts.CheckValue(runtimeRoute, nameof(runtimeRoute));

            _transformBuilder.Build(source.Transforms, out var transforms); // TODO: HeaderTransforms, etc...

            // NOTE: `new RouteConfig(...)` needs a reference to the list of ASP .NET Core endpoints,
            // but the ASP .NET Core endpoints cannot be created without a `RouteConfig` metadata item.
            // We solve this chicken-egg problem by creating an (empty) list first
            // and passing a read-only wrapper of it to `RouteConfig.ctor`.
            // Recall that `List<T>.AsReadOnly()` creates a wrapper over the original list,
            // and changes to the underlying list *are* reflected on the read-only view.
            var aspNetCoreEndpoints = new List<Endpoint>(1);
            var newRouteConfig = new RouteConfig(
                runtimeRoute,
                source.GetConfigHash(),
                source.Priority,
                backendOrNull,
                aspNetCoreEndpoints.AsReadOnly(),
                transforms);

            // TODO: Handle arbitrary AST's properly
            // Catch-all pattern when no path was specified
            var pathPattern = string.IsNullOrEmpty(source.Path) ? "/{**catchall}" : source.Path;

            // TODO: Propagate route priority
            var endpointBuilder = new AspNetCore.Routing.RouteEndpointBuilder(
                requestDelegate: _pipeline ?? Invoke,
                routePattern: AspNetCore.Routing.Patterns.RoutePatternFactory.Parse(pathPattern),
                order: 0);
            endpointBuilder.DisplayName = source.RouteId;
            endpointBuilder.Metadata.Add(newRouteConfig);

            if (!string.IsNullOrEmpty(source.Host))
            {
                endpointBuilder.Metadata.Add(new AspNetCore.Routing.HostAttribute(source.Host));
            }

            if (source.Methods != null && source.Methods.Count > 0)
            {
                endpointBuilder.Metadata.Add(new AspNetCore.Routing.HttpMethodMetadata(source.Methods));
            }

            var endpoint = endpointBuilder.Build();
            aspNetCoreEndpoints.Add(endpoint);

            return newRouteConfig;
        }

        // This indirection is needed because on startup the routes are loaded from config and built before the
        // proxy pipeline gets built.
        private Task Invoke(HttpContext context)
        {
            var pipeline = _pipeline ?? throw new InvalidOperationException("The pipeline hasn't been provided yet.");
            return pipeline(context);
        }
    }
}
