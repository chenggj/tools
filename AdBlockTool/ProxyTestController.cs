﻿using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Exceptions;
using Titanium.Web.Proxy.Helpers;
using Titanium.Web.Proxy.Http;
using Titanium.Web.Proxy.Models;
using Titanium.Web.Proxy.StreamExtended.Network;

namespace Titanium.Web.Proxy.Basic
{
    public class Filter
    {
        public string[] keys { get; set; }
    }

    public class ProxyTestController : IDisposable
    {
        private readonly ProxyServer proxyServer;

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        private readonly ConcurrentQueue<Tuple<ConsoleColor?, string>> consoleMessageQueue
            = new ConcurrentQueue<Tuple<ConsoleColor?, string>>();

        private ExplicitProxyEndPoint explicitEndPoint;

        public static Filter ReadJsonFile()
        {
            string filePath = @"filter.json";

            try
            {
                using (StreamReader fileReader = new StreamReader(filePath))
                {
                    string jsonContent = fileReader.ReadToEnd();
                    Filter filter = JsonConvert.DeserializeObject<Filter>(jsonContent);

                    return filter;
                }
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"Error: 文件 {filePath} 未找到。");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: 读取或解析JSON文件时发生错误。{ex.Message}");
                throw;
            }
        }

        public Filter filter = null;
        public ProxyTestController()
        {
            filter = ReadJsonFile();

            Task.Run(() => ListenToConsole());

            proxyServer = new ProxyServer();

            proxyServer.ExceptionFunc = async exception =>
            {
                if (exception is ProxyHttpException phex)
                    WriteToConsole(exception.Message + ": " + phex.InnerException?.Message, ConsoleColor.Red);
                else
                    WriteToConsole(exception.Message, ConsoleColor.Red);
            };

            proxyServer.TcpTimeWaitSeconds = 10;
            proxyServer.ConnectionTimeOutSeconds = 15;
            proxyServer.ReuseSocket = false;
            proxyServer.EnableConnectionPool = false;
            proxyServer.ForwardToUpstreamGateway = true;
            proxyServer.CertificateManager.SaveFakeCertificates = true;

        }

        private CancellationToken CancellationToken => cancellationTokenSource.Token;

        public void Dispose()
        {
            cancellationTokenSource.Dispose();
            proxyServer.Dispose();
        }

        public void StartProxy()
        {
            proxyServer.BeforeRequest += OnRequest;
            proxyServer.BeforeResponse += OnResponse;
            proxyServer.AfterResponse += OnAfterResponse;

            proxyServer.ServerCertificateValidationCallback += OnCertificateValidation;
            proxyServer.ClientCertificateSelectionCallback += OnCertificateSelection;

            explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, 8000);

            explicitEndPoint.BeforeTunnelConnectRequest += OnBeforeTunnelConnectRequest;
            explicitEndPoint.BeforeTunnelConnectResponse += OnBeforeTunnelConnectResponse;

            proxyServer.AddEndPoint(explicitEndPoint);
            proxyServer.Start();

            foreach (var endPoint in proxyServer.ProxyEndPoints)
                Console.WriteLine("Listening on '{0}' endpoint at Ip {1} and port: {2} ", endPoint.GetType().Name,
                    endPoint.IpAddress, endPoint.Port);

