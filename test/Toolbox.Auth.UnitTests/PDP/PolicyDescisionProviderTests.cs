﻿using Microsoft.Extensions.Caching.Memory;
using Moq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Toolbox.Auth.Options;
using Toolbox.Auth.PDP;
using Xunit;

namespace Toolbox.Auth.UnitTests.PDP
{
    public class PolicyDescisionProviderTests
    {
        private string _application = "APP";
        private string _userId = "user123";
        private string _pdpUrl = "http://test.com";
        private string requestedresource = "requestedResource";
        private AuthOptions _options;

        public PolicyDescisionProviderTests()
        {
            _options = new AuthOptions { PdpUrl = _pdpUrl, PdpCacheDuration = 60 };
        }

        [Fact]
        public void ThrowsExceptionIfCacheIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new PolicyDescisionProvider(null,
                Options.Create(new AuthOptions()),
                Mock.Of<HttpClientHandler>()));
        }

        [Fact]
        public void ThrowsExceptionIfOptionsWrapperIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new PolicyDescisionProvider(Mock.Of<IMemoryCache>(), null,
                Mock.Of<HttpClientHandler>()));
        }

        [Fact]
        public void ThrowsExceptionIfOptionsAreNull()
        {
            Assert.Throws<ArgumentNullException>(() => new PolicyDescisionProvider(Mock.Of<IMemoryCache>(),
                Options.Create<AuthOptions>(null),
                Mock.Of<HttpClientHandler>()));
        }

        [Fact]
        public void ThrowsExceptionIfClientIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new PolicyDescisionProvider(Mock.Of<IMemoryCache>(),
                Options.Create(new AuthOptions()),
               null));
        }

        [Fact]
        public async Task GetResponse()
        {
            var mockedCache = CreateEmptyMockedCache();

            var pdpResponse = new PdpResponse
            {
                applicationId = _application,
                userId = _userId,
                permissions = new List<String>(new string[] { requestedresource })
            };

            var mockHandler =new MockMessageHandler(HttpStatusCode.OK, pdpResponse);
            var provider = new PolicyDescisionProvider(mockedCache.Object, Options.Create(_options), mockHandler);
            var result = await provider.GetPermissions(_userId, _application);

            Assert.Equal(pdpResponse, result);
        }

        [Fact]
        public async Task ReturnsNullIfUserUnknown()
        {
            var mockedCache = CreateEmptyMockedCache();

            var pdpResponse = new PdpResponse
            {
                applicationId = _application,
                userId = _userId,
                permissions = new List<String>(new string[] { requestedresource })
            };

            var mockHandler = new MockMessageHandler(HttpStatusCode.NotFound, null);
            var provider = new PolicyDescisionProvider(mockedCache.Object, Options.Create(_options), mockHandler);
            var result = await provider.GetPermissions("otherUser", _application);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetCachedPdpResponse()
        {
            var pdpResponse = new PdpResponse
            {
                applicationId = _application,
                userId = _userId,
                permissions = new List<String>(new string[] { requestedresource })
            };

            var mockedCache = CreateMockedCache(BuildCacheKey(_userId), pdpResponse);
            var mockHandler = new MockMessageHandler(HttpStatusCode.NotFound, null);
            var provider = new PolicyDescisionProvider(mockedCache.Object, Options.Create(_options), mockHandler);

            var result = await provider.GetPermissions(_userId, _application);

            Assert.Equal(pdpResponse, result);
        }

        [Fact]
        public async Task SetResponseToCache()
        {
            var mockedCache = CreateEmptyMockedCache();

            var pdpResponse = new PdpResponse
            {
                applicationId = _application,
                userId = _userId,
                permissions = new List<String>(new string[] { requestedresource })
            };

            var mockHandler = new MockMessageHandler(HttpStatusCode.OK, pdpResponse);
            var provider = new PolicyDescisionProvider(mockedCache.Object, Options.Create(_options), mockHandler);

            var result = await provider.GetPermissions(_userId, _application);

            mockedCache.Verify(m => m.Set((object)BuildCacheKey(_userId), (object)pdpResponse, It.IsAny<MemoryCacheEntryOptions>()), Times.Once);
        }

        [Fact]
        public async Task CacheDurationFromOptionsIsUsed()
        {
            MemoryCacheEntryOptions memoryCacheEntryOptions = null;

            var mockedCache = CreateEmptyMockedCache();
            mockedCache.Setup(c => c.Set(It.IsAny<object>(), It.IsAny<object>(), It.IsAny<MemoryCacheEntryOptions>()))
                .Callback<object, object, MemoryCacheEntryOptions>((a, b, options) => memoryCacheEntryOptions = options);


            var pdpResponse = new PdpResponse
            {
                applicationId = _application,
                userId = _userId,
                permissions = new List<String>(new string[] { requestedresource })
            };

            var mockHandler = new MockMessageHandler(HttpStatusCode.OK, pdpResponse);
            var provider = new PolicyDescisionProvider(mockedCache.Object, Options.Create(_options), mockHandler);

            var result = await provider.GetPermissions(_userId, _application);

            Assert.True(memoryCacheEntryOptions.AbsoluteExpirationRelativeToNow.Value == new TimeSpan(0, _options.PdpCacheDuration, 0));
        }

        private string BuildCacheKey(string userId) => $"pdpResponse-{userId}";

        private Mock<IMemoryCache> CreateEmptyMockedCache()
        {
            return CreateMockedCache("", null);
        }

        private Mock<IMemoryCache> CreateMockedCache(string key, PdpResponse pdpResponse)
        {
            var mockCache = new Mock<IMemoryCache>();
            var cachedObject = pdpResponse as object;

            mockCache.Setup(c => c.TryGetValue("", out cachedObject))
                .Returns(false);

            mockCache.Setup(c => c.TryGetValue(key, out cachedObject))
                .Returns(true);

            return mockCache;
        }
    }
}
