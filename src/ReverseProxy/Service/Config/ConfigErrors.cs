// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.ReverseProxy.Service
{
    internal static class ConfigErrors
    {
        internal const string BackendDuplicateId = "Backend_DuplicateId";

        internal const string BackendEndpointDuplicateId = "BackendEndpoint_DuplicateId";
        internal const string BackendEndpointUnknownBackend = "BackendEndpoint_UnknownBackend";

        internal const string RouteDuplicateId = "Route_DuplicateId";
        internal const string RouteUnknownBackend = "Route_UnknownBackend";
        internal const string RouteNoBackends = "Route_NoBackends";
        internal const string RouteUnsupportedAction = "Route_UnsupportedAction";

        internal const string ParsedRouteMissingId = "ParsedRoute_MissingId";
        internal const string ParsedRouteRuleHasNoMatchers = "ParsedRoute_RuleHasNoMatchers";
        internal const string ParsedRouteRuleInvalidMatcher = "ParsedRoute_RuleInvalidMatcher";
        internal const string TransformInvalid = "Transform_Invalid";

        internal const string ConfigBuilderBackendIdMismatch = "ConfigBuilder_BackendIdMismatch";
        internal const string ConfigBuilderBackendNoProviderFoundForSessionAffinityMode = "ConfigBuilder_BackendNoProviderFoundForSessionAffinityMode";
        internal const string ConfigBuilderBackendNoAffinityFailurePolicyFoundForSpecifiedName = "ConfigBuilder_NoAffinityFailurePolicyFoundForSpecifiedName";
        internal const string ConfigBuilderBackendException = "ConfigBuilder_BackendException";
        internal const string ConfigBuilderRouteException = "ConfigBuilder_RouteException";
    }
}
