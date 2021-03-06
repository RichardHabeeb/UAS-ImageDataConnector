﻿namespace ImageDataClass
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// A class used to help with embeddeding and reading embedded data in an image.
    /// </summary>
    public class ImageData
    {
        #region Enum
        public enum ProportyItemId
        {
           gpsLatRef     =    0x1,            
           gpsLat        =    0x2,
           gpsLongRef    =    0x3,
           gpsLong       =    0x4,
           gpsAltRef     =    0x5,
           gpsAlt        =    0x6,
           gpsTime       =    0x7,
           gpsSatTag     =    0x8,
           gpsStatus     =    0x9,
           gps2D3D       =    0xA,
           gpsSpeedRef   =    0xC,
           gpsSpeed      =    0xD,
           gpsDirectRef  =    0xE,
           gpsDirection  =    0xF,
           yaw           =    0x11,  
            
           dateTimeTaken =    0x0132, 
           cameraModel   =    0x110,
           focalLength   =    0x920A
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Focal length that camera embedded in image.  Read only.
        /// Returns -1 if was not present in image.
        /// </summary>
        public int FocalLength
        {
            get
            {
                return focalLength;
            }

            set
            {
                if (value < 0)
                {
                    focalLength = -1;
                }

                focalLength = value;
            }
        }
        private int focalLength = -1;

        /// <summary>
        /// Model of the Camera used to take the picture.
        /// </summary>
        public string CameraModel { get; set; }

        /// <summary>
        /// Hieght in pixles of the image
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Width in pixles of the image
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Date and Time of Image being created/taken
        /// </summary>
        public DateTime DateTimeCreated { get; set; }

        /// <summary>
        /// The hours of GPS Time
        /// </summary>
        public int GPSHours { get; set; }

        /// <summary>
        /// The minutes of GPS Time
        /// </summary>
        public int GPSMinutes { get; set; }

        /// <summary>
        /// The seconds of GPS Time
        /// </summary>
        public float GPSSeconds { get; set; }

        /// <summary>
        /// GPS Altitude above sea level in meters
        /// </summary>
        public float GPSAltitude { get; set; }

        /// <summary>
        /// Indicates whether or not Radians were used when setting latitude. Read only.
        /// </summary>
        public bool GPSLatUsedRads { get; private set; }

        /// <summary>
        /// Indicates whether or not Radians were used when setting longitude. Read only.
        /// </summary>
        public bool GPSLongUsedRads { get; private set; }

        /// <summary>
        /// North or South for GPS Latitude Reference
        /// Use "N" for north and "S" for south.
        /// Defaults to "N" if not set or assigned an invalid value.
        /// </summary>
        public string GPSLatitudeRef
        {
            get
            {
                return gpsLatitudeRef;
            }

            set
            {
                gpsLatitudeRef = value;
            }
        }

        /// <summary>
        /// Holds GPS Latitude reference for property defined above
        /// </summary>
        private string gpsLatitudeRef = "N";

        /// <summary>
        /// GPS Latitude expressed in radians
        /// Positive value indicates North reference and will be assigned automatically to GPSLatitudeRef
        /// Negative value indicates South reference and will be assigned automatically to GPSLatitudeRef
        /// Will return positive (North) or negative (South) numbers
        /// Default value is 0
        /// </summary>
        public double GPSLatitudeRadians
        {
            get
            {
                return gpsLatitudeRef.Equals("N") ? gpsLatitudeRadians : gpsLatitudeRadians * -1;
            }

            set
            {
                GPSLatUsedRads = true;
                if (value >= 0)
                {
                    gpsLatitudeRadians = value;
                    gpsLatitudeRef = "N";
                }
                else
                {
                    gpsLatitudeRadians = value * -1;
                    gpsLatitudeRef = "S";
                }
            }
        }

        /// <summary>
        /// Holds absolute value of GPS Latitude in radians for property defined above
        /// </summary>
        private double gpsLatitudeRadians = 0;

        /// <summary>
        /// Holds absolute value of GPS Latitude in degrees for property defined below
        /// </summary>
        private double gpsLatitudeDegrees = 0;

        /// <summary>
        /// GPS Latitude expressed in degrees
        /// Positive value indicates North reference and will be assigned automatically to GPSLatitudeRef
        /// Negative value indicates South reference and will be assigned automatically to GPSLatitudeRef
        /// Will return positive (North) or negative (South) numbers
        /// Default value is 0
        /// </summary>
        public double GPSLatitudeDegrees
        {
            get
            {
                if (GPSLatUsedRads)
                {
                    return Math.Abs(GPSLatitudeRadians / 2 / Math.PI * 360);
                }
                else
                {
                    return gpsLatitudeDegrees;
                }
            }

            set
            {
                GPSLatUsedRads = false;
                if (value < 0)
                {
                    gpsLatitudeDegrees = -1 * value;
                    gpsLatitudeRef = "S";
                }
                else
                {
                    gpsLatitudeDegrees = value;
                    gpsLatitudeRef = "N";
                }
            }
        }

        /// <summary>
        /// West or East for GPS Longitude Reference
        /// Use "W" for West and "E" for East.
        /// Defaults to "W" if not set or invalid parameter is given.
        /// </summary>
        public string GPSLongitudeRef
        {
            get
            {
                return gpsLongitudeRef;
            }

            set
            {
                gpsLongitudeRef = value.ToLower().Equals("e") ? "E" : "W";
            }
        }

        /// <summary>
        /// Holds GPS Latitude reference for property defined above
        /// </summary>
        private string gpsLongitudeRef = "W";

        /// <summary>
        /// GPS Longitude expressed in radians
        /// Positive value indicates East reference and will be assigned automatically to GPSLongitudeRef
        /// Negative value indicates West reference and will be assigned automatically to GPSLongitudeRef
        /// Will return positive (East) or negative (West) numbers
        /// Default value is 0
        /// </summary>
        public double GPSLongitudeRadians
        {
            get
            {
                if (GPSLongUsedRads)
                {
                    return gpsLongitudeRef.Equals("E") ? gpsLongitudeRadians : gpsLongitudeRadians * -1;
                }
                else
                {
                    return gpsLongitudeDegrees * 2 * Math.PI / 360;
                }
            }

            set
            {
                GPSLongUsedRads = true;
                if (value >= 0)
                {
                    gpsLongitudeRadians = value;
                    gpsLongitudeRef = "E";
                }
                else
                {
                    gpsLongitudeRadians = value * -1;
                    gpsLongitudeRef = "W";
                }
            }
        }

        /// <summary>
        /// Holds absolute value of GPS Longitude in radians for property defined above.
        /// </summary>
        private double gpsLongitudeRadians = 0;

        /// <summary>
        /// Holds absolute value of GPS longitude in degrees for property defined below.
        /// </summary>
        private double gpsLongitudeDegrees = 0;

        /// <summary>
        /// GPS Longitude expressed in degrees
        /// Positive value indicates East reference and will be assigned automatically to GPSLongitudeRef
        /// Negative value indicates West reference and will be assigned automatically to GPSLongitudeRef
        /// Will return positive (East) or negative (West) numbers
        /// Default value is 0
        /// </summary>
        public double GPSLongitudeDegrees
        {
            get
            {
                if (GPSLongUsedRads)
                {
                    return Math.Abs(GPSLongitudeRadians / 2 / Math.PI * 360);
                }
                else
                {
                    return gpsLongitudeDegrees;
                }
            }

            set
            {
                GPSLongUsedRads = false;
                if (value < 0)
                {
                    gpsLongitudeRef = "W";
                    gpsLongitudeDegrees = -1 * value;
                }
                else
                {
                    gpsLongitudeRef = "E";
                    gpsLongitudeDegrees = value;
                }
                
            }
        }

        /// <summary>
        /// Yaw in degrees
        /// </summary>
        public float Yaw { get; set; }

        /// <summary>
        /// Yaw in degrees
        /// </summary>
        public float Pitch { get; set; }

        /// <summary>
        /// Yaw in degrees
        /// </summary>
        public float Roll { get; set; }

        /// <summary>
        /// Combines GPS Time Minutes and GPS Time Seconds into one property.
        /// Useful for our SQLite Query.
        /// </summary>
        public float TotalSecs
        {
            get { return (float)(GPSMinutes * 60) + GPSSeconds; }

            set
            {
                GPSSeconds = value % 60f;
                GPSMinutes = (int)value / 60;
            }

        }
        #endregion

        #region Private Fields
        /// <summary>
        /// ASCII Encoder used to encode/decode strings
        /// </summary>
        private static ASCIIEncoding asciiEncoder = new ASCIIEncoding();
        #endregion

        #region Public Static Methods
        /// <summary>
        /// Opens an image and reads and returns the embedded image data.
        /// </summary>
        /// <param name="filename">Full path to location of image to read embedded data from.</param>
        /// <returns>ImageData class with the embedded data.</returns>
        /// <exception cref="System.IO.IOException">Filename given is not found, unable to be loaded, etc.</exception>
        /// <exception cref="System.Exception">Image loaded does not have all embedded data expected</exception>
        public static ImageData ReadImage(string filename)
        {
            Image imageData = Image.FromFile(filename, false);
            return ReadImage(imageData);
        }

        /// <summary>
        /// Reads the embedded image data from an image already in memory.
        /// </summary>
        /// <param name="image">Image in memory in which to read the embedded data</param>
        /// <returns>ImageData class with the embedded data.</returns>
        /// <exception cref="System.Exception">
        /// Image loaded does not have all embedded data expected
        /// </exception>
        public static ImageData ReadImage(Image image)
        {
            float num = 0;
            float den = 0;
            int degrees;
            int minutes;
            double seconds;
            String str;
            int height = image.Height;
            int width = image.Width; 

            ImageData retData = new ImageData();
            lock (image)
            {
                PropertyItem[] propItems = image.PropertyItems;
                PropertyItem gpsTime = propItems.FirstOrDefault(p => p.Id == (int)ProportyItemId.gpsTime);
                PropertyItem gpsLat = propItems.FirstOrDefault(p => p.Id == (int)ProportyItemId.gpsLat);
                PropertyItem gpsLong = propItems.FirstOrDefault(p => p.Id == (int)ProportyItemId.gpsLong);
                PropertyItem gpsAlt = propItems.FirstOrDefault(p => p.Id == (int)ProportyItemId.gpsAlt);
                PropertyItem yaw = propItems.FirstOrDefault(p => p.Id == (int)ProportyItemId.yaw);
                PropertyItem gpsLatRef = propItems.FirstOrDefault(p => p.Id == (int)ProportyItemId.gpsLatRef);
                PropertyItem gpsLongRef = propItems.FirstOrDefault(p => p.Id == (int)ProportyItemId.gpsLongRef);
                PropertyItem dateTimeTaken = propItems.FirstOrDefault(p => p.Id == (int)ProportyItemId.dateTimeTaken);
                PropertyItem cameraModel = propItems.FirstOrDefault(p => p.Id == (int)ProportyItemId.cameraModel);
                PropertyItem focalLength = propItems.FirstOrDefault(p => p.Id == (int)ProportyItemId.focalLength);

                if (cameraModel != null)
                {
                    str = asciiEncoder.GetString(cameraModel.Value);
                    retData.CameraModel = str;
                }

                if(height > 0)
                {
                    retData.Height = height;
                }

                if (width > 0)
                {
                    retData.Width = width;
                }

                if (dateTimeTaken != null)
                {
                    str = asciiEncoder.GetString(dateTimeTaken.Value);
                    str = str.Trim();
                    str = str.Substring(0, str.Length - 1);
                    StringBuilder s = new StringBuilder(str);
                    s[4] = '/';
                    s[7] = '/';
                    retData.DateTimeCreated = DateTime.Parse(s.ToString());
                }

                if (gpsLatRef != null)
                {
                    retData.GPSLatitudeRef = asciiEncoder.GetString(gpsLatRef.Value, 0, 1);
                }

                if (gpsLongRef != null)
                {
                    retData.GPSLongitudeRef = asciiEncoder.GetString(gpsLongRef.Value, 0, 1);
                }

                if (gpsTime != null)
                {
                    retData.GPSHours = BitConverter.ToInt32(gpsTime.Value, 0);
                    retData.GPSMinutes = BitConverter.ToInt32(gpsTime.Value, 8);
                    num = (float)BitConverter.ToInt32(gpsTime.Value, 16);
                    den = (float)BitConverter.ToInt32(gpsTime.Value, 20);
                    retData.GPSSeconds = num / den;
                }

                if (gpsAlt != null)
                {
                    num = (float)BitConverter.ToInt32(gpsAlt.Value, 0);
                    den = (float)BitConverter.ToInt32(gpsAlt.Value, 4);
                    retData.GPSAltitude = num / den;
                }

                // Latitude
                if (gpsLat != null)
                {
                    degrees = BitConverter.ToInt32(gpsLat.Value, 0);
                    minutes = BitConverter.ToInt32(gpsLat.Value, 8);
                    seconds = (double)BitConverter.ToInt32(gpsLat.Value, 16) / (double)BitConverter.ToInt32(gpsLat.Value, 20);
                    retData.GPSLatitudeDegrees = (double)(degrees * 3600 + minutes * 60 + seconds) / 3600d;
                }

                // Longitude
                if (gpsLong != null)
                {
                    degrees = BitConverter.ToInt32(gpsLong.Value, 0);
                    minutes = BitConverter.ToInt32(gpsLong.Value, 8);
                    seconds = (double)BitConverter.ToInt32(gpsLong.Value, 16) / (double)BitConverter.ToInt32(gpsLong.Value, 20);
                    retData.GPSLongitudeDegrees = (double)(degrees * 3600 + minutes * 60 + seconds) / 3600d;
                }

                if (yaw != null)
                {
                    num = (float)BitConverter.ToInt32(yaw.Value, 0);
                    den = (float)BitConverter.ToInt32(yaw.Value, 4);
                    retData.Yaw = num / den;
                }

                if (focalLength != null)
                {
                    retData.FocalLength = BitConverter.ToInt32(focalLength.Value, 0);
                }
            }

            return retData;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Embed the data into given image
        /// </summary>
        /// <param name="filename">Full path to image to embed the data</param>
        /// <exception cref="System.IO.IOException">Filename given is not found, unable to be loaded, etc.</exception>
        /// <exception cref="System.Exception">Not all image data was given correctly.</exception>
        public void SaveToImage(string filename)
        {
            Image imageData = Image.FromFile(filename);
            SaveToImage(imageData);
            imageData.Save(filename + ".tmp");
            imageData.Dispose();
            File.Delete(filename);
            File.Move(filename + ".tmp", filename);
        }

        /// <summary>
        /// Embed the data into given image in memory.
        /// NOTE: You will still need to save the image to disk yourself
        /// </summary>
        /// <param name="image">Image in memory in which to write the embedded data</param>
        /// <exception cref="System.Exception">Not all image data was given correctly.</exception>
        public void SaveToImage(Image image)
        {
            if (image.PropertyItems.Length == 0)
                throw new Exception("No property items detected on the given Image object.");


            PropertyItem tempPropItem = image.PropertyItems[0];
            byte[] b_Time = new byte[24];
            byte[] justOne = BitConverter.GetBytes(1);
            byte[] temp = BitConverter.GetBytes(GPSHours);
            Array.Copy(temp, 0, b_Time, 0, 4);
            Array.Copy(justOne, 0, b_Time, 4, 4);
            temp = BitConverter.GetBytes(GPSMinutes);
            Array.Copy(temp, 0, b_Time, 8, 4);
            Array.Copy(justOne, 0, b_Time, 12, 4);
            int[] frac = ConvertToFraction(GPSSeconds);
            Array.Copy(BitConverter.GetBytes(frac[0]), 0, b_Time, 16, 4);
            Array.Copy(BitConverter.GetBytes(frac[1]), 0, b_Time, 20, 4);
            tempPropItem.Id = (int)ProportyItemId.gpsTime;
            tempPropItem.Type = 5;
            tempPropItem.Len = b_Time.Length;
            tempPropItem.Value = b_Time;
            image.SetPropertyItem(tempPropItem);

            if (!String.IsNullOrEmpty(CameraModel))
            {
                byte[] byteoriginal = asciiEncoder.GetBytes(CameraModel);
                byte[] bytes = new byte[(byteoriginal.Length + 1)];
                Array.Copy(byteoriginal, 0, bytes, 0, byteoriginal.Length);
                bytes[(bytes.Length - 1)] = 0;
                tempPropItem.Id = (int)ProportyItemId.cameraModel;
                tempPropItem.Type = 2;
                tempPropItem.Len = bytes.Length;
                tempPropItem.Value = bytes;
                image.SetPropertyItem(tempPropItem);
            }

            byte[] latBytes = GetBytesGPSLatitude();
            tempPropItem.Id = (int)ProportyItemId.gpsLat;
            tempPropItem.Type = 5;
            tempPropItem.Len = latBytes.Length;
            tempPropItem.Value = latBytes;
            image.SetPropertyItem(tempPropItem);

            byte[] longBytes = GetBytesGPSLongitude();
            tempPropItem.Id = (int)ProportyItemId.gpsLong;
            tempPropItem.Type = 5;
            tempPropItem.Len = longBytes.Length;
            tempPropItem.Value = longBytes;
            image.SetPropertyItem(tempPropItem);

            byte[] b_AltRef = new byte[] { 0x0 };
            tempPropItem.Id = (int)ProportyItemId.gpsAltRef;
            tempPropItem.Type = 1;
            tempPropItem.Len = 1;
            tempPropItem.Value = b_AltRef;
            image.SetPropertyItem(tempPropItem);

            byte[] latRefBytes = new byte[2];
            latRefBytes[0] = asciiEncoder.GetBytes(GPSLatitudeRef)[0];
            tempPropItem.Id = (int)ProportyItemId.gpsLatRef;
            tempPropItem.Type = 2;
            tempPropItem.Len = latRefBytes.Length;
            tempPropItem.Value = latRefBytes;
            image.SetPropertyItem(tempPropItem);

            byte[] longRefBytes = new byte[2];
            longRefBytes[0] = asciiEncoder.GetBytes(GPSLongitudeRef)[0];
            tempPropItem.Id = (int)ProportyItemId.gpsLongRef;
            tempPropItem.Type = 2;
            tempPropItem.Len = longRefBytes.Length;
            tempPropItem.Value = longRefBytes;
            image.SetPropertyItem(tempPropItem);

            byte[] b_Heading = GetYAWBytes();
            tempPropItem.Id = (int)ProportyItemId.yaw;
            tempPropItem.Type = 5;
            tempPropItem.Len = b_Heading.Length;
            tempPropItem.Value = b_Heading;
            image.SetPropertyItem(tempPropItem);

            //tempPropItem = image.
            byte[] b_Alt = GetGPSAltBytes();
            tempPropItem.Id = (int)ProportyItemId.gpsAlt;
            tempPropItem.Type = 5;
            tempPropItem.Len = b_Alt.Length;
            tempPropItem.Value = b_Alt;
            image.SetPropertyItem(tempPropItem);
        }

        #endregion

        #region Private Methods
        private byte[] GetYAWBytes()
        {
            int[] fraction = ConvertToFraction(Yaw);
            byte[] temp = BitConverter.GetBytes(fraction[0]);
            byte[] temp2 = BitConverter.GetBytes(fraction[1]);
            byte[] ret = new byte[8];
            Array.Copy(temp, 0, ret, 0, 4);
            Array.Copy(temp2, 0, ret, 4, 4);
            return ret;
        }

        private byte[] GetGPSAltBytes()
        {
            int[] fraction = ConvertToFraction(GPSAltitude);
            byte[] temp = BitConverter.GetBytes(fraction[0]);
            byte[] temp2 = BitConverter.GetBytes(fraction[1]);
            byte[] ret = new byte[8];
            Array.Copy(temp, 0, ret, 0, 4);
            Array.Copy(temp2, 0, ret, 4, 4);
            return ret;
        }

        private byte[] GetBytesGPSLatitude()
        {
            double sec = GPSLatitudeDegrees * 3600d;
            int deg = (int)sec / 3600;
            sec = sec % 3600d;
            int min = (int)sec / 60;
            sec %= 60d;
            byte[] temp;
            byte[] b_Lat = new byte[24];
            byte[] oneValue = BitConverter.GetBytes(1);
            temp = BitConverter.GetBytes(deg);
            Array.Copy(temp, 0, b_Lat, 0, temp.Length);
            Array.Copy(oneValue, 0, b_Lat, 4, oneValue.Length);
            temp = BitConverter.GetBytes(min);
            Array.Copy(temp, 0, b_Lat, 8, temp.Length);
            Array.Copy(oneValue, 0, b_Lat, 12, oneValue.Length);
            int[] secBytes = ConvertToFraction(sec);
            temp = BitConverter.GetBytes(secBytes[0]);
            Array.Copy(temp, 0, b_Lat, 16, temp.Length);
            temp = BitConverter.GetBytes(secBytes[1]);
            Array.Copy(temp, 0, b_Lat, 20, temp.Length);
            return b_Lat;
        }

        private byte[] GetBytesGPSLongitude()
        {
            double sec = GPSLongitudeDegrees * 3600d;
            int deg = (int)sec / 3600;
            sec = sec % 3600d;
            int min = (int)sec / 60;
            sec %= 60d;
            byte[] temp;
            byte[] b_Long = new byte[24];
            byte[] oneValue = BitConverter.GetBytes(1);
            temp = BitConverter.GetBytes(deg);
            Array.Copy(temp, 0, b_Long, 0, temp.Length);
            Array.Copy(oneValue, 0, b_Long, 4, oneValue.Length);
            temp = BitConverter.GetBytes(min);
            Array.Copy(temp, 0, b_Long, 8, temp.Length);
            Array.Copy(oneValue, 0, b_Long, 12, oneValue.Length);
            int[] secBytes = ConvertToFraction(sec);
            temp = BitConverter.GetBytes(secBytes[0]);
            Array.Copy(temp, 0, b_Long, 16, temp.Length);
            temp = BitConverter.GetBytes(secBytes[1]);
            Array.Copy(temp, 0, b_Long, 20, temp.Length);
            return b_Long;
        }

        /// <summary>
        /// Converts a double into a fraction.
        /// </summary>
        /// <param name="dValue">Double value to convert</param>
        /// <returns>Integer array in the format: [numerator,denominator]</returns>
        public static int[] ConvertToFraction(double dValue)
        {
            //if (dValue > 2147483647)
            if (dValue < 1.0 / 2147483647)
                return new int[] { 0, 1 };
            if (dValue % 1 == 0)	// if whole number
                return new int[] { Convert.ToInt32(dValue), 1 };
            else
            {
                double dTemp = dValue;
                int iMultiple = 1;
                string strTemp = dValue.ToString();
                int i = 0;
                while (strTemp[i] != '.')
                    i++;
                int iDigitsAfterDecimal = strTemp.Length - i - 1;
                while (dTemp * 10 < 2147483647 && iMultiple * 10 < 2147483647 && iDigitsAfterDecimal > 0)
                {
                    dTemp *= 10;
                    iMultiple *= 10;
                    iDigitsAfterDecimal--;
                }
                int temp1 = (int)Math.Round(dTemp);//numerator
                int temp2 = iMultiple;//denominator
                return ReduceFraction(temp1, temp2);
            }

        }

        /// <summary>
        /// Reduces a given fraction
        /// </summary>
        /// <param name="numerator">Numerator of input fraction</param>
        /// <param name="denominator">Denominator of input fraction</param>
        /// <returns>Integer array in the format: [numerator,denominator]</returns>
        public static int[] ReduceFraction(int numerator, int denominator)
        {

            if (numerator == 0)
            {
                denominator = 1;
                return new int[] { numerator, denominator };
            }

            int iGCD = GCD(numerator, denominator);
            numerator /= iGCD;
            denominator /= iGCD;

            if (denominator < 0)	// if -ve sign in denominator
            {
                //pass -ve sign to numerator
                numerator *= -1;
                denominator *= -1;
            }

            return new int[] { numerator, denominator };
        }

        private static int GCD(int iNo1, int iNo2)
        {
            // take absolute values
            if (iNo1 < 0) iNo1 = -iNo1;
            if (iNo2 < 0) iNo2 = -iNo2;

            do
            {
                if (iNo1 < iNo2)
                {
                    int tmp = iNo1; // swap the two operands
                    iNo1 = iNo2;
                    iNo2 = tmp;
                }
                iNo1 = iNo1 % iNo2;
            } while (iNo1 != 0);
            return iNo2;
        }

        #endregion

        public static ImageData interpolate(DateTime imageTime, ImageData earlierData, ImageData laterData)
        {
            float totalTimeSpan = (laterData.DateTimeCreated.Ticks - earlierData.DateTimeCreated.Ticks);
            if (totalTimeSpan == 0)
                return earlierData;

            float scaling = (imageTime.Ticks - earlierData.DateTimeCreated.Ticks) / totalTimeSpan;
            ImageData result = new ImageData();
            result.DateTimeCreated = new DateTime((long)((laterData.DateTimeCreated.Ticks + earlierData.DateTimeCreated.Ticks) * scaling));
            result.GPSHours = earlierData.GPSHours + (int)((-earlierData.GPSHours + laterData.GPSHours) * scaling);
            result.GPSMinutes = earlierData.GPSMinutes + (int)((-earlierData.GPSMinutes + laterData.GPSMinutes) * scaling);
            result.GPSSeconds = earlierData.GPSSeconds + (-earlierData.GPSSeconds + laterData.GPSSeconds) * scaling;
            result.GPSLatitudeDegrees = earlierData.GPSLatitudeDegrees + (-earlierData.GPSLatitudeDegrees + laterData.GPSLatitudeDegrees) * scaling;
            result.GPSLongitudeDegrees = earlierData.GPSLongitudeDegrees + (-earlierData.GPSLongitudeDegrees + laterData.GPSLongitudeDegrees) * scaling;
            result.GPSAltitude = earlierData.GPSAltitude + (-earlierData.GPSAltitude + laterData.GPSAltitude) * scaling;
            result.Yaw = earlierData.Yaw + (-earlierData.Yaw + laterData.Yaw) * scaling;
            result.Pitch = earlierData.Pitch + (-earlierData.Pitch + laterData.Pitch) * scaling;
            result.Roll = earlierData.Roll + (-earlierData.Roll + laterData.Roll) * scaling;

            return result;

            /*
            float scaling;

            if (laterData.DateTimeCreated.Ticks - earlierData.DateTimeCreated.Ticks == 0)
                scaling = 0;
            else
                scaling = (laterData.DateTimeCreated.Ticks - imageTime.Ticks) / (laterData.DateTimeCreated.Ticks - earlierData.DateTimeCreated.Ticks);

            ImageData result = new ImageData();
            result.DateTimeCreated =        new DateTime((long) ((laterData.DateTimeCreated.Ticks + earlierData.DateTimeCreated.Ticks) * scaling));
            result.GPSHours =              (int) (( earlierData.GPSHours +                laterData.GPSHours                ) * scaling);
            result.GPSMinutes =            (int) (( earlierData.GPSMinutes +              laterData.GPSMinutes              ) * scaling);
            result.GPSSeconds =            ( earlierData.GPSSeconds +              laterData.GPSSeconds              ) * scaling;
            result.GPSLatitudeDegrees =    ( earlierData.GPSLatitudeDegrees +      laterData.GPSLatitudeDegrees      ) * scaling;
            result.GPSLongitudeDegrees =   ( earlierData.GPSLongitudeDegrees +     laterData.GPSLongitudeDegrees     ) * scaling;
            result.GPSAltitude =           ( earlierData.GPSAltitude +             laterData.GPSAltitude             ) * scaling;
            result.Yaw =                   ( earlierData.Yaw +                     laterData.Yaw                     ) * scaling;
            result.Pitch =                 ( earlierData.Pitch +                   laterData.Pitch                   ) * scaling;
            result.Roll =                  ( earlierData.Roll +                    laterData.Roll                    ) * scaling;
                                                                                                                       
            return result;
             * */
        }
    }
}
