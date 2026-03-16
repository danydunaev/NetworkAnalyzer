using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetworkAnalyzer.Models;

namespace NetworkAnalyzer.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<NetworkInterfaceInfo> _networkInterfaces = new();

    [ObservableProperty]
    private NetworkInterfaceInfo? _selectedInterface;

    [ObservableProperty]
    private string _inputUrl = string.Empty;

    [ObservableProperty]
    private UrlAnalysisResult _currentAnalysis = new();

    [ObservableProperty]
    private ObservableCollection<UrlAnalysisResult> _history = new();

    public MainWindowViewModel()
    {
        _networkInterfaces = new ObservableCollection<NetworkInterfaceInfo>();
        _history = new ObservableCollection<UrlAnalysisResult>();
        _inputUrl = string.Empty;
        _currentAnalysis = new UrlAnalysisResult();
        LoadNetworkInterfaces();
    }

    private void LoadNetworkInterfaces()
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var ni in interfaces)
            {
                // Пропускаем нерабочие интерфейсы
                if (ni.OperationalStatus != OperationalStatus.Up && ni.OperationalStatus != OperationalStatus.Down)
                    continue;

                var ipProps = ni.GetIPProperties();
                var ipv4Address = ipProps.UnicastAddresses
                    .FirstOrDefault(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork);

                NetworkInterfaces.Add(new NetworkInterfaceInfo
                {
                    Name = ni.Name,
                    Description = ni.Description,
                    IpAddress = ipv4Address?.Address?.ToString() ?? "N/A",
                    SubnetMask = ipv4Address?.IPv4Mask?.ToString() ?? "N/A",
                    MacAddress = ni.GetPhysicalAddress().ToString(),
                    Status = ni.OperationalStatus,
                    Speed = ni.Speed,
                    InterfaceType = ni.NetworkInterfaceType.ToString()
                });
            }

            if (NetworkInterfaces.Count > 0)
                SelectedInterface = NetworkInterfaces[0];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки интерфейсов: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task AnalyzeUrl()
    {
        if (string.IsNullOrWhiteSpace(InputUrl))
            return;

        await AnalyzeUrlInternal(InputUrl);
    }

    private async Task AnalyzeUrlInternal(string url)
    {
        var result = new UrlAnalysisResult
        {
            OriginalUrl = url,
            Timestamp = DateTime.Now
        };

        try
        {
            string urlToParse = url;
            if (!urlToParse.StartsWith("http://") && !urlToParse.StartsWith("https://"))
                urlToParse = "http://" + urlToParse;

            var uri = new Uri(urlToParse);
            result.Scheme = uri.Scheme;
            result.Host = uri.Host;
            result.Port = uri.Port;
            result.Path = string.IsNullOrEmpty(uri.AbsolutePath) ? "/" : uri.AbsolutePath;
            result.Query = uri.Query;
            result.Fragment = uri.Fragment;

            // Определение типа адреса
            result.AddressType = GetAddressType(uri.Host);

            // DNS информация
            try
            {
                var hostEntry = await Dns.GetHostEntryAsync(uri.Host);
                result.DnsInfo = string.Join(", ", hostEntry.AddressList.Select(a => a.ToString()));
            }
            catch (Exception ex)
            {
                result.DnsInfo = $"Ошибка DNS: {ex.Message}";
            }

            // Ping
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(uri.Host, 3000);
                result.IsPingSuccessful = reply.Status == IPStatus.Success;
                result.PingReply = $"{reply.Status} (время: {reply.RoundtripTime} мс)";
            }
            catch (Exception ex)
            {
                result.PingReply = $"Ошибка Ping: {ex.Message}";
            }
        }
        catch (UriFormatException ex)
        {
            result.Scheme = "Ошибка";
            result.Host = "Некорректный URL";
            result.PingReply = ex.Message;
        }

        CurrentAnalysis = result;
        History.Insert(0, result);
    }

    // Новая команда для загрузки URL из истории
    [RelayCommand]
    private void LoadFromHistory(UrlAnalysisResult selectedItem)
    {
        if (selectedItem != null)
        {
            InputUrl = selectedItem.OriginalUrl;
            // Опционально: сразу анализируем
            // AnalyzeUrlCommand.Execute(null);
        }
    }

    private string GetAddressType(string host)
    {
        if (IPAddress.TryParse(host, out var ip))
        {
            if (IPAddress.IsLoopback(ip))
                return "Loopback (127.0.0.1)";
            if (IsPrivateIp(ip))
                return "Локальный (частный)";
            return "Публичный";
        }
        return "Доменное имя";
    }

    private bool IsPrivateIp(IPAddress ip)
    {
        byte[] bytes = ip.GetAddressBytes();
        return bytes[0] == 10 ||
               (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
               (bytes[0] == 192 && bytes[1] == 168);
    }
}