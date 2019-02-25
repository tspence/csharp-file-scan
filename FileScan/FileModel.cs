using System;
using System.Collections.Generic;
using System.Text;

namespace FileScan
{
    public class FileModel
    {
        public long id { get; set; }
        public string name { get; set; }
        public long parent_folder_id { get; set; }
        public long size { get; set; }
        public string hash { get; set; }
        public string last_modified { get; set; }
    }
}
