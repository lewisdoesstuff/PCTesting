﻿using System;
using System.IO;
using System.Threading;
using System.Net;
using System.Diagnostics;
using CommandLine;
using System.Windows.Automation;

namespace PCTesting
{
    public class Options
    {
        //Get commandline options, yay automation!
        [Option('c',"cpu", Required = false, HelpText = "Start CPU test only.")]
        public bool cpu { get; set;}
        [Option('g', "gpu", Required = false, HelpText = "Start GPU test only.")]
        public bool gpu { get; set; }
        [Option('b', "both", Required = false, HelpText = "Start Combined test.")]
        public bool both { get; set; }
        [Option('t', "time", Required = false, HelpText= "Set time (in Seconds) to run the specified tests. Combines with any other option.")]
        public int time { get; set; }  
        [Option('a', "awd", Required = false, HelpText = "Sets time to 3600s (1hr), then runs the combined test. Used in-house for PC testing")]
        public bool awd { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            //Set version and print copywrite
            string version = "0.2.1";
            Console.WriteLine("PC Testing Script v" + version + ". Made by Lew :) 2021");

            //detect commandline args, shamelessly copy-pasted from the docs.
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(o =>
                {
                    int time = o.time * 1000;
                    if (o.cpu)
                    {
                        Console.WriteLine("Starting CPU Only Test.");
                        stressCPU(true, time);
                    }
                    else if (o.gpu)
                    {
                        Console.WriteLine(time);
                        Console.WriteLine("Starting GPU Only Test.");
                        stressGPU(true, time);
                    }
                    else if (o.both)
                    {
                        Console.WriteLine("Starting Combined Test.");
                        startBoth(true, time);
                    }
                    else if (o.awd)
                    {
                      //  Console.WriteLine("Would you like to test the PC? (y/n)");
                       //  string input = Console.ReadLine();
                      //  input = input.ToLower();
                      
                     //       if (input == "y")
                    //        {
                            Console.WriteLine("Starting 1-Hour Combined Test.");
                            Console.WriteLine("Opening Task Manager");
                            taskmgr();
                            startBoth(true, 3600 * 1000);
                    //    }
                   //         else if (input == "n")
                   //         {
                   //             Console.WriteLine("Exiting.");
                  //              Thread.Sleep(1000);
                   //             Environment.Exit(0);
                   //         }
                  //          else { 
                   //         Console.WriteLine("Invalid Input. Try Again");
                   //         Thread.Sleep(1000);
                   //         Console.Clear();
                   //         Main(args);
                   //     }


                        }


                    

                });

            //if no args detected, carry on and print menu :)
            Console.WriteLine("Please choose an option:");
            bool again = true;
            //Basic menu and option select
            Console.WriteLine("Menu:");
            Console.WriteLine("1. CPU Stress Test");
            Console.WriteLine("2. GPU Stress Test");
            Console.WriteLine("3. Combined Stress Test");
            while (again)
            {
                again = false;
                switch (Console.ReadLine())
                {
                    case "1":
                        Console.WriteLine("Starting CPU Stress Test");
                        stressCPU(false, 0);
                        break;
                    case "2":
                        Console.WriteLine("Starting Furmark GPU Test");
                        stressGPU(false, 0);
                        break;
                    case "3":
                        Console.WriteLine("Starting combined CPU/GPU Test");
                        startBoth(false, 0);
                        break;

                    default:
                        Console.WriteLine("Incorrect option, try again :)");
                        again = true;
                        break;
                }
            }
        }
        public static void stressCPU(bool arg, int time)
        {
            //Check for cpu stress test, download if not, then run with -console option.
            Console.Clear();
            if (!File.Exists("cpustress.exe"))
            {
                download("cpustress.exe");
                Console.WriteLine("File downloaded!");
            }
            else
            {
                Console.WriteLine("File already exists, continuing!");
            }

            //Start CPU Stress test, pass args to PressureService to run in console mode. Press q to quit!
            Process process = new Process();
            process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            process.StartInfo.Arguments = "-console";
            process.StartInfo.FileName = "cpustress.exe";
            process.Start();
            //if no time passed, skip. otherwise, wait for the timeout then kill the process. thanks Jon!
            if (time != 0)
            {
                if (!process.WaitForExit(time))
                {
                    process.Kill(); // this will probably take longer than the specified time. it hogs all of your cpu time so this takes a few seconds longer. shouldn't really be an issue for longer times hopefully.
                }
            }
            process.WaitForExit();
            //If program invoked with cli arguments, exit after completition, otherwise, show menu.
            if (arg) System.Environment.Exit(0);
        }

