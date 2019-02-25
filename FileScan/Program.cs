using System;

namespace FileScan
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var folder = FolderScan.ScanFolder("f:\\git");
                var db = new FileScanDb("Data Source=filescan.db;Version=3;");
                db.Write(folder).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
