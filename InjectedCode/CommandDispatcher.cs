// <copyright file="CommandDispatcher.cs" company="Jim Evans">
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
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Phone.Controls;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace InjectedCode
{
    /// <summary>
    /// Dispatches received commands to the correct handlers.
    /// </summary>
    public class CommandDispatcher
    {
        private static CommandDispatcher instance;
        private StreamSocketListener listener;

        /// <summary>
        /// Prevents a default instance of the <see cref="CommandDispatcher"/> class from being created.
        /// </summary>
        private CommandDispatcher()
        {
        }

        /// <summary>
        /// Gets the singleton instance of the CommandDispatcher class.
        /// </summary>
        public static CommandDispatcher Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CommandDispatcher();
                }

                return instance;
            }
        }

        /// <summary>
        /// Dispatches a command and returns its response.
        /// </summary>
        /// <param name="serializedCommand">A JSON serialized representation of a <see cref="Command"/> object.</param>
        /// <returns>A JSON value serializing the response for the command.</returns>
        public string DispatchCommand(string serializedCommand)
        {
            return string.Format("Yeah, yeah, I heard you. You said '{0}'.", serializedCommand);
        }

        /// <summary>
        /// Starts listening for incoming commands.
        /// </summary>
        public async void Start()
        {
            string address = GetIPAddress();
            this.listener = new StreamSocketListener();
            this.listener.Control.QualityOfService = SocketQualityOfService.Normal;
            this.listener.ConnectionReceived += this.ConnectionReceivedEventHandler;
            await this.listener.BindServiceNameAsync("4444");
            string port = this.listener.Information.LocalPort;

            var storage = IsolatedStorageFile.GetUserStoreForApplication();
            using (var stream = storage.CreateFile("networkInfo.txt"))
            {
                string networkInfo = string.Format("{0}:{1}", address, port);
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(networkInfo);
                stream.Write(buffer, 0, buffer.Length);
            }

            var ls = storage.GetFileNames();
        }

        private static string GetIPAddress()
        {
            string address = string.Empty;
            List<string> addresses = new List<string>();
            var hostNames = NetworkInformation.GetHostNames();
            foreach (var hostName in hostNames)
            {
                if (hostName.IPInformation != null && (hostName.IPInformation.NetworkAdapter.IanaInterfaceType == 71 || hostName.IPInformation.NetworkAdapter.IanaInterfaceType == 6))
                {
                    string hostDisplayName = hostName.DisplayName;
                    addresses.Add(hostDisplayName);
                }
            }

            if (addresses.Count > 0)
            {
                address = addresses[addresses.Count - 1];
            }

            return address;
        }

        private async void ConnectionReceivedEventHandler(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            await Task.Run(() => { this.HandleRequest(args.Socket); });
        }

        private async void HandleRequest(StreamSocket socket)
        {
            DataReader reader = new DataReader(socket.InputStream);
            DataWriter writer = new DataWriter(socket.OutputStream);
            writer.UnicodeEncoding = UnicodeEncoding.Utf8;

            string serializedRequest = await this.ReadData(reader);
            string commandResponse = this.DispatchCommand(serializedRequest);
            int length = System.Text.Encoding.UTF8.GetByteCount(commandResponse);
            string serializedResponse = string.Format("{0}:{1}", length, commandResponse);
            writer.WriteString(serializedResponse);
            await writer.StoreAsync();
            socket.Dispose();
        }

        private async Task<string> ReadData(DataReader reader)
        {
            string length = string.Empty;
            bool lengthFound = false;
            while (!lengthFound)
            {
                await reader.LoadAsync(1);
                byte character = reader.ReadByte();
                if (character == ':')
                {
                    lengthFound = true;
                }
                else
                {
                    length += Convert.ToChar(character);
                }
            }

            if (string.IsNullOrEmpty(length))
            {
                return string.Empty;
            }
            
            int dataLength = int.Parse(length);
            byte[] dataBuffer = new byte[dataLength];
            await reader.LoadAsync(Convert.ToUInt32(dataLength));
            reader.ReadBytes(dataBuffer);
            return System.Text.Encoding.UTF8.GetString(dataBuffer, 0, dataLength);
        }
    }
}
