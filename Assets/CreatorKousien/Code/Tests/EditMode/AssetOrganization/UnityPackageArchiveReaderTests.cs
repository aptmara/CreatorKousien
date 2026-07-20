using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace CreatorKousien.Editor.AssetOrganization.Tests
{
    public sealed class UnityPackageArchiveReaderTests
    {
        [Test]
        public void Inspect_ReadsPathGuidAndContentHash()
        {
            const string guid = "1234567890abcdef1234567890abcdef";
            string packagePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".unitypackage");
            try
            {
                WritePackage(packagePath, guid, "Assets/Enemy/Bat/Bat.prefab");

                PackageInspection inspection = new UnityPackageArchiveReader().Inspect(packagePath);

                Assert.That(inspection.Assets, Has.Count.EqualTo(1));
                UnityPackageAssetRecord asset = inspection.Assets.Single();
                Assert.That(asset.SourcePath, Is.EqualTo("Assets/Enemy/Bat/Bat.prefab"));
                Assert.That(asset.Guid, Is.EqualTo(guid));
                Assert.That(asset.HasAsset, Is.True);
                Assert.That(asset.HasMeta, Is.True);
                Assert.That(asset.Sha256, Has.Length.EqualTo(64));
                Assert.That(asset.MetaSha256, Has.Length.EqualTo(64));
            }
            finally
            {
                File.Delete(packagePath);
            }
        }

        private static void WritePackage(string path, string guid, string assetPath)
        {
            using (FileStream file = File.Create(path))
            using (GZipStream gzip = new GZipStream(file, CompressionLevel.Optimal))
            {
                WriteTarEntry(gzip, guid + "/pathname", Encoding.UTF8.GetBytes(assetPath + "\n"));
                WriteTarEntry(gzip, guid + "/asset", Encoding.UTF8.GetBytes("%YAML 1.1\n"));
                WriteTarEntry(gzip, guid + "/asset.meta", Encoding.UTF8.GetBytes("fileFormatVersion: 2\nguid: " + guid + "\n"));
                gzip.Write(new byte[1024], 0, 1024);
            }
        }

        private static void WriteTarEntry(Stream stream, string name, byte[] content)
        {
            byte[] header = new byte[512];
            WriteAscii(header, 0, 100, name);
            WriteOctal(header, 100, 8, 420);
            WriteOctal(header, 108, 8, 0);
            WriteOctal(header, 116, 8, 0);
            WriteOctal(header, 124, 12, content.Length);
            WriteOctal(header, 136, 12, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            for (int i = 148; i < 156; i++)
            {
                header[i] = 32;
            }

            header[156] = (byte)'0';
            WriteAscii(header, 257, 6, "ustar");
            int checksum = header.Sum(value => value);
            string checksumText = Convert.ToString(checksum, 8).PadLeft(6, '0');
            WriteAscii(header, 148, 6, checksumText);
            header[154] = 0;
            header[155] = 32;

            stream.Write(header, 0, header.Length);
            stream.Write(content, 0, content.Length);
            int padding = (512 - (content.Length % 512)) % 512;
            if (padding > 0)
            {
                stream.Write(new byte[padding], 0, padding);
            }
        }

        private static void WriteAscii(byte[] buffer, int offset, int length, string value)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(value);
            Array.Copy(bytes, 0, buffer, offset, Math.Min(length, bytes.Length));
        }

        private static void WriteOctal(byte[] buffer, int offset, int length, long value)
        {
            string text = Convert.ToString(value, 8).PadLeft(length - 1, '0');
            WriteAscii(buffer, offset, length - 1, text);
            buffer[offset + length - 1] = 0;
        }
    }
}
