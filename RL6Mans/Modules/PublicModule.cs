﻿using Discord.Commands;
using Example.Attributes;
using Example.Enums;
using System;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using System.Collections;
using Discord;
using RL6Mans;
using System.IO;
using System.Threading;

namespace Example.Modules
{



    [Name("RL")]
    public class PublicModule : ModuleBase<SocketCommandContext>
    {

        ulong LOG_CHANNEL = 278751518453268489;
        int tableSpaces = 8;

        int listTableSpaces = 20;

        static ArrayList playersInQueue = new ArrayList();
        static ArrayList dmChannelsForQueue = new ArrayList();

        static volatile Semaphore updatingQueueStatusLock = new Semaphore(1, 1);


        [Command("queue"), Alias("q")]
        [Remarks("Queue without joining channel.")]
        [MinPermissions(AccessLevel.User)]
        public async Task Queue()
        {
            if(Context.Channel is IDMChannel)
            {
                await ReplyAsync("Sorry, queueing through DMs doesn't work as well as I had hoped. Please use the server channels.\nI'll try to fix this tomorrow.");
                return;
            }

            var voiceChannel = Context.Client.GetChannel(276557465619922946) as SocketVoiceChannel;
            if (!voiceChannel.Users.Contains(Context.User) && !playersInQueue.Contains(Context.User))
            {
                playersInQueue.Add(Context.User);
                var dm = await Context.User.CreateDMChannelAsync();

                dmChannelsForQueue.Add(dm);

                if (playersInQueue.Count + voiceChannel.Users.Count >= 6)
                {
                    foreach (Discord.Rest.RestDMChannel d in dmChannelsForQueue)
                    {
                        await d.SendMessageAsync("The queue is full! Join the queue channel to begin picking teams!");
                    }

                    dmChannelsForQueue.Clear();
                    playersInQueue.Clear();
                }

                var ch = await Context.User.CreateDMChannelAsync();
                await ch.SendMessageAsync("You have joined the text-only queue.");


                updateQueueStatusMessage(Context.Client);

            }
            else
            {
                var dm = await Context.User.CreateDMChannelAsync();
                if (playersInQueue.Contains(Context.User))
                {

                    await dm.SendMessageAsync("You are already in the text-only queue!");
                }
                if (voiceChannel.Users.Contains(Context.User))
                {
                    await dm.SendMessageAsync("You can't do that while already sitting in the queue channel!");
                }


            }
        }


        [Command("dequeue"), Alias("dq")]
        [Remarks("Dequeue from the text-only queue.")]
        [MinPermissions(AccessLevel.User)]
        public async Task Dequeue()
        {

            if (Context.Channel is IDMChannel)
            {
                await ReplyAsync("Sorry, queueing through DMs doesn't work as well as I had hoped. Please use the server channels.");
                return;
            }

            if (playersInQueue.Contains(Context.User))
            {
                playersInQueue.Remove(Context.User);

                dmChannelsForQueue.Clear();

                foreach (SocketGuildUser u in playersInQueue)
                {
                    var dm = await u.CreateDMChannelAsync();
                    dmChannelsForQueue.Add(dm);
                }
                var ch = await Context.User.CreateDMChannelAsync();
                await ch.SendMessageAsync("You have left the text-only queue.");

                updateQueueStatusMessage(Context.Client);
            }
            else
            {
                var ch = await Context.User.CreateDMChannelAsync();
                await ch.SendMessageAsync("You weren't in the text-only queue to begin with!");
            }
        }


        [Command("stats"), Alias("s")]
        [Remarks("View your own stats.")]
        [MinPermissions(AccessLevel.User)]
        public async Task ViewStats()
        {
            string stats = "No stats available.";

            using (SQLiteConnection conn = new SQLiteConnection("data source=C:\\Users\\SamSSD\\Documents\\Visual Studio 2015\\Projects\\RL6Mans\\RL6Mans\\bin\\Debug\\players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = "SELECT UID, WINS, LOSSES FROM playerScores WHERE UID = $id;";
                    cmd.Parameters.AddWithValue("$id", "<@" + Context.User.Id + ">");

                    using (SQLiteDataReader r = cmd.ExecuteReader())
                    {

                        while (r.Read())
                        {

                            stats = "";

                            stats += Convert.ToString(r["WINS"]) + " / " + Convert.ToString(r["LOSSES"]);
                            if (Convert.ToInt32(r["LOSSES"]) + Convert.ToInt32(r["WINS"]) != 0)
                            {
                                stats += "\nRatio: " + Convert.ToInt32(r["WINS"]) / (float)(Convert.ToInt32(r["LOSSES"]) + Convert.ToInt32(r["WINS"]));
                            }
                            else
                            {
                                stats += "\nNo ratio yet.";
                            }
                        }

                    }

                }

            }

            await ReplyAsync("\u200B" + stats);
        }

        
        [Command("ban")]
        [Remarks("Ban someone from the discord.")]
        [MinPermissions(AccessLevel.User)]
        public async Task BanPlayer(string user)
        {
            await ReplyAsync("\u200B" + "<@" + Context.User.Id + ">" + " has just banned " + user + " from the discord!");
        }

