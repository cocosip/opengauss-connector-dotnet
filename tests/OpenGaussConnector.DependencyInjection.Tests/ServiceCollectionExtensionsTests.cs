using System;
using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using OpenGaussConnector;
using Xunit;

namespace OpenGaussConnector.DependencyInjection.Tests
{
    public class ServiceCollectionExtensionsTests
    {
        const string ConnectionString = "Host=127.0.0.1;Port=5432;Database=test;Username=test;Password=secret";

        static ServiceProvider Build(Action<IServiceCollection> register)
        {
            var services = new ServiceCollection();
            register(services);
            return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        }

        [Fact]
        public void AddOpenGaussConnection_registers_closed_transient_connections()
        {
            using var provider = Build(s => s.AddOpenGaussConnection(ConnectionString));

            using var first = provider.GetRequiredService<OpenGaussConnection>();
            using var second = provider.GetRequiredService<OpenGaussConnection>();

            Assert.NotSame(first, second);
            Assert.Equal(System.Data.ConnectionState.Closed, first.State);
            Assert.Equal("test", first.Database);
        }

        [Fact]
        public void AddOpenGaussConnection_aliases_the_ado_net_base_type()
        {
            using var provider = Build(s => s.AddOpenGaussConnection(ConnectionString));

            using var connection = provider.GetRequiredService<DbConnection>();

            Assert.IsType<OpenGaussConnection>(connection);
        }

        [Fact]
        public void AddOpenGaussConnection_keeps_an_existing_DbConnection_registration()
        {
            var services = new ServiceCollection();
            services.AddSingleton<DbConnection>(new FakeDbConnection());
            services.AddOpenGaussConnection(ConnectionString);

            using var provider = services.BuildServiceProvider();

            Assert.IsType<FakeDbConnection>(provider.GetRequiredService<DbConnection>());
            Assert.IsType<OpenGaussConnection>(provider.GetRequiredService<OpenGaussConnection>());
        }

