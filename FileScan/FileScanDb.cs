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

        public async Task Write(FolderModel folder)
        {
            using (var conn = new SQLiteConnection(_connection_string))
            {
                conn.Open();
                var tran = conn.BeginTransaction();
                try
                {
                    // Keep track of what folders we have to insert
                    Queue<FolderModel> queue = new Queue<FolderModel>();
                    queue.Enqueue(folder);

                    // Create a folder insert command
                    var folder_cmd = conn.CreateCommand();
                    folder_cmd.CommandText = "INSERT OR IGNORE INTO folders "
                        + "(name, parent_folder_id) "
                        + "VALUES (@name, @parent_folder_id); "
                        + "SELECT last_insert_rowid();";
                    folder_cmd.Parameters.AddWithValue("@name", "");
                    folder_cmd.Parameters.AddWithValue("@parent_folder_id", 0);

                    // Create a file insert command
                    var file_cmd = conn.CreateCommand();
                    file_cmd.CommandText = "INSERT OR IGNORE INTO files "
                        + "(name, parent_folder_id, size, hash, last_modified) "
                        + "VALUES (@name, @parent_folder_id, @size, @hash, @last_modified)";
                    file_cmd.Parameters.AddWithValue("@name", "");
                    file_cmd.Parameters.AddWithValue("@parent_folder_id", 0);
                    file_cmd.Parameters.AddWithValue("@size", 0);
                    file_cmd.Parameters.AddWithValue("@hash", "");
                    file_cmd.Parameters.AddWithValue("@last_modified", "");

                    // Start processing them as fast as possible
                    while (queue.Count > 0)
                    {
                        var current_folder = queue.Dequeue();
                        Console.Write($"\rInserting {queue.Count} items into database...");

                        // Insert this folder and get its ID
                        folder_cmd.Parameters["@name"].Value = current_folder.name;
                        folder_cmd.Parameters["@parent_folder_id"].Value = current_folder.parent_folder_id;

                        // Try to insert file
                        try
                        {
                            current_folder.id = (long)(await folder_cmd.ExecuteScalarAsync());
                        } catch (Exception e1)
                        {
                            System.Diagnostics.Debug.WriteLine("Exception: " + e1.ToString());
                        }

                        // Assign the ID to all children
                        foreach (var f in current_folder.files)
                        {
                            f.parent_folder_id = current_folder.id;
                            file_cmd.Parameters["@name"].Value = f.name;
                            file_cmd.Parameters["@parent_folder_id"].Value = f.parent_folder_id;
                            file_cmd.Parameters["@size"].Value = f.size;
                            file_cmd.Parameters["@hash"].Value = f.hash;
                            file_cmd.Parameters["@last_modified"].Value = f.last_modified;

                            // Try to insert file
                            try
                            {
                                await file_cmd.ExecuteNonQueryAsync();
                            }
                            catch (Exception e2)
                            {
                                System.Diagnostics.Debug.WriteLine("Exception: " + e2.ToString());
                            }
                            current_folder.id = (long)(await folder_cmd.ExecuteScalarAsync());
                        }

                        // Queue up all the child folders
                        foreach (var f in current_folder.folders)
                        {
                            f.parent_folder_id = current_folder.id;
                            queue.Enqueue(f);
                        }
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
