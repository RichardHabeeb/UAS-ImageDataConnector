using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using ImageDataClass;

namespace ImageDataConnector
{
    public class DatabaseHandler
    {
        public const string DatabaseAddress = "192.168.15.15"; //.13 on wifi
        public const string DatabaseName = "AUVSI";
        public const string DatabaseUsername = "sa";
        public const string DatabasePassword = "QaWsEdRfTg1!";

        private static DatabaseHandler Instance;
        SqlConnection DatabaseConnection = null;
        Func<ImageData[]> DataRecievedCallback;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static DatabaseHandler GetInstance()
        {
            if (ReferenceEquals(Instance, null)) Instance = new DatabaseHandler();

            return Instance;
        }

        /// <summary>
        /// 
        /// </summary>
        private DatabaseHandler()
        {
            SetupDatabaseConnection();
        }


        /// <summary>
        /// 
        /// </summary>
        private void SetupDatabaseConnection()
        {
            SqlConnectionStringBuilder sqlConnectionStringBuilder = new SqlConnectionStringBuilder();

            sqlConnectionStringBuilder.DataSource = DatabaseAddress;      //use the IP address of the AUVSI laptop
            sqlConnectionStringBuilder.InitialCatalog = DatabaseName;         //this is the database name
            sqlConnectionStringBuilder.IntegratedSecurity = false;                //set to false and user a UID and PWD later
            sqlConnectionStringBuilder.UserID = DatabaseUsername;
            sqlConnectionStringBuilder.Password = DatabasePassword;
            sqlConnectionStringBuilder.MinPoolSize = 5;
            sqlConnectionStringBuilder.MaxPoolSize = 2000;
            sqlConnectionStringBuilder.Pooling = true;
            sqlConnectionStringBuilder.ConnectTimeout = 200;                  //added to solve timeout problem



            while (ReferenceEquals(DatabaseConnection, null))
            {
                try
                {
                    Console.WriteLine("Attempting to connect to database");
                    DatabaseConnection = new SqlConnection(sqlConnectionStringBuilder.ConnectionString);
                    DatabaseConnection.Open();
                    System.Diagnostics.Debug.Assert(DatabaseConnection.State == ConnectionState.Open);
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine("Failed to connect to database: " + ex.Message);
                    DatabaseConnection = null;
                    Thread.Sleep(1000);
                }
            }
        }

        public bool GetBorderingData(DateTime time, out ImageData before, out ImageData after)
        {
            before = GetClosestDataBefore(time);
            after = GetClosestDataAfter(time);

            if (before != null && after != null)
                return true;

            return false;
        }

        public ImageData GetClosestDataBefore(DateTime time)
        {
            SqlCommand command = new SqlCommand(
                        "SELECT TOP 1 ID, TimeStamp, Latitude, Longitude, Altitude, Azimuth, Pitch, Roll, PhotoCounter, IsPhoto " +
                        "FROM FlightTelemetry " +
                        " WHERE Timestamp <= @Time;",
                        DatabaseConnection);

            command.Parameters.AddWithValue("@Time", time);

            return GetImageDataFromDatabase(command);
        }

        public ImageData GetClosestDataAfter(DateTime time)
        {
            SqlCommand command = new SqlCommand(
                        "SELECT TOP 1 ID, TimeStamp, Latitude, Longitude, Altitude, Azimuth, Pitch, Roll, PhotoCounter, IsPhoto " +
                        "FROM FlightTelemetry " +
                        " WHERE Timestamp >= @Time;",
                        DatabaseConnection);

            command.Parameters.AddWithValue("@Time", time);

            return GetImageDataFromDatabase(command);
        }

        public ImageData GetFirstPhotoData()
        {
            SqlCommand command = new SqlCommand(
                        "SELECT TOP 1 ID, TimeStamp, Latitude, Longitude, Altitude, Azimuth, Pitch, Roll, PhotoCounter, IsPhoto " +
                        "FROM FlightTelemetry " +
                        " WHERE IsPhoto = @IsPhoto " +
                        "ORDER BY TimeStamp",
                        DatabaseConnection);

            command.Parameters.AddWithValue("@IsPhoto", 1);

            return GetImageDataFromDatabase(command);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sqlConnection"></param>
        public ImageData GetImageDataFromDatabase(SqlCommand command)
        {
            SqlDataReader reader = null;
            try
            {
                //Console.WriteLine("Attempting to get image data.");
                reader = command.ExecuteReader();
                ImageData Row = new ImageData();

                if(!reader.HasRows)
                {
                    reader.Close();
                    return null;
                }

                if (reader.Read())
                {
                    //ID, TimeStamp, Latitude, Longitude, Altitude, Azimuth, Pitch, Roll, PhotoCounter, IsPhoto

                    int rowId = reader.GetInt32(0);
                    DateTime timestamp = reader.GetDateTime(1);
                    Row.DateTimeCreated = timestamp;
                    Row.GPSHours = timestamp.Hour;
                    Row.GPSMinutes = timestamp.Minute;
                    Row.GPSSeconds = timestamp.Second; // TODO: figure out if this is how the DT should be stored
                    Row.GPSLatitudeDegrees = reader.GetDouble(2);
                    Row.GPSLongitudeDegrees = reader.GetDouble(3);
                    Row.GPSAltitude = (float) reader.GetDouble(4);
                    Row.Yaw = (float) reader.GetDouble(5);
                    Row.Pitch = (float) reader.GetDouble(6);
                    Row.Roll = (float) reader.GetDouble(7);
                    int photocount = reader.GetInt32(8);
                }

                reader.Close();

                return Row;
            }
            catch (Exception e)
            {
                //TODO better error handling
                //Console.WriteLine("Failed to get image data, closing and re-opening database connection.");
                //DatabaseConnection.Close();
               // DatabaseConnection = null;
                //SetupDatabaseConnection();
               // return GetImageDataFromDatabase(command);
                Console.WriteLine("Error reading from database: " + e + " " + e.Message);
                if(reader != null)
                    reader.Close();
                return null;
            }
        }

    }
}
