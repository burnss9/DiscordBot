using Discord.Commands;
using Example.Attributes;
using Example.Enums;
using System;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using System.Collections;

namespace Example.Modules
{



    [Name("RL")]
    public class PublicModule : ModuleBase<SocketCommandContext>
    {

        ulong LOG_CHANNEL = 278218957679362048;
        int tableSpaces = 8;


        [Command("stats"), Alias("s")]
        [Remarks("View your own stats.")]
        [MinPermissions(AccessLevel.User)]
        public async Task ViewStats()
        {
            string stats = "No stats available.";

            using (SQLiteConnection conn = new SQLiteConnection("data source=C:\\Users\\SamSSD\\Documents\\Visual Studio 2015\\Projects\\RL6Mans\\RL6Mans\\bin\\Debug\\players.db"))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    conn.Open();
                    cmd.CommandText = "SELECT UID, WINS, LOSSES FROM playerScores WHERE UID = $id";
                    cmd.Parameters.AddWithValue("$id", "<@" + Context.User.Id + ">");

                    SQLiteDataReader r = cmd.ExecuteReader();

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

            await ReplyAsync("\u200B" + stats);
        }


        [Command("register")]
        [Remarks("Unecessary command used to add you to the database if you want to see your stats before actually playing a game.")]
        [MinPermissions(AccessLevel.User)]
        public async Task Register(string player)
        {
            using (SQLiteConnection conn = new SQLiteConnection("data source=players.db"))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    conn.Open();
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
        public async Task ReportScore(string p1, string p2, string p3, string wl, string p4, string p5, string p6)
        {

            reportScore(p1, true);
            reportScore(p2, true);
            reportScore(p3, true);
            reportScore(p4, false);
            reportScore(p5, false);
            reportScore(p6, false);

            await ReplyAsync("Score reported.");

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
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    conn.Open();
                    cmd.CommandText = "SELECT UID, WINS, LOSSES FROM playerScores WHERE UID = $id";
                    cmd.Parameters.AddWithValue("$id", player);

                    SQLiteDataReader r = cmd.ExecuteReader();

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
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    conn.Open();
                    cmd.CommandText = "SELECT UID, WINS FROM playerScores ORDER BY WINS DESC";

                    SQLiteDataReader r = cmd.ExecuteReader();

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
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    conn.Open();
                    cmd.CommandText = "SELECT UID, LOSSES FROM playerScores ORDER BY LOSSES DESC";

                    SQLiteDataReader r = cmd.ExecuteReader();

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

            var dm = await (Context.User as SocketGuildUser).CreateDMChannelAsync();
            await dm.SendMessageAsync("\u200B" + losses);
        }

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

        [Command("captains"), Alias("caps", "c")]
        [Remarks("Pick 2 random captains from your queueing channel")]
        [MinPermissions(AccessLevel.User)]
        public async Task PickCaptains()
        {
            string captains = "";

            SocketVoiceChannel v = (Context.User as SocketGuildUser).VoiceChannel;

            if(v == null)
            {
                await ReplyAsync("\u200B" + "You need to be in a queue channel to start picking teams!");
                return;
            }


            if(v.Name != "Queue Channel")
            {
                await ReplyAsync("\u200B" + "You need to be in a queue channel to start picking teams!");
                return;
            }


            if (v.Users.Count == 6)
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

                captains += "Captain 1: " + "<@" + cap1ID + ">" + "\nCaptain 2: " + "<@" + cap2ID + ">";

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
            string stats = "";
            using (SQLiteConnection conn = new SQLiteConnection("data source=C:\\Users\\SamSSD\\Documents\\Visual Studio 2015\\Projects\\RL6Mans\\RL6Mans\\bin\\Debug\\players.db"))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    conn.Open();
                    cmd.CommandText = "SELECT UID, WINS, LOSSES FROM playerScores";

                    SQLiteDataReader r = cmd.ExecuteReader();

                    while (r.Read())
                    {

                        stats += Convert.ToString(r["UID"]) + "```\t\t\t\t\t";

                        stats += Convert.ToString(r["WINS"]) + " / " + Convert.ToString(r["LOSSES"]);
                        if (Convert.ToInt32(r["LOSSES"]) + Convert.ToInt32(r["WINS"]) != 0)
                        {
                            stats += "\tRatio: " + Convert.ToInt32(r["WINS"]) / (float)(Convert.ToInt32(r["LOSSES"]) + Convert.ToInt32(r["WINS"])) + "\n```";
                        }
                        else
                        {
                            stats += "\tNo ratio yet.```\n";
                        }
                    }


                }

            }

            var dm = await (Context.User as SocketGuildUser).CreateDMChannelAsync();
            await dm.SendMessageAsync("\u200B" + stats);

        }



