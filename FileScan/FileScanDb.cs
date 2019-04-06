using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SQLite;
using Dapper;
using System.Threading.Tasks;
using System.Linq;

namespace FileScan
{
    public class FileScanDb
    {
        string _connection_string;
        public FileScanDb(string connstr)
        {
            _connection_string = connstr;

            // Setup tables
            using (var conn = new SQLiteConnection(_connection_string))
            {
                conn.Execute("drop table if exists folders;");
                conn.Execute("drop table if exists files;");
                conn.Execute(@"create table if not exists folders (
                    id integer primary key not null,
                    parent_folder_id integer not null,
                    total_size integer null,
                    name text not null
                ); ");
                conn.Execute(@"create table if not exists files (
                    id integer primary key not null,
                    parent_folder_id integer not null,
                    name text not null,
                    hash text not null,
                    size integer not null,
                    last_modified text not null
                );");
            }
        }

        /// <summary>
        /// Write just this folder and its files to the DB
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public async Task Write(FolderModel folder)
        {
            using (var conn = new SQLiteConnection(_connection_string))
            {
                conn.Open();
                var tran = conn.BeginTransaction();
                try
                {
                    // Insert the folder
                    var result = await conn.QueryAsync<long>("INSERT OR IGNORE INTO folders "
                        + "(name, total_size, parent_folder_id) "
                        + "VALUES (@name, @total_size, @parent_folder_id); "
                        + "SELECT last_insert_rowid();", folder, tran, null, System.Data.CommandType.Text);
                    folder.id = result.FirstOrDefault();

                    // Assign the ID to all children and insert them
                    foreach (var file in folder.files)
                    {
                        file.parent_folder_id = folder.id;
                        var result2 = await conn.QueryAsync<long>("INSERT OR IGNORE INTO files "
                        + "(name, parent_folder_id, size, hash, last_modified) "
                        + "VALUES (@name, @parent_folder_id, @size, @hash, @last_modified);"
                        + "SELECT last_insert_rowid();", file, tran, null, System.Data.CommandType.Text);
                        file.id = result2.FirstOrDefault();
                    }

                    // Update all the child folders
                    foreach (var subfolder in folder.folders)
                    {
                        await conn.ExecuteAsync("UPDATE folders SET parent_folder_id = @parent_folder_id WHERE id = @id;",
                            new { parent_folder_id = folder.id, id = subfolder.id }, tran, null, System.Data.CommandType.Text);
                    }

                    // We're done
                    tran.Commit();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    tran.Rollback();
                }
            }
        }
    }
}
