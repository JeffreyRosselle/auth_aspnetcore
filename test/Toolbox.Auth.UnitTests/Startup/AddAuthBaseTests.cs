﻿using Microsoft.AspNet.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.OptionsModel;
using System;
using System.Linq;
using System.Net.Http;
using Toolbox.Auth.Authorization;
using Toolbox.Auth.Jwt;
using Toolbox.Auth.Options;
using Toolbox.Auth.PDP;
using Xunit;

namespace Toolbox.Auth.UnitTests.Startup
{
    public abstract class AddAuthBaseTests
    {
        public Action<ServiceCollection> Act { get; set; }

        [Fact]
        public void ActionNullRaisesArgumentException()
        {
            Action<AuthOptions> nullAction = null;
            var services = new ServiceCollection();

            var ex = Assert.Throws<ArgumentNullException>(() => services.AddAuth(nullAction));

            Assert.Equal("setupAction", ex.ParamName);
        }

        [Fact]
        public void PolicyDescisionProviderIsRegistratedAsSingleton()
        {
            var services = new ServiceCollection();

            Act(services);

            var registrations = services.Where(sd => sd.ServiceType == typeof(IPolicyDescisionProvider) &&
                                                     sd.ImplementationType == typeof(PolicyDescisionProvider))
                                        .ToArray();

            Assert.Equal(1, registrations.Count());
            Assert.Equal(ServiceLifetime.Singleton, registrations[0].Lifetime);
        }


        [Fact]
        public void AuthorizePermissionsHandlerIsRegistratedAsSingleton()
        {
            var services = new ServiceCollection();

            Act(services);

            var registrations = services.Where(sd => sd.ServiceType == typeof(IAuthorizationHandler) &&
                                                     sd.ImplementationType == typeof(ConventionBasedAuthorizationHandler))
                                        .ToArray();

            Assert.Equal(1, registrations.Count());
            Assert.Equal(ServiceLifetime.Singleton, registrations[0].Lifetime);
        }


        [Fact]
        public void AllowedResourceResolverIsRegistratedAsSingleton()
        {
            var services = new ServiceCollection();

            Act(services);

            var registrations = services.Where(sd => sd.ServiceType == typeof(IRequiredPermissionsResolver) &&
                                                     sd.ImplementationType == typeof(RequiredPermissionsResolver))
                                        .ToArray();

            Assert.Equal(1, registrations.Count());
            Assert.Equal(ServiceLifetime.Singleton, registrations[0].Lifetime);
        }


        [Fact]
        public void HttpClientHandlerIsRegistratedAsSingleton()
        {
            var services = new ServiceCollection();

            Act(services);

            var registrations = services.Where(sd => sd.ServiceType == typeof(HttpMessageHandler) &&
                                                     sd.ImplementationType == typeof(HttpClientHandler))
                                        .ToArray();

            Assert.Equal(1, registrations.Count());
            Assert.Equal(ServiceLifetime.Singleton, registrations[0].Lifetime);
        }

        [Fact]
        public void PermissionsClaimsTransformerIsRegistratedAsSingleton()
        {
            var services = new ServiceCollection();

            Act(services);

            var registrations = services.Where(sd => sd.ServiceType == typeof(PermissionsClaimsTransformer) &&
                                                     sd.ImplementationType == typeof(PermissionsClaimsTransformer))
                                        .ToArray();

            Assert.Equal(1, registrations.Count());
            Assert.Equal(ServiceLifetime.Singleton, registrations[0].Lifetime);
        }

        [Fact]
        public void AuthOptionsAreRegistratedAsSingleton()
        {
            var services = new ServiceCollection();

            Act(services);

            var registrations = services.Where(sd => sd.ServiceType == typeof(IConfigureOptions<AuthOptions>))
                                        .ToArray();

            Assert.Equal(1, registrations.Count());
            Assert.Equal(ServiceLifetime.Singleton, registrations[0].Lifetime);

            var configOptions = registrations[0].ImplementationInstance as IConfigureOptions<AuthOptions>;
            Assert.NotNull(configOptions);

            var authOptions = new AuthOptions();
            configOptions.Configure(authOptions);

            Assert.Equal("AppName", authOptions.ApplicationName);
            Assert.Equal("http://test.pdp.be/", authOptions.PdpUrl);
            Assert.Equal(60, authOptions.PdpCacheDuration);
            Assert.Equal("audience", authOptions.JwtAudience);
            Assert.Equal("issuer", authOptions.JwtIssuer);
            Assert.Equal("sub", authOptions.JwtUserIdClaimType);
        }

        [Fact]
        public void ConventionBasedPolicyIsAdded()
        {
            var services = new ServiceCollection();

            Act(services);

            var authorizationOptions = services.BuildServiceProvider().GetRequiredService<IOptions<AuthorizationOptions>>()?.Value;

            var conventionBasedPolicy = authorizationOptions?.GetPolicy(Policies.ConventionBased);

            Assert.NotNull(conventionBasedPolicy);
            Assert.NotEmpty(conventionBasedPolicy.Requirements.Where(r => r.GetType() == typeof(ConventionBasedRequirement)));
        }

        [Fact]
        public void CustomBasedPolicyIsAdded()
        {
            var services = new ServiceCollection();

            Act(services);

            var authorizationOptions = services.BuildServiceProvider().GetRequiredService<IOptions<AuthorizationOptions>>()?.Value;

            var customBasedPolicy = authorizationOptions?.GetPolicy(Policies.CustomBased);

            Assert.NotNull(customBasedPolicy);
            Assert.NotEmpty(customBasedPolicy.Requirements.Where(r => r.GetType() == typeof(CustomBasedRequirement)));
        }

        [Fact]
        public void JwtSecurityKeyProviderIsRegistratedAsSingleton()
        {
            var services = new ServiceCollection();

            Act(services);

            var registrations = services.Where(sd => sd.ServiceType == typeof(IJwtSigningKeyProvider) &&
                                                     sd.ImplementationType == typeof(JwtSigningKeyProvider))
                                        .ToArray();

            Assert.Equal(1, registrations.Count());
            Assert.Equal(ServiceLifetime.Singleton, registrations[0].Lifetime);
        }

        [Fact]
        public void JwtTokenSignatureValidatorIsRegistratedAsSingleton()
        {
            var services = new ServiceCollection();

            Act(services);

            var registrations = services.Where(sd => sd.ServiceType == typeof(IJwtTokenSignatureValidator) &&
                                                     sd.ImplementationType == typeof(JwtTokenSignatureValidator))
                                        .ToArray();

            Assert.Equal(1, registrations.Count());
            Assert.Equal(ServiceLifetime.Singleton, registrations[0].Lifetime);
        }
    }
}
