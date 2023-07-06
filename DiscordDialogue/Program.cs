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
using System.Drawing;
using System.Net;
using Discord.Rest;

// 8-15 words per sentence
// <@1124775606157058098>

namespace DiscordDialogue
{
    internal class Program
    {
        private DiscordSocketClient _client;
        private CommandService _commands;
        Random rand = new Random(69420);

        public volatile List<guild> guilds = new List<guild>();

        string[] replacements;
        string[] bannedWords;
        string[] quotes;
        char[] delimitors;
        string token;
        Bitmap portrait;
        Bitmap silhouette;
        string root;

        public static void Main(string[] args)
            => new Program().RunBotAsync().GetAwaiter().GetResult();

        static double ConvertBytesToMegabytes(long bytes)
        {
            return Math.Round((bytes / 1024f) / 1024f);
        }
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

            Stopwatch stopw = Stopwatch.StartNew();
            Console.WriteLine("Caching...");
            rand = new Random(DateTime.Now.Millisecond*DateTime.Now.Second);
            root = Directory.GetParent(Application.ExecutablePath).FullName;
            bannedWords = File.ReadAllLines(@"data\banWords.txt");
            replacements = File.ReadAllLines(@"data\replace.txt");
            delimitors = File.ReadAllText(@"data\delimitors.txt").ToCharArray();
            token = File.ReadAllText(@"data\token.txt");
            quotes = File.ReadAllLines(@"data\quotes.txt");
            portrait = (Bitmap)Bitmap.FromFile($@"{root}\data\portrait.png");
            silhouette = (Bitmap)Bitmap.FromFile($@"{root}\data\silhouette.png");
            Directory.CreateDirectory($@"{root}\data\tmp");
            stopw.Stop();
            Console.WriteLine($"Cached! {stopw.ElapsedMilliseconds}MS");

            _client.Log += Log;
            _client.MessageReceived += MessageReceived;
            _client.GuildAvailable += GuildFound;
            

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            await Task.Delay(-1);
        }

