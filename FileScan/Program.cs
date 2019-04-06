using System;
using System.Threading.Tasks;

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
                var scan = new FolderScan("f:\\");
                scan.Begin().Wait();
                scan.PrintDuplicates().Wait();

                // Print out results
                var total_items = scan.RootFolder.TotalItems();
                sw.Stop();
                Console.WriteLine($"\rFinished scanning {total_items} items in {sw.Elapsed.ToString()}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
