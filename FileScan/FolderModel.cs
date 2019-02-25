using System;
using System.Collections.Generic;
using System.Text;

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
    }
}
