using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Microsoft.SqlServer.Server;
using OMRON.Compolet.CIP;
using OMRON.Compolet.Variable;



namespace FrameGrabber
{
    class Program
    {

        /// <summary>
        /// static ip address of GAT PC
        /// </summary>
        static readonly string _ipAdress = "10.1.1.201";

        /// <summary>
        /// build http url string bassed on command name
        /// </summary>
        /// <param name="command">command name supported by GAT</param>
        /// <returns></returns>
        public static string BuildWebRequest(string command)
        {
            return "http://" + _ipAdress + "/cgi-bin/" + command;
        }

        /// <summary>
        /// send an http web request to GAT
        /// </summary>
        /// <param name="url">the url web request</param>
        /// <returns>return the string contents of the http response</returns>
        public static string SendWebRequest(string url)
        {
            string responseString;
            try
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.Timeout = 10000;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                var encoding = Encoding.ASCII;
                using (var reader = new StreamReader(response.GetResponseStream(), encoding)) { responseString = reader.ReadToEnd(); }
                return responseString;
            }
            catch (Exception ex) { return ex.Message; }
        }

        /// <summary>
        /// Download an image from the GAT to the local PC.  The GAT saves the latest image from the image stream 
        /// to the GAT hard-drive. This method passes along the latest image to the local pc.
        /// </summary>
        /// <param name="url">http url web request</param>
        /// <returns>location of where the image was stored to the local pc</returns>
        public static string GetImageFromWebRequest(string url, string sn = "")
        {
            try
            {
                string directoryRoot = "C:\\Gentex Corporation\\GAT\\Images\\";
                if (!Directory.Exists(directoryRoot))
                    Directory.CreateDirectory(directoryRoot);

                string dateTime = DateTime.Now.ToString("yyyy_MM_dd_HHmmssFFF");
                if (!Directory.Exists(directoryRoot + dateTime))
                    Directory.CreateDirectory(directoryRoot + dateTime);

                WebClient webClient = new WebClient();
                string fileLocation = "";
                if (sn == "")
                    fileLocation = directoryRoot + dateTime + "\\image.png";
                else
                    fileLocation = directoryRoot + dateTime + "\\" + sn + ".png";

                webClient.DownloadFile(url, fileLocation);
                return fileLocation;
            }
            catch (Exception ex) { return ex.Message; }
        }

        /// <summary>
        /// Write a pretty message to a command terminal.  Allow user to adjust location using params.
        /// </summary>
        /// <param name="text1">first message</param>
        /// <param name="text2">second message</param>
        /// <param name="text1_startPos">curser start of first message</param>
        /// <param name="text2_startPos">curser start of second message</param>
        public static void WriteToGUI(string text1, string text2, int text1_startPos, int text2_startPos)
        {
            int leftCurser = Console.CursorLeft;
            int topCurser = Console.CursorTop;
            int text1_leftCurser = leftCurser + text1_startPos;
            int text1_topCurser = topCurser;
            int text2_leftCurser = leftCurser + text2_startPos;
            int text2_topCurser = topCurser;

            Console.SetCursorPosition(text1_leftCurser, text1_topCurser);
            Console.Write(text1);
            Console.SetCursorPosition(text2_leftCurser, text2_topCurser);
            Console.Write(text2);
            Console.WriteLine("");
        }

