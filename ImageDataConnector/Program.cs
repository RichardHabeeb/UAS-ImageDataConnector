using ImageDataClass;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDataConnector
{
    class Program
    {
        private static string IMAGE_FOLDER = "U:\\UAS\\pics";

        static void Main(string[] args)
        {
            DatabaseHandler dbHandler = DatabaseHandler.GetInstance();
            DirectoryInfo directory = new DirectoryInfo(IMAGE_FOLDER);
            FolderMonitor monitor = new FolderMonitor(directory);
            long imageTimeOffset;

            //wait for first image and gps data to calculate time offset
            while(true)
            {
                List<PendingImage> imageQueue = monitor.GetCopyOfQueue();
                if(imageQueue.Count > 0)
                {
                    ImageData firstImageData = dbHandler.GetFirstPhotoData();
                    if (firstImageData != null)
                    {
                        long imageTime = ParseTimeFromImgName(imageQueue[0].file.Name);
                        imageTimeOffset = imageTime - firstImageData.DateTimeCreated.
                    }
                }
            }

            //start main processing loop
            while(true)
            {
                //avoid accessing the original list to avoid multithreading problems
                List<PendingImage> imageQueue = monitor.GetCopyOfQueue();

                foreach (PendingImage image in imageQueue)
                {

                }
            }
        }

        private static long ParseTimeFromImgName(string name)
        {
            //Format is {local img num}_{remote img num}_{time in ms}
            long imgTime;
            if(long.TryParse(name.Split('_')[2], out imgTime))
            {
                return imgTime;
            }
            else
            {
                Console.WriteLine("Failed to parse image time: " + name);
                return -1;
            }
        }

    }
}