        [Command("admin list")]
        [Remarks("View the list of all player's records.")]
        [MinPermissions(AccessLevel.ServerAdmin)]
        public async Task AdminListAllPlayers()
        {
            string stats = "";
            using (SQLiteConnection conn = new SQLiteConnection("data source=C:\\Users\\SamSSD\\Documents\\Visual Studio 2015\\Projects\\RL6Mans\\RL6Mans\\bin\\Debug\\players.db"))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    conn.Open();
                    cmd.CommandText = "SELECT UID, WINS, LOSSES FROM playerScores";

                    SQLiteDataReader r = cmd.ExecuteReader();

                    while (r.Read())
                    {

                        stats += Convert.ToString(r["UID"]) + "```\t\t\t\t\t";

                        stats += Convert.ToString(r["WINS"]) + " / " + Convert.ToString(r["LOSSES"]);
                        if (Convert.ToInt32(r["LOSSES"]) + Convert.ToInt32(r["WINS"]) != 0)
                        {
                            stats += "\tRatio: " + Convert.ToInt32(r["WINS"]) / (float)(Convert.ToInt32(r["LOSSES"]) + Convert.ToInt32(r["WINS"])) + "\n```";
                        }
                        else
                        {
                            stats += "\tNo ratio yet.```\n";
                        }
                    }


                }

            }

            await ReplyAsync("\u200B" + stats);

        }

        [Command("admin top wins"), Alias("atw")]
        [Remarks("Display the top 10 leaders in wins.")]
        [MinPermissions(AccessLevel.ServerAdmin)]
        public async Task AdminTopWins()
        {
            string wins = "Most wins: \n";

            using (SQLiteConnection conn = new SQLiteConnection("data source=C:\\Users\\SamSSD\\Documents\\Visual Studio 2015\\Projects\\RL6Mans\\RL6Mans\\bin\\Debug\\players.db"))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    conn.Open();
                    cmd.CommandText = "SELECT UID, WINS FROM playerScores ORDER BY WINS DESC";

                    SQLiteDataReader r = cmd.ExecuteReader();

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

            await ReplyAsync("\u200B" + wins);
        }

        [Command("admin top losses"), Alias("atl")]
        [Remarks("Display the top 10 leaders (lol) in losses.")]
        [MinPermissions(AccessLevel.ServerAdmin)]
        public async Task AdminTopLosses()
        {
            string losses = "Most losses: \n";

            using (SQLiteConnection conn = new SQLiteConnection("data source=C:\\Users\\SamSSD\\Documents\\Visual Studio 2015\\Projects\\RL6Mans\\RL6Mans\\bin\\Debug\\players.db"))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    conn.Open();
                    cmd.CommandText = "SELECT UID, LOSSES FROM playerScores ORDER BY LOSSES DESC";

                    SQLiteDataReader r = cmd.ExecuteReader();

                    int count = 0;

                    while (r.Read() && count < 10)
                    {

                        var spaces = tableSpaces - Convert.ToString(r["LOSSES"]).Length;
                        var stringspaces = "";
                        for(int i = 0; i < spaces; i++)
                        {
                            stringspaces += "\u2002";
                        }

                        losses += "`" + Convert.ToString(r["LOSSES"]) + "`" + stringspaces + Convert.ToString(r["UID"]) + "\n";
                        count++;

                    }
                }

            }



            await ReplyAsync("\u200B" + losses);
        }

        [Command("admin top ratio"), Alias("atr")]
        [Remarks("Display the top 10 leaders in ratio.")]
        [MinPermissions(AccessLevel.ServerAdmin)]
        public async Task AdminTopRatios()
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

            await ReplyAsync("\u200B" + ratios);
        }

        [Command("admin lookup"), Alias("al")]
        [Remarks("View someone's stats.")]
        [MinPermissions(AccessLevel.ServerAdmin)]
        public async Task AdminLookupPlayer(string player)
        {
            string stats = "No stats available.";

            using (SQLiteConnection conn = new SQLiteConnection("data source=C:\\Users\\SamSSD\\Documents\\Visual Studio 2015\\Projects\\RL6Mans\\RL6Mans\\bin\\Debug\\players.db"))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    conn.Open();
                    cmd.CommandText = "SELECT UID, WINS, LOSSES FROM playerScores WHERE UID = $id";
                    cmd.Parameters.AddWithValue("$id", player);

                    SQLiteDataReader r = cmd.ExecuteReader();

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

            await ReplyAsync("\u200B" + stats);
        }

        [Command("admin players create"), Alias("apc")]
        [Remarks("Create a player.")]
        [MinPermissions(AccessLevel.ServerAdmin)]
        public async Task AdminCreatePlayer(string player, int wins, int losses)
        {

            using (SQLiteConnection conn = new SQLiteConnection("data source=players.db"))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    conn.Open();
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
        [MinPermissions(AccessLevel.ServerAdmin)]
        public async Task AdminCreatePlayer(string player)
        {

            using (SQLiteConnection conn = new SQLiteConnection("data source=players.db"))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    conn.Open();
                    cmd.CommandText = "DELETE FROM playerScores WHERE UID = $id";
                    cmd.Parameters.AddWithValue("$id", player);
                    cmd.ExecuteNonQuery();

                }

            }

            await ReplyAsync("\u200B" + "Player deleted.");
        }


        [Command("admin players update"), Alias("apu")]
        [Remarks("Update a player record.")]
        [MinPermissions(AccessLevel.ServerAdmin)]
        public async Task AdminUpdatePlayer(string player, int wins, int losses)
        {
            using (SQLiteConnection conn = new SQLiteConnection("data source=players.db"))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    conn.Open();
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
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {

                    conn.Open();
                    cmd.CommandText = "SELECT UID, WINS, LOSSES FROM playerScores WHERE UID = $id;";
                    cmd.Parameters.AddWithValue("$id", uid);

                    SQLiteDataReader r = cmd.ExecuteReader();

                    while (r.Read())
                    {
                        Console.WriteLine(Convert.ToString(r["UID"]));

                        p = new Player(Convert.ToString(r["UID"]), Convert.ToInt32(r["WINS"]), Convert.ToInt32(r["LOSSES"]));
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


    }





}
