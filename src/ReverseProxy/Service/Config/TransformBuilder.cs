// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Net.Http.Headers;
using Microsoft.ReverseProxy.Abstractions;
using Microsoft.ReverseProxy.Service.RuntimeModel.Transforms;

namespace Microsoft.ReverseProxy.Service.Config
{
    public class TransformBuilder : ITransformBuilder
    {
        private readonly TemplateBinderFactory _binderFactory;

        public TransformBuilder(TemplateBinderFactory binderFactory)
        {
            _binderFactory = binderFactory;
        }

        public bool Validate(IList<IDictionary<string, string>> rawTransforms, string routeId, IConfigErrorReporter errorReporter)
        {
            var success = true;
            if (rawTransforms == null || rawTransforms.Count == 0)
            {
                return success;
            }

            foreach (var rawTransform in rawTransforms)
            {
                if (rawTransform.TryGetValue("PathPrefix", out var pathPrefix))
                {
                    success &= TryCheckTooManyParameters(rawTransform, routeId, expected: 1, errorReporter);
                }
                else if (rawTransform.TryGetValue("PathRemovePrefix", out var pathRemovePrefix))
                {
                    success &= TryCheckTooManyParameters(rawTransform, routeId, expected: 1, errorReporter);
                }
                else if (rawTransform.TryGetValue("PathSet", out var pathSet))
                {
                    success &= TryCheckTooManyParameters(rawTransform, routeId, expected: 1, errorReporter);
                }
                else if (rawTransform.TryGetValue("PathPattern", out var pathPattern))
                {
                    success &= TryCheckTooManyParameters(rawTransform, routeId, expected: 1, errorReporter);
                }
                else if (rawTransform.TryGetValue("RequestHeadersCopy", out var copyHeaders))
                {
                    success &= TryCheckTooManyParameters(rawTransform, routeId, expected: 1, errorReporter);
                    if (!string.Equals("True", copyHeaders, StringComparison.OrdinalIgnoreCase) && !string.Equals("False", copyHeaders, StringComparison.OrdinalIgnoreCase))
                    {
                        errorReporter.ReportError(ConfigErrors.TransformInvalid, routeId, $"Unexpected value for RequestHeaderCopy: {copyHeaders}. Expected 'true' or 'false'.");
                        success = false;
                    }
                }
                else if (rawTransform.TryGetValue("RequestHeaderOriginalHost", out var originalHost))
                {
                    success &= TryCheckTooManyParameters(rawTransform, routeId, expected: 1, errorReporter);
                    if (!string.Equals("True", originalHost, StringComparison.OrdinalIgnoreCase) && !string.Equals("False", originalHost, StringComparison.OrdinalIgnoreCase))
                    {
                        errorReporter.ReportError(ConfigErrors.TransformInvalid, routeId, $"Unexpected value for RequestHeaderOriginalHost: {originalHost}. Expected 'true' or 'false'.");
                        success = false;
                    }
                }
                else if (rawTransform.TryGetValue("RequestHeader", out var headerName))
                {
                    success &= TryCheckTooManyParameters(rawTransform, routeId, expected: 2, errorReporter);
                    if (!rawTransform.TryGetValue("Set", out var _) && !rawTransform.TryGetValue("Append", out var _))
                    {
                        errorReporter.ReportError(ConfigErrors.TransformInvalid, routeId, $"Unexpected parameters for RequestHeader: {string.Join(';', rawTransform.Keys)}. Expected 'Set' or 'Append'.");
                        success = false;
                    }
                }
                else if (rawTransform.TryGetValue("ResponseHeader", out var _))
                {
                    if (rawTransform.TryGetValue("When", out var whenValue))
                    {
                        success &= TryCheckTooManyParameters(rawTransform, routeId, expected: 3, errorReporter);
                        if (!string.Equals("Always", whenValue, StringComparison.OrdinalIgnoreCase) && !string.Equals("OnSuccess", whenValue, StringComparison.OrdinalIgnoreCase))
                        {
                            errorReporter.ReportError(ConfigErrors.TransformInvalid, routeId, $"Unexpected value for ResponseHeader:When: {whenValue}. Expected 'Always' or 'OnSuccess'.");
                            success = false;
                        }
                    }
                    else
                    {
                        success &= TryCheckTooManyParameters(rawTransform, routeId, expected: 2, errorReporter);
                    }

                    if (!rawTransform.TryGetValue("Set", out var _) && !rawTransform.TryGetValue("Append", out var _))
                    {
                        errorReporter.ReportError(ConfigErrors.TransformInvalid, routeId, $"Unexpected parameters for ResponseHeader: {string.Join(';', rawTransform.Keys)}. Expected 'Set' or 'Append'.");
                        success = false;
                    }
                }
                else if (rawTransform.TryGetValue("ResponseTrailer", out var _))
                {
                    if (rawTransform.TryGetValue("When", out var whenValue))
                    {
                        success &= TryCheckTooManyParameters(rawTransform, routeId, expected: 3, errorReporter);
                        if (!string.Equals("Always", whenValue, StringComparison.OrdinalIgnoreCase) && !string.Equals("OnSuccess", whenValue, StringComparison.OrdinalIgnoreCase))
                        {
                            errorReporter.ReportError(ConfigErrors.TransformInvalid, routeId, $"Unexpected value for ResponseTrailer:When: {whenValue}. Expected 'Always' or 'OnSuccess'.");
                            success = false;
                        }
                    }
                    else
                    {
                        success &= TryCheckTooManyParameters(rawTransform, routeId, expected: 2, errorReporter);
                    }

                    if (!rawTransform.TryGetValue("Set", out var _) && !rawTransform.TryGetValue("Append", out var _))
                    {
                        errorReporter.ReportError(ConfigErrors.TransformInvalid, routeId, $"Unexpected parameters for ResponseTrailer: {string.Join(';', rawTransform.Keys)}. Expected 'Set' or 'Append'.");
                        success = false;
                    }
                }
                else if (rawTransform.TryGetValue("X-Forwarded", out var xforwardedHeaders))
                {
                    var expected = 1;

                    var append = true;
                    if (rawTransform.TryGetValue("Append", out var appendValue))
                    {
                        expected++;
                        append = string.Equals("true", appendValue, StringComparison.OrdinalIgnoreCase);

                        if (!string.Equals("True", appendValue, StringComparison.OrdinalIgnoreCase) && !string.Equals("False", appendValue, StringComparison.OrdinalIgnoreCase))
                        {
                            errorReporter.ReportError(ConfigErrors.TransformInvalid, routeId, $"Unexpected value for X-Forwarded:Append: {appendValue}. Expected 'true' or 'false'.");
                            success = false;
                        }
                    }

                    if (rawTransform.TryGetValue("Prefix", out var _))
                    {
                        expected++;
                    }

                    success &= TryCheckTooManyParameters(rawTransform, routeId, expected, errorReporter);

                    // for, host, proto, PathBase
                    var tokens = xforwardedHeaders.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var token in tokens)
                    {
                        if (!string.Equals(token, "For", StringComparison.OrdinalIgnoreCase)
                            && !string.Equals(token, "Host", StringComparison.OrdinalIgnoreCase)
                            && !string.Equals(token, "Proto", StringComparison.OrdinalIgnoreCase)
                            && !string.Equals(token, "PathBase", StringComparison.OrdinalIgnoreCase))
                        {
                            errorReporter.ReportError(ConfigErrors.TransformInvalid, routeId, $"Unexpected value for X-Forwarded: {token}. Expected 'for', 'host', 'proto', or 'PathBase'.");
                            success = false;
                        }
                    }
                }
                else if (rawTransform.TryGetValue("ClientCert", out var clientCertHeader))
                {
                    success &= TryCheckTooManyParameters(rawTransform, routeId, expected: 1, errorReporter);
                }
                else
                {
                    errorReporter.ReportError(ConfigErrors.TransformInvalid, routeId, $"Unknown transform: {string.Join(';', rawTransform.Keys)}.");
                    success = false;
                }
            }

