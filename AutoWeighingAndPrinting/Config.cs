using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Wordprocessing;
using Newtonsoft.Json;
using System;
using System.IO;

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
            string productName = "Auto Weighing and Printing V3";

            fileDirectory = commonApplicationDataDirectory + "\\" + manufacturer + "\\" + productName;
            filePath = fileDirectory + "\\" + fileName;

            if (!File.Exists(filePath))
            {
                if (!Directory.Exists(fileDirectory))  // if it doesn't exist, create
                    Directory.CreateDirectory(fileDirectory);

                // Create a new file
                File.CreateText(filePath).Dispose();

                //Assign default values to the new file
                AppSettings defaultSettings = GetDefaultSettings();

                File.WriteAllText(filePath, JsonConvert.SerializeObject(defaultSettings, Formatting.Indented));

            }



        }

        internal AppSettings ReadSettings()
        {
            string tempFilePath = filePath + ".tmp";

            // 1️. Recover from temp file if it exists
            if (File.Exists(tempFilePath))
            {
                try
                {
                    // If main file is missing or broken, recover temp
                    if (!File.Exists(filePath) || !IsValidJson(filePath))
                    {
                        MoveFileOverwrite(tempFilePath, filePath);
                    }
                    else
                    {
                        File.Delete(tempFilePath);
                    }
                }
                catch
                {
                    // Best effort recovery
                    MoveFileOverwrite(tempFilePath, filePath);
                }
            }

            // 2️. Read config safely
            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    appSettings = JsonConvert.DeserializeObject<AppSettings>(json);
                }
            }
            catch
            {
                // 3️. Fallback to default if corrupted
                appSettings = GetDefaultSettings();
            }

            return appSettings;
        }

        internal void WriteSettings(AppSettings NewSettings)
        {
            if (!Directory.Exists(fileDirectory))
                Directory.CreateDirectory(fileDirectory);

            string tempFilePath = filePath + ".tmp";
            string json = JsonConvert.SerializeObject(NewSettings, Formatting.Indented);

            // Write to temp file first
            File.WriteAllText(tempFilePath, json);

            // Atomically replace original file
            if (File.Exists(filePath))
            {
                File.Replace(tempFilePath, filePath, null);
            }
            else
            {
                File.Move(tempFilePath, filePath);
            }

        }

        private AppSettings GetDefaultSettings()
        {
            return new AppSettings
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
                SelectedSize = "",
                PrinterName = "",
                EnablePrintingHistory = false,
                DeleteHistoryDays = 180,
                HistoryDeletedDate = DateTime.Now,
                LicenseKey = "MRLMH-TMJAO-UPKEP-ETHTI",
                IsUseExDialog = true
            };
        }

        private void MoveFileOverwrite(string source, string destination)
        {
            if (File.Exists(destination))
                File.Delete(destination);

            File.Move(source, destination);
        }
        private bool IsValidJson(string path)
        {
            try
            {
                JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(path));
                return true;
            }
            catch
            {
                return false;
            }
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
        public string SelectedSize { get; set; }
        public string PrinterName { get; set; }
        public bool EnablePrintingHistory { get; set; }
        public int DeleteHistoryDays { get; set; }
        public DateTime HistoryDeletedDate { get; set; }
        public string LicenseKey { get; set; }
        public bool IsUseExDialog { get; set; }



    }
}
