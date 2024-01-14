using ICSharpCode.SharpZipLib.Zip;

namespace Define.Zip;

/// <summary>
/// Extension methods for <see cref="DefDatabase"/>
/// that add support for encrypted ZIP files.
/// </summary>
public static class ZipFileMethods
{
    /// <summary>
    /// Loads all def (.xml) files from inside the ZIP file at the specified path.
    /// The ZIP file is assumed to be encrypted and can be decrypted by the supplied <paramref name="password"/>.
    /// </summary>
    /// <param name="db">The def database.</param>
    /// <param name="zipFilePath">The file path of the ZIP file.</param>
    /// <param name="password">The password that was used to encrypt the zip file. If null or empty, the zip file is assumed to be un-encrypted.</param>
    /// <returns>True if adding the defs was successful, false otherwise.</returns>    
    public static bool AddDefsFromZip(this DefDatabase db, string zipFilePath, string password)
    {
        // If not using a password the regular method can be used.
        if (string.IsNullOrEmpty(password))
            return db.AddDefsFromZip(zipFilePath);
        
        if (!File.Exists(zipFilePath))
        {
            DefDebugger.Error($"Failed to find zip file at '{zipFilePath}'");
            return false;
        }

        bool success = true;
        try
        {
            using var fs = File.OpenRead(zipFilePath);
            using var zip = new ZipFile(fs);
            zip.Password = password;

            foreach (ZipEntry entry in zip)
            {
                if (!entry.IsFile)
                    continue;

                if (!entry.Name.EndsWith(".xml"))
                    continue;
                
                using var zipStream = zip.GetInputStream(entry);
                bool worked = db.AddDefDocument(zipStream, entry.Name);
                if (!worked)
                    success = false;
            }

            return success;
        }
        catch (Exception e)
        {
            DefDebugger.Error($"Expected exception when reading from encrypted def zip file:", e);
            return false;
        }
    }
    
    /// <summary>
    /// Loads all def (.xml) files from inside the ZIP file at the specified path.
    /// The ZIP file is assumed to be encrypted and can be decrypted by the supplied <paramref name="password"/>.
    /// </summary>
    /// <param name="db">The def database.</param>
    /// <param name="zipFilePath">The file path of the ZIP file.</param>
    /// <param name="password">The password that was used to encrypt the zip file. If null or empty, the zip file is assumed to be un-encrypted.</param>
    /// <returns>True if adding the defs was successful, false otherwise.</returns>    
    public static async Task<bool> AddDefsFromZipAsync(this DefDatabase db, string zipFilePath, string password)
    {
        // If not using a password the regular method can be used.
        if (string.IsNullOrEmpty(password))
            return await db.AddDefsFromZipAsync(zipFilePath);
        
        if (!File.Exists(zipFilePath))
        {
            DefDebugger.Error($"Failed to find zip file at '{zipFilePath}'");
            return false;
        }

        bool success = true;
        try
        {
            await using var fs = File.OpenRead(zipFilePath);
            using var zip = new ZipFile(fs);
            zip.Password = password;

            foreach (ZipEntry entry in zip)
            {
                if (!entry.IsFile)
                    continue;

                if (!entry.Name.EndsWith(".xml"))
                    continue;

                await using var zipStream = zip.GetInputStream(entry);
                bool worked = await db.AddDefDocumentAsync(zipStream, entry.Name);
                if (!worked)
                    success = false;
            }

            return success;
        }
        catch (Exception e)
        {
            DefDebugger.Error($"Expected exception when reading from encrypted def zip file:", e);
            return false;
        }
    }
}