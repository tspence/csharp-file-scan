using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace FileScan
{
    public class FolderModel
    {
        public long id { get; set; }
        public string name { get; set; }
        public long parent_folder_id { get; set; }

        /// <summary>
        /// Child files within this folder
        /// </summary>
        public List<FileModel> files { get; set; }

        /// <summary>
        /// Child folders within this folder
        /// </summary>
        public List<FolderModel> folders { get; set; }

        /// <summary>
        /// Total all files and folders inside this thing
        /// </summary>
        /// <returns>The items.</returns>
        public int TotalItems()
        {
            return 1 + files.Count + (from f in folders select f.TotalItems()).Sum();
        }

    }
}
