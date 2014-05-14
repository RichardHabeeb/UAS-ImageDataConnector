using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageDataConnector
{
    class FolderMonitor
    {
        private static string RECIEVING_IMAGE_FOLDER_BACKUP = "J:\\pics\\raw_backup\\";

        private List<PendingFile> list = new List<PendingFile>();

        public FolderMonitor(DirectoryInfo directory)
        {
            //add any existing files to the queue
            FileInfo[] files = directory.GetFiles();
            foreach (FileInfo file in files)
            {
                if (file.Extension.ToLower() == ".jpg" && file.Length > 0 || file.Extension.ToLower() == ".imgtime")
                {
                    Console.WriteLine("Adding file: " + file.Name);
                    if (!System.IO.File.Exists(RECIEVING_IMAGE_FOLDER_BACKUP + file.Name))
                    {
                        try
                        {
                            System.IO.File.Copy(file.FullName, RECIEVING_IMAGE_FOLDER_BACKUP + file.Name);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Failed to backup raw image " + file.Name + " : " + e);
                        }
                    }
                    list.Add(new PendingFile(file, DateTime.Now));
                }
                else
                {
                    Console.WriteLine("Ignoring file: " + file.Name);
                }
            }
             
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = directory.FullName;

            watcher.Changed += new FileSystemEventHandler(OnChanged);
            //watcher.Created += new FileSystemEventHandler(OnChanged);

            watcher.EnableRaisingEvents = true;
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            FileInfo file = new FileInfo(e.FullPath);
            if((file.Extension.ToLower() == ".jpg" && file.Length > 0) || file.Extension.ToLower() == ".imgtime")
            {
                //check if in list
                if (list.Find(item => item.file.Name == file.Name) == null)
                {
                    //create backup
                    Console.WriteLine("Adding file: " + e.Name);
                    if (!System.IO.File.Exists(RECIEVING_IMAGE_FOLDER_BACKUP + file.Name))
                    {
                        try
                        {
                            System.IO.File.Copy(file.FullName, RECIEVING_IMAGE_FOLDER_BACKUP + file.Name);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Failed to backup raw image " + file.Name + " : " + ex);
                        }
                    }
                    
                    //add to list
                    PendingFile newImg = new PendingFile(file, DateTime.Now);
                    SafeAdd(newImg);
                }
            }
        }

        public List<PendingFile> GetCopyOfQueue()
        {
            lock (list)
            {
                return new List<PendingFile>(list);
            }
        }

        private void SafeAdd(PendingFile img)
        {
            lock (list)
            {
                list.Add(img);
            }
        }

        public void Remove(PendingFile img)
        {
            lock (list)
            {
                list.Remove(img);
            }
        }
    }
}