        [Command("kick")]
        [Remarks("Kick someone from the discord.")]
        [MinPermissions(AccessLevel.User)]
        public async Task KickPlayer(string user)
        {
            await ReplyAsync("\u200B" + "<@" + Context.User.Id + ">" + " has just kicked " + user + " from the discord!");
        }

        [Command("register")]
        [Remarks("Unecessary command used to add you to the database if you want to see your stats before actually playing a game.")]
        [MinPermissions(AccessLevel.User)]
        public async Task Register(string player)
        {
            using (SQLiteConnection conn = new SQLiteConnection("data source=players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {

                    cmd.CommandText = "INSERT INTO playerScores (UID, WINS, LOSSES) VALUES ($id, $w, $l);";
                    cmd.Parameters.AddWithValue("$id", player);
                    cmd.Parameters.AddWithValue("$w", 0);
                    cmd.Parameters.AddWithValue("$l", 0);
                    cmd.ExecuteNonQuery();

                }

            }
            await ReplyAsync("\u200B" + "Player registered.");
        }
        

        [Command("report")]
        [Remarks("Report scores."), Alias("rep", "r")]
        [MinPermissions(AccessLevel.User)]
        public async Task ReportScore(string p1, string p2, string p3, int score1, int score2, string p4, string p5, string p6)
        {

            bool firstTeamWon = true;

            if (score1 > score2) firstTeamWon = true;
            if (score2 > score1) firstTeamWon = false;

            if (score1 == score2)
            {
                await ReplyAsync("\u200B" + "The series score doesn't seem right...");
                return;
            }

            string[] pids = { p1, p2, p3, p4, p5, p6 };

            if (firstTeamWon)
            {
                reportMatch(pids, true, score1, score2);
            }
            else
            {
                reportMatch(pids, false, score2, score1);
            }

            reportScore(p1, firstTeamWon);
            reportScore(p2, firstTeamWon);
            reportScore(p3, firstTeamWon);
            reportScore(p4, !firstTeamWon);
            reportScore(p5, !firstTeamWon);
            reportScore(p6, !firstTeamWon);

            await ReplyAsync("\u200B" + "Score reported.");

            await ((Context.Client.GetChannel(LOG_CHANNEL)) as Discord.ITextChannel).SendMessageAsync("\u200B" + Context.Message.ToString() + "```at: " + Context.Message.Timestamp + " by: " + Context.Message.Author + "```");

        }


        [Command("lookup"), Alias("l")]
        [Remarks("View someone's stats.")]
        [MinPermissions(AccessLevel.User)]
        public async Task LookupPlayer(string player)
        {
            string stats = "No stats available.";

            using (SQLiteConnection conn = new SQLiteConnection("data source=C:\\Users\\SamSSD\\Documents\\Visual Studio 2015\\Projects\\RL6Mans\\RL6Mans\\bin\\Debug\\players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {

                    cmd.CommandText = "SELECT UID, WINS, LOSSES FROM playerScores WHERE UID = $id";
                    cmd.Parameters.AddWithValue("$id", player);

                    using (SQLiteDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {

                            stats = "";

                            stats += Convert.ToString(r["WINS"]) + " / " + Convert.ToString(r["LOSSES"]);
                            if (Convert.ToInt32(r["LOSSES"]) + Convert.ToInt32(r["WINS"]) != 0)
                            {
                                stats += "\nRatio: " + Convert.ToInt32(r["WINS"]) / (float)(Convert.ToInt32(r["LOSSES"]) + Convert.ToInt32(r["WINS"]));
                            }
                            else
                            {
                                stats += "\nNo ratio yet.";
                            }
                        }
                    }

                }

            }
            var dm = await (Context.User as SocketGuildUser).CreateDMChannelAsync();
            await dm.SendMessageAsync("\u200B" + stats);
        }


