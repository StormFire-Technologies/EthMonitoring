﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EthMonitoring
{
    class SGMiner
    {
        private string host;
        private int port;
        private LogWriter logger = new LogWriter();

        private string getCommand(string _cmd)
        {
            string minerData = "";
            var clientSocket = new System.Net.Sockets.TcpClient();

            if (clientSocket.ConnectAsync(this.host, this.port).Wait(5000))
            {
                //string get_menu_request = "threads|";
                NetworkStream serverStream = clientSocket.GetStream();
                byte[] outStream = System.Text.Encoding.ASCII.GetBytes(_cmd);
                serverStream.Write(outStream, 0, outStream.Length);
                serverStream.Flush();

                byte[] inStream = new byte[clientSocket.ReceiveBufferSize];
                serverStream.Read(inStream, 0, (int)clientSocket.ReceiveBufferSize);
                string _returndata = System.Text.Encoding.ASCII.GetString(inStream);
                minerData = _returndata.Substring(0, _returndata.LastIndexOf("|") + 1);
            }
            else
            {
                Console.WriteLine("SGMiner socket failed");
            }

            // Close socket
            clientSocket.Close();
            clientSocket = null;

            return minerData;
        }

        public Stats getStats(string _host, int _port)
        {
            // Set vars
            this.host = _host;
            this.port = _port;

            // Create stats
            Stats stats = new Stats()
            {
                online = false,
                uptime = "",
                ex = null,
                version = "",
                hashrates = new List<string>(),
                dcr_hashrates = new List<string>(),
                temps = new List<string>(),
                fan_speeds = new List<string>(),
                power_usage = new List<string>(),
                type = 7,
                dual_accepted = 0,
                dual_rejected = 0,
                total_dual_hashrate = ""
            };

            try
            {
                // Fetch summary data
                string minerData = getCommand("summary");
                
                logger.LogWrite("SUMMARY DATA:" + minerData);

                if (minerData.Length > 0)
                {
                    string[] summary_data = minerData.Split('|');
                    string[] status = summary_data[0].Split(',');

                    string Summary = summary_data[1];
                    string[] data = Summary.Split(',');

                    // Version
                    string version = status[4].Split('=')[1];

                    stats.version = version;
                    stats.total_hashrate = data[5].Split('=')[1];
                    stats.accepted = Int32.Parse(data[8].Split('=')[1]);
                    stats.rejected = Int32.Parse(data[9].Split('=')[1]);

                    // GPU Data
                    string gpuData = getCommand("devs");

                    logger.LogWrite("GPU DATA:" + gpuData);

                    string[] gpus = gpuData.Split('|');

                    if (gpus.Length > 0)
                    {
                        for (int i = 1; i < (gpus.Length - 1); i++)
                        {
                            string[] gpu = gpus[i].Split(',');
                            double hashrate = double.Parse(gpu[14].Split('=')[1].Split('.')[0]);

                            stats.hashrates.Add(hashrate.ToString());
                            stats.temps.Add(gpu[3].Split('=')[1]);
                            stats.power_usage.Add("0");
                            stats.fan_speeds.Add(gpu[5].Split('=')[1]);
                        }
                    }
                    

                    stats.online = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SGMiner Exception: " + ex.StackTrace);
            }

            return stats;
        }
    }
}