        public static void stressGPU(bool arg, int time)
        {
            //find if FurMark exists in either Program files or in testing tools directory
            string path = null;
            if (!File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86).ToString() + "\\Geeks3D\\Benchmarks\\FurMark\\FurMark.exe") && (!File.Exists("C:\\AWD Testing Tools\\FurMark\\FurMark.exe")))
            {
                download("furmark.exe");
                try
                {
                    //run installer, some of this is unneeded but idk what ty stackoverflow
                    Console.WriteLine("Starting to install application");
                    Process installer = new Process();
                    installer.StartInfo.FileName = "furmark.exe";
                    installer.StartInfo.Arguments = string.Format(" /qb /i \"{0}\" ALLUSERS=1", "furmark.exe");
                    installer.StartInfo.Arguments = "/quiet";
                    installer.Start();
                    installer.WaitForExit();
                    Console.WriteLine("Application installed successfully!");
                    path = (Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86).ToString() + "\\Geeks3D\\Benchmarks\\FurMark\\FurMark.exe");
                    //set path to newly installed furmark
                }
                catch
                {
                    Console.WriteLine("There was a problem installing the application!");
                }
            }
            else
            {
                // find which one exists, set path. probs cleaner way of doing this
                if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86).ToString() + "\\Geeks3D\\Benchmarks\\FurMark\\FurMark.exe")) {
                    path = (Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86).ToString() + "\\Geeks3D\\Benchmarks\\FurMark\\FurMark.exe");
                }
                else if (File.Exists("C:\\AWD Testing Tools\\FurMark\\FurMark.exe"))
                {
                    path = "C:\\AWD Testing Tools\\FurMark\\FurMark.exe";
                }
            }
            //start furmark with args
            Console.WriteLine("File already exists, continuing!");
            Process process = new Process();

            ProcessStartInfo startInfo = new ProcessStartInfo(path);
            process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            process.StartInfo.Arguments = "/nogui /width 1280 /height 720 /run_mode 2 /app_process_priority 50";
            process.StartInfo.FileName = path;
            process.Start();
            Console.WriteLine(time);
            //set to high priority so it doesn't totally fall over when cpu bench is running
            Process[] processes = Process.GetProcessesByName("FurMark");
            foreach (Process task in processes)
            {
                task.PriorityClass = ProcessPriorityClass.High;
            }
            if (time != 0)
            {
                if (!process.WaitForExit(time))
                {
                    process.Kill();
                }
            }

            process.WaitForExit();
            if (arg) System.Environment.Exit(0);

        }

        public static void startBoth(bool arg, int time)
        {
            //start both, does what it says on the tin
            Thread CPU = new Thread(() => stressCPU(arg, time));
            Thread GPU = new Thread(() => stressGPU(arg, time));
            GPU.Start();
            // starting the cpu test too soon causes the gpu test to take way longer to load
            Thread.Sleep(5000);
            CPU.Start();
            GPU.Join();
            CPU.Join();
        }

        public static void download(string name)
        {
            //download file from my website. boo hoo hardcoded variables
            WebClient downloader = new WebClient();
            string uri = "https://lew.ooo/files/", file;
            file = uri + name;
            Console.WriteLine("Downloading " + file);
            downloader.DownloadFile(file, name);
            Console.WriteLine("Downloaded " + file + " from " + uri);


        }
        public static void taskmgr()
        {
            // Kill existing instances
            foreach (Process pOld in Process.GetProcessesByName("taskmgr"))
            {
                pOld.Kill();
            }

            // Create a new instance
            Process p = new Process();
            p.StartInfo.FileName = "taskmgr";
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.Verb = "runas";
            p.Start();

        }
    

    }
    
}

