namespace Qian.BarcodeScanner
{
    /// <summary>
    /// Represents information about a device, such as a keyboard or barcode scanner.
    /// Parses and stores details like VID, PID, and MI from the device path.
    /// </summary>
    public class DeviceInfo
    {
        /// <summary>
        /// Gets or sets the full device path.
        /// </summary>
        public string DevicePath { get; set; }

        /// <summary>
        /// Gets or sets the Vendor ID (VID) of the device.
        /// </summary>
        public string VID { get; set; }

        /// <summary>
        /// Gets or sets the Product ID (PID) of the device.
        /// </summary>
        public string PID { get; set; }

        /// <summary>
        /// Gets or sets the instance ID of the device.
        /// </summary>
        public string InstanceId { get; set; }

        /// <summary>
        /// Gets or sets the device interface GUID.
        /// </summary>
        public string DeviceInterfaceGuid { get; set; }

        /// <summary>
        /// Gets or sets the MI (Multiple Interface) value of the device.
        /// </summary>
        public string MI { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the device is a barcode scanner.
        /// </summary>
        public bool IsBarcodeScanner { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceInfo"/> class and parses the device path.
        /// </summary>
        /// <param name="devicePath">The full device path.</param>
        public DeviceInfo(string devicePath)
        {
            DevicePath = devicePath;
            ParseDevicePath(devicePath);
        }

        /// <summary>
        /// Returns a string representation of the device, including VID, PID, and optionally MI.
        /// </summary>
        /// <returns>A formatted string representing the device.</returns>
        public override string ToString()
        {
            if (string.IsNullOrEmpty(PID) || string.IsNullOrEmpty(VID))
                return "";

            if (string.IsNullOrEmpty(MI))
                return string.Format("VID_{0}&PID_{1}", VID, PID);

            return string.Format("VID_{0}&PID_{1}&MI_{2}", VID, PID, MI);
        }

        /// <summary>
        /// Parses the device path to extract details like VID, PID, and MI.
        /// </summary>
        /// <param name="devicePath">The full device path to parse.</param>
        private void ParseDevicePath(string devicePath)
        {
            if (string.IsNullOrEmpty(devicePath))
                return;

            string[] parts = devicePath.Split('#');

            if (parts.Length >= 3)
            {
                string hidPart = parts[1];
                string instanceIdPart = parts[2];
                string guidPart = parts[3];

                string[] hidParts = hidPart.Split('&');
                foreach (var part in hidParts)
                {
                    if (part.StartsWith("VID_"))
                    {
                        VID = part.Substring(4);
                    }
                    else if (part.StartsWith("PID_"))
                    {
                        PID = part.Substring(4);
                    }
                    else if (part.StartsWith("MI_"))
                    {
                        MI = part.Substring(3);
                    }
                }

                InstanceId = instanceIdPart;
                DeviceInterfaceGuid = guidPart.Trim('{', '}');
            }
        }
    }
}
