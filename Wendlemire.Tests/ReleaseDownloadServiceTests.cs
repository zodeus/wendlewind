using Wendlemire.NetCode;
using Xunit;

namespace Wendlemire.Tests;

public class ReleaseDownloadServiceTests
{
    [Fact]
    public void ServesLocalZipAndVersionFromManifest()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ww-dl-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(dir);
            var zip = Path.Combine(dir, "Wendlemire-0.1-win-x64.zip");
            File.WriteAllText(zip, "zip");
            File.WriteAllText(
                Path.Combine(dir, "latest.json"),
                """{"version":"0.1","files":{"win-x64":"Wendlemire-0.1-win-x64.zip"}}""");

            var service = new ReleaseDownloadService(dir);
            var file = service.Find(ReleaseDownloadService.WinX64);
            Assert.NotNull(file);
            Assert.Equal("Wendlemire-0.1-win-x64.zip", file.FileName);
            Assert.Null(service.Find(ReleaseDownloadService.OsxArm));

            var catalog = service.Catalog(true);
            Assert.True(catalog.Unlocked);
            Assert.Equal("0.1", catalog.Version);
            Assert.Single(catalog.Assets);
            Assert.Equal(ReleaseDownloadService.WinX64, catalog.Assets[0].Id);
            Assert.Null(catalog.Error);

            Assert.False(service.Catalog(false).Unlocked);
            Assert.Empty(service.Catalog(false).Assets);
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }

    [Fact]
    public void DiscoversZipWhenManifestIsMissing()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ww-dl-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "Wendlemire-0.2-osx-arm64.zip"), "zip");

            var service = new ReleaseDownloadService(dir);
            var file = service.Find(ReleaseDownloadService.OsxArm);
            Assert.NotNull(file);
            Assert.Equal("0.2", service.Catalog(true).Version);
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
