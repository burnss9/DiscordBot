//using Discord;
//using Discord.Commands;
//using System.Threading.Tasks;
//using System.Data.SQLite;
//using System;
//using System.Collections;
//using Discord.WebSocket;

//using Example;

//namespace RL6Mans
//{
//    class RLBot
//    {

//        DiscordSocketClient discord;
//        CommandHandler commands;

//        ulong adminID = 138176392641642498;

//        public RLBot()
//        { }

//        public async Task Start()
//        {
//            discord = new DiscordSocketClient();

//            var token = "Mjc3OTgzNTY2MTk1ODUxMjY0.C3lutQ.De39RFAWQFKIdhzNwlXvMJxuRYk";

//            await discord.LoginAsync(TokenType.Bot, token);
//            await discord.ConnectAsync();

//            var map = new DependencyMap();
//            map.Add(discord);

//            commands = new CommandHandler();
//            await commands.Install(discord);

//            await Task.Delay(-1);
//        }

//        private Task Log(LogMessage msg)
//        {
//            Console.WriteLine(msg.ToString());
//            return Task.CompletedTask;
//        }



//        private void reportScore(string uid, bool won)
//        {

//            Player p = null;

//            using (SQLiteConnection conn = new SQLiteConnection("data source=C:\\Users\\SamSSD\\Documents\\Visual Studio 2015\\Projects\\RL6Mans\\RL6Mans\\bin\\Debug\\players.db"))
//            {
//                using (SQLiteCommand cmd = new SQLiteCommand(conn))
//                {

//                    conn.Open();
//                    cmd.CommandText = "SELECT UID, WINS, LOSSES FROM playerScores WHERE UID = $id;";
//                    cmd.Parameters.AddWithValue("$id", uid);

//                    SQLiteDataReader r = cmd.ExecuteReader();

//                    while (r.Read())
//                    {
//                        Console.WriteLine(Convert.ToString(r["UID"]));

//                        p = new Player(Convert.ToString(r["UID"]), Convert.ToInt32(r["WINS"]), Convert.ToInt32(r["LOSSES"]));
//                    }



//                    if (p != null)
//                    {
//                        if (won)
//                        {
//                            p.wins++;
//                        }
//                        else
//                        {
//                            p.losses++;
//                        }

//                        using (SQLiteCommand cmd2 = new SQLiteCommand(conn))
//                        {

//                            cmd2.CommandText = "UPDATE playerScores SET WINS = $w, LOSSES = $l WHERE UID = $id;";
//                            cmd2.Parameters.AddWithValue("$id", p.UID);
//                            cmd2.Parameters.AddWithValue("$w", p.wins);
//                            cmd2.Parameters.AddWithValue("$l", p.losses);

//                            cmd2.ExecuteNonQuery();
//                        }
//                    }

//                    else
//                    {
//                        using (SQLiteCommand cmd2 = new SQLiteCommand(conn))
//                        {
//                            cmd2.CommandText = "INSERT INTO playerScores (UID, WINS, LOSSES) VALUES ($id, $w, $l);";
//                            cmd2.Parameters.AddWithValue("$id", uid);
//                            cmd2.Parameters.AddWithValue("$w", won ? 1 : 0);
//                            cmd2.Parameters.AddWithValue("$l", won ? 0 : 1);
//                            cmd2.ExecuteNonQuery();
//                        }
//                    }

//                }

//            }

//        }


//    }
//}




