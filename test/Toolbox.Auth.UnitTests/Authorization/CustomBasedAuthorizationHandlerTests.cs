﻿
using Microsoft.AspNetCore.Authorization;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using Toolbox.Auth.Authorization;
using Toolbox.Auth.Options;
using Xunit;

namespace Toolbox.Auth.UnitTests.Authorization
{
    public class CustomBasedAuthorizationHandlerTests
    {
        private readonly AuthOptions _authOptions;
        private readonly string _userId = "user123";

        public CustomBasedAuthorizationHandlerTests()
        {
            _authOptions = new AuthOptions
            {
                ApplicationName = "APP"
            };
        }

        [Fact]
        public void ThrowsExceptionIfRequiredPermissionsResolverIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new CustomBasedAuthorizationHandler(null));
        }

        [Fact]
        public void SucceedWhenPermissionsGranted()
        {
            var requiredPermissions = new[] { "customPermission" };
            var mockRequiredPermissionsResolver = CreateMockRequiredPermissionsResolver(requiredPermissions);
            var handler = new CustomBasedAuthorizationHandler(mockRequiredPermissionsResolver);

            var permissionClaims = new List<Claim>(new Claim[]
                {
                    new Claim(Claims.PermissionsType, requiredPermissions[0])
                });

            var context = CreateAuthorizationContext(permissionClaims);

            handler.Handle(context);

            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public void NotSucceedWhenPermissionsRefused()
        {
            var requiredPermissions = new[] { "customPermission" };
            var mockRequiredPermissionsResolver = CreateMockRequiredPermissionsResolver(requiredPermissions);
            var handler = new CustomBasedAuthorizationHandler(mockRequiredPermissionsResolver);

            var permissionClaims = new List<Claim>(new Claim[]
                {
                    new Claim(Claims.PermissionsType, "otherresource")
                });

            var context = CreateAuthorizationContext(new List<Claim>());

            handler.Handle(context);

            Assert.False(context.HasSucceeded);
        }

        private IRequiredPermissionsResolver CreateMockRequiredPermissionsResolver(IEnumerable<string> requiredPermissions)
        {
            var mockRequiredPermissionsResolver = new Mock<IRequiredPermissionsResolver>();
            mockRequiredPermissionsResolver.Setup(r => r.ResolveFromAttributeProperties(It.IsAny<AuthorizationContext>()))
                .Returns(requiredPermissions);

            return mockRequiredPermissionsResolver.Object;
        }

        private AuthorizationContext CreateAuthorizationContext(List<Claim> claims)
        {
            var requirements = new IAuthorizationRequirement[] { new CustomBasedRequirement() };

            claims.Add(new Claim(ClaimTypes.Name, _userId));
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
            var context = new AuthorizationContext(requirements, user, null);
            return context;
        }
    }
}
