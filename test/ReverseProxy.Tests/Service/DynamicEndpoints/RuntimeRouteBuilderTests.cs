// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.ReverseProxy.ConfigModel;
using Microsoft.ReverseProxy.RuntimeModel;
using Microsoft.ReverseProxy.Service.Management;
using Microsoft.ReverseProxy.Service.Proxy.Infrastructure;
using Moq;
using Tests.Common;
using Xunit;

namespace Microsoft.ReverseProxy.Service.Tests
{
    public class RuntimeRouteBuilderTests : TestAutoMockBase
    {
        [Fact]
        public void Constructor_Works()
        {
            Create<RuntimeRouteBuilder>();
        }

        [Fact]
        public void BuildEndpoints_HostAndPath_Works()
        {
            // Arrange
            var builder = Create<RuntimeRouteBuilder>();
            var parsedRoute = new ParsedRoute
            {
                RouteId = "route1",
                Host = "example.com",
                Path = "/a",
                Priority = 12,
            };
            var backend = new BackendInfo("backend1", new DestinationManager(), new Mock<IProxyHttpClientFactory>().Object);
            var routeInfo = new RouteInfo("route1");

            // Act
            var config = builder.Build(parsedRoute, backend, routeInfo);

            // Assert
            Assert.Same(backend, config.BackendOrNull);
            Assert.Equal(12, config.Priority);
            Assert.Equal(parsedRoute.GetConfigHash(), config.ConfigHash);
            Assert.Single(config.Endpoints);
            var routeEndpoint = config.Endpoints[0] as AspNetCore.Routing.RouteEndpoint;
            Assert.Equal("route1", routeEndpoint.DisplayName);
            Assert.Same(config, routeEndpoint.Metadata.GetMetadata<RouteConfig>());
            Assert.Equal("/a", routeEndpoint.RoutePattern.RawText);

            var hostMetadata = routeEndpoint.Metadata.GetMetadata<AspNetCore.Routing.HostAttribute>();
            Assert.NotNull(hostMetadata);
            Assert.Single(hostMetadata.Hosts);
            Assert.Equal("example.com", hostMetadata.Hosts[0]);
        }

        [Fact]
        public void BuildEndpoints_JustHost_Works()
        {
            // Arrange
            var builder = Create<RuntimeRouteBuilder>();
            var parsedRoute = new ParsedRoute
            {
                RouteId = "route1",
                Host = "example.com",
                Priority = 12,
            };
            var backend = new BackendInfo("backend1", new DestinationManager(), new Mock<IProxyHttpClientFactory>().Object);
            var routeInfo = new RouteInfo("route1");

            // Act
            var config = builder.Build(parsedRoute, backend, routeInfo);

            // Assert
            Assert.Same(backend, config.BackendOrNull);
            Assert.Equal(12, config.Priority);
            Assert.Equal(parsedRoute.GetConfigHash(), config.ConfigHash);
            Assert.Single(config.Endpoints);
            var routeEndpoint = config.Endpoints[0] as AspNetCore.Routing.RouteEndpoint;
            Assert.Equal("route1", routeEndpoint.DisplayName);
            Assert.Same(config, routeEndpoint.Metadata.GetMetadata<RouteConfig>());
            Assert.Equal("/{**catchall}", routeEndpoint.RoutePattern.RawText);

            var hostMetadata = routeEndpoint.Metadata.GetMetadata<AspNetCore.Routing.HostAttribute>();
            Assert.NotNull(hostMetadata);
            Assert.Single(hostMetadata.Hosts);
            Assert.Equal("example.com", hostMetadata.Hosts[0]);
        }

