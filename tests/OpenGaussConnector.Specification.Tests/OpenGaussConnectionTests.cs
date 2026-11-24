using AdoNet.Specification.Tests;

namespace OpenGaussConnector.Specification.Tests
{
    public sealed class OpenGaussConnectionTests : ConnectionTestBase<OpenGaussDbFactoryFixture>
    {
        public OpenGaussConnectionTests(OpenGaussDbFactoryFixture fixture)
            : base(fixture)
        {
        }
    }
}
