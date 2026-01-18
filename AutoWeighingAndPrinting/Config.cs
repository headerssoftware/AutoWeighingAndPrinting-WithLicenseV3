using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AutoWeighingAndPrinting
{
    public class Config
    {
        private AppSettings appSettings = null;
        private readonly string fileDirectory;
        readonly string fileName = "Config.txt";
        readonly string filePath;
        public Config()
        {
            string commonApplicationDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string manufacturer = "Headers Software Solutions Pvt Ltd";
            string productName = "Auto Weighing and Printing";

            fileDirectory = commonApplicationDataDirectory + "\\" + manufacturer + "\\" + productName;
            filePath = fileDirectory + "\\" + fileName;

            if (!File.Exists(filePath))
            {
                if (!Directory.Exists(fileDirectory))  // if it doesn't exist, create
                    Directory.CreateDirectory(fileDirectory);

                // Create a new file
                File.CreateText(filePath).Dispose();

                //Assign default values to the new file
                AppSettings defaultSettings = new AppSettings
                {
                    PortName = "COM3",
                    BaudRate = 9600,
                    DataBits = 8,
                    Parity = "None",
                    StopBits = "One",
                    NewLine = "none",
                    DivideValue = "1000",
                    DBFilePath = "",
                    LastCustomerSelected = "",
                    PrintingFileName = "",
                    PrinterName = "",
                    EnablePrintingHistory = false,
                    DeleteHistoryDays = 180,
                    HistoryDeletedDate = DateTime.Now,
                    LicenseKey = "MRLMH-TMJAO-UPKEP-ETHTI"

                };
                File.WriteAllText(filePath, JsonConvert.SerializeObject(defaultSettings, Formatting.Indented));

            }



        }

        internal AppSettings ReadSettings()
        {
            if (File.Exists(filePath))
            {
                appSettings = JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(filePath));
            }

            return appSettings;
        }

        internal void WriteSettings(AppSettings NewSettings)
        {
            if (!File.Exists(filePath))
            {
                if (!Directory.Exists(fileDirectory))  // if it doesn't exist, create
                    Directory.CreateDirectory(fileDirectory);

                // Create a new file
                File.CreateText(filePath).Dispose();
            }
                
            File.WriteAllText(filePath, JsonConvert.SerializeObject(NewSettings, Formatting.Indented));
            
        }
    }
    public class AppSettings
    {
        public string PortName { get; set; } 
        public int BaudRate { get; set; }
        public string Parity { get; set; }
        public int DataBits { get; set; }
        public string StopBits { get; set; }
        public string NewLine { get; set; }
        public string DivideValue { get; set; }
        public string DBFilePath { get; set; }
        public string LastCustomerSelected { get; set; }
        public string PrintingFileName { get; set; }
        public string PrinterName { get; set; }
        public bool EnablePrintingHistory { get; set; }
        public int DeleteHistoryDays { get; set; }
        public DateTime HistoryDeletedDate { get; set; }
        public string LicenseKey { get; set; }
        


    }
}