        private Task MessageReceived(SocketMessage msg)
        {
            var message = msg as SocketUserMessage;
            var context = new SocketCommandContext(_client, message);
            SocketGuild guild = context.Guild;
            guild _guild = guilds.Find(item => item._guild == guild);
            Process proc = Process.GetCurrentProcess();
            string nickname = "";
            if (_guild == null) Console.WriteLine("Guild was null");


            #region check if eligable
            bool isEligable = true;
            bool isEligableLocal = true;
            foreach (string banWord in bannedWords)
            {
                if (msg.Content.ToLower().Contains(banWord.ToLower()))
                {
                    isEligable = false;
                    isEligableLocal = false;
                }
            }
            if (msg.Content.Split(delimitors).Length < 3)
            {
                isEligable = false;
                isEligableLocal = false;
            }
            if (isEligable)
            {
                using (StreamWriter sw = File.AppendText(@"data\global\globalSentances"))
                {
                    sw.WriteLine("This");
                }
            }
            #endregion

            if (message.Author is SocketGuildUser guildUser)
            {
                var botUser = guildUser.Guild.CurrentUser;
                nickname = botUser.Nickname;
            }
            if (msg.Author.IsBot) return Task.CompletedTask;
            if (msg.Content.Contains("!train"))
            {
                Stopwatch stopw = new Stopwatch();
                stopw.Start();
                IUserMessage _msg = message.ReplyAsync("Training...").Result;
                try
                {
                    Uri uriResult;
                    bool result = Uri.TryCreate(msg.Content.Split(' ')[1], UriKind.Absolute, out uriResult)
                        && uriResult.Scheme == Uri.UriSchemeHttp;
                    if (!result)
                    {
                        using (WebClient client = new WebClient())
                        {
                            //client.Credentials = new NetworkCredential(username, password);
                            client.DownloadFile(msg.Content.Split(' ')[1], $@"data\guilds\{context.Guild.Id}\sentances.txt");
                        }
                        long length = new FileInfo($@"data\guilds\{context.Guild.Id}\sentances.txt").Length / 1000;
                        string[] data = util.train(File.ReadAllLines($@"data\guilds\{context.Guild.Id}\sentances.txt"));
                        File.WriteAllLines($@"data\guilds\{context.Guild.Id}\dataset.txt", data);
                        long length2 = new FileInfo($@"data\guilds\{context.Guild.Id}\dataset.txt").Length / 1000;
                        _msg.ModifyAsync(__msg => __msg.Content = $"Success. {length}KB in {stopw.ElapsedMilliseconds}MS to {length2}KB");
                        //message.ReplyAsync($"Success. {length}KB in {stopw.ElapsedMilliseconds}MS to {length2}KB");
                    }
                    else
                    {
                        message.ReplyAsync($"Invalid url. \"{msg.Content.Split(' ')[1]}\"");
                    }
                }
                catch (Exception ex)
                {
                    message.ReplyAsync($"Invalid url. {ex.Message}");
                }
                stopw.Stop();
            }
            if (msg.Content.Contains("!setchannel"))
            {
                _guild.targetChannel = msg.Channel.Id;
                _guild.Append();
                message.ReplyAsync($"Channel set to: #{msg.Channel.Name} ({msg.Channel.Id})");
            }
            if (msg.Content.Contains("!quote"))
            {
                string quote = quotes[rand.Next(0, quotes.Length - 1)];
                Bitmap bmp = new Bitmap(512, 256);
                Graphics g = Graphics.FromImage(bmp);
                Font nickFont = util.FindFont(g, nickname, new Size(180, 180), SystemFonts.DefaultFont);
                Font quoteFont = util.FindFont(g, StringUtility.WordWrap(quote, 15), new Size(256, 256), SystemFonts.DefaultFont);

                g.DrawImage(portrait, 0, 0, 192, 256);
                g.DrawImage(silhouette, 0,0, 512,256);
                g.DrawString(nickname, nickFont, Brushes.White, Point.Empty);
                g.DrawString(quote, quoteFont, Brushes.White, new Point(190,0));

                bmp.Save($@"{root}\data\tmp\{msg.Id}.png");
                context.Channel.SendFileAsync($@"{root}\data\tmp\{msg.Id}.png", msg.Author.Mention,false,messageReference:message.Reference).Wait();
                File.Delete($@"{root}\data\tmp\{msg.Id}.png");
            }
            if (msg.Content.Contains("!useglobal"))
            {
                _guild.useGlobal = !_guild.useGlobal;
                _guild.Append();
                message.ReplyAsync($"Global dataset set to: {_guild.useGlobal}"); 
            }
            if (msg.Content.Contains("<@1124775606157058098>"))
            {
                #region god help me for what comes next
                Stopwatch stopw = new Stopwatch();
                stopw.Start();  
                string finishedMessage = "";
                EmbedBuilder eb = new EmbedBuilder();

                finishedMessage += "im a nigger";

                eb.AddField($"Famous words of {nickname}",finishedMessage);
                eb.Description = $"||Generated in: {stopw.ElapsedMilliseconds}MS\nMemory used: {ConvertBytesToMegabytes(proc.PrivateMemorySize64)}MB||";
                GC.Collect();
                stopw.Stop();
                message.ReplyAsync(embed:eb.Build());
                #endregion
            }

            guilds[guilds.FindIndex(item => item._guild == guild)] = _guild;
            GC.Collect();

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
                File.CreateText($@"data\guilds\{guild.Id}\bannedWords.txt");
                File.WriteAllText($@"data\guilds\{guild.Id}\useGlobal.txt",bool.FalseString);
            }
            else
            {
                guild gld = new guild(guild);
                gld.Initialize(gld._guild);
                guilds.Add(gld);
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