        [Command("top wins"), Alias("tw")]
        [Remarks("Display the top 10 leaders in wins.")]
        [MinPermissions(AccessLevel.User)]
        public async Task TopWins()
        {
            string wins = "Most wins: \n";

            using (SQLiteConnection conn = new SQLiteConnection("data source=C:\\Users\\SamSSD\\Documents\\Visual Studio 2015\\Projects\\RL6Mans\\RL6Mans\\bin\\Debug\\players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {

                    cmd.CommandText = "SELECT UID, WINS FROM playerScores ORDER BY WINS DESC";

                    using (SQLiteDataReader r = cmd.ExecuteReader())
                    {

                        int count = 0;

                        while (r.Read() && count < 10)
                        {

                            var spaces = tableSpaces - Convert.ToString(r["WINS"]).Length;
                            var stringspaces = "";
                            for (int i = 0; i < spaces; i++)
                            {
                                stringspaces += "\u2002";
                            }

                            wins += "`" + Convert.ToString(r["WINS"]) + "`" + stringspaces + Convert.ToString(r["UID"]) + "\n";
                            count++;
                        }
                    }

                }

            }
            var dm = await (Context.User as SocketGuildUser).CreateDMChannelAsync();
            await dm.SendMessageAsync("\u200B" + wins);
        }

        [Command("top losses"), Alias("tl")]
        [Remarks("Display the top 10 leaders (lol) in losses.")]
        [MinPermissions(AccessLevel.User)]
        public async Task TopLosses()
        {
            string losses = "Most losses: \n";

            using (SQLiteConnection conn = new SQLiteConnection("data source=C:\\Users\\SamSSD\\Documents\\Visual Studio 2015\\Projects\\RL6Mans\\RL6Mans\\bin\\Debug\\players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {

                    cmd.CommandText = "SELECT UID, LOSSES FROM playerScores ORDER BY LOSSES DESC";

                    using (SQLiteDataReader r = cmd.ExecuteReader())
                    {

                        int count = 0;

                        while (r.Read() && count < 10)
                        {
                            var spaces = tableSpaces - Convert.ToString(r["LOSSES"]).Length;
                            var stringspaces = "";
                            for (int i = 0; i < spaces; i++)
                            {
                                stringspaces += "\u2002";
                            }

                            losses += "`" + Convert.ToString(r["LOSSES"]) + "`" + stringspaces + Convert.ToString(r["UID"]) + "\n";
                            count++;
                        }
                    }
                }

            }

            var dm = await (Context.User as SocketGuildUser).CreateDMChannelAsync();
            await dm.SendMessageAsync("\u200B" + losses);
        }

        //Top Ratio 
        /*
                [Command("top ratio"), Alias("tr")]
                [Remarks("Display the top 10 leaders in ratio.")]
                [MinPermissions(AccessLevel.User)]
                public async Task TopRatios()
                {
                    string ratios = "Ratio list is certainly broken (temporarily) sorry: \n";

                    using (SQLiteConnection conn = new SQLiteConnection("data source=C:\\Users\\SamSSD\\Documents\\Visual Studio 2015\\Projects\\RL6Mans\\RL6Mans\\bin\\Debug\\players.db"))
                    {
                        using (SQLiteCommand cmd = new SQLiteCommand(conn))
                        {
                            conn.Open();
                            cmd.CommandText = "SELECT UID, WINS, LOSSES FROM playerScores ORDER BY WINS/(WINS+LOSSES) DESC";

                            SQLiteDataReader r = cmd.ExecuteReader();

                            int count = 0;

                            while (r.Read() && count < 10)
                            {

                                if (Convert.ToInt32(r["LOSSES"]) + Convert.ToInt32(r["WINS"]) == 0)
                                {
                                    break;
                                }

                                ratios += (Convert.ToInt32(r["WINS"]) / (float)(Convert.ToInt32(r["LOSSES"]) + Convert.ToInt32(r["WINS"]))).ToString("0.000") + "\t\t\t\t" + Convert.ToString(r["UID"]) + "\n";
                                count++;
                            }
                        }

                    }

                    var dm = await (Context.User as SocketGuildUser).CreateDMChannelAsync();
                    await dm.SendMessageAsync("\u200B" + ratios);
                }
        */


