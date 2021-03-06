﻿using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GameService.Domain.DTOs;
using GameService.Domain.Models;
using GameService.TCP.EventHandling;
using GameService.TCP.Events;
using GameService.TCP.Models;
using Newtonsoft.Json;
using Serilog;

namespace GameService.TCP
{
    public class TcpManager : ITcpManager
    {
        private TcpListener _listener;
        private NetworkStream _gameStream;
        private StreamWriter _gameStreamWriter;
        private Task _serverTask;
        private readonly IEventManager _eventManager;

        public TcpManager(IEventManager eventManager)
        {
            _eventManager = eventManager;
        }

        public void StartServer(int port)
        {
            _serverTask = new Task(() => StartListening(port), TaskCreationOptions.LongRunning);
            _serverTask.Start();
        }

        public void StopServer()
        {
            //TODO: close connection
            //_gameStream?.Close();
            // _listener?.Stop();
        }

        private async void StartListening(int port)
        {
            try
            {
                _listener = TcpListener.Create(port);
                _listener.Start();
                Log.Information($"Started TCP listening on port {port}");
                while (true)
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    Log.Information($"Accepted client {client.Client.LocalEndPoint}");
                    new Task(() => ListenToClient(client), TaskCreationOptions.LongRunning).Start();
                }
            }
            catch (SocketException e)
            {
                Log.Error("Error in connecting", e);
            }
        }

        private async void ListenToClient(TcpClient client)
        {
            if (client == null) return;
            try
            {
                var stream = client.GetStream();
                int length;
                do
                {
                    byte[] buffer = new byte[512];
                    length = await stream.ReadAsync(buffer, 0, buffer.Length);

                    var packet = Packet.FromJson(ParseMessage(buffer));
                    
                    switch (packet?.MetaData)
                    {
                        case Meta.Connect:
                            if (packet?.Message == "game")
                            {
                                _gameStream = stream;
                                _gameStreamWriter = new StreamWriter(_gameStream);
                                await _eventManager.Execute<GameServerConnectedEvent>(new GameServerConnectedEventArgs());
                            }
                            break;
                        case Meta.Message:
                            await Task.Run(() => ProcessPacket(packet));
                            break;
                        case Meta.Disconnect:
                            await Task.Run(() => ProcessPacket(packet));
                            //await DisconnectAsync();
                            break;
                    }

                } while (length > 0);

            }
            catch (SocketException e)
            {
                Log.Error("Error accepting the client", e);
            }
        }
        
        
        public async Task SendPacketAsync(Packet packet)
        {
            try
            {
                await _gameStreamWriter.WriteAsync(packet.ToJson());
                await _gameStreamWriter.FlushAsync();
            }
            catch (SocketException e)
            {
                Log.Error("Error during sending a message", e);
            }
        }

        private string ParseMessage(byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0) return "";
            return Encoding.ASCII.GetString(buffer).Replace(" ", "");
        }

        private void ProcessPacket(Packet packet)
        {
            if (char.IsDigit(packet.Message[0]))
            {
                string[] content = packet.Message.Split(' ');
                //TODO: to be replaced with the resultant displacement e.g. 150. -200
                int clicks = int.Parse(content[0]) == 1 ? 1 : -1;
                string code = content[1];
                _eventManager.Execute<MovementReceivedEvent>(new MovementReceivedEventArgs(code, clicks));
                return;
            }

            var message = JsonConvert.DeserializeObject<Message>(packet.Message);
            switch (message.ContentType)
            {
                case GameAction.Score:
                    var team = JsonConvert.DeserializeObject<Team>(message.Content);
                    _eventManager.Execute<BallScoredEvent>(new BallScoredEventArgs(team));
                    break;
                case GameAction.FinishGame:
                    var args = JsonConvert.DeserializeObject<GameSummaryDTO>(message.Content);
                    _eventManager.Execute<GameFinishedEvent>(new GameFinishedEventArgs(args));
                    break;
            }
        }

        public Task ConnectAsync(string ip, int port)
        {
            //TODO: connect to gateway
            throw new NotImplementedException();
        }

        public Task DisconnectAsync()
        {
            //TODO: disconnect from the gateway
            throw new NotImplementedException();
        }
    }
}