using Anthropometry.Application.Profiles;
using Microsoft.Maui.Storage;
using System.Buffers;

namespace Anthropometry.App.Features.Profiles;

public sealed class MauiProfileTransferFileService : IProfileTransferFileService
{
    private const string ExportPrefix = "anthropometry-";

    public async Task ShareAsync(ProfileExportFile file, string title, CancellationToken cancellationToken)
    {
        CleanupPreviousExports();
        var path = Path.Combine(FileSystem.CacheDirectory, Path.GetFileName(file.FileName));
        await File.WriteAllBytesAsync(path, file.Content, cancellationToken);
        await Share.Default.RequestAsync(
                new ShareFileRequest
                {
                    Title = title,
                    File = new ShareFile(path)
                })
            .WaitAsync(cancellationToken);
    }

    public async Task<ProfileTransferFile?> PickTransferAsync(string title, CancellationToken cancellationToken)
    {
        var result = await FilePicker.Default.PickAsync(
                new PickOptions
                {
                    PickerTitle = title,
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        [DevicePlatform.Android] = ["application/octet-stream", ".anthropometry", "text/csv", ".csv"]
                    })
                })
            .WaitAsync(cancellationToken);
        if (result is null)
        {
            return null;
        }

        await using var source = await result.OpenReadAsync();
        var buffer = ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            using var copy = new MemoryStream();
            while (true)
            {
                var read = await source.ReadAsync(buffer.AsMemory(), cancellationToken);
                if (read == 0)
                {
                    break;
                }

                if (copy.Length + read > ProfileTransferProtection.MaxProtectedFileBytes)
                {
                    throw new InvalidDataException("The profile transfer file is too large.");
                }

                copy.Write(buffer, 0, read);
            }

            return new ProfileTransferFile(result.FileName, copy.ToArray());
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }

    private static void CleanupPreviousExports()
    {
        foreach (var path in Directory.EnumerateFiles(FileSystem.CacheDirectory, $"{ExportPrefix}*.anthropometry")
            .Concat(Directory.EnumerateFiles(FileSystem.CacheDirectory, $"{ExportPrefix}*.csv")))
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
                // A file may still be held by the Android Sharesheet.
            }
            catch (UnauthorizedAccessException)
            {
                // Cache cleanup is best-effort and must not block a new export.
            }
        }
    }
}
