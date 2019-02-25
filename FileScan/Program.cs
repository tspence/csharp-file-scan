using System;

namespace FileScan
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Scan the folder
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                var folder = FolderScan.ScanFolder("/users/tspence/fbsource");
                var total_items = folder.TotalItems();
                sw.Stop();
                Console.WriteLine($"\rFinished scanning {total_items} items in {sw.Elapsed.ToString()}.");

                // Connect to the database
                var db = new FileScanDb("Data Source=filescan.db;Version=3;");

                // Insert into the clean, empty database
                sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                db.Write(folder).Wait();
                sw.Stop();
                Console.WriteLine($"\rFinished inserting {total_items} items in {sw.Elapsed.ToString()}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
