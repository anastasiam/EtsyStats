namespace EtsyStats.Helpers;

public class DirectoryHelper
{
    public void CopyAll(string sourceDirectory, string targetDirectory)
    {
        var sourceDirectoryInfo = new DirectoryInfo(sourceDirectory);
        var targetDirectoryInfo = new DirectoryInfo(targetDirectory);

        CopyAll(sourceDirectoryInfo, targetDirectoryInfo);
    }

    private void CopyAll(DirectoryInfo source, DirectoryInfo target)
    {
        Directory.CreateDirectory(target.FullName);

        foreach (var fi in source.GetFiles())
        {
            fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
        }

        foreach (var diSourceSubDir in source.GetDirectories())
        {
            var nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
            CopyAll(diSourceSubDir, nextTargetSubDir);
        }
    }
}