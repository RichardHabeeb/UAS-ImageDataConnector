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
            Console.WriteLine("Connected");
            DirectoryInfo directory = new DirectoryInfo(RECIEVING_IMAGE_FOLDER);
            FolderMonitor monitor = new FolderMonitor(directory);

            bool canonOffsetCalculated = false;
            long canonOffset = 0;
            bool thermalOffsetCalculated = false;
            long thermalOffset = 0;

            List<PendingFile> canonImgList = new List<PendingFile>();
            List<PendingFile> canonDataList = new List<PendingFile>();
            List<PendingFile> thermalImgList = new List<PendingFile>();

            
            while (true)
            {
                //SEPARATE NEW FILES INTO LISTS
                List<PendingFile> fileQueue = monitor.GetCopyOfQueue();

                for(int i = fileQueue.Count - 1; i >= 0; i--)
                {
                    PendingFile pendingFile = fileQueue[i];
                    string imgType = pendingFile.file.Name.Split('_')[0].ToLower();

                    if (pendingFile.file.Extension.ToLower() == ".jpg")
                    {
                        if (imgType == "canon")
                            canonImgList.Add(pendingFile);
                        else if (imgType == "thermal")
                            thermalImgList.Add(pendingFile);
                        else
                            Console.WriteLine("INVALID IMG TYPE: " + imgType);
                    }
                    else if (pendingFile.file.Extension.ToLower() == ".imgtime")
                    {
                        if (imgType == "canon")
                            canonDataList.Add(pendingFile);
                        else
                            Console.WriteLine("INVALID IMG TYPE: " + imgType);
                    }

                    monitor.Remove(pendingFile);
                }

                //SORT LISTS
                canonImgList.Sort((a, b) => a.file.Name.CompareTo(b.file.Name));
                canonDataList.Sort((a, b) => a.file.Name.CompareTo(b.file.Name));
                thermalImgList.Sort((a, b) => a.file.Name.CompareTo(b.file.Name));

                //CANON TIME OFFSET INITIALIZATION
                if (!canonOffsetCalculated && canonDataList.Count > 0)
                {
                    ImageData firstTriggerData = dbHandler.GetFirstPhotoData();
                    if (firstTriggerData != null)
                    {
                        Console.WriteLine("First trigger data found.");
                        long imageTime;
                        if(GetCanonImageTime(canonDataList, canonDataList[0], out imageTime))
                        {
                            long dataTime = firstTriggerData.DateTimeCreated.Ticks / 10000;
                            canonOffset = imageTime - dataTime;
                            canonOffsetCalculated = true;
                            Console.WriteLine("Image time:\t" + imageTime);
                            Console.WriteLine("Data time:\t" + dataTime);
                            Console.WriteLine("CANON OFFSET CALCULATED: " + canonOffset);
                        }
                    }
                }

                //CANNON PROCESS
                for (int i = canonImgList.Count - 1; i >= 0; i--)
                    {
                    PendingFile canonImg = canonImgList[i];

                    if(i != canonImgList.Count - 1 && canonOffsetCalculated)
                    {
                        long imageTime;
                        if (GetCanonImageTime(canonDataList, canonImg, out imageTime))
                        {
                            DateTime time = new DateTime(imageTime * 10000 - (canonOffset*10000));
                            ImageData before, after;
                            if(dbHandler.GetBorderingData(time, out before, out after))
                            {
                                //embed
                                ImageData.interpolate(time, before, after).SaveToImage(canonImg.file.FullName);
                                Console.WriteLine("Embedded " + canonImg.file.Name);

                                //move to embed
                                try
                                {
                                    File.Move(canonImg.file.FullName, DESTINATION_IMAGE_FOLDER + canonImg.file.Name);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Failed to move embedded image to image folder " + e);
                                }

                                //copy to embed backup
                                try
                                {
                                    if (!File.Exists(DESTINATION_IMAGE_FOLDER_BACKUP + canonImg.file.Name))
                                        File.Copy(DESTINATION_IMAGE_FOLDER + canonImg.file.Name, DESTINATION_IMAGE_FOLDER_BACKUP + canonImg.file.Name);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Failed to backup embedded image to backup embedded folder " + e);
                                }

                                canonImgList.RemoveAt(i);
                                continue;
                            }
                        }
                    }

                    //if an image has been without data for too long, send it with no embedding
                    if (DateTime.Now >= canonImg.recieved.AddSeconds(SECS_BEFORE_SKIP_EMBED))
                    {
                        Console.WriteLine(canonImg.file.Name + " reached data timeout"); //could still have valid data, this just means the data for the next image wasn't received (which ensures this data is valid)
                        long imageTime;
                        if (GetCanonImageTime(canonDataList, canonImg, out imageTime))
                        {
                            DateTime time = new DateTime(imageTime * 10000 - canonOffset*10000);
                            ImageData before, after;
                            if (dbHandler.GetBorderingData(time, out before, out after))
                            {
                                //embed
                                ImageData.interpolate(time, before, after).SaveToImage(canonImg.file.FullName);
                                Console.WriteLine("Embedded " + canonImg.file.Name);
                            }
                        }

                        //move to embed
                        try
                        {
                            File.Move(canonImg.file.FullName, DESTINATION_IMAGE_FOLDER + canonImg.file.Name);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Failed to move embedded image to image folder " + e);
                        }

                        //copy to embed backup
                        try
                        {
                            if (!File.Exists(DESTINATION_IMAGE_FOLDER_BACKUP + canonImg.file.Name))
                                File.Copy(DESTINATION_IMAGE_FOLDER + canonImg.file.Name, DESTINATION_IMAGE_FOLDER_BACKUP + canonImg.file.Name);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Failed to backup embedded image to backup embedded folder " + e);
                        }

                        canonImgList.RemoveAt(i);
                    }
                }

                //THERMAL TIME OFFSET INITIALIZATION
                if (!thermalOffsetCalculated && thermalImgList.Count > 0)
                {
                    ImageData firstTriggerData = dbHandler.GetFirstPhotoData();
                    if (firstTriggerData != null)
                    {
                        Console.WriteLine("First trigger data found.");
                        long imageTime;
                        if (Int64.TryParse(thermalImgList[0].file.Name.Split('_')[1].Replace(thermalImgList[0].file.Extension, ""), out imageTime))
                        {
                            long dataTime = firstTriggerData.DateTimeCreated.Ticks / 10000;
                            thermalOffset = imageTime - dataTime;
                            thermalOffsetCalculated = true;
                            Console.WriteLine("Image time:\t" + imageTime);
                            Console.WriteLine("Data time:\t" + dataTime);
                            Console.WriteLine("THERMAL OFFSET CALCULATED: " + thermalOffset);
                        }
                    }
                }

                //THERMAL PROCESSING
                for (int i = thermalImgList.Count - 1; i >= 0; i--)
                {
                    PendingFile thermalImg = thermalImgList[i];

                    if (thermalOffsetCalculated)
                    {
                        long imageTime;
                        if (Int64.TryParse(thermalImgList[0].file.Name.Split('_')[1] ,out imageTime))
                        {
                            DateTime time = new DateTime(imageTime * 10000 - canonOffset*10000);
                            ImageData before, after;
                            if (dbHandler.GetBorderingData(time, out before, out after))
                            {
                                //embed
                                ImageData.interpolate(time, before, after).SaveToImage(thermalImg.file.FullName);
                                Console.WriteLine("Embedded " + thermalImg.file.Name);

                                //move to embed
                                try
                                {
                                    File.Move(thermalImg.file.FullName, DESTINATION_IMAGE_FOLDER + thermalImg.file.Name);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Failed to move embedded image to image folder " + e);
                                }

                                //copy to embed backup
                                try
                                {
                                    File.Copy(DESTINATION_IMAGE_FOLDER + thermalImg.file.Name, DESTINATION_IMAGE_FOLDER_BACKUP + thermalImg.file.Name);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Failed to backup embedded image to backup embedded folder " + e);
                                }

                                thermalImgList.RemoveAt(i);
                                continue;
                            }
                        }
                    }

                    //if an image has been without data for too long, send it with no embedding
                    if (DateTime.Now >= thermalImg.recieved.AddSeconds(SECS_BEFORE_SKIP_EMBED))
                    {
                        Console.WriteLine(thermalImg.file.Name + " reached data timeout");
                        //move to embed
                        try
                        {
                            File.Move(thermalImg.file.FullName, DESTINATION_IMAGE_FOLDER + thermalImg.file.Name);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Failed to move embedded image to image folder " + e);
                        }

                        //copy to embed backup
                        try
                        {
                            if (!File.Exists(DESTINATION_IMAGE_FOLDER_BACKUP + thermalImg.file.Name))
                                File.Copy(DESTINATION_IMAGE_FOLDER + thermalImg.file.Name, DESTINATION_IMAGE_FOLDER_BACKUP + thermalImg.file.Name);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Failed to backup embedded image to backup embedded folder " + e);
                        }

                        thermalImgList.RemoveAt(i);
                    }
                }
            }
        }

        private static bool GetCanonImageTime(List<PendingFile> canonDataList, PendingFile image, out long imageTime)
        {
            imageTime = 0;
            for (int i = canonDataList.Count - 1; i >= 0; i--)
            {
                PendingFile pendingFile = canonDataList[i];
                if (pendingFile.file.Name.Split('_')[1].ToLower() == image.file.Name.Split('_')[1].Replace(image.file.Extension, ""))
                {
                    if (Int64.TryParse(pendingFile.file.Name.Split('_')[2].Replace(pendingFile.file.Extension, ""), out imageTime))
                    {
                        return true;
                    }

                    Console.WriteLine("FAILED TO PARSE TIME FROM: " + pendingFile.file.Name);
                    return false;
                }
            }

            return false;
        }

        private static bool PackageAndShipImage(PendingFile image, ImageData data)
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
                if (!System.IO.File.Exists(destinationFile))
                    System.IO.File.Move(image.file.FullName, destinationFile);

                if (!System.IO.File.Exists(DESTINATION_IMAGE_FOLDER_BACKUP + image.file.Name))
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
