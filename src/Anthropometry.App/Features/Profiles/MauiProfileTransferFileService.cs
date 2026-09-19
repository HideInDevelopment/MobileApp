using Anthropometry.Application.Profiles;
using Microsoft.Maui.Storage;

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

    public async Task<Stream?> PickCsvAsync(string title, CancellationToken cancellationToken)
    {
        var result = await FilePicker.Default.PickAsync(
                new PickOptions
                {
                    PickerTitle = title,
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        [DevicePlatform.Android] = ["text/csv", ".csv"]
                    })
                })
            .WaitAsync(cancellationToken);
        if (result is null)
        {
            return null;
        }

        await using var source = await result.OpenReadAsync();
        var copy = new MemoryStream();
        await source.CopyToAsync(copy, cancellationToken);
        copy.Position = 0;
        return copy;
    }

    private static void CleanupPreviousExports()
    {
        foreach (var path in Directory.EnumerateFiles(FileSystem.CacheDirectory, $"{ExportPrefix}*.csv"))
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
