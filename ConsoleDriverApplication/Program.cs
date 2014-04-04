// <copyright file="Program.cs" company="Jim Evans">
//
// Copyright 2014 Jim Evans
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleDriverApplication
{
    /// <summary>
    /// Main program class of the command line application.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Entry point for the application.
        /// </summary>
        /// <param name="args">Command line arguments of the application.</param>
        public static void Main(string[] args)
        {
            string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string xapPath = GetPackagePath(assemblyDirectory);

            Console.WriteLine("Injecting code into application package {0}", xapPath);
            XapInfo appInfo = XapInfo.ReadApplicationInfo(xapPath);
            string tempFile = appInfo.ExtractFileFromXap(@"TestPhoneApplication.dll");
            AssemblyModifier.ModifyAssembly(tempFile, "TestPhoneApplication.MainPage", ".ctor");
            appInfo.DeleteFileFromXap(@"TestPhoneApplication.dll");
            appInfo.InsertFileIntoXap(tempFile, @"TestPhoneApplication.dll");
            appInfo.InsertFileIntoXap(Path.Combine(assemblyDirectory, @"InjectedCode.dll"), "InjectedCode.dll");
            
            var controller = new DeviceController();
            controller.Start();
            Console.WriteLine("Ready for command.");
            string message = string.Empty;
            while (message.ToLowerInvariant() != "quit")
            {
                message = Console.ReadLine();
                if (message.ToLowerInvariant() == "start")
                {
                    controller.StartSession();
                }
                else if (message.ToLowerInvariant() == "quit")
                {
                    controller.StopSession();
                }
                else
                {
                    string response = SendMessage(controller.Address, controller.Port, message);
                    Console.WriteLine("Received response: {0}", response);
                }
            }
        }

        private static string SendMessage(string address, string port, string message)
        {
            string receivedMessage = string.Empty;
            Console.WriteLine("Attempting to send to {0}:{1}", address, port);
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Connect(address, int.Parse(port));
                using (NetworkStream sendStream = new NetworkStream(socket, false))
                {
                    int length = Encoding.UTF8.GetByteCount(message);
                    string datagram = string.Format("{0}:{1}", length, message);
                    sendStream.Write(Encoding.UTF8.GetBytes(datagram), 0, Encoding.UTF8.GetByteCount(datagram));
                }

                using (NetworkStream receiveStream = new NetworkStream(socket, false))
                {
                    StringBuilder dataLengthBuilder = new StringBuilder();
                    int byteValue = receiveStream.ReadByte();
                    char currentChar = Convert.ToChar(byteValue);
                    while (currentChar != ':')
                    {
                        dataLengthBuilder.Append(currentChar);
                        byteValue = receiveStream.ReadByte();
                        currentChar = Convert.ToChar(byteValue);
                    }

                    int dataLength = int.Parse(dataLengthBuilder.ToString());
                    byte[] buffer = new byte[dataLength];
                    int received = receiveStream.Read(buffer, 0, dataLength);
                    receivedMessage = Encoding.UTF8.GetString(buffer, 0, received);
                }
            }

            return receivedMessage;
        }

        private static string GetPackagePath(string directory)
        {
            List<string> fileNames = new List<string>() { "TestPhoneApplication.xap" };
            foreach (string fileName in fileNames)
            {
                string fullCandidatePath = Path.Combine(directory, fileName);
                if (File.Exists(fullCandidatePath))
                {
                    return fullCandidatePath;
                }
            }

            return string.Empty;
        }
    }
}
