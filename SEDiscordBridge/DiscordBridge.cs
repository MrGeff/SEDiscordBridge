﻿using DSharpPlus;
using DSharpPlus.Entities;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Torch.API;
using Torch.API.Managers;
using Torch.Commands;
using VRage.Game;

namespace SEDiscordBridge
{
    public class DiscordBridge
    {
        private static SEDicordBridgePlugin Plugin;
        private static DiscordClient discord;
        private Thread thread;
        private DiscordGame game;

        private bool ready = false;
        public bool Ready { get => ready; set => ready = value; }

        public DiscordBridge(SEDicordBridgePlugin plugin)
        {
            Plugin = plugin;

            thread = new Thread(() =>
            {
                RegisterDiscord().ConfigureAwait(false).GetAwaiter().GetResult();
            });
            thread.Start();            
        }

        public void Stopdiscord()
        {
            DisconnectDiscord().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private async Task DisconnectDiscord()
        {            
            await discord.DisconnectAsync();
        }

        private Task RegisterDiscord()
        {            
            discord = new DiscordClient(new DiscordConfiguration
            {
                Token = Plugin.Config.BotToken,
                TokenType = TokenType.Bot
            });
            discord.ConnectAsync();

            discord.MessageCreated += Discord_MessageCreated;
            game = new DiscordGame();

            discord.Ready += async e =>
            {
                Ready = true;
                //start message
                if (Plugin.Config.Started.Length > 0)
                    await discord.SendMessageAsync(discord.GetChannelAsync(ulong.Parse(Plugin.Config.StatusChannelId)).Result, Plugin.Config.Started);
            };
            return Task.CompletedTask;
        }

        public void SendStatus(string status)
        {
            if (Ready)
            {
                game.Name = status;
                discord.UpdateStatusAsync(game);
            }            
        }

        public void SendChatMessage(string user, string msg)
        {
            if (Plugin.Config.ChatChannelId.Length > 0)
            {
                DiscordChannel chann = discord.GetChannelAsync(ulong.Parse(Plugin.Config.ChatChannelId)).Result;
                //mention
                msg = MentionNameToID(msg, chann);

                if (user != null)
                {
                    msg = Plugin.Config.Format.Replace("{msg}", msg).Replace("{p}", user);
                }
                discord.SendMessageAsync(chann, msg);
            }            
        }

        public void SendStatusMessage(string user, string msg)
        {
            if (Plugin.Config.StatusChannelId.Length > 0)
            {
                DiscordChannel chann = discord.GetChannelAsync(ulong.Parse(Plugin.Config.StatusChannelId)).Result;
                
                if (user != null)
                {
                    msg = msg.Replace("{p}", user);
                }

                //mention
                msg = MentionNameToID(msg, chann);
                discord.SendMessageAsync(chann, msg);
            }                
        }

        private Task Discord_MessageCreated(DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            if (!e.Author.IsBot)
            {
                if (e.Channel.Id.Equals(ulong.Parse(Plugin.Config.ChatChannelId)))
                {
                    string sender = Plugin.Config.ServerName;

                    if (!Plugin.Config.AsServer)
                        sender = e.Author.Username;
                    
                    Plugin.Torch.Invoke(() =>
                    {
                        var manager = Plugin.Torch.CurrentSession.Managers.GetManager<IChatManagerServer>();
                        manager.SendMessageAsOther(Plugin.Config.Format2.Replace("{p}", sender), MentionIDToName(e.Message), MyFontEnum.White);
                    });                        
                }
                if (e.Channel.Id.Equals(ulong.Parse(Plugin.Config.CommandChannelId)) && e.Message.Content.StartsWith(Plugin.Config.CommandPrefix))
                {
                    Plugin.Torch.Invoke(() =>
                    {
                        string cmd = e.Message.Content.Substring(Plugin.Config.CommandPrefix.Length);
                        var cmdText = new string(cmd.Skip(1).ToArray());
                        DiscordMessage dms = null;

                        if (Plugin.Torch.GameState != TorchGameState.Loaded)                            
                        {
                            dms = discord.SendMessageAsync(e.Channel, "Error: Server is not running.").Result;
                        }
                        else
                        {
                            var manager = Plugin.Torch.CurrentSession.Managers.GetManager<CommandManager>();
                            var command = manager.Commands.GetCommand(cmdText, out string argText);

                            if (command == null)
                            {
                                dms = discord.SendMessageAsync(e.Channel, "R: Command not found: " + cmdText).Result;
                            }
                            else
                            {
                                var cmdPath = string.Join(".", command.Path);
                                var splitArgs = Regex.Matches(argText, "(\"[^\"]+\"|\\S+)").Cast<Match>().Select(x => x.ToString().Replace("\"", "")).ToList();
                                Plugin.Log.Trace($"Invoking {cmdPath} for server.");

                                var context = new SEDBCommandHandler(Plugin.Torch, command.Plugin, Sync.MyId, argText, splitArgs);
                                if (command.TryInvoke(context))
                                {
                                    if (context.Response.ToString().Length > 0)
                                        dms = discord.SendMessageAsync(e.Channel, "R: " + context.Response.ToString()).Result;
                                    Plugin.Log.Info($"Server ran command '{cmd}'");
                                }
                                else
                                {
                                    dms = discord.SendMessageAsync(e.Channel, "R: Error executing command: " + cmdText).Result;
                                }
                            }
                        }                     
                        Task.Delay(10000).ContinueWith(t => dms?.DeleteAsync());
                    });                                          
                }
            }            
            return Task.CompletedTask;
        }

        private string MentionNameToID(string msg, DiscordChannel chann)
        {
            var parts = msg.Split(' ');
            foreach (string part in parts)
            {
                if (part.Length > 2)
                {
                    if (part.StartsWith("@"))
                    {
                        string name = Regex.Replace(part.Substring(1), @"[,#]", "");
                        if (String.Compare(name, "everyone", true) == 0 && !Plugin.Config.MentEveryone)
                        {
                            msg = msg.Replace(part, part.Substring(1));
                            continue;
                        }

                        var members = chann.Guild.GetAllMembersAsync().Result;

                        if (!Plugin.Config.MentOthers)
                        {
                            continue;
                        }
                        if (members.Any(u => String.Compare(u.Username, name, true) == 0))
                        {
                            msg = msg.Replace(part, "<@" + members.Where(u => String.Compare(u.Username, name, true) == 0).First().Id + ">");
                        }
                    }

                    var emojis = chann.Guild.Emojis;
                    if (part.StartsWith(":") && part.EndsWith(":") && emojis.Any(e => String.Compare(e.GetDiscordName(), part, true) == 0))
                    {
                        msg = msg.Replace(part, "<"+ part + emojis.Where(e => String.Compare(e.GetDiscordName(), part, true) == 0).First().Id + ">");
                    }
                }                
            }
            return msg;
        }

        private string MentionIDToName(DiscordMessage ddMsg)
        {
            string msg = ddMsg.Content;
            var parts = msg.Split(' ');
            foreach (string part in parts)
            {
                if (part.StartsWith("<@") && part.EndsWith(">"))
                {
                    ulong id = ulong.Parse(part.Substring(2, part.Length - 3));
                    msg = msg.Replace(part, "@"+discord.GetUserAsync(id).Result.Username);
                }
                if (part.StartsWith("<:") && part.EndsWith(">"))
                {
                    string id = part.Substring(2, part.Length - 3);
                    msg = msg.Replace(part, ":"+ id.Split(':')[0]+":");
                }
            }
            return msg;
        }
    }
}
