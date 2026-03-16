using System.Net.NetworkInformation;

namespace NetworkAnalyzer.Models;

public class NetworkInterfaceInfo
{
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string SubnetMask { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public OperationalStatus Status { get; set; }
    public long Speed { get; set; } // бит/с
    public string InterfaceType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}