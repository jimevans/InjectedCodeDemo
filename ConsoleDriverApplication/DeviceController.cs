// <copyright file="DeviceController.cs" company="Jim Evans">
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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.XPath;
using Microsoft.SmartDevice.Connectivity;

namespace ConsoleDriverApplication
{
    /// <summary>
    /// Determines the kind of controller.
    /// </summary>
    public enum ControllerKind
    {
        /// <summary>
        /// Controller controls an actual device.
        /// </summary>
        Device,

        /// <summary>
        /// Controller controls an emulator.
        /// </summary>
        Emulator
    }

    /// <summary>
    /// Provides control of a Windows Phone device or emulator.
    /// </summary>
    public class DeviceController
    {
        private ControllerKind kind = ControllerKind.Emulator;
        private string deviceName = "Emulator";
        private string address = string.Empty;
        private string port = string.Empty;
        private bool hasSession;
        private RemoteApplication browserApplication;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceController"/> class.
        /// </summary>
        public DeviceController()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceController"/> class connecting to the
        /// specified IP address and port.
        /// </summary>
        /// <param name="address">The IP address of the device to connect to.</param>
        /// <param name="port">The port of the device to connect to.</param>
        public DeviceController(string address, string port)
        {
            this.address = address;
            this.port = port;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceController"/> class connecting to the
        /// specified kind of device with the specified name.
        /// </summary>
        /// <param name="kind">The <see cref="ControllerKind"/> of the controller.</param>
        /// <param name="deviceName">The name of the device to connect to.</param>
        public DeviceController(ControllerKind kind, string deviceName)
        {
            this.kind = kind;
            this.deviceName = deviceName;
        }

        /// <summary>
        /// Gets the IP of the address of the device being controlled.
        /// </summary>
        public string Address
        {
            get { return this.address; }
        }

        /// <summary>
        /// Gets the port of the device being controlled.
        /// </summary>
        public string Port
        {
            get { return this.port; }
        }

        /// <summary>
        /// Gets a value indicating whether there is a session.
        /// </summary>
        public bool HasSession
        {
            get { return this.hasSession; }
        }

        /// <summary>
        /// Starts the controller.
        /// </summary>
        public void Start()
        {
            if (!string.IsNullOrEmpty(this.address) && !string.IsNullOrEmpty(this.port))
            {
                return;
            }

            Device device = this.FindDevice();

            if (device == null)
            {
                throw new ConsoleDriverException(string.Format("Found no matching devices for {0}", this.deviceName));
            }
            else
            {
                Console.WriteLine("Found device {0}.", device.Name);
                string assemblyDirectory = Path.GetDirectoryName(this.GetType().Assembly.Location);
                string xapPath = GetPackagePath(assemblyDirectory);
                XapInfo appInfo = XapInfo.ReadApplicationInfo(xapPath);
                Guid applicationId = appInfo.ApplicationId.Value;
                string iconPath = appInfo.ExtractIconFile();

                bool isConnectedToDevice = false;
                try
                {
                    device.Connect();
                    isConnectedToDevice = device.IsConnected();
                }
                catch (SmartDeviceException ex)
                {
                    Console.WriteLine("WARNING! Exception encountered when connecting to device. HRESULT: {0:X}, message: {1}", ex.HResult, ex.Message);
                    System.Threading.Thread.Sleep(500);
                }

                if (!isConnectedToDevice)
                {
                    // TODO: Create connection mitigation routine.
                    Console.WriteLine("WARNING! Was unable to connect to device!");
                }
                else
                {
                    if (!device.IsApplicationInstalled(applicationId))
                    {
                        var apps = device.GetInstalledApplications();
                        Console.WriteLine("Installing application {0}.", xapPath);
                        this.browserApplication = device.InstallApplication(applicationId, applicationId, "WindowsPhoneDriverBrowser", iconPath, xapPath);
                    }
                    else
                    {
                        Console.WriteLine("Application already installed.");
                        this.browserApplication = device.GetApplication(applicationId);
                    }
                }

                File.Delete(iconPath);
            }
        }

        /// <summary>
        /// Starts a session with the specified device.
        /// </summary>
        public void StartSession()
        {
            if (!string.IsNullOrEmpty(this.address) && !string.IsNullOrEmpty(this.port))
            {
                return;
            }

            Console.WriteLine("Launching application.");
            this.browserApplication.Launch();
            string localFile = this.RetrieveNetworkInfoFile();

            string networkInfo = File.ReadAllText(localFile);
            Console.WriteLine("Contents of network info file: \"{0}\"", networkInfo);
            string[] parts = networkInfo.Split(':');
            this.address = parts[0];
            this.port = parts[1];
            this.hasSession = true;
            File.Delete(localFile);
        }

        /// <summary>
        /// Stops a session of the WindowsPhoneDriverBrowser application on the device.
        /// </summary>
        public void StopSession()
        {
            try
            {
                this.browserApplication.TerminateRunningInstances();
            }
            catch (Exception)
            {
            }

            this.address = null;
            this.port = null;
            this.hasSession = false;
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

        private Device FindDevice()
        {
            DatastoreManager manager = new DatastoreManager(1033);
            Collection<Platform> platforms = manager.GetPlatforms();
            if (platforms.Count == 0)
            {
                throw new ConsoleDriverException("Found no platforms");
            }

            Platform platform = platforms.FirstOrDefault((p) => { return p.Name.StartsWith("Windows Phone "); });
            Console.WriteLine("Found platform {0}.", platform.Name);
            Collection<Device> devices = platform.GetDevices();
            if (devices.Count == 0)
            {
                throw new ConsoleDriverException("Found no devices");
            }

            Device device = devices.FirstOrDefault((d) => { return platform.Name == this.deviceName && d.IsEmulator() == (this.kind == ControllerKind.Emulator); });
            if (device != null)
            {
                Console.WriteLine("Found device {0}.", device.Name);
            }
            else
            {
                Console.WriteLine("No device found with name exactly matching '{0}'; looking for device with name contains '{0}'.", this.deviceName);
                device = devices.FirstOrDefault((d) => { return d.Name.Contains(this.deviceName) && d.IsEmulator() == (this.kind == ControllerKind.Emulator); });
            }

            return device;
        }

        private string RetrieveNetworkInfoFile()
        {
            Console.WriteLine("Waiting for network information file to be written to device.");
            RemoteIsolatedStorageFile storage = null;
            int retryCount = 0;
            string remoteFileName = string.Empty;
            while (string.IsNullOrEmpty(remoteFileName) && retryCount < 4)
            {
                // Need sleep here to allow application to launch.
                System.Threading.Thread.Sleep(1000);
                storage = this.browserApplication.GetIsolatedStore();
                List<RemoteFileInfo> files = storage.GetDirectoryListing(string.Empty);
                DateTime findTimeout = DateTime.Now.Add(TimeSpan.FromSeconds(15));
                while (DateTime.Now < findTimeout)
                {
                    foreach (RemoteFileInfo info in files)
                    {
                        if (info.Name.Contains("networkInfo.txt"))
                        {
                            remoteFileName = info.Name;
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(remoteFileName))
                    {
                        break;
                    }

                    System.Threading.Thread.Sleep(500);
                    storage = this.browserApplication.GetIsolatedStore();
                    files = storage.GetDirectoryListing(string.Empty);
                }

                retryCount++;
            }

            if (string.IsNullOrEmpty(remoteFileName))
            {
                throw new ConsoleDriverException("Application was installed and launched, but did not write network info file");
            }

            Console.WriteLine("Retrieving network information file from device.");
            string localFile = Path.Combine(Path.GetTempPath(), "networkInfo.txt");
            storage.ReceiveFile("networkInfo.txt", localFile, true);
            storage.DeleteFile("networkInfo.txt");
            return localFile;
        }
    }
}
