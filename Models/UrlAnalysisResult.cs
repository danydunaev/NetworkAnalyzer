using System;

namespace NetworkAnalyzer.Models;

public class UrlAnalysisResult
{
    public string OriginalUrl { get; set; } = string.Empty;
    public string Scheme { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public string Fragment { get; set; } = string.Empty;
    public bool IsPingSuccessful { get; set; }
    public string PingReply { get; set; } = string.Empty;
    public string DnsInfo { get; set; } = string.Empty;
    public string AddressType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}