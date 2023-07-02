﻿using Discord.Commands;
using Discord.WebSocket;
using Discord;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Forms;

// 8-15 words per sentence
// <@1124775606157058098>

namespace DiscordDialogue
{
    internal class Program
    {
        private DiscordSocketClient _client;
        private CommandService _commands;
        Random rand = new Random(69420);

        string[] replacements;
        string[] bannedWords;

        public static void Main(string[] args)
            => new Program().RunBotAsync().GetAwaiter().GetResult();

        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }
        public async Task RunBotAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
            });
            _commands = new CommandService();

            _client.Log += Log;
            _client.MessageReceived += MessageReceived;
            _client.GuildAvailable += GuildFound;

            await _client.LoginAsync(TokenType.Bot, File.ReadAllText(@"data\token.txt"));
            await _client.StartAsync();
            await Task.Delay(-1);
        }

        private Task MessageReceived(SocketMessage msg)
        {
            var message = msg as SocketUserMessage;
            var context = new SocketCommandContext(_client, message);
            SocketGuild guild = context.Guild;

            if (msg.Author.IsBot)
                return Task.CompletedTask;
            if (msg.Content == "!train")
            {

            }
            if (msg.Content.Contains("!setchannel"))
            {
                File.WriteAllText($@"data\guilds\{guild.Id}\channel.txt", msg.Channel.Id.ToString());
                message.ReplyAsync($"Channel set to: {msg.Channel.Name}({msg.Channel.Id})");
            }
            if (msg.Content.Contains("<@1124775606157058098>"))
            {
                #region god help me for what comes next
                Stopwatch stopw = new Stopwatch();
                stopw.Start();
                string finishedMessage = "";
                


                stopw.Stop();
                finishedMessage += $"\n||Generated in: {stopw.ElapsedMilliseconds}MS||";
                message.ReplyAsync(finishedMessage);
                #endregion
            }
            return Task.CompletedTask;
        }
        private Task GuildFound(SocketGuild guild)
        {
            string[] dirs = Directory.GetDirectories(@"data\guilds");
            ulong[] storedGuilds = new ulong[dirs.Length];
            bool isStored = false;

            for (int i = 0; i < dirs.Length; i++)
            {
                ulong unique = ulong.Parse(Path.GetFileName(dirs[i]));
                storedGuilds[i] = unique;
                if (guild.Id == unique) isStored = true;
            }
            if (isStored == false)
            {
                Directory.CreateDirectory($@"data\guilds\{guild.Id}");
                File.CreateText($@"data\guilds\{guild.Id}\sentances.txt");
                File.CreateText($@"data\guilds\{guild.Id}\dataset.txt");
                File.CreateText($@"data\guilds\{guild.Id}\channel.txt");
            }

            return Task.CompletedTask;
        }
    }
}
/*
 * using (StreamWriter sw = File.AppendText(path))
        {
            sw.WriteLine("This");
            sw.WriteLine("is Extra");
            sw.WriteLine("Text");
        }	
*/