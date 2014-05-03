using ImageDataClass;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageDataConnector
{
    class Program
    {
        //make sure to add / to the end of folder paths
        private static string RECIEVING_IMAGE_FOLDER = "J:\\pics\\raw\\";
        private static string DESTINATION_IMAGE_FOLDER = "J:\\pics\\embedded\\";

        //there is also a backup const for recieving folder in FolderMonitor
        private static string DESTINATION_IMAGE_FOLDER_BACKUP = "J:\\pics\\embedded_backup\\";

        //seconds until an image will be moved to the destination folder with no data
        private static int SECS_BEFORE_SKIP_EMBED = 10;

        private static int CAMERA_TRIGGER_LATENCY_MS = 70;

        static void Main(string[] args)
        {
            DatabaseHandler dbHandler = DatabaseHandler.GetInstance();
            DirectoryInfo directory = new DirectoryInfo(RECIEVING_IMAGE_FOLDER);
            FolderMonitor monitor = new FolderMonitor(directory);
            long dataImageTimeOffset;

            //wait for first image and gps data to calculate time offset
            Console.WriteLine("Waiting for first image data for offset calculation...");
            while(true)
            {
                List<PendingImage> imageQueue = monitor.GetCopyOfQueue();
                imageQueue.RemoveAll(item => item.file.Name.ToLower().Contains("thermal"));
                if(imageQueue.Count > 0)
                {
                    int imgNum;
                    if(!int.TryParse(imageQueue[0].file.Name.Split('_')[0], out imgNum))
                    {
                        Console.WriteLine("Failed to parse img num: " + imageQueue[0].file.Name);
                        continue;
                    }

                    ImageData firstImageData = dbHandler.GetPhotoData(imgNum + 1);
                    if (firstImageData != null)
                    {
                        Console.WriteLine("First image data recieved");
                        DateTime cameraImageTime = ParseTimeFromImgName(imageQueue[0].file.Name);
                        DateTime dataTime = firstImageData.DateTimeCreated;

                        dataImageTimeOffset = dataTime.Ticks - cameraImageTime.Ticks + CAMERA_TRIGGER_LATENCY_MS * 10000;
                        
                        Console.WriteLine("Camera time: " + cameraImageTime);
                        Console.WriteLine("Data time: " + dataTime);
                        Console.WriteLine("Time offset (data - camera): " + dataImageTimeOffset);
                        break;
                    }
                }

                Thread.Sleep(1000);
            }

            int cout = 0;
            //start main processing loop
            while(true)
            {

                //avoid accessing the original list to avoid multithreading problems
                List<PendingImage> imageQueue = monitor.GetCopyOfQueue();
                Console.WriteLine("Get copy of queue " + cout++);

                foreach (PendingImage image in imageQueue)
                {
                    Console.WriteLine("Attempting to process: " + image.file.Name);
                    DateTime cameraTime = ParseTimeFromImgName(image.file.Name);
                    DateTime dataTime = cameraTime.AddTicks(dataImageTimeOffset);

                    Console.WriteLine("Looking up data bordering: " + dataTime.Ticks);
                    ImageData before = dbHandler.GetClosestDataBefore(dataTime);
                    ImageData after = dbHandler.GetClosestDataAfter(dataTime);

                    if(before != null && after != null)
                    {
                        Console.WriteLine("Data found for: " + image.file.Name);
                        ImageData interpolated = ImageData.interpolate(dataTime, before, after);

                        //embed
                        if( PackageAndShipImage(image, interpolated) )
                        {
                            //succesfully processed, remove from list
                            monitor.Remove(image);
                        }
                        
                    }
                    else if (image.recieved.AddSeconds(SECS_BEFORE_SKIP_EMBED) >= DateTime.Now)
                    {
                        Console.WriteLine("Didn't recieve data after capture time for " + image.file.Name + " in " + SECS_BEFORE_SKIP_EMBED + " seconds. Moving to destination with bad data.");
                        PackageAndShipImage(image, before);
                    }
                }

                //imageQueue = null;

                Thread.Sleep(1000);
            }
        }

        private static bool PackageAndShipImage(PendingImage image, ImageData data)
        {
            //embed
            if (data != null)
            {
                Console.WriteLine("Embedding data for: " + image.file.Name);
                try
                {
                    data.SaveToImage(image.file.FullName);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to embed: " + image.file.Name + ": " + e + ", " + e.Message);
                    return false;
                }
            }
            else
            {
                Console.WriteLine("No data for image: " + image.file.Name + " (is database empty or failing to connect?). Moving to destination with no data.");
            }

            //move
            string destinationFile = DESTINATION_IMAGE_FOLDER + image.file.Name.Split('_')[0];
            if (image.file.Name.ToLower().Contains("thermal"))
                destinationFile += "_" + image.file.Name.Split('_')[1];
            destinationFile += image.file.Extension;

            Console.WriteLine("Moving " + image.file.Name + " to " + destinationFile);
            try
            {
                System.IO.File.Move(image.file.FullName, destinationFile);
                System.IO.File.Copy(destinationFile, DESTINATION_IMAGE_FOLDER_BACKUP + image.file.Name);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to move " + image.file.Name + " to " + destinationFile + ":" + e + ", " + e.Message);
                return false;
            }

            return true;
        }

        private static DateTime ParseTimeFromImgName(string name)
        {
            //Format is {local img num}_{remote img num}_{time in ms}
            string[] splitName = name.Split('_');

            try
            { 
                long imgTime;
                if(long.TryParse(name.Split('_')[2].ToLower().Replace(".jpg", ""), out imgTime))
                {
                     return new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(imgTime);
                }
            }
            catch(Exception) {}

            Console.WriteLine("Failed to parse image time: " + name);
            return DateTime.MinValue;
        }

    }
}
