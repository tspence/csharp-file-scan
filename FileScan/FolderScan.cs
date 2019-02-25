using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FileScan
{
    public class FolderScan
    {
        public static FolderModel ScanFolder(string path)
        {
            var this_dir = new DirectoryInfo(path);
            Console.Write($"Scanning {path}...");

            // Insert this folder
            var folder = new FolderModel()
            {
                name = path,
                files = new List<FileModel>(),
                folders = new List<FolderModel>()
            };

            // Insert all files
            foreach (var f in this_dir.GetFiles())
            {
                folder.files.Add(new FileModel()
                {
                    name = f.Name,
                    size = f.Length,
                    last_modified = f.LastWriteTime.ToString(),
                    hash = ""
                });
            }

            // Insert subfolders
            foreach (var d in this_dir.GetDirectories())
            {
                folder.folders.Add(ScanFolder(d.FullName));
            }

            // Here you go
            Console.WriteLine($"{folder.files.Count} files.");
            return folder;
        }
    }
}
