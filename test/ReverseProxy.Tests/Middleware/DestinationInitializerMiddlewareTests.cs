// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.ReverseProxy.RuntimeModel;
using Microsoft.ReverseProxy.Service.Management;
using Microsoft.ReverseProxy.Service.Proxy.Infrastructure;
using Moq;
using Tests.Common;
using Xunit;

namespace Microsoft.ReverseProxy.Middleware.Tests
{
    public class DestinationInitializerMiddlewareTests : TestAutoMockBase
    {
        public DestinationInitializerMiddlewareTests()
        {
            Provide<RequestDelegate>(context => Task.CompletedTask);
        }

        [Fact]
        public void Constructor_Works()
        {
            Create<DestinationInitializerMiddleware>();
        }
        
        [Fact]
        public async Task Invoke_SetsFeatures()
        {
            var proxyHttpClientFactoryMock = new Mock<IProxyHttpClientFactory>();
            var backend1 = new BackendInfo(
                backendId: "backend1",
                destinationManager: new DestinationManager(),
                proxyHttpClientFactory: proxyHttpClientFactoryMock.Object);
            var destination1 = backend1.DestinationManager.GetOrCreateItem(
                "destination1",
                destination =>
                {
                    destination.Config.Value = new DestinationConfig("https://localhost:123/a/b/");
                    destination.DynamicState.Value = new DestinationDynamicState(DestinationHealth.Healthy);
                });

            var aspNetCoreEndpoints = new List<Endpoint>();
            var routeConfig = new RouteConfig(
                new RouteInfo("route1"),
                configHash: 0,
                priority: null,
                backend1,
                aspNetCoreEndpoints.AsReadOnly(),
                transforms: null);
            var aspNetCoreEndpoint = CreateAspNetCoreEndpoint(routeConfig);
            aspNetCoreEndpoints.Add(aspNetCoreEndpoint);
            var httpContext = new DefaultHttpContext();
            httpContext.SetEndpoint(aspNetCoreEndpoint);

            var sut = Create<DestinationInitializerMiddleware>();

            await sut.Invoke(httpContext);

            var feature = httpContext.Features.Get<IAvailableDestinationsFeature>();
            Assert.NotNull(feature);
            Assert.NotNull(feature.Destinations);
            Assert.Equal(1, feature.Destinations.Count);
            Assert.Same(destination1, feature.Destinations[0]);

            var backend = httpContext.Features.Get<BackendInfo>();
            Assert.Same(backend1, backend);

            Assert.Equal(200, httpContext.Response.StatusCode);
        }

        [Fact]
        public async Task Invoke_NoHealthyEndpoints_503()
        {
            var proxyHttpClientFactoryMock = new Mock<IProxyHttpClientFactory>();
            var backend1 = new BackendInfo(
                backendId: "backend1",
                destinationManager: new DestinationManager(),
                proxyHttpClientFactory: proxyHttpClientFactoryMock.Object);
            backend1.Config.Value = new BackendConfig(
                new BackendConfig.BackendHealthCheckOptions(enabled: true, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan, 0, ""),
                new BackendConfig.BackendLoadBalancingOptions(),
                new BackendConfig.BackendSessionAffinityOptions());
            var destination1 = backend1.DestinationManager.GetOrCreateItem(
                "destination1",
                destination =>
                {
                    destination.Config.Value = new DestinationConfig("https://localhost:123/a/b/");
                    destination.DynamicState.Value = new DestinationDynamicState(DestinationHealth.Unhealthy);
                });

            var aspNetCoreEndpoints = new List<Endpoint>();
            var routeConfig = new RouteConfig(
                route: new RouteInfo("route1"),
                configHash: 0,
                priority: null,
                backendOrNull: backend1,
                aspNetCoreEndpoints: aspNetCoreEndpoints.AsReadOnly(),
                transforms: null);
            var aspNetCoreEndpoint = CreateAspNetCoreEndpoint(routeConfig);
            aspNetCoreEndpoints.Add(aspNetCoreEndpoint);
            var httpContext = new DefaultHttpContext();
            httpContext.SetEndpoint(aspNetCoreEndpoint);

            var sut = Create<DestinationInitializerMiddleware>();

            await sut.Invoke(httpContext);

            var feature = httpContext.Features.Get<IAvailableDestinationsFeature>();
            Assert.Null(feature);

            var backend = httpContext.Features.Get<BackendInfo>();
            Assert.Null(backend);

            Assert.Equal(503, httpContext.Response.StatusCode);
        }

        private static Endpoint CreateAspNetCoreEndpoint(RouteConfig routeConfig)
        {
            var endpointBuilder = new RouteEndpointBuilder(
                requestDelegate: httpContext => Task.CompletedTask,
                routePattern: RoutePatternFactory.Parse("/"),
                order: 0);
            endpointBuilder.Metadata.Add(routeConfig);
            return endpointBuilder.Build();
        }
    }
}
