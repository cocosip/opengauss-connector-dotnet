using System;
using NUnit.Framework;
using OpenGaussConnector.Internal;

namespace OpenGaussConnector.Tests
{
    /// <summary>
    /// Identification of the openGauss product version and validation of the Server Version setting.
    /// Needs no server: everything here is parsing and configuration.
    /// </summary>
    class ServerVersionTests
    {
        [Test]
        public void Parses_product_version_without_a_postgresql_prefix()
            => Assert.That(
                OpenGaussServerVersion.TryParseProductVersion(
                    "(openGauss 6.0.6 build 25f7a4d0) compiled at 2024-03-30 11:00:00 commit 0 last mr "),
                Is.EqualTo(new Version(6, 0, 6)));

        [Test]
        public void Parses_product_version_after_a_postgresql_prefix()
            => Assert.That(
                OpenGaussServerVersion.TryParseProductVersion(
                    "PostgreSQL 9.2.4 (openGauss 7.0.0 build 6a35e83b) on x86_64-pc-linux-gnu, compiled by gcc"),
                Is.EqualTo(new Version(7, 0, 0)));

        [Test]
        public void Parses_a_two_component_product_version()
            => Assert.That(
                OpenGaussServerVersion.TryParseProductVersion("openGauss 6.0 build 1"),
                Is.EqualTo(new Version(6, 0)));

        [TestCase("PostgreSQL 14.2 on x86_64-pc-linux-gnu, compiled by gcc")]
        [TestCase("openGauss")]
        [TestCase("openGauss build 25f7a4d0")]
        [TestCase("")]
        [TestCase(null)]
        public void Does_not_invent_a_product_version(string? longVersion)
            => Assert.That(OpenGaussServerVersion.TryParseProductVersion(longVersion), Is.Null);

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Auto")]
        [TestCase("auto")]
        public void Auto_means_identify_the_server(string? configured)
            => Assert.That(OpenGaussServerVersion.ParseConfigured(configured, "Server Version"), Is.Null);

        [TestCase("6.0.6", 6, 0, 6)]
        [TestCase("7.0", 7, 0, -1)]
        [TestCase(" 5.0.0 ", 5, 0, 0)]
        public void Parses_a_configured_version(string configured, int major, int minor, int build)
            => Assert.That(
                OpenGaussServerVersion.ParseConfigured(configured, "Server Version"),
                Is.EqualTo(build < 0 ? new Version(major, minor) : new Version(major, minor, build)));

        [TestCase("6")]
        [TestCase("not-a-version")]
        [TestCase("6.x")]
        public void Rejects_a_configured_value_that_is_not_a_version(string configured)
            => Assert.That(
                () => OpenGaussServerVersion.ParseConfigured(configured, "Server Version"),
                Throws.ArgumentException.With.Message.Contains("Server Version"));

        [TestCase(6, 0, 6, 6, 0, 6, true)]
        [TestCase(6, 0, 6, 6, 0, 5, false)]
        [TestCase(6, 0, 6, 7, 0, 0, false)]
        [TestCase(6, 0, -1, 6, 0, 6, true)]
        [TestCase(6, 0, -1, 6, 1, 0, false)]
        public void Compares_only_the_components_that_were_configured(
            int major, int minor, int build, int productMajor, int productMinor, int productBuild, bool compatible)
        {
            var configured = build < 0 ? new Version(major, minor) : new Version(major, minor, build);
            var product = new Version(productMajor, productMinor, productBuild);

            Assert.That(OpenGaussServerVersion.IsCompatible(configured, product), Is.EqualTo(compatible));
        }

        [Test]
        public void An_unknown_product_version_accepts_any_configuration()
            => Assert.That(OpenGaussServerVersion.IsCompatible(new Version(6, 0, 6), null), Is.True);

        [Test]
        public void Connection_string_round_trips_the_version()
        {
            var builder = new OpenGaussConnectionStringBuilder { Host = "h", ServerVersion = "6.0.6" };

            Assert.That(builder.ServerVersion, Is.EqualTo("6.0.6"));
            Assert.That(builder.ServerVersionParsed, Is.EqualTo(new Version(6, 0, 6)));
            Assert.That(builder.ConnectionString, Does.Contain("Server Version=6.0.6"));
            Assert.That(new OpenGaussConnectionStringBuilder(builder.ConnectionString).ServerVersion, Is.EqualTo("6.0.6"));
        }

        [Test]
        public void Connection_string_keeps_auto_out_of_the_canonical_form()
        {
            var builder = new OpenGaussConnectionStringBuilder { Host = "h", ServerVersion = "Auto" };

            Assert.That(builder.ServerVersion, Is.Null);
            Assert.That(builder.ConnectionString, Does.Not.Contain("Server Version"));
        }

        [Test]
        public void Connection_string_rejects_an_invalid_version()
            => Assert.That(
                () => new OpenGaussConnectionStringBuilder { ServerVersion = "latest" },
                Throws.ArgumentException.With.Message.Contains("Server Version"));
    }
}
