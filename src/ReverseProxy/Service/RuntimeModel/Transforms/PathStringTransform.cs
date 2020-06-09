// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.ReverseProxy.Service.RuntimeModel.Transforms
{
    public class PathStringTransform : RequestParametersTransform
    {
        public PathStringTransform(PathTransformMode mode, PathString value)
        {
            Mode = mode;
            Value = value;
        }

        public PathString Value { get; }

        public PathTransformMode Mode { get; }

        public override void Apply(RequestParametersTransformContext context)
        {
            var input = context.Path;
            PathString result;
            switch (Mode)
            {
                case PathTransformMode.Set:
                    result = Value;
                    break;
                case PathTransformMode.Prefix:
                    result = Value + input;
                    break;
                case PathTransformMode.RemovePrefix:
                    result = input.StartsWithSegments(Value, out var remainder) ? remainder : input;
                    break;
                default:
                    throw new NotImplementedException(Mode.ToString());
            }

            context.Path = result;
        }

        public enum PathTransformMode
        {
            Set,
            Prefix,
            RemovePrefix,
        }
    }
}