        [Command("captains"), Alias("caps", "c")]
        [Remarks("Pick 2 random captains from your queueing channel")]
        [MinPermissions(AccessLevel.User)]
        public async Task PickCaptains()
        {
            string captains = "";

            SocketVoiceChannel v = (Context.User as SocketGuildUser).VoiceChannel;

            if (v == null)
            {
                await ReplyAsync("\u200B" + "You need to be in a queue channel to start picking teams!");
                return;
            }


            if (v.Name != "Queue Channel" && v.Name != "Queue Channel 2" && v.Name != "Queue Channel 3" && v.Name != "The Bathroom")
            {
                await ReplyAsync("\u200B" + "You need to be in a queue channel to start picking teams!");
                return;
            }


            if (v.Users.Count >= 4)
            {
                var userList = v.Users;

                var users = new ArrayList();

                foreach (SocketGuildUser u in userList)
                {
                    users.Add(u.Id.ToString());
                }

                Random r = new Random();

                int cap1 = r.Next(users.Count);
                string cap1ID = (string)users[cap1];
                users.RemoveAt(cap1);

                int cap2 = r.Next(users.Count);
                string cap2ID = (string)users[cap2];
                users.RemoveAt(cap2);

                if (r.Next(2) == 1)
                {
                    captains += "Captain 1: " + "<@" + cap1ID + ">" + "\nCaptain 2: " + "<@" + cap2ID + ">";
                }
                else
                {
                    captains += "Captain 1: " + "<@" + cap2ID + ">" + "\nCaptain 2: " + "<@" + cap1ID + ">";
                }

                await ReplyAsync("\u200B" + captains);

            }
            else
            {
                await ReplyAsync("\u200B" + "You need 6 people to start picking teams!");
            }


        }


        [Command("list")]
        [Remarks("View the list of all player's records.")]
        [MinPermissions(AccessLevel.User)]
        public async Task ListAllPlayers()
        {
            var dm = await (Context.User as SocketGuildUser).CreateDMChannelAsync();

            string stats = "";
            using (SQLiteConnection conn = new SQLiteConnection("data source=C:\\Users\\SamSSD\\Documents\\Visual Studio 2015\\Projects\\RL6Mans\\RL6Mans\\bin\\Debug\\players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {

                    cmd.CommandText = "SELECT UID, WINS, LOSSES FROM playerScores ORDER BY WINS;";

                    using (SQLiteDataReader r = cmd.ExecuteReader())
                    {

                        while (r.Read())
                        {

                            stats += "`";

                            stats += Convert.ToString(r["WINS"]) + " / " + Convert.ToString(r["LOSSES"]);
                            if (Convert.ToInt32(r["LOSSES"]) + Convert.ToInt32(r["WINS"]) != 0)
                            {
                                var spaces = listTableSpaces - ((Convert.ToInt32(r["WINS"]) / (float)(Convert.ToInt32(r["LOSSES"]) + Convert.ToInt32(r["WINS"]))).ToString("0.000") + Convert.ToString(r["WINS"]) + " / " + Convert.ToString(r["LOSSES"])).Length;
                                var stringspaces = "";
                                for (int i = 0; i < spaces; i++)
                                {
                                    stringspaces += "\u2002";
                                }


                                stats += "\tRatio: " + (Convert.ToInt32(r["WINS"]) / (float)(Convert.ToInt32(r["LOSSES"]) + Convert.ToInt32(r["WINS"]))).ToString("0.000") + "`" + stringspaces + Convert.ToString(r["UID"]) + "\n";
                            }
                            else
                            {
                                var spaces = listTableSpaces - ("    No ratio yet." + Convert.ToString(r["WINS"]) + " / " + Convert.ToString(r["LOSSES"])).Length;
                                var stringspaces = "";
                                for (int i = 0; i < spaces; i++)
                                {
                                    stringspaces += "\u2002";
                                }


                                stats += "    No ratio yet.`                    " + stringspaces + Convert.ToString(r["UID"]) + "\n";
                            }

                            if (stats.Length > 1000)
                            {
                                await dm.SendMessageAsync("\u200B" + stats);
                                stats = "";
                            }

                        }

                    }
                }

            }

            await dm.SendMessageAsync("\u200B" + stats);

        }



        [Command("matches")]
        [Remarks("View the list of all your played matches.")]
        [MinPermissions(AccessLevel.User)]
        public async Task ListMyMatches()
        {
            var dm = await (Context.User as SocketGuildUser).CreateDMChannelAsync();

            string stats = "";
            using (SQLiteConnection conn = new SQLiteConnection("data source=C:\\Users\\SamSSD\\Documents\\Visual Studio 2015\\Projects\\RL6Mans\\RL6Mans\\bin\\Debug\\players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {

                    cmd.CommandText = "SELECT W1, W2, W3, L1, L2, L3, WINS, LOSSES FROM NormalMatches WHERE $pid = W1 OR $pid = W2 OR $pid = W3 OR $pid = L1 OR $pid = L2 OR $pid = L3;";
                    cmd.Parameters.AddWithValue("$pid", "<@" + Context.User.Id + ">");

                    using (SQLiteDataReader r = cmd.ExecuteReader())
                    {

                        while (r.Read())
                        {

                            stats += Convert.ToString(r["W1"]) + " " + Convert.ToString(r["W2"]) + " " + Convert.ToString(r["W3"]) + " " +
                                     Convert.ToString(r["WINS"]) + " " + Convert.ToString(r["LOSSES"]) + " " + Convert.ToString(r["L1"]) + " " +
                                     Convert.ToString(r["L2"]) + " " + Convert.ToString(r["L3"] + "\n");

                            if (stats.Length > 1000)
                            {
                                await dm.SendMessageAsync("\u200B" + stats);
                                stats = "";
                            }
                        }


                    }

                }
            }
            await dm.SendMessageAsync("\u200B" + stats);

        }

