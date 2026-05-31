using System;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace ScrcpyGui.Models
{
    public class AdbDevice
    {
        public string Serial { get; set; } = string.Empty;
        public string Model { get; set; } = "Unknown Device";
        public string Status { get; set; } = "offline"; // device, unauthorized, offline
        public string ConnectionType { get; set; } = "USB"; // USB, Wireless
        
        public bool IsAuthorized => Status.Equals("device", StringComparison.OrdinalIgnoreCase);
        
        public string DisplayName => $"{Model} ({Serial})";
        
        public string ConnectionIcon => ConnectionType.Equals("Wireless", StringComparison.OrdinalIgnoreCase) ? "\uE701" : "\uECF0";

        public Brush StatusBrush => Status.ToLower() switch
        {
            "device" => new SolidColorBrush(ColorHelper.FromArgb(255, 16, 124, 65)),       // Green
            "unauthorized" => new SolidColorBrush(ColorHelper.FromArgb(255, 247, 99, 12)),  // Orange
            _ => new SolidColorBrush(ColorHelper.FromArgb(255, 232, 17, 35))               // Red
        };
    }
}
