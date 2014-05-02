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
        private static string RECIEVING_IMAGE_FOLDER = "C:/incoming_pics/";
        private static string DESTINATION_IMAGE_FOLDER = "C:/embedded_pics/";

        //seconds until an image will be moved to the destination folder with no data
        private static int SECS_BEFORE_SKIP_EMBED = 10;

        static void Main(string[] args)
        {
            DatabaseHandler dbHandler = DatabaseHandler.GetInstance();
            DirectoryInfo directory = new DirectoryInfo(RECIEVING_IMAGE_FOLDER);
            FolderMonitor monitor = new FolderMonitor(directory);
            long dataImageTimeOffset;

            //wait for first image and gps data to calculate time offset
            while(true)
            {
                Console.WriteLine("Waiting for first image data for offset calculation...");
                List<PendingImage> imageQueue = monitor.GetCopyOfQueue();
                if(imageQueue.Count > 0)
                {
                    ImageData firstImageData = dbHandler.GetFirstPhotoData();
                    if (firstImageData != null)
                    {
                        Console.WriteLine("First image data recieved");
                        long cameraImageTime = ParseTimeFromImgName(imageQueue[0].file.Name);
                        long dataImageTime = firstImageData.DateTimeCreated.Ticks;
                        dataImageTimeOffset = dataImageTime - cameraImageTime;
                        
                        Console.WriteLine("Camera time: " + cameraImageTime);
                        Console.WriteLine("Data time: " + dataImageTime);
                        Console.WriteLine("Time offset (data - camera): " + dataImageTimeOffset);
                        break;
                    }
                }

                Thread.Sleep(1000);
            }

            //start main processing loop
            while(true)
            {
                //avoid accessing the original list to avoid multithreading problems
                List<PendingImage> imageQueue = monitor.GetCopyOfQueue();

                foreach (PendingImage image in imageQueue)
                {
                    Console.WriteLine("Attempting to process: " + image.file.Name);

                    DateTime time = new DateTime(ParseTimeFromImgName(image.file.Name) + dataImageTimeOffset);

                    Console.WriteLine("Looking up data bordering: " + time.Ticks);
                    ImageData before = dbHandler.GetClosestDataBefore(time);
                    ImageData after = dbHandler.GetClosestDataAfter(time);

                    if(before != null && after != null)
                    {
                        Console.WriteLine("Data found for: " + image.file.Name);
                        ImageData interpolated = ImageData.interpolate(before, after);

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
            Console.WriteLine("Moving " + image.file.Name + " to " + destinationFile);
            try
            {
                System.IO.File.Move(image.file.FullName, destinationFile);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to move " + image.file.Name + " to " + destinationFile + ":" + e + ", " + e.Message);
                return false;
            }

            return true;
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