        [Command("matches")]
        [Remarks("View the list of all your played matches.")]
        [MinPermissions(AccessLevel.User)]
        public async Task ListPlayerMatches(string player)
        {
            var dm = await (Context.User as SocketGuildUser).CreateDMChannelAsync();

            string stats = "";
            using (SQLiteConnection conn = new SQLiteConnection("data source=C:\\Users\\SamSSD\\Documents\\Visual Studio 2015\\Projects\\RL6Mans\\RL6Mans\\bin\\Debug\\players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {

                    cmd.CommandText = "SELECT W1, W2, W3, L1, L2, L3, WINS, LOSSES FROM NormalMatches WHERE $pid = W1 OR $pid = W2 OR $pid = W3 OR $pid = L1 OR $pid = L2 OR $pid = L3;";
                    cmd.Parameters.AddWithValue("$pid", player);

                    using (SQLiteDataReader r = cmd.ExecuteReader())
                    {

                        while (r.Read())
                        {

                            stats += Convert.ToString(r["W1"]) + " " + Convert.ToString(r["W2"]) + " " + Convert.ToString(r["W3"]) + " " +
                                     Convert.ToString(r["WINS"]) + " " + Convert.ToString(r["LOSSES"]) + " " + Convert.ToString(r["L1"]) + " " +
                                     Convert.ToString(r["L2"]) + " " + Convert.ToString(r["L3"] + "\n");

                            if (stats.Length > 1000)
                            {
                                await dm.SendMessageAsync("\u200B" + stats);
                                stats = "";
                            }
                        }


                    }

                }
            }
            await dm.SendMessageAsync("\u200B" + stats);

        }



        [Command("matches all")]
        [Remarks("View the list of all your played matches.")]
        [MinPermissions(AccessLevel.User)]
        public async Task ListAllMatches()
        {
            var dm = await (Context.User as SocketGuildUser).CreateDMChannelAsync();

            string stats = "";
            using (SQLiteConnection conn = new SQLiteConnection("data source=C:\\Users\\SamSSD\\Documents\\Visual Studio 2015\\Projects\\RL6Mans\\RL6Mans\\bin\\Debug\\players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {

                    cmd.CommandText = "SELECT W1, W2, W3, L1, L2, L3, WINS, LOSSES FROM NormalMatches";

                    using (SQLiteDataReader r = cmd.ExecuteReader())
                    {

                        while (r.Read())
                        {

                            stats += Convert.ToString(r["W1"]) + " " + Convert.ToString(r["W2"]) + " " + Convert.ToString(r["W3"]) + " " +
                                     Convert.ToString(r["WINS"]) + " " + Convert.ToString(r["LOSSES"]) + " " + Convert.ToString(r["L1"]) + " " +
                                     Convert.ToString(r["L2"]) + " " + Convert.ToString(r["L3"] + "\n");

                            if (stats.Length > 1000)
                            {
                                await dm.SendMessageAsync("\u200B" + stats);
                                stats = "";
                            }
                        }


                    }

                }
            }
            await dm.SendMessageAsync("\u200B" + stats);

        }



