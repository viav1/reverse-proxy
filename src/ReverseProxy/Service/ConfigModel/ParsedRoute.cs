// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.ReverseProxy.ConfigModel
{
    // TODO: Do we even need the ParsedRoute? It now matches the ProxyRoute 1:1
    internal class ParsedRoute
    {
        /// <summary>
        /// Unique identifier of this route.
        /// </summary>
        public string RouteId { get; set; }

        /// <summary>
        /// Only match requests that use these optional HTTP methods. E.g. GET, POST.
        /// </summary>
        public IReadOnlyList<string> Methods { get; set; }

        /// <summary>
        /// Only match requests with the given Host header.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Only match requests with the given Path pattern.
        /// </summary>
        public string Path { get; set; }

        // TODO:
        /// <summary>
        /// Only match requests that contain all of these query parameters.
        /// </summary>
        // public ICollection<KeyValuePair<string, string>> QueryParameters { get; set; }

        // TODO:
        /// <summary>
        /// Only match requests that contain all of these request headers.
        /// </summary>
        // public ICollection<KeyValuePair<string, string>> Headers { get; set; }

        /// <summary>
        /// Gets or sets the priority of this route.
        /// Routes with higher priority are evaluated first.
        /// </summary>
        public int? Priority { get; set; }

        /// <summary>
        /// Gets or sets the backend that requests matching this route
        /// should be proxied to.
        /// </summary>
        public string BackendId { get; set; }

        /// <summary>
        /// Arbitrary key-value pairs that further describe this route.
        /// </summary>
        public IDictionary<string, string> Metadata { get; set; }

        public IList<IDictionary<string, string>> Transforms { get; set; }

        // Used to diff for config changes
        internal int GetConfigHash()
        {
            var hash = 0;

            if (!string.IsNullOrEmpty(RouteId))
            {
                hash ^= RouteId.GetHashCode();
            }

            if (Methods != null && Methods.Count > 0)
            {
                // Assumes un-ordered
                hash ^= Methods.Select(item => item.GetHashCode())
                    .Aggregate((total, nextCode) => total ^ nextCode);
            }

            if (!string.IsNullOrEmpty(Host))
            {
                hash ^= Host.GetHashCode();
            }

            if (!string.IsNullOrEmpty(Path))
            {
                hash ^= Path.GetHashCode();
            }

            if (Priority.HasValue)
            {
                hash ^= Priority.GetHashCode();
            }

            if (!string.IsNullOrEmpty(BackendId))
            {
                hash ^= BackendId.GetHashCode();
            }

            if (Metadata != null)
            {
                hash ^= Metadata.Select(item => HashCode.Combine(item.Key.GetHashCode(), item.Value.GetHashCode()))
                    .Aggregate((total, nextCode) => total ^ nextCode);
            }

            if (Transforms != null)
            {
                // TODO: Doesn't handle list reordering
                hash ^= Transforms.Select(transform =>
                    transform.Select(item => HashCode.Combine(item.Key.GetHashCode(), item.Value.GetHashCode()))
                        .Aggregate((total, nextCode) => total ^ nextCode))
                    .Aggregate((total, nextCode) => total ^ nextCode);
            }

            return hash;
        }
    }
}