        /// <summary>
        /// Convert the http string response to a list of bytes
        /// </summary>
        /// <param name="rawData">http response from GAT</param>
        /// <param name="nvmData">out the raw response to a list of bytes</param>
        /// <returns>true if conversion works</returns>
        public static bool ParseNVMData(string rawData, out List<byte> nvmData)
        {
            nvmData = new List<byte>();
            try
            {
                // check to see if the needed data is the string
                if (!rawData.Contains("[") || !rawData.Contains("]") || !rawData.Contains(","))
                    return false;

                string parsedNVM;
                parsedNVM = rawData.Replace("[", "").Replace("]", "").Trim();
                while (parsedNVM.Contains(","))
                {
                    int index = parsedNVM.IndexOf(",");
                    string element = parsedNVM.Substring(0, index);
                    byte data = Convert.ToByte(element);
                    nvmData.Add(data);

                    parsedNVM = parsedNVM.Remove(0, index + 1);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// extract camera serial number from HeaderNVM data
        /// </summary>
        /// <param name="nvmData">raw nvm data</param>
        public static void DecodeHeaderNVMData(ref List<byte> nvmData, out string sn, bool printSN = true)
        {
            sn = null;

            // pull out header data from nvm stream
            List<byte> headerNVM = new List<byte>();
            for (int i = 0x00; i < 0x20; i++) { headerNVM.Add(nvmData[i]); }

            // to validate stored NVM data is good, calculate a crc based on the data and compare it to the stored crc
            ushort calculatedCRC = CalculateCRC(ref headerNVM, Convert.ToUInt32(30), Convert.ToUInt16(0xFFFF));
            ushort storedCRC = Convert.ToUInt16(headerNVM[30] << 8 | headerNVM[31]);
            if (calculatedCRC != storedCRC)
            {
                WriteToGUI("Calculated CRC in header does not match stored CRC.  NVM data is corrupted.", "", 8, 40);
                return;
            }

            // pull out serial number data and decode it
            char c1 = Convert.ToChar(headerNVM[6]);
            char c2 = Convert.ToChar(headerNVM[7]);
            char c3 = Convert.ToChar(headerNVM[8]);
            char c4 = Convert.ToChar(headerNVM[9]);
            char c5 = Convert.ToChar(headerNVM[10]);
            string timeStamp = headerNVM[11].ToString("X2") + headerNVM[12].ToString("X2") + headerNVM[13].ToString("X2") + headerNVM[14].ToString("X2") + headerNVM[15].ToString("X2");
            sn = c1.ToString() + c2.ToString() + c3.ToString() + c4.ToString() + c5.ToString() + timeStamp;

            if (printSN)
                WriteToGUI("Serial Number", sn, 8, 40);
        }

        /// <summary>
        /// Gentex uses CRC's to validate the recieved data stream is accurate. It is recommended to compute a CRC across the received data stream
        /// and compare it to the stored CRC in NVM.  Gentex cameras use CRC-16 (Modbus), i.e. seed = 0xFFFF.
        /// https://www.lammertbies.nl/comm/info/crc-calculation
        /// </summary>
        /// <param name="data">the raw nvm data</param>
        /// <param name="len">the length of the data in which the crc will calculate over</param>
        /// <param name="seed">the seed / starting point of the CRC </param>
        /// <returns></returns>
        public static ushort CalculateCRC(ref List<byte> data, uint len, ushort seed)
        {
            ushort crc = seed;
            uint byte_index;
            uint bit_index;

            for (byte_index = 0U; byte_index < len; byte_index++)
            {
                crc ^= data[Convert.ToInt32(byte_index)];
                for (bit_index = 0U; bit_index < 8U; bit_index++)
                {
                    ushort eor = Convert.ToUInt16(((crc & 1U) != 0U) ? 0xA001U : 0U);
                    crc >>= 1;
                    crc ^= eor;
                }
            }
            return crc;
        }

        /// <summary>
        /// Display output of GAT to user
        /// </summary>
        /// <param name="result"></param>
        public static void WriteGATResponseToGUI(string result)
        {
            Console.WriteLine("GAT Response:");
            Console.WriteLine(result);
        }

        /// Program starts here  
        /// 
        static void Main()
        {
            while (true)
            {

                //Check for IO File from PLC Data project
                if (File.Exists("C:\\Gentex Corporation\\GAT\\Images\\FrameGrab_Out0.txt"))
                {

                    //Timer to catch camera start failure
                    DateTime testStart = DateTime.Now;
                    DateTime startcameraTimeout = DateTime.Now.AddSeconds(2);

                    // configure GAT
                    WriteToGUI("Configuring hardware...", "", 8, 8);
                    string configRsp = SendWebRequest("http://" + _ipAdress + "/cgi-bin/write?Channel=0&Type=Sierra_8bit&Mode=FrameGrabber");
                    WriteGATResponseToGUI(configRsp);

                    // bring up camera
                    WriteToGUI("Bringing up camera...", "", 8, 8);
                    string startRsp = SendWebRequest(BuildWebRequest("start"));
                    WriteGATResponseToGUI(startRsp);

                    //If first letter of startRsp is E jump to end
                    bool result;
                    result = startRsp[0].Equals('E');
                    if (result == true)
                    {
                        File.Delete("C:\\Gentex Corporation\\GAT\\Current_Image.bmp");
                        goto Finish;
                    }

                    // get camera SN
                    WriteToGUI("Getting camera serial number...", "", 8, 8);
                    string cameraNVMRsp = SendWebRequest(BuildWebRequest("cameraNVM"));
                    string sn = "";
                    if (ParseNVMData(cameraNVMRsp, out List<byte> nvmData))
                    {
                        DecodeHeaderNVMData(ref nvmData, out sn, false);
                        WriteToGUI(sn, "", 0, 0);
                        Console.WriteLine("");
                        Console.WriteLine("");

                    }
                    try
                    {
                        // write current SN to .txt file for PLC to display and Sherlock to double check
                        StreamWriter sw = new StreamWriter("C:\\Gentex Corporation\\GAT\\CurrentCameraSN.txt");
                        sw.WriteLine(sn);
                        sw.Close();
                    }
                    catch (Exception)
                    {
                    }

                    // change camera exposure
                    WriteToGUI("Changing camera expsosure to 2000...", "", 8, 8);
                    string wrtExpRsp = SendWebRequest(BuildWebRequest("writemem?Target=Imager&Address.U16=0x3192&Data.U8[]=[7,208]"));
                    WriteGATResponseToGUI(wrtExpRsp);

                    // save png image
                    WriteToGUI("Saving image data...", "", 8, 8);
                    string fileLocPNG = GetImageFromWebRequest(BuildWebRequest("frameSave"), sn);
                    WriteToGUI("Image saved to: ", fileLocPNG, 0, 16);

                    // save bmp image
                    System.Drawing.Image dummy = System.Drawing.Image.FromFile(fileLocPNG);
                    string fileLocBMP = "C:\\Gentex Corporation\\GAT\\Current_Image.bmp";
                    dummy.Save(fileLocBMP, System.Drawing.Imaging.ImageFormat.Bmp);
                    WriteToGUI("Image saved to: ", fileLocBMP, 0, 16);
                    Console.WriteLine("");
                    Console.WriteLine("");

                    // bring down camera
                    WriteToGUI("Powering down camera...", "", 8, 8);
                    string stopRsp = SendWebRequest(BuildWebRequest("stop"));
                    WriteGATResponseToGUI(stopRsp);

                    // check timestamp of file to confirm the image is being replaced correctly
                    DateTime lastWrite = File.GetLastWriteTimeUtc(fileLocBMP);
                    if (testStart < lastWrite)
                    {
                        Console.WriteLine("Date&Time Passed");
                    }

                    else
                    {
                        Console.WriteLine("Date&Time Failed");
                    }

                Finish:
                    //Delete the IO file to prevent errors
                    File.Delete("C:\\Gentex Corporation\\GAT\\Images\\FrameGrab_Out0.txt");

                    //Console.Clear();
                    //removed above to leave previous script on console

                }
                else
                {

                }

                try
                {
                    // Cleaning of image folder to prevent overfilling computer
                    string[] dirs = Directory.GetDirectories(@"C:\Gentex Corporation\GAT\Images");

                    foreach (string dir in dirs)
                    {
                        DirectoryInfo dirInfo = new DirectoryInfo(dir);
                        if (dirInfo.CreationTime < DateTime.Now.AddHours(-72))
                        {
                            //Delete files first
                            string[] files = Directory.GetFiles(dir);
                            foreach (string file in files)
                            {
                                File.Delete(file);
                            }
                            //Then delete directory
                            Directory.Delete(dir, true);
                        }

                    }
                }
                catch (Exception)
                {
                }

                //Small delay to slow looping
                Thread.Sleep(250);


            }

        }
    }
}
