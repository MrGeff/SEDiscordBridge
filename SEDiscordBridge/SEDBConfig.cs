﻿using System;
using System.Collections.Generic;
using System.Text;
using Torch;

namespace SEDiscordBridge
{
    public class SEDBConfig : ViewModel
    {
        private string _token;
        public string BotToken { get => _token; set => SetValue(ref _token, value); }

        private string _chatChannelID;
        public string ChannelId { get => _chatChannelID; set => SetValue(ref _chatChannelID, value); }

        private string _format = ":rocket: **{p}**: {msg}";
        public string Format { get => _format; set => SetValue(ref _format, value); }

        private string _format2 = "[D]{p}";
        public string Format2 { get => _format2; set => SetValue(ref _format2, value); }

        private string _commandChannelID;
        public string CommandChannelId { get => _commandChannelID; set => SetValue(ref _commandChannelID, value); }

        private string _commandPrefix = ";;";
        public string CommandPrefix { get => _commandPrefix; set => SetValue(ref _commandPrefix, value); }

        private bool _asServer = false;
        public bool AsServer { get => _asServer; set => SetValue(ref _asServer, value); }

        private string _serverName = "Discord";
        public string ServerName { get => _serverName; set => SetValue(ref _serverName, value); }

        private string _started = ":white_check_mark: Server Started!";
        public string Started { get => _started; set => SetValue(ref _started, value); }

        private string _stopped = ":x: Server Stopped!";
        public string Stopped { get => _stopped; set => SetValue(ref _stopped, value); }

        private string _join = ":sunny: The player {p} joined the server";
        public string Join { get => _join; set => SetValue(ref _join, value); }

        private string _leave = ":new_moon: The player {p} left the server";
        public string Leave { get => _leave; set => SetValue(ref _leave, value); }
        
        private bool _useStatus = true;
        public bool UseStatus { get => _useStatus; set => SetValue(ref _useStatus, value); }

        private string _status = "{p} players | SS {ss}";
        public string Status { get => _status; set => SetValue(ref _status, value); }        
    }
}