            return success;
        }

        public void Build(IList<IDictionary<string, string>> rawTransforms, out Transforms transforms)
        {
            transforms = null;
            if (rawTransforms == null || rawTransforms.Count == 0)
            {
                return;
            }

            bool? copyRequestHeaders = null;
            bool? useOriginalHost = null;
            bool? forwardersSet = null;
            var requestTransforms = new List<RequestParametersTransform>();
            var requestHeaderTransforms = new Dictionary<string, RequestHeaderTransform>();
            var responseHeaderTransforms = new Dictionary<string, ResponseHeaderTransform>();
            var responseTrailerTransforms = new Dictionary<string, ResponseHeaderTransform>();

            foreach (var rawTransform in rawTransforms)
            {
                if (rawTransform.TryGetValue("PathPrefix", out var pathPrefix))
                {
                    CheckTooManyParameters(rawTransform, expected: 1);
                    var path = MakePathString(pathPrefix);
                    requestTransforms.Add(new PathStringTransform(PathStringTransform.PathTransformMode.Prepend, path));
                }
                else if (rawTransform.TryGetValue("PathRemovePrefix", out var pathRemovePrefix))
                {
                    CheckTooManyParameters(rawTransform, expected: 1);
                    var path = MakePathString(pathRemovePrefix);
                    requestTransforms.Add(new PathStringTransform(PathStringTransform.PathTransformMode.RemovePrefix, path));
                }
                else if (rawTransform.TryGetValue("PathSet", out var pathSet))
                {
                    CheckTooManyParameters(rawTransform, expected: 1);
                    var path = MakePathString(pathSet);
                    requestTransforms.Add(new PathStringTransform(PathStringTransform.PathTransformMode.Set, path));
                }
                else if (rawTransform.TryGetValue("PathPattern", out var pathPattern))
                {
                    CheckTooManyParameters(rawTransform, expected: 1);
                    if (!string.IsNullOrEmpty(pathPattern) && !pathPattern.StartsWith("/", StringComparison.Ordinal))
                    {
                        pathPattern = "/" + pathPattern;
                    }
                    requestTransforms.Add(new PathRouteValueTransform(pathPattern, _binderFactory));
                }
                else if (rawTransform.TryGetValue("RequestHeadersCopy", out var copyHeaders))
                {
                    CheckTooManyParameters(rawTransform, expected: 1);
                    copyRequestHeaders = string.Equals("True", copyHeaders, StringComparison.OrdinalIgnoreCase);
                }
                else if (rawTransform.TryGetValue("RequestHeaderOriginalHost", out var originalHost))
                {
                    CheckTooManyParameters(rawTransform, expected: 1);
                    useOriginalHost = string.Equals("True", originalHost, StringComparison.OrdinalIgnoreCase);
                }
                else if (rawTransform.TryGetValue("RequestHeader", out var headerName))
                {
                    CheckTooManyParameters(rawTransform, expected: 2);
                    if (rawTransform.TryGetValue("Set", out var setValue))
                    {
                        requestHeaderTransforms[headerName] = new RequestHeaderValueTransform(setValue, append: false);
                    }
                    else if (rawTransform.TryGetValue("Append", out var appendValue))
                    {
                        requestHeaderTransforms[headerName] = new RequestHeaderValueTransform(appendValue, append: true);
                    }
                    else
                    {
                        throw new NotImplementedException(string.Join(';', rawTransform.Keys));
                    }
                }
                else if (rawTransform.TryGetValue("ResponseHeader", out var responseHeaderName))
                {
                    var always = false;
                    if (rawTransform.TryGetValue("When", out var whenValue) && string.Equals("always", whenValue, StringComparison.OrdinalIgnoreCase))
                    {
                        CheckTooManyParameters(rawTransform, expected: 3);
                        always = true;
                    }
                    else
                    {
                        CheckTooManyParameters(rawTransform, expected: 2);
                    }

                    if (rawTransform.TryGetValue("Set", out var setValue))
                    {
                        responseHeaderTransforms[responseHeaderName] = new ResponseHeaderValueTransform(setValue, append: false, always);
                    }
                    else if (rawTransform.TryGetValue("Append", out var appendValue))
                    {
                        responseHeaderTransforms[responseHeaderName] = new ResponseHeaderValueTransform(appendValue, append: true, always);
                    }
                    else
                    {
                        throw new NotImplementedException(string.Join(';', rawTransform.Keys));
                    }
                }
                else if (rawTransform.TryGetValue("ResponseTrailer", out var responseTrailerName))
                {
                    var always = false;
                    if (rawTransform.TryGetValue("When", out var whenValue))
                    {
                        CheckTooManyParameters(rawTransform, expected: 3);
                        always = string.Equals("always", whenValue, StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        CheckTooManyParameters(rawTransform, expected: 2);
                    }

                    if (rawTransform.TryGetValue("Set", out var setValue))
                    {
                        responseTrailerTransforms[responseTrailerName] = new ResponseHeaderValueTransform(setValue, append: false, always);
                    }
                    else if (rawTransform.TryGetValue("Append", out var appendValue))
                    {
                        responseTrailerTransforms[responseTrailerName] = new ResponseHeaderValueTransform(appendValue, append: true, always);
                    }
                    else
                    {
                        throw new NotImplementedException(string.Join(';', rawTransform.Keys));
                    }
                }
                else if (rawTransform.TryGetValue("X-Forwarded", out var xforwardedHeaders))
                {
                    forwardersSet = true;
                    var expected = 1;

                    var append = true;
                    if (rawTransform.TryGetValue("Append", out var appendValue))
                    {
                        expected++;
                        append = string.Equals("true", appendValue, StringComparison.OrdinalIgnoreCase);
                    }

                    var prefix = "X-Forwarded-";
                    if (rawTransform.TryGetValue("Prefix", out var prefixValue))
                    {
                        expected++;
                        prefix = prefixValue;
                    }

                    CheckTooManyParameters(rawTransform, expected);

                    // for, host, proto, PathBase
                    var tokens = xforwardedHeaders.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var token in tokens)
                    {
                        if (string.Equals(token, "For", StringComparison.OrdinalIgnoreCase))
                        {
                            requestHeaderTransforms[prefix + "For"] = new RequestHeaderXForwardedForTransform(append);
                        }
                        else if (string.Equals(token, "Host", StringComparison.OrdinalIgnoreCase))
                        {
                            requestHeaderTransforms[prefix + "Host"] = new RequestHeaderXForwardedHostTransform(append);
                        }
                        else if (string.Equals(token, "Proto", StringComparison.OrdinalIgnoreCase))
                        {
                            requestHeaderTransforms[prefix + "Proto"] = new RequestHeaderXForwardedProtoTransform(append);
                        }
                        else if (string.Equals(token, "PathBase", StringComparison.OrdinalIgnoreCase))
                        {
                            requestHeaderTransforms[prefix + "PathBase"] = new RequestHeaderXForwardedPathBaseTransform(append);
                        }
                        else
                        {
                            throw new NotImplementedException(token);
                        }
                    }
                }
                else if (rawTransform.TryGetValue("ClientCert", out var clientCertHeader))
                {
                    CheckTooManyParameters(rawTransform, expected: 1);
                    requestHeaderTransforms[clientCertHeader] = new RequestHeaderClientCertTransform();
                }
                else
                {
                    throw new NotImplementedException(string.Join(';', rawTransform.Keys));
                }
            }

            // If there's no transform defined for Host, suppress the host by default
            if (!requestHeaderTransforms.ContainsKey(HeaderNames.Host) && !(useOriginalHost ?? false))
            {
                requestHeaderTransforms[HeaderNames.Host] = new RequestHeaderValueTransform(null, append: false);
            }

            // Add default forwarders
            if (!forwardersSet.GetValueOrDefault())
            {
                if (!requestHeaderTransforms.ContainsKey(ForwardedHeadersDefaults.XForwardedProtoHeaderName))
                {
                    requestHeaderTransforms[ForwardedHeadersDefaults.XForwardedProtoHeaderName] = new RequestHeaderXForwardedProtoTransform(append: true);
                }
                if (!requestHeaderTransforms.ContainsKey(ForwardedHeadersDefaults.XForwardedHostHeaderName))
                {
                    requestHeaderTransforms[ForwardedHeadersDefaults.XForwardedHostHeaderName] = new RequestHeaderXForwardedHostTransform(append: true);
                }
                if (!requestHeaderTransforms.ContainsKey(ForwardedHeadersDefaults.XForwardedForHeaderName))
                {
                    requestHeaderTransforms[ForwardedHeadersDefaults.XForwardedForHeaderName] = new RequestHeaderXForwardedForTransform(append: true);
                }
                if (!requestHeaderTransforms.ContainsKey("X-Forwarded-PathBase"))
                {
                    requestHeaderTransforms["X-Forwarded-PathBase"] = new RequestHeaderXForwardedPathBaseTransform(append: true);
                }
            }

            transforms = new Transforms(requestTransforms, copyRequestHeaders, requestHeaderTransforms, responseHeaderTransforms, responseTrailerTransforms);
        }

        private bool TryCheckTooManyParameters(IDictionary<string, string> rawTransform, string routeId, int expected, IConfigErrorReporter errorReporter)
        {
            if (rawTransform.Count > expected)
            {
                errorReporter.ReportError(ConfigErrors.TransformInvalid, routeId, $"The transform contains more parameters than the expected {expected}: {string.Join(';', rawTransform.Keys)}.");
                return false;
            }

            return true;
        }

        private void CheckTooManyParameters(IDictionary<string, string> rawTransform, int expected)
        {
            if (rawTransform.Count > expected)
            {
                throw new NotImplementedException("The transform contains more parameters than expected: " + string.Join(';', rawTransform.Keys));
            }
        }

        private PathString MakePathString(string path)
        {
            if (!string.IsNullOrEmpty(path) && !path.StartsWith("/", StringComparison.Ordinal))
            {
                path = "/" + path;
            }
            return new PathString(path);
        }
    }
}
