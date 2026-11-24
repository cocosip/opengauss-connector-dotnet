using System;
using BenchmarkDotNet.Attributes;
using OpenGaussConnector.Internal.TypeHandlers;

namespace OpenGaussConnector.Benchmarks.TypeHandlers
{
    [Config(typeof(Config))]
    public class Uuid : TypeHandlerBenchmarks<Guid>
    {
        public Uuid() : base(new UuidHandler(GetPostgresType("uuid"))) { }
    }
}
