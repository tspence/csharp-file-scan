using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FileScan
{
    public class FolderScan
    {
        public static int FoldersScanned = 0;
        public static int FilesScanned = 0;
        public static DateTime LastPrintTime = DateTime.MinValue;

        public static FolderModel ScanFolder(string path)
        {
            // Insert this folder
            var folder = new FolderModel()
            {
                name = path,
                files = new List<FileModel>(),
                folders = new List<FolderModel>()
            };

            // Scan and capture problems
            try
            {
                var this_dir = new DirectoryInfo(path);

                // Insert all files
                foreach (var f in this_dir.GetFiles())
                {
                    folder.files.Add(new FileModel()
                    {
                        name = f.Name,
                        size = f.Length,
                        last_modified = f.LastWriteTime.ToString("o"),
                        hash = ""
                    });
                    FilesScanned++;
                }

                // Insert subfolders
                foreach (var d in this_dir.GetDirectories())
                {
                    folder.folders.Add(ScanFolder(d.FullName));
                }
            } 
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception scanning {path}: {ex.Message}.");
            }

            // Here's the folder we scanned
            FoldersScanned++;
            var ts = DateTime.UtcNow - LastPrintTime;
            if (ts.TotalMilliseconds > 100)
            {
                Console.Write($"\rScanned {FoldersScanned} folders, {FilesScanned} files...");
                LastPrintTime = DateTime.UtcNow;
            }
            return folder;
        }
    }
}
