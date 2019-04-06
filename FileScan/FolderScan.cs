using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileScan
{
    public class FolderScan
    {
        private static readonly string FILESCAN_DB_NAME = "filescan.db";
        private string _root_path;
        private string _db_path;
        private FileScanDb _db;

        private Dictionary<long, List<FileModel>> _potentialDuplicates;

        public int FoldersFound { get; private set; }
        public int FoldersScanned { get; private set; }
        public int FilesScanned { get; private set; }
        public FolderModel RootFolder { get; private set; }
        public DateTime LastPrintTime = DateTime.MinValue;


        public FolderScan(string root_path)
        {
            _root_path = root_path;
            _potentialDuplicates = new Dictionary<long, List<FileModel>>();

            // Assemble a connection string
            _db_path = Path.Combine(root_path, FILESCAN_DB_NAME);
            File.Delete(_db_path);
            _db = new FileScanDb($"Data Source={_db_path};Version=3;");
        }

        public async Task Begin()
        {
            RootFolder = await ScanFolder(_root_path);
        }

        public bool should_ignore(string path)
        {
            return String.Equals(path, _db_path, StringComparison.OrdinalIgnoreCase)
                || path.Contains("$RECYCLE.BIN");
        }

        public async Task<FolderModel> ScanFolder(string path)
        {
            FoldersFound++;

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
                    // Ignore the filescan DB
                    if (!should_ignore(f.FullName))
                    {
                        var file = new FileModel()
                        {
                            name = f.Name,
                            size = f.Length,
                            last_modified = f.LastWriteTime.ToString("o"),
                            hash = null
                        };
                        folder.files.Add(file);
                        TrackFile(file);
                        FilesScanned++;
                    }
                }

                // Check all subfolders
                var subfolder_tasks = (from d in this_dir.GetDirectories() where !should_ignore(d.FullName) select ScanFolder(d.FullName));
                await Task.WhenAll(subfolder_tasks.ToArray());
                foreach (var t in subfolder_tasks)
                {
                    folder.folders.Add(t.Result);
                }
            } 
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception scanning {path}: {ex.Message}.");
            }

            // Update information about this folder, then save it to the database
            folder.total_size =
                (from f in folder.files select f.size).Sum()
                + (from sub in folder.folders select folder.total_size).Sum();

            // Here's the folder we scanned
            FoldersScanned++;
            var ts = DateTime.UtcNow - LastPrintTime;
            if (ts.TotalMilliseconds > 100)
            {
                Console.Write($"\rScanned {FoldersScanned}/{FoldersFound} folders, found {FilesScanned} files...");
                LastPrintTime = DateTime.UtcNow;
            }
            return folder;
        }

        private void TrackFile(FileModel file)
        {
            // We don't care about empty files
            if (file.size > 0)
            {
                lock (_potentialDuplicates)
                {
                    List<FileModel> list = null;
                    if (!_potentialDuplicates.TryGetValue(file.size, out list))
                    {
                        list = new List<FileModel>();
                        _potentialDuplicates[file.size] = list;
                    }
                    list.Add(file);
                }
            }
        }

        public async Task PrintDuplicates()
        {
            // Print potential file duplicates
            var fn = Path.Combine(_root_path, "duplicates.txt");
            using (var sw = File.CreateText(fn))
            {
                foreach (var kvp in _potentialDuplicates)
                {
                    if (kvp.Value.Count > 1)
                    {
                        await sw.WriteLineAsync($"Potential duplicates of size {kvp.Key}:\n");
                        foreach (var f in kvp.Value)
                        {
                            await sw.WriteLineAsync($"    {f.name}");
                        }
                    }
                }
            }

            // Print directory sizes
            fn = Path.Combine(_root_path, "directories.txt");
            using (var sw = File.CreateText(fn))
            {
                await PrintFolderRecursively(RootFolder, sw);
            }
        }

        private async Task PrintFolderRecursively(FolderModel f, StreamWriter sw)
        {
            await sw.WriteLineAsync($"{f.name}: {f.total_size}");
            foreach (var child in f.folders)
            {
                await PrintFolderRecursively(child, sw);
            }
        }
    }
}
