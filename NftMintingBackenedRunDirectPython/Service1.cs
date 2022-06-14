using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Threading;
using System.IO;
using System.Net.NetworkInformation;
using System.Timers;

namespace NftMintingBackenedRunDirectPython
{
    public partial class Service1 : ServiceBase
    {      
        private System.Timers.Timer timer = new System.Timers.Timer();       
        int ScheduleTime = Convert.ToInt32(ConfigurationManager.AppSettings["ThreadTime"]);
        string _workingDirectory = ConfigurationManager.AppSettings["WorkingDirectory"];
        string _RunServerCommand = ConfigurationManager.AppSettings["RunServerCommand"];
        
        int sent = 0;
        public Service1()
        {
            InitializeComponent();  
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                CommonFeature.WriteToFile("timer instantiated");
                timer.Elapsed += OnElapsedTime;
                timer.Interval = 5000;
                timer.Enabled = true;
                timer.AutoReset = false;
                timer.Start();
            }
            catch (Exception ex)
            {
                CommonFeature.WriteToFile(ex.Message);
            }
        }
        private void OnElapsedTime(object sender, ElapsedEventArgs e)
        {
            Execute();
        }        

        protected override void OnStop()
        {
            timer.Enabled = false;
            timer.Stop();
            ThreadStart stop = new ThreadStart(ClosePythonInstaller);
            Thread stopThread = new Thread(stop);
            stopThread.Start();
            Thread.Sleep(ScheduleTime * 5000);
            stopThread.Abort();            
        }

        private static void ClosePythonInstaller()
        {
            Process[] processList = Process.GetProcesses();
            foreach (Process clsProcess in processList)
            {
                if (clsProcess.ProcessName == "python")
                {
                    if (!clsProcess.HasExited)
                    {
                        clsProcess.Kill();
                        clsProcess.WaitForExit();
                    }
                }
            }
        }

        private void Execute()
        {
            //CommonFeature.WriteToFile(string.Format("{0}", _counter1++));
            try
            {
                if (Process.GetProcessesByName("python").Count() == 0)
                {
                    CommonFeature.WriteToFile("Service started");

                    var workingDirectory = _workingDirectory;
                    var runServerCommand = _RunServerCommand;

                    Process process = new Process();
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardInput = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.WorkingDirectory = workingDirectory;
                    process.Exited += new EventHandler(process_Exited);
                    process.Start();

                    CommonFeature.WriteToFile("process started");
                    process.StandardInput.WriteLine(runServerCommand);
                    process.StandardInput.Flush();
                    process.StandardInput.Close();
                    using (StreamReader reader = process.StandardOutput)
                    {
                        CommonFeature.WriteToFile("cmd command executed");
                        //CommonFeature.WriteToFile(string.Format("{0}", _counter2++));
                        string result = reader.ReadToEnd();
                        Console.Write(result);
                    }
                    CommonFeature.WriteToFile("script run in cmd complete");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        protected void process_Exited(object sender, EventArgs e)
        {
            var p = (Process)sender;
            p.WaitForExit();
            sent += p.ExitCode;
        }

    }
    public class CommonFeature
    {
        public static void WriteToFile(string Message)
        {
            string LogStatus = ConfigurationManager.AppSettings.Get("LogStatus");

            if (LogStatus != null && LogStatus.ToUpper() == "ON")
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
                if (!File.Exists(filepath))
                {
                    // Create a file to write to.   
                    using (StreamWriter sw = File.CreateText(filepath))
                    {
                        sw.WriteLine(Message);
                    }
                }
                else
                {
                    using (StreamWriter sw = File.AppendText(filepath))
                    {
                        sw.WriteLine(Message);
                    }
                }
            }
        }

    }
}