        [Command("admin list")]
        [Remarks("View the list of all player's records.")]
        [MinPermissions(AccessLevel.ServerMod)]
        public async Task AdminListAllPlayers()
        {
            var dm = await (Context.User as SocketGuildUser).CreateDMChannelAsync();

            string stats = "";
            using (SQLiteConnection conn = new SQLiteConnection("data source=C:\\Users\\SamSSD\\Documents\\Visual Studio 2015\\Projects\\RL6Mans\\RL6Mans\\bin\\Debug\\players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {

                    cmd.CommandText = "SELECT UID, WINS, LOSSES FROM playerScores ORDER BY WINS";

                    using (SQLiteDataReader r = cmd.ExecuteReader())
                    {

                        while (r.Read())
                        {

                            stats += "`";

                            stats += Convert.ToString(r["WINS"]) + " / " + Convert.ToString(r["LOSSES"]);
                            if (Convert.ToInt32(r["LOSSES"]) + Convert.ToInt32(r["WINS"]) != 0)
                            {
                                var spaces = listTableSpaces - ((Convert.ToInt32(r["WINS"]) / (float)(Convert.ToInt32(r["LOSSES"]) + Convert.ToInt32(r["WINS"]))).ToString("0.000") + Convert.ToString(r["WINS"]) + " / " + Convert.ToString(r["LOSSES"])).Length;
                                var stringspaces = "";
                                for (int i = 0; i < spaces; i++)
                                {
                                    stringspaces += "\u2002";
                                }


                                stats += "\tRatio: " + (Convert.ToInt32(r["WINS"]) / (float)(Convert.ToInt32(r["LOSSES"]) + Convert.ToInt32(r["WINS"]))).ToString("0.000") + "`" + stringspaces + Convert.ToString(r["UID"]) + "\n";
                            }
                            else
                            {
                                var spaces = listTableSpaces - ("    No ratio yet." + Convert.ToString(r["WINS"]) + " / " + Convert.ToString(r["LOSSES"])).Length;
                                var stringspaces = "";
                                for (int i = 0; i < spaces; i++)
                                {
                                    stringspaces += "\u2002";
                                }

                                string pidString = Convert.ToString(r["UID"]);
                                ulong pid = Convert.ToUInt64(pidString.Substring(2, pidString.Length - 3));
                                Console.WriteLine("converted");
                                SocketGuildUser u = Context.Client.GetUser(pid) as SocketGuildUser;
                                Console.WriteLine("got user");
                                string username = u.Username;
                                Console.WriteLine("got name");
                                stats += "    No ratio yet.`                    " + stringspaces + username + "\n";
                            }

                            if (stats.Length > 1000)
                            {
                                await ReplyAsync("\u200B" + stats);
                                stats = "";
                            }

                        }

                    }
                }

            }

            await ReplyAsync("\u200B" + stats);

        }

        [Command("admin top wins"), Alias("atw")]
        [Remarks("Display the top 10 leaders in wins.")]
        [MinPermissions(AccessLevel.ServerMod)]
        public async Task AdminTopWins()
        {
            string wins = "Most wins: \n";

            using (SQLiteConnection conn = new SQLiteConnection("data source=C:\\Users\\SamSSD\\Documents\\Visual Studio 2015\\Projects\\RL6Mans\\RL6Mans\\bin\\Debug\\players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {

                    cmd.CommandText = "SELECT UID, WINS FROM playerScores ORDER BY WINS DESC";

                    using (SQLiteDataReader r = cmd.ExecuteReader())
                    {

                        int count = 0;

                        while (r.Read() && count < 10)
                        {
                            var spaces = tableSpaces - Convert.ToString(r["WINS"]).Length;
                            var stringspaces = "";
                            for (int i = 0; i < spaces; i++)
                            {
                                stringspaces += "\u2002";
                            }

                            wins += "`" + Convert.ToString(r["WINS"]) + "`" + stringspaces + Convert.ToString(r["UID"]) + "\n";
                            count++;
                        }
                    }

                }

            }

            await ReplyAsync("\u200B" + wins);
        }

        [Command("admin top losses"), Alias("atl")]
        [Remarks("Display the top 10 leaders (lol) in losses.")]
        [MinPermissions(AccessLevel.ServerMod)]
        public async Task AdminTopLosses()
        {
            string losses = "Most losses: \n";

            using (SQLiteConnection conn = new SQLiteConnection("data source=C:\\Users\\SamSSD\\Documents\\Visual Studio 2015\\Projects\\RL6Mans\\RL6Mans\\bin\\Debug\\players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {

                    cmd.CommandText = "SELECT UID, LOSSES FROM playerScores ORDER BY LOSSES DESC";

                    using (SQLiteDataReader r = cmd.ExecuteReader())
                    {

                        int count = 0;

                        while (r.Read() && count < 10)
                        {

                            var spaces = tableSpaces - Convert.ToString(r["LOSSES"]).Length;
                            var stringspaces = "";
                            for (int i = 0; i < spaces; i++)
                            {
                                stringspaces += "\u2002";
                            }

                            losses += "`" + Convert.ToString(r["LOSSES"]) + "`" + stringspaces + Convert.ToString(r["UID"]) + "\n";
                            count++;

                        }
                    }
                }

            }



            await ReplyAsync("\u200B" + losses);
        }

