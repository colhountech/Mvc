﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    public class ViewComponentInvokerCache
    {
        private readonly IViewComponentDescriptorCollectionProvider _collectionProvider;

        private volatile InnerCache _currentCache;

        public ViewComponentInvokerCache(IViewComponentDescriptorCollectionProvider collectionProvider)
        {
            _collectionProvider = collectionProvider;
        }

        private InnerCache CurrentCache
        {
            get
            {
                var current = _currentCache;
                var actionDescriptors = _collectionProvider.ViewComponents;

                if (current == null || current.Version != actionDescriptors.Version)
                {
                    current = new InnerCache(actionDescriptors.Version);
                    _currentCache = current;
                }

                return current;
            }
        }

        public ObjectMethodExecutor GetViewComponentMethodExecutor(ViewComponentContext viewComponentContext)
        {
            var cache = CurrentCache;
            var viewComponentDescriptor = viewComponentContext.ViewComponentDescriptor;

            ObjectMethodExecutor executor;
            if (cache.Entries.TryGetValue(viewComponentDescriptor, out executor))
            {
                return executor;
            }

            executor = ObjectMethodExecutor.Create(viewComponentDescriptor.MethodInfo, viewComponentDescriptor.TypeInfo);

            cache.Entries.TryAdd(viewComponentDescriptor, executor);
            return executor;
        }

        private class InnerCache
        {
            public InnerCache(int version)
            {
                Version = version;
            }

            public ConcurrentDictionary<ViewComponentDescriptor, ObjectMethodExecutor> Entries { get; } =
                new ConcurrentDictionary<ViewComponentDescriptor, ObjectMethodExecutor>();

            public int Version { get; }
        }
    }
}
