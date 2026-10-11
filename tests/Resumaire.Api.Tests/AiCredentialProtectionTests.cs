using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Resumaire.Api.Ai;
using Resumaire.Api.Configuration;

namespace Resumaire.Api.Tests;

public sealed class AiCredentialProtectionTests
{
    [Fact]
    public void Production_RequiresProtectedPersistentKeysAndExplicitProxy()
    {
        var configuration = new ConfigurationBuilder().Build();
        var error = Assert.Throws<InvalidOperationException>(() => new ServiceCollection()
            .AddPersonalAiServices(configuration, new ProductionEnvironment()));
        Assert.Contains("DataProtection:KeyPath", error.Message);
    }

    [Fact]
    public void ProtectedCredentials_SurviveFreshHost_AndReceiptsExpire()
    {
        var temp = Path.Combine(Path.GetTempPath(), "resumaire-protection-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=Resumaire test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
            var certificatePath = Path.Combine(temp, "test.pfx");
            File.WriteAllBytes(certificatePath, certificate.Export(X509ContentType.Pfx));
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataProtection:KeyPath"] = Path.Combine(temp, "keys"), ["DataProtection:CertificatePath"] = certificatePath,
                ["Proxy:KnownProxies:0"] = "127.0.0.1"
            }).Build();
            string encrypted;
            using (var first = new ServiceCollection().AddLogging().AddPersonalAiServices(configuration, new ProductionEnvironment()).BuildServiceProvider())
                encrypted = first.GetRequiredService<AiCredentialProtector>().Protect("owner", "private-key-1234");
            var keyFile = File.ReadAllText(Directory.GetFiles(Path.Combine(temp, "keys"), "*.xml").Single());
            Assert.Contains("encryptedSecret", keyFile);
            var clock = new MutableClock();
            using var second = new ServiceCollection().AddLogging().AddPersonalAiServices(configuration, new ProductionEnvironment())
                .AddSingleton<TimeProvider>(clock).BuildServiceProvider();
            var protector = second.GetRequiredService<AiCredentialProtector>();
            Assert.Equal("private-key-1234", protector.Unprotect("owner", encrypted));
            var registry = new AiProviderRegistry();
            var connection = new AiConnection(registry.Get("openai"), registry.GetModel("openai", "gpt-4o-mini"), "private-key-1234");
            var receipt = protector.IssueReceipt("owner", connection);
            Assert.Equal(clock.GetUtcNow(), protector.VerifyReceipt("owner", connection, receipt));
            clock.Now = clock.Now.AddMinutes(11);
            Assert.Equal("test_required", Assert.Throws<AiConnectionException>(() => protector.VerifyReceipt("owner", connection, receipt)).Code);
        }
        finally
        {
            Assert.StartsWith(Path.GetFullPath(Path.GetTempPath()), Path.GetFullPath(temp));
            Directory.Delete(temp, recursive: true);
        }
    }

    private sealed class MutableClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.FromUnixTimeSeconds(1700000000);
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class ProductionEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Resumaire";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