        [Command("admin top ratio"), Alias("atr")]
        [Remarks("Display the top 10 leaders in ratio.")]
        [MinPermissions(AccessLevel.ServerMod)]
        public async Task AdminTopRatios()
        {
            string ratios = "Ratio list is certainly broken (temporarily) sorry: \n";

            using (SQLiteConnection conn = new SQLiteConnection("data source=C:\\Users\\SamSSD\\Documents\\Visual Studio 2015\\Projects\\RL6Mans\\RL6Mans\\bin\\Debug\\players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {

                    cmd.CommandText = "SELECT UID, WINS, LOSSES FROM playerScores ORDER BY WINS/(WINS+LOSSES) DESC";

                    using (SQLiteDataReader r = cmd.ExecuteReader())
                    {
                        int count = 0;

                        while (r.Read() && count < 10)
                        {

                            if (Convert.ToInt32(r["LOSSES"]) + Convert.ToInt32(r["WINS"]) == 0)
                            {
                                break;
                            }

                            ratios += (Convert.ToInt32(r["WINS"]) / (float)(Convert.ToInt32(r["LOSSES"]) + Convert.ToInt32(r["WINS"]))).ToString("0.000") + "\t\t\t\t" + Convert.ToString(r["UID"]) + "\n";
                            count++;
                        }

                    }
                }

            }

            await ReplyAsync("\u200B" + ratios);
        }

        [Command("admin lookup"), Alias("al")]
        [Remarks("View someone's stats.")]
        [MinPermissions(AccessLevel.ServerMod)]
        public async Task AdminLookupPlayer(string player)
        {
            string stats = "No stats available.";

            using (SQLiteConnection conn = new SQLiteConnection("data source=C:\\Users\\SamSSD\\Documents\\Visual Studio 2015\\Projects\\RL6Mans\\RL6Mans\\bin\\Debug\\players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {

                    cmd.CommandText = "SELECT UID, WINS, LOSSES FROM playerScores WHERE UID = $id";
                    cmd.Parameters.AddWithValue("$id", player);

                    using (SQLiteDataReader r = cmd.ExecuteReader())
                    {

                        while (r.Read())
                        {

                            stats = "";

                            stats += Convert.ToString(r["WINS"]) + " / " + Convert.ToString(r["LOSSES"]);
                            if (Convert.ToInt32(r["LOSSES"]) + Convert.ToInt32(r["WINS"]) != 0)
                            {
                                stats += "\nRatio: " + Convert.ToInt32(r["WINS"]) / (float)(Convert.ToInt32(r["LOSSES"]) + Convert.ToInt32(r["WINS"]));
                            }
                            else
                            {
                                stats += "\nNo ratio yet.";
                            }
                        }
                    }

                }

            }

            await ReplyAsync("\u200B" + stats);
        }

        [Command("admin players create"), Alias("apc")]
        [Remarks("Create a player.")]
        [MinPermissions(AccessLevel.ServerMod)]
        public async Task AdminCreatePlayer(string player, int wins, int losses)
        {

            using (SQLiteConnection conn = new SQLiteConnection("data source=players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {

                    cmd.CommandText = "INSERT INTO playerScores (UID, WINS, LOSSES) VALUES ($id, $w, $l);";
                    cmd.Parameters.AddWithValue("$id", player);
                    cmd.Parameters.AddWithValue("$w", wins);
                    cmd.Parameters.AddWithValue("$l", losses);
                    cmd.ExecuteNonQuery();

                }

            }

            await ReplyAsync("\u200B" + "Player created.");
        }

        [Command("admin players delete"), Alias("delete")]
        [Remarks("Delete a player.")]
        [MinPermissions(AccessLevel.ServerMod)]
        public async Task AdminCreatePlayer(string player)
        {

            using (SQLiteConnection conn = new SQLiteConnection("data source=players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {

                    cmd.CommandText = "DELETE FROM playerScores WHERE UID = $id";
                    cmd.Parameters.AddWithValue("$id", player);
                    cmd.ExecuteNonQuery();

                }

            }

            await ReplyAsync("\u200B" + "Player deleted.");
        }


        [Command("admin players update"), Alias("apu")]
        [Remarks("Update a player record.")]
        [MinPermissions(AccessLevel.ServerMod)]
        public async Task AdminUpdatePlayer(string player, int wins, int losses)
        {
            using (SQLiteConnection conn = new SQLiteConnection("data source=players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {

                    cmd.CommandText = "UPDATE playerScores SET WINS = $w, LOSSES = $l WHERE UID = $id;";
                    cmd.Parameters.AddWithValue("$id", player);
                    cmd.Parameters.AddWithValue("$w", wins);
                    cmd.Parameters.AddWithValue("$l", losses);
                    cmd.ExecuteNonQuery();

                }

            }
            await ReplyAsync("\u200B" + "Player updated.");
        }