            if (RunTime.IsWindows) proxyServer.SetAsSystemProxy(explicitEndPoint, ProxyProtocolType.AllHttp);
        }

        public void Stop()
        {
            explicitEndPoint.BeforeTunnelConnectRequest -= OnBeforeTunnelConnectRequest;
            explicitEndPoint.BeforeTunnelConnectResponse -= OnBeforeTunnelConnectResponse;

            proxyServer.BeforeRequest -= OnRequest;
            proxyServer.BeforeResponse -= OnResponse;
            proxyServer.ServerCertificateValidationCallback -= OnCertificateValidation;
            proxyServer.ClientCertificateSelectionCallback -= OnCertificateSelection;

            proxyServer.Stop();

        }


        private async Task OnBeforeTunnelConnectRequest(object sender, TunnelConnectSessionEventArgs e)
        {
            var hostname = e.HttpClient.Request.RequestUri.Host;
            e.GetState().PipelineInfo.AppendLine(nameof(OnBeforeTunnelConnectRequest) + ":" + hostname);
            WriteToConsole("Tunnel to: " + hostname);

            var clientLocalIp = e.ClientLocalEndPoint.Address;
            if (!clientLocalIp.Equals(IPAddress.Loopback) && !clientLocalIp.Equals(IPAddress.IPv6Loopback))
                e.HttpClient.UpStreamEndPoint = new IPEndPoint(clientLocalIp, 0);

            e.DecryptSsl = true;
            //if (hostname.Contains("11111.com"))
            //    e.DecryptSsl = false;
        }

        private void WebSocket_DataSent(object sender, DataEventArgs e)
        {
            var args = (SessionEventArgs)sender;
            WebSocketDataSentReceived(args, e, true);
        }

        private void WebSocket_DataReceived(object sender, DataEventArgs e)
        {
            var args = (SessionEventArgs)sender;
            WebSocketDataSentReceived(args, e, false);
        }

        private void WebSocketDataSentReceived(SessionEventArgs args, DataEventArgs e, bool sent)
        {
            var color = sent ? ConsoleColor.Green : ConsoleColor.Blue;

            foreach (var frame in args.WebSocketDecoder.Decode(e.Buffer, e.Offset, e.Count))
            {
                if (frame.OpCode == WebsocketOpCode.Binary)
                {
                    var data = frame.Data.ToArray();
                    var str = string.Join(",", data.ToArray().Select(x => x.ToString("X2")));
                    WriteToConsole(str, color);
                }

                if (frame.OpCode == WebsocketOpCode.Text) WriteToConsole(frame.GetText(), color);
            }
        }

        private Task OnBeforeTunnelConnectResponse(object sender, TunnelConnectSessionEventArgs e)
        {
            e.GetState().PipelineInfo
                .AppendLine(nameof(OnBeforeTunnelConnectResponse) + ":" + e.HttpClient.Request.RequestUri);

            return Task.CompletedTask;
        }

        // intercept & cancel redirect or update requests
        private async Task OnRequest(object sender, SessionEventArgs e)
        {
            e.GetState().PipelineInfo.AppendLine(nameof(OnRequest) + ":" + e.HttpClient.Request.RequestUri);

            foreach (string it in filter.keys)
            {
                if (e.HttpClient.Request.RequestUri.ToString().Contains(it))
                {
                    WriteToConsole(e.HttpClient.Request.RequestUri.ToString(), ConsoleColor.Red);
                    e.HttpClient.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    //e.Ok("<!DOCTYPE html>" +
                    //      "<html><body><h1>" +
                    //      "Website Blocked" +
                    //      "</h1>" +
                    //      "<p>Blocked by Test.</p>" +
                    //      "</body>" +
                    //      "</html>");
                    return;
                }
            }



            var clientLocalIp = e.ClientLocalEndPoint.Address;
            if (!clientLocalIp.Equals(IPAddress.Loopback) && !clientLocalIp.Equals(IPAddress.IPv6Loopback))
                e.HttpClient.UpStreamEndPoint = new IPEndPoint(clientLocalIp, 0);

            if (e.HttpClient.Request.Url.Contains("yahoo.com"))
                e.CustomUpStreamProxy = new ExternalProxy("localhost", 8888);

            WriteToConsole("Active Client Connections:" + ((ProxyServer)sender).ClientConnectionCount);
            WriteToConsole(e.HttpClient.Request.Url);

        }

        // Modify response
        private async Task MultipartRequestPartSent(object sender, MultipartRequestPartSentEventArgs e)
        {
            e.GetState().PipelineInfo.AppendLine(nameof(MultipartRequestPartSent));

            var session = (SessionEventArgs)sender;
            WriteToConsole("Multipart form data headers:");
            foreach (var header in e.Headers) WriteToConsole(header.ToString());
        }

        private async Task OnResponse(object sender, SessionEventArgs e)
        {
            e.GetState().PipelineInfo.AppendLine(nameof(OnResponse));

            if (e.HttpClient.ConnectRequest?.TunnelType == TunnelType.Websocket)
            {
                e.DataSent += WebSocket_DataSent;
                e.DataReceived += WebSocket_DataReceived;
            }

            WriteToConsole("Active Server Connections:" + ((ProxyServer)sender).ServerConnectionCount);

            var ext = Path.GetExtension(e.HttpClient.Request.RequestUri.AbsolutePath);

        }

        private async Task OnAfterResponse(object sender, SessionEventArgs e)
        {
            WriteToConsole($"Pipelineinfo: {e.GetState().PipelineInfo}", ConsoleColor.Yellow);
        }

        /// <summary>
        ///     Allows overriding default certificate validation logic
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public Task OnCertificateValidation(object sender, CertificateValidationEventArgs e)
        {
            e.GetState().PipelineInfo.AppendLine(nameof(OnCertificateValidation));

            // set IsValid to true/false based on Certificate Errors
            if (e.SslPolicyErrors == SslPolicyErrors.None) e.IsValid = true;

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Allows overriding default client certificate selection logic during mutual authentication
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public Task OnCertificateSelection(object sender, CertificateSelectionEventArgs e)
        {
            e.GetState().PipelineInfo.AppendLine(nameof(OnCertificateSelection));

            // set e.clientCertificate to override

            return Task.CompletedTask;
        }

        private void WriteToConsole(string message, ConsoleColor? consoleColor = null)
        {
            consoleMessageQueue.Enqueue(new Tuple<ConsoleColor?, string>(consoleColor, message));
        }

        private async Task ListenToConsole()
        {
            while (!CancellationToken.IsCancellationRequested)
            {
                while (consoleMessageQueue.TryDequeue(out var item))
                {
                    var consoleColor = item.Item1;
                    var message = item.Item2;

                    if (consoleColor.HasValue)
                    {
                        var existing = Console.ForegroundColor;
                        Console.ForegroundColor = consoleColor.Value;
                        Console.WriteLine(message);
                        Console.ForegroundColor = existing;
                    }
                    else
                    {
                        Console.WriteLine(message);
                    }
                }

                //reduce CPU usage
                await Task.Delay(50);
            }
        }

    }
}