        [Fact]
        public void BuildEndpoints_JustHostWithWildcard_Works()
        {
            // Arrange
            var builder = Create<RuntimeRouteBuilder>();
            var parsedRoute = new ParsedRoute
            {
                RouteId = "route1",
                Host = "*.example.com",
                Priority = 12,
            };
            var backend = new BackendInfo("backend1", new DestinationManager(), new Mock<IProxyHttpClientFactory>().Object);
            var routeInfo = new RouteInfo("route1");

            // Act
            var config = builder.Build(parsedRoute, backend, routeInfo);

            // Assert
            Assert.Same(backend, config.BackendOrNull);
            Assert.Equal(12, config.Priority);
            Assert.Equal(parsedRoute.GetConfigHash(), config.ConfigHash);
            Assert.Single(config.Endpoints);
            var routeEndpoint = config.Endpoints[0] as AspNetCore.Routing.RouteEndpoint;
            Assert.Equal("route1", routeEndpoint.DisplayName);
            Assert.Same(config, routeEndpoint.Metadata.GetMetadata<RouteConfig>());
            Assert.Equal("/{**catchall}", routeEndpoint.RoutePattern.RawText);

            var hostMetadata = routeEndpoint.Metadata.GetMetadata<AspNetCore.Routing.HostAttribute>();
            Assert.NotNull(hostMetadata);
            Assert.Single(hostMetadata.Hosts);
            Assert.Equal("*.example.com", hostMetadata.Hosts[0]);
        }

        [Fact]
        public void BuildEndpoints_JustPath_Works()
        {
            // Arrange
            var builder = Create<RuntimeRouteBuilder>();
            var parsedRoute = new ParsedRoute
            {
                RouteId = "route1",
                Path = "/a",
                Priority = 12,
            };
            var backend = new BackendInfo("backend1", new DestinationManager(), new Mock<IProxyHttpClientFactory>().Object);
            var routeInfo = new RouteInfo("route1");

            // Act
            var config = builder.Build(parsedRoute, backend, routeInfo);

            // Assert
            Assert.Same(backend, config.BackendOrNull);
            Assert.Equal(12, config.Priority);
            Assert.Equal(parsedRoute.GetConfigHash(), config.ConfigHash);
            Assert.Single(config.Endpoints);
            var routeEndpoint = config.Endpoints[0] as AspNetCore.Routing.RouteEndpoint;
            Assert.Equal("route1", routeEndpoint.DisplayName);
            Assert.Same(config, routeEndpoint.Metadata.GetMetadata<RouteConfig>());
            Assert.Equal("/a", routeEndpoint.RoutePattern.RawText);

            var hostMetadata = routeEndpoint.Metadata.GetMetadata<AspNetCore.Routing.HostAttribute>();
            Assert.Null(hostMetadata);
        }

        [Fact]
        public void BuildEndpoints_NullMatchers_Works()
        {
            // Arrange
            var builder = Create<RuntimeRouteBuilder>();
            var parsedRoute = new ParsedRoute
            {
                RouteId = "route1",
                Priority = 12,
            };
            var backend = new BackendInfo("backend1", new DestinationManager(), new Mock<IProxyHttpClientFactory>().Object);
            var routeInfo = new RouteInfo("route1");

            // Act
            var config = builder.Build(parsedRoute, backend, routeInfo);

            // Assert
            Assert.Same(backend, config.BackendOrNull);
            Assert.Equal(12, config.Priority);
            Assert.NotEqual(0, config.ConfigHash);
            Assert.Single(config.Endpoints);
            var routeEndpoint = config.Endpoints[0] as AspNetCore.Routing.RouteEndpoint;
            Assert.Equal("route1", routeEndpoint.DisplayName);
            Assert.Same(config, routeEndpoint.Metadata.GetMetadata<RouteConfig>());
            Assert.Equal("/{**catchall}", routeEndpoint.RoutePattern.RawText);

            var hostMetadata = routeEndpoint.Metadata.GetMetadata<AspNetCore.Routing.HostAttribute>();
            Assert.Null(hostMetadata);
        }

        [Fact]
        public void BuildEndpoints_InvalidPath_BubblesOutException()
        {
            // Arrange
            var builder = Create<RuntimeRouteBuilder>();
            var parsedRoute = new ParsedRoute
            {
                RouteId = "route1",
                Path = "/{invalid",
                Priority = 12,
            };
            var backend = new BackendInfo("backend1", new DestinationManager(), new Mock<IProxyHttpClientFactory>().Object);
            var routeInfo = new RouteInfo("route1");

            // Act
            Action action = () => builder.Build(parsedRoute, backend, routeInfo);

            // Assert
            Assert.Throws<AspNetCore.Routing.Patterns.RoutePatternException>(action);
        }
    }
}