        [Fact]
        public void AddOpenGaussConnection_rejects_a_duplicate_registration()
        {
            var services = new ServiceCollection().AddOpenGaussConnection(ConnectionString);

            var exception = Assert.Throws<InvalidOperationException>(() => services.AddOpenGaussConnection(ConnectionString));
            Assert.Contains("already registered", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void AddOpenGaussConnection_rejects_an_empty_connection_string()
        {
            var services = new ServiceCollection();

            Assert.Throws<ArgumentException>(() => services.AddOpenGaussConnection(" "));
        }

        [Fact]
        public void AddOpenGaussDataSource_registers_one_instance_for_concrete_and_base_type()
        {
            using var provider = Build(s => s.AddOpenGaussDataSource(ConnectionString));

            var concrete = provider.GetRequiredService<OpenGaussDataSource>();
            var baseType = provider.GetRequiredService<DbDataSource>();

            Assert.Same(concrete, baseType);
            Assert.Contains("Host=127.0.0.1", concrete.ConnectionString, StringComparison.Ordinal);
            Assert.DoesNotContain("secret", concrete.ConnectionString, StringComparison.Ordinal);
        }

        [Fact]
        public void AddOpenGaussDataSource_hands_out_closed_connections_with_a_fixed_connection_string()
        {
            using var provider = Build(s => s.AddOpenGaussDataSource(ConnectionString));
            var dataSource = provider.GetRequiredService<OpenGaussDataSource>();

            using var connection = dataSource.CreateConnection();

            Assert.Equal(System.Data.ConnectionState.Closed, connection.State);
            Assert.Equal(dataSource.ConnectionString, connection.ConnectionString);
            Assert.Throws<InvalidOperationException>(() => connection.ConnectionString = ConnectionString);
        }

        [Fact]
        public void AddOpenGaussDataSource_creates_command_and_batch_from_the_data_source()
        {
            using var provider = Build(s => s.AddOpenGaussDataSource(ConnectionString));
            var dataSource = provider.GetRequiredService<OpenGaussDataSource>();

            Assert.Equal("SELECT 1", dataSource.CreateCommand("SELECT 1").CommandText);
            Assert.IsType<OpenGaussBatch>(dataSource.CreateBatch());
        }

        [Fact]
        public void AddOpenGaussDataSource_runs_the_configure_callback_before_build()
        {
            using var provider = Build(s => s.AddOpenGaussDataSource(
                "Host=ignored;Username=test",
                (_, builder) => builder.ConnectionString = ConnectionString));

            var dataSource = provider.GetRequiredService<OpenGaussDataSource>();
            Assert.Contains("Database=test", dataSource.ConnectionString, StringComparison.Ordinal);
        }

        [Fact]
        public void AddOpenGaussDataSource_disposes_the_data_source_with_the_container()
        {
            var provider = Build(s => s.AddOpenGaussDataSource(ConnectionString));
            var dataSource = provider.GetRequiredService<OpenGaussDataSource>();

            provider.Dispose();

            Assert.Throws<ObjectDisposedException>(() => dataSource.CreateConnection());
        }

        [Fact]
        public void AddOpenGaussDataSource_rejects_a_duplicate_registration()
        {
            var services = new ServiceCollection().AddOpenGaussDataSource(ConnectionString);

            Assert.Throws<InvalidOperationException>(() => services.AddOpenGaussDataSource(ConnectionString));
        }

        [Fact]
        public void AddKeyedOpenGaussDataSource_gives_each_key_its_own_data_source_and_pool()
        {
            using var provider = Build(s => s.AddKeyedOpenGaussDataSource("a", ConnectionString)
                                              .AddKeyedOpenGaussDataSource("b", ConnectionString));

            var a = provider.GetRequiredKeyedService<OpenGaussDataSource>("a");
            var b = provider.GetRequiredKeyedService<OpenGaussDataSource>("b");
            var aBase = provider.GetRequiredKeyedService<DbDataSource>("a");

            Assert.NotSame(a, b);
            Assert.Same(a, aBase);
            Assert.NotSame(a.Source, b.Source);
        }

        [Fact]
        public void AddKeyedOpenGaussDataSource_resolves_the_same_instance_per_key()
        {
            using var provider = Build(s => s.AddKeyedOpenGaussDataSource(1, ConnectionString));

            Assert.Same(provider.GetRequiredKeyedService<OpenGaussDataSource>(1), provider.GetRequiredKeyedService<OpenGaussDataSource>(1));
        }

        [Fact]
        public void AddKeyedOpenGaussDataSource_rejects_a_duplicate_key()
        {
            var services = new ServiceCollection().AddKeyedOpenGaussDataSource("a", ConnectionString);

            Assert.Throws<InvalidOperationException>(() => services.AddKeyedOpenGaussDataSource("a", ConnectionString));
        }

        [Fact]
        public void AddKeyedOpenGaussDataSource_rejects_a_null_or_any_key()
        {
            var services = new ServiceCollection();

            Assert.Throws<ArgumentNullException>(() => services.AddKeyedOpenGaussDataSource(null, ConnectionString));
            Assert.Throws<ArgumentException>(() => services.AddKeyedOpenGaussDataSource(KeyedService.AnyKey, ConnectionString));
        }


        sealed class FakeDbConnection : DbConnection
        {
            [System.Diagnostics.CodeAnalysis.AllowNull]
            public override string ConnectionString { get; set; } = string.Empty;
            public override string Database => "fake";
            public override string DataSource => "fake";
            public override string ServerVersion => "0";
            public override System.Data.ConnectionState State => System.Data.ConnectionState.Closed;
            public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();
            public override void Close() { }
            public override void Open() => throw new NotSupportedException();
            protected override DbTransaction BeginDbTransaction(System.Data.IsolationLevel isolationLevel) => throw new NotSupportedException();
            protected override DbCommand CreateDbCommand() => throw new NotSupportedException();
        }
    }
}
