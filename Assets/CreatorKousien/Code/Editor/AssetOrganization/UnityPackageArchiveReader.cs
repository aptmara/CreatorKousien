using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CreatorKousien.Editor.AssetOrganization
{
    public sealed class UnityPackageArchiveReader
    {
        private const int TarBlockSize = 512;

        public PackageInspection Inspect(string packagePath)
        {
            if (string.IsNullOrWhiteSpace(packagePath) || !File.Exists(packagePath))
            {
                throw new FileNotFoundException("UnityPackage was not found.", packagePath);
            }

            Dictionary<string, RecordBuilder> builders = new Dictionary<string, RecordBuilder>(StringComparer.Ordinal);
            ReadArchive(packagePath, (entryName, size, stream) =>
            {
                string normalizedName = entryName.Replace('\\', '/').Trim('/');
                int separatorIndex = normalizedName.IndexOf('/');
                if (separatorIndex <= 0)
                {
                    Drain(stream);
                    return;
                }

                string archiveId = normalizedName.Substring(0, separatorIndex);
                string leafName = normalizedName.Substring(separatorIndex + 1);
                if (!builders.TryGetValue(archiveId, out RecordBuilder builder))
                {
                    builder = new RecordBuilder(archiveId);
                    builders.Add(archiveId, builder);
                }

                switch (leafName)
                {
                    case "pathname":
                        builder.SourcePath = ReadText(stream).TrimEnd('\0', '\r', '\n');
                        break;
                    case "asset.meta":
                        builder.HasMeta = true;
                        byte[] metaBytes = ReadBytes(stream);
                        builder.MetaText = Encoding.UTF8.GetString(metaBytes);
                        builder.MetaSha256 = ComputeSha256(metaBytes);
                        break;
                    case "asset":
                        builder.HasAsset = true;
                        builder.AssetSize = size;
                        using (SHA256 sha256 = SHA256.Create())
                        {
                            builder.Sha256 = ToHex(sha256.ComputeHash(stream));
                        }

                        break;
                    default:
                        Drain(stream);
                        break;
                }
            });

            List<UnityPackageAssetRecord> records = builders.Values
                .Select(builder => builder.Build())
                .Where(record => !string.IsNullOrWhiteSpace(record.SourcePath))
                .OrderBy(record => record.SourcePath, StringComparer.Ordinal)
                .ToList();

            return new PackageInspection
            {
                PackagePath = packagePath,
                Assets = records,
            };
        }

        public void Extract(PackageInspection inspection, string destinationRoot)
        {
            Extract(inspection, destinationRoot, inspection?.Assets);
        }

        public void Extract(
            PackageInspection inspection,
            string destinationRoot,
            IEnumerable<UnityPackageAssetRecord> selectedRecords)
        {
            if (inspection == null)
            {
                throw new ArgumentNullException(nameof(inspection));
            }

            string projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName
                ?? throw new InvalidOperationException("Project root could not be resolved.");
            string normalizedRoot = destinationRoot.Replace('\\', '/').TrimEnd('/');
            if (!normalizedRoot.StartsWith("Assets/_Incoming/", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("UnityPackage extraction is restricted to Assets/_Incoming.");
            }

            string absoluteRoot = Path.GetFullPath(Path.Combine(projectRoot, normalizedRoot));
            string incomingRoot = Path.GetFullPath(Path.Combine(projectRoot, "Assets/_Incoming"));
            if (!IsUnderDirectory(absoluteRoot, incomingRoot))
            {
                throw new InvalidOperationException("Extraction path escaped Assets/_Incoming.");
            }

            Dictionary<string, UnityPackageAssetRecord> records = (selectedRecords ?? Enumerable.Empty<UnityPackageAssetRecord>())
                .ToDictionary(record => record.ArchiveId, StringComparer.Ordinal);

            ReadArchive(inspection.PackagePath, (entryName, entrySize, stream) =>
            {
                string normalizedName = entryName.Replace('\\', '/').Trim('/');
                int separatorIndex = normalizedName.IndexOf('/');
                if (separatorIndex <= 0)
                {
                    Drain(stream);
                    return;
                }

                string archiveId = normalizedName.Substring(0, separatorIndex);
                string leafName = normalizedName.Substring(separatorIndex + 1);
                if (!records.TryGetValue(archiveId, out UnityPackageAssetRecord record))
                {
                    Drain(stream);
                    return;
                }

                if (!PackagePathUtility.TryNormalizeAssetPath(record.SourcePath, out string sourcePath, out _))
                {
                    throw new InvalidDataException($"Invalid package pathname: {record.SourcePath}");
                }

                string relativePath = sourcePath == "Assets" ? string.Empty : sourcePath.Substring("Assets/".Length);
                string destination = Path.GetFullPath(Path.Combine(absoluteRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
                if (!IsUnderDirectory(destination, absoluteRoot) && !string.Equals(destination, absoluteRoot, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException($"Package entry escaped its extraction root: {record.SourcePath}");
                }

                if (leafName == "asset")
                {
                    WriteEntry(destination, stream);
                }
                else if (leafName == "asset.meta")
                {
                    WriteEntry(destination + ".meta", stream);
                }
                else
                {
                    Drain(stream);
                }
            });
        }

        public string ExtractForComparison(
            PackageInspection inspection,
            string comparisonName,
            IEnumerable<UnityPackageAssetRecord> selectedRecords)
        {
            if (inspection == null)
            {
                throw new ArgumentNullException(nameof(inspection));
            }

            string projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName
                ?? throw new InvalidOperationException("Project root could not be resolved.");
            string safeName = PackagePathUtility.SanitizeSegment(comparisonName, "Package");
            string relativeRoot = $"Library/CreatorKousien/PackageComparisons/{safeName}";
            string absoluteRoot = Path.GetFullPath(Path.Combine(projectRoot, relativeRoot));
            string comparisonRoot = Path.GetFullPath(Path.Combine(projectRoot, "Library/CreatorKousien/PackageComparisons"));
            if (!IsUnderDirectory(absoluteRoot, comparisonRoot))
            {
                throw new InvalidOperationException("Comparison path escaped its allowed directory.");
            }

            ExtractArchiveEntries(inspection, absoluteRoot, selectedRecords);
            return absoluteRoot;
        }

        private static void ExtractArchiveEntries(
            PackageInspection inspection,
            string absoluteRoot,
            IEnumerable<UnityPackageAssetRecord> selectedRecords)
        {
            Dictionary<string, UnityPackageAssetRecord> records = (selectedRecords ?? Enumerable.Empty<UnityPackageAssetRecord>())
                .ToDictionary(record => record.ArchiveId, StringComparer.Ordinal);

            ReadArchive(inspection.PackagePath, (entryName, entrySize, stream) =>
            {
                string normalizedName = entryName.Replace('\\', '/').Trim('/');
                int separatorIndex = normalizedName.IndexOf('/');
                if (separatorIndex <= 0)
                {
                    Drain(stream);
                    return;
                }

                string archiveId = normalizedName.Substring(0, separatorIndex);
                string leafName = normalizedName.Substring(separatorIndex + 1);
                if (!records.TryGetValue(archiveId, out UnityPackageAssetRecord record))
                {
                    Drain(stream);
                    return;
                }

                if (!PackagePathUtility.TryNormalizeAssetPath(record.SourcePath, out string sourcePath, out _))
                {
                    throw new InvalidDataException($"Invalid package pathname: {record.SourcePath}");
                }

                string relativePath = sourcePath == "Assets" ? string.Empty : sourcePath.Substring("Assets/".Length);
                string destination = Path.GetFullPath(Path.Combine(absoluteRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
                if (!IsUnderDirectory(destination, absoluteRoot) && !string.Equals(destination, absoluteRoot, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException($"Package entry escaped its extraction root: {record.SourcePath}");
                }

                if (leafName == "asset")
                {
                    WriteEntry(destination, stream);
                }
                else if (leafName == "asset.meta")
                {
                    WriteEntry(destination + ".meta", stream);
                }
                else
                {
                    Drain(stream);
                }
            });
        }

        private static void WriteEntry(string path, Stream source)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(path))
            {
                throw new IOException($"Extraction would overwrite an existing file: {path}");
            }

            using (FileStream destination = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                source.CopyTo(destination);
            }
        }

        private static bool IsUnderDirectory(string path, string directory)
        {
            string prefix = directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        private static string ReadText(Stream stream)
        {
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true))
            {
                return reader.ReadToEnd();
            }
        }

        private static byte[] ReadBytes(Stream stream)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                stream.CopyTo(memory);
                return memory.ToArray();
            }
        }

        private static string ComputeSha256(byte[] bytes)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return ToHex(sha256.ComputeHash(bytes));
            }
        }

        private static void Drain(Stream stream)
        {
            byte[] buffer = new byte[81920];
            while (stream.Read(buffer, 0, buffer.Length) > 0)
            {
            }
        }

        private static string ToHex(byte[] bytes)
        {
            StringBuilder builder = new StringBuilder(bytes.Length * 2);
            foreach (byte value in bytes)
            {
                builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        private static void ReadArchive(string packagePath, Action<string, long, Stream> visitor)
        {
            using (FileStream file = File.OpenRead(packagePath))
            using (GZipStream gzip = new GZipStream(file, CompressionMode.Decompress))
            {
                byte[] header = new byte[TarBlockSize];
                while (ReadBlock(gzip, header))
                {
                    if (IsZeroBlock(header))
                    {
                        break;
                    }

                    string name = ReadNullTerminatedString(header, 0, 100);
                    string prefix = ReadNullTerminatedString(header, 345, 155);
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        name = prefix + "/" + name;
                    }

                    long size = ParseOctal(header, 124, 12);
                    using (LimitedReadStream entryStream = new LimitedReadStream(gzip, size))
                    {
                        visitor(name, size, entryStream);
                        Drain(entryStream);
                    }

                    long padding = (TarBlockSize - (size % TarBlockSize)) % TarBlockSize;
                    SkipExactly(gzip, padding);
                }
            }
        }

        private static bool ReadBlock(Stream stream, byte[] buffer)
        {
            int offset = 0;
            while (offset < buffer.Length)
            {
                int read = stream.Read(buffer, offset, buffer.Length - offset);
                if (read == 0)
                {
                    if (offset == 0)
                    {
                        return false;
                    }

                    throw new EndOfStreamException("Unexpected end of tar header.");
                }

                offset += read;
            }

            return true;
        }

        private static bool IsZeroBlock(byte[] block)
        {
            for (int i = 0; i < block.Length; i++)
            {
                if (block[i] != 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static string ReadNullTerminatedString(byte[] buffer, int offset, int count)
        {
            int length = 0;
            while (length < count && buffer[offset + length] != 0)
            {
                length++;
            }

            return Encoding.UTF8.GetString(buffer, offset, length).Trim();
        }

        private static long ParseOctal(byte[] buffer, int offset, int count)
        {
            string value = ReadNullTerminatedString(buffer, offset, count).Trim();
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            return Convert.ToInt64(value, 8);
        }

        private static void SkipExactly(Stream stream, long count)
        {
            byte[] buffer = new byte[4096];
            long remaining = count;
            while (remaining > 0)
            {
                int read = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                if (read == 0)
                {
                    throw new EndOfStreamException("Unexpected end of tar padding.");
                }

                remaining -= read;
            }
        }

        private sealed class RecordBuilder
        {
            public RecordBuilder(string archiveId)
            {
                ArchiveId = archiveId;
            }

            public string ArchiveId { get; }
            public string SourcePath { get; set; }
            public string MetaText { get; set; }
            public long AssetSize { get; set; }
            public string Sha256 { get; set; }
            public string MetaSha256 { get; set; }
            public bool HasAsset { get; set; }
            public bool HasMeta { get; set; }

            public UnityPackageAssetRecord Build()
            {
                return new UnityPackageAssetRecord
                {
                    ArchiveId = ArchiveId,
                    SourcePath = SourcePath,
                    Guid = ParseGuid(MetaText) ?? ArchiveId,
                    AssetSize = AssetSize,
                    Sha256 = Sha256,
                    MetaSha256 = MetaSha256,
                    HasAsset = HasAsset,
                    HasMeta = HasMeta,
                };
            }

            private static string ParseGuid(string metaText)
            {
                if (string.IsNullOrEmpty(metaText))
                {
                    return null;
                }

                using (StringReader reader = new StringReader(metaText))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("guid: ", StringComparison.Ordinal))
                        {
                            return line.Substring("guid: ".Length).Trim();
                        }
                    }
                }

                return null;
            }
        }

        private sealed class LimitedReadStream : Stream
        {
            private readonly Stream _inner;
            private long _remaining;

            public LimitedReadStream(Stream inner, long length)
            {
                _inner = inner;
                _remaining = length;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_remaining == 0)
                {
                    return 0;
                }

                int read = _inner.Read(buffer, offset, (int)Math.Min(count, _remaining));
                if (read == 0)
                {
                    throw new EndOfStreamException("Unexpected end of tar entry.");
                }

                _remaining -= read;
                return read;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    Drain(this);
                }

                base.Dispose(disposing);
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush()
            {
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