        private void reportScore(string uid, bool won)
        {

            Player p = null;

            using (SQLiteConnection conn = new SQLiteConnection("data source=C:\\Users\\SamSSD\\Documents\\Visual Studio 2015\\Projects\\RL6Mans\\RL6Mans\\bin\\Debug\\players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {


                    cmd.CommandText = "SELECT UID, WINS, LOSSES FROM playerScores WHERE UID = $id;";
                    cmd.Parameters.AddWithValue("$id", uid);

                    using (SQLiteDataReader r = cmd.ExecuteReader())
                    {

                        while (r.Read())
                        {
                            Console.WriteLine(Convert.ToString(r["UID"]));

                            p = new Player(Convert.ToString(r["UID"]), Convert.ToInt32(r["WINS"]), Convert.ToInt32(r["LOSSES"]));
                        }

                    }

                    if (p != null)
                    {
                        if (won)
                        {
                            p.wins++;
                        }
                        else
                        {
                            p.losses++;
                        }

                        using (SQLiteCommand cmd2 = new SQLiteCommand(conn))
                        {

                            cmd2.CommandText = "UPDATE playerScores SET WINS = $w, LOSSES = $l WHERE UID = $id;";
                            cmd2.Parameters.AddWithValue("$id", p.UID);
                            cmd2.Parameters.AddWithValue("$w", p.wins);
                            cmd2.Parameters.AddWithValue("$l", p.losses);

                            cmd2.ExecuteNonQuery();
                        }
                    }

                    else
                    {
                        using (SQLiteCommand cmd2 = new SQLiteCommand(conn))
                        {
                            cmd2.CommandText = "INSERT INTO playerScores (UID, WINS, LOSSES) VALUES ($id, $w, $l);";
                            cmd2.Parameters.AddWithValue("$id", uid);
                            cmd2.Parameters.AddWithValue("$w", won ? 1 : 0);
                            cmd2.Parameters.AddWithValue("$l", won ? 0 : 1);
                            cmd2.ExecuteNonQuery();
                        }
                    }

                }

            }

        }

        private void reportMatch(string[] ids, bool first3won, int wins, int losses)
        {

            using (SQLiteConnection conn = new SQLiteConnection("data source=C:\\Users\\SamSSD\\Documents\\Visual Studio 2015\\Projects\\RL6Mans\\RL6Mans\\bin\\Debug\\players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {

                    cmd.CommandText = "INSERT INTO NormalMatches (W1, W2, W3, L1, L2, L3, WINS, LOSSES) VALUES ($w1, $w2, $w3, $l1, $l2, $l3, $wins, $losses);";
                    cmd.Parameters.AddWithValue("$w1", ids[0]);
                    cmd.Parameters.AddWithValue("$w2", ids[1]);
                    cmd.Parameters.AddWithValue("$w3", ids[2]);
                    cmd.Parameters.AddWithValue("$l1", ids[3]);
                    cmd.Parameters.AddWithValue("$l2", ids[4]);
                    cmd.Parameters.AddWithValue("$l3", ids[5]);
                    cmd.Parameters.AddWithValue("$wins", wins);
                    cmd.Parameters.AddWithValue("$losses", losses);

                    cmd.ExecuteNonQuery();

                }

            }

        }


        public static async void removeFromQueue(SocketGuildUser user)
        {
            if (playersInQueue.Contains(user))
            {
                playersInQueue.Remove(user);

                dmChannelsForQueue.Clear();

                foreach (SocketGuildUser u in playersInQueue)
                {
                    var dm = await u.CreateDMChannelAsync();
                    dmChannelsForQueue.Add(dm);
                }
            }
        }

        public static async void checkForFullQueueOnVoiceJoin(SocketVoiceChannel v)
        {
            if (playersInQueue.Count + v.Users.Count >= 6)
            {

                foreach (Discord.Rest.RestDMChannel d in dmChannelsForQueue)
                {
                    await d.SendMessageAsync("The queue is full! Join the queue channel to begin picking teams!");
                }

                dmChannelsForQueue.Clear();
                playersInQueue.Clear();
            }


        }


        public static async void updateQueueStatusMessage(DiscordSocketClient client)
        {
            var channel = (client.GetChannel(280243653291802625) as SocketTextChannel);
            var vchannel = (client.GetChannel(276557465619922946) as SocketVoiceChannel);


            updatingQueueStatusLock.WaitOne();
            {
                var messages = await channel.GetMessagesAsync(100).Flatten();

                await channel.DeleteMessagesAsync(messages);
                var bytearray = ImageCreation.CreateQueueImage(playersInQueue.Count, vchannel.Users.Count);

                using (var mem = new MemoryStream(bytearray))
                {
                    mem.Seek(0, SeekOrigin.Begin);
                    await channel.SendFileAsync(mem, "queue.png", "Info:");
                }
                await channel.SendMessageAsync("The text-only queue(green) currently has " + playersInQueue.Count + (playersInQueue.Count == 1 ? " player" : " players") + " in it.\nThe voice queue(blue) has " + vchannel.Users.Count + ".");

            }
            updatingQueueStatusLock.Release();


        }

    }


}




