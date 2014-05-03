using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDataConnector
{
    class FolderMonitor
    {
        private static string RECIEVING_IMAGE_FOLDER_BACKUP = "J:\\pics\\raw_backup\\";

        private List<PendingImage> list = new List<PendingImage>();

        public FolderMonitor(DirectoryInfo directory)
        {
            //add any existing files to the queue
            FileInfo[] files = directory.GetFiles();
            foreach (FileInfo file in files)
            {
                if (file.Extension.ToLower() == ".jpg" && file.Length > 0)
                {
                    Console.WriteLine("Adding file: " + file.Name);
                    if (!System.IO.File.Exists(RECIEVING_IMAGE_FOLDER_BACKUP + file.Name))
                        System.IO.File.Copy(file.FullName, RECIEVING_IMAGE_FOLDER_BACKUP + file.Name);
                    list.Add(new PendingImage(file, DateTime.Now));
                }
                else
                {
                    Console.WriteLine("Ignoring file (non-jpg or empty): " + file.Name);
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
            if(file.Extension.ToLower() == ".jpg" && file.Length > 0)
            {
                //add if not already in list
                if (list.Find(item => item.file.Name == file.Name) == null)
                {
                    Console.WriteLine("Adding file: " + e.Name);
                    System.IO.File.Copy(file.FullName, RECIEVING_IMAGE_FOLDER_BACKUP + file.Name);
                    PendingImage newImg = new PendingImage(file, DateTime.Now);
                    SafeAdd(newImg);
                }
            }
        }

        public List<PendingImage> GetCopyOfQueue()
        {
            lock (list)
            {
                return new List<PendingImage>(list);
            }
        }

        private void SafeAdd(PendingImage img)
        {
            lock (list)
            {
                list.Add(img);
            }
        }

        public void Remove(PendingImage img)
        {
            lock (list)
            {
                list.Remove(img);
            }
        }
    }
}
