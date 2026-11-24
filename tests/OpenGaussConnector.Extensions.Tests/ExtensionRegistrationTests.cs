using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenGaussConnector.GeoJSON.Internal;
using OpenGaussConnector.Json.NET.Internal;
using OpenGaussConnector.NetTopologySuite.Internal;
using OpenGaussConnector.NodaTime.Internal;
using OpenGaussConnector.TypeMapping;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Xunit;

namespace OpenGaussConnector.Extensions.Tests
{
    /// <summary>
    /// Offline checks for the five extension packages: each plugin must register its resolver factory on the
    /// global mapper, and the OpenTelemetry plugin must subscribe the core activity source. Behaviour that
    /// needs a live server is covered by the specification tests.
    /// </summary>
    public class ExtensionRegistrationTests : IDisposable
    {
        readonly IOpenGaussTypeMapper _mapper = GlobalTypeMapper.Instance;

        public ExtensionRegistrationTests() => GlobalTypeMapper.Instance.Reset();

        public void Dispose() => GlobalTypeMapper.Instance.Reset();

        /// <summary>
        /// Resolver factories registered by plugins, excluding the built-in one that every mapper starts with.
        /// </summary>
        static Type[] PluginFactories()
            => GlobalTypeMapper.Instance.ResolverFactories
                .Where(f => f is not BuiltInTypeHandlerResolverFactory)
                .Select(f => f.GetType())
                .ToArray();

        [Fact]
        public void A_reset_mapper_only_has_the_built_in_factory()
        {
            Assert.IsType<BuiltInTypeHandlerResolverFactory>(Assert.Single(GlobalTypeMapper.Instance.ResolverFactories));
        }

        [Fact]
        public void UseGeoJson_registers_its_resolver_factory()
        {
            Assert.Same(_mapper, _mapper.UseGeoJson());

            Assert.Equal(new[] { typeof(GeoJSONTypeHandlerResolverFactory) }, PluginFactories());
        }

        [Fact]
        public void UseJsonNet_registers_its_resolver_factory()
        {
            Assert.Same(_mapper, _mapper.UseJsonNet());

            Assert.Equal(new[] { typeof(JsonNetTypeHandlerResolverFactory) }, PluginFactories());
        }

        [Fact]
        public void UseNetTopologySuite_registers_its_resolver_factory()
        {
            Assert.Same(_mapper, _mapper.UseNetTopologySuite());

            Assert.Equal(new[] { typeof(NetTopologySuiteTypeHandlerResolverFactory) }, PluginFactories());
        }

        [Fact]
        public void UseNodaTime_registers_its_resolver_factory()
        {
            Assert.Same(_mapper, _mapper.UseNodaTime());

            Assert.Equal(new[] { typeof(NodaTimeTypeHandlerResolverFactory) }, PluginFactories());
        }

        [Fact]
        public void Plugins_register_independently_and_are_dropped_by_a_reset()
        {
            _mapper.UseGeoJson().UseJsonNet().UseNodaTime();

            Assert.Equal(3, PluginFactories().Length);

            GlobalTypeMapper.Instance.Reset();

            Assert.Empty(PluginFactories());
        }

        [Fact]
        public void AddOpenGauss_subscribes_the_core_activity_source()
        {
            var processor = new CapturingProcessor();

            using var provider = Sdk.CreateTracerProviderBuilder()
                .AddOpenGauss()
                .AddProcessor(processor)
                .Build();

            using var source = new ActivitySource("OpenGauss");
            using (source.StartActivity("command")) { }

            Assert.Equal("command", Assert.Single(processor.Activities).DisplayName);
        }

        [Fact]
        public void AddOpenGauss_leaves_other_sources_alone()
        {
            var processor = new CapturingProcessor();

            using var provider = Sdk.CreateTracerProviderBuilder()
                .AddOpenGauss()
                .AddProcessor(processor)
                .Build();

            using var source = new ActivitySource("SomeOtherSource");
            using (source.StartActivity("unrelated")) { }

            Assert.Empty(processor.Activities);
        }

        sealed class CapturingProcessor : BaseProcessor<Activity>
        {
            public List<Activity> Activities { get; } = new();

            public override void OnEnd(Activity data) => Activities.Add(data);
        }
    }
}
