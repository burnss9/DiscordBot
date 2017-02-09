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



    [Name("Tournament (VERY unfinished)")]
    public class TournamentModule : ModuleBase<SocketCommandContext>
    {

        ulong LOG_CHANNEL = 278218957679362048;
        int tableSpaces = 8;

        string currentPickingCaptain = "";
        bool currentCaptainHasPicked = false;
        int currentDraftingTournament = 0;
        int currentDraftingRound = 0;

        ArrayList draftPlayers = new ArrayList();
        ArrayList draftCaptains = new ArrayList();



        [Command("admin tournament create")]
        [Remarks("Create a tournament.")]
        [MinPermissions(AccessLevel.ServerMod)]
        public async Task AdminCreateTournament(int maxTeams, string name)
        {
            using (SQLiteConnection conn = new SQLiteConnection("data source=players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = "INSERT INTO Tournaments (WINTEAM, NUMTEAMS, MAXTEAMS, REGISTEROPEN, TOURNAMENTCLOSED, NAME) VALUES (null, 0, $max, 1, 0, $name);";
                    cmd.Parameters.AddWithValue("$max", maxTeams);
                    cmd.Parameters.AddWithValue("$name", name);
                    cmd.ExecuteNonQuery();

                }

            }
            await ReplyAsync("\u200B" + "Tournament created.");
        }


        [Command("admin tournament delete")]
        [Remarks("Delete a tournament.")]
        [MinPermissions(AccessLevel.ServerMod)]
        public async Task AdminDeleteTournament(int id)
        {
            using (SQLiteConnection conn = new SQLiteConnection("data source=players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = "DELETE FROM Tournaments WHERE $tid = TOURNAMENTID;";
                    cmd.Parameters.AddWithValue("$tid", id);

                    cmd.ExecuteNonQuery();

                }

            }
            await ReplyAsync("\u200B" + "Tournament deleted.");
        }


        [Command("admin tournament regend")]
        [Remarks("End the registration period for a tournament.")]
        [MinPermissions(AccessLevel.ServerMod)]
        public async Task AdminEndRegistration(int id)
        {
            using (SQLiteConnection conn = new SQLiteConnection("data source=players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = "UPDATE FROM Tournaments SET REGISTEROPEN = 0 WHERE $tid = TOURNAMENTID;";
                    cmd.Parameters.AddWithValue("$tid", id);

                    cmd.ExecuteNonQuery();

                }

            }
            await ReplyAsync("\u200B" + "Tournament deleted.");
        }

        [Command("admin tournament captains")]
        [Remarks("Decide captains for a tournament.")]
        [MinPermissions(AccessLevel.ServerMod)]
        public async Task AdminTournamentCaptains(int id)
        {

            var players = new ArrayList();
            var captains = new ArrayList();
            Random random = new Random();


            using (SQLiteConnection conn = new SQLiteConnection("data source=players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {


                    cmd.CommandText = "SELECT UID FROM TournamentPlayers WHERE $tid = TOURNAMENTID;";
                    cmd.Parameters.AddWithValue("$tid", id);

                    using (SQLiteDataReader r = cmd.ExecuteReader())
                    {

                        while (r.Read())
                        {
                            players.Add(Convert.ToString(r["UID"]));
                        }
                    }

                    int numCaptains = players.Count / 3;

                    for (int i = 0; i < numCaptains; i++)
                    {

                        var index = random.Next(players.Count);
                        var cap = players[index];
                        players.RemoveAt(index);
                        captains.Add(cap);

                    }

                }

                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    string capNicks = "";

                    foreach (string c in captains)
                    {
                        cmd.CommandText = "UPDATE TournamentPlayers SET CAPTAIN = 1 WHERE PID = $pid;";
                        cmd.Parameters.AddWithValue("$pid", c);
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT INTO Teams (TOURNAMENTID, CAPTAIN) VALUES ($pid, $tid)";
                        cmd.Parameters.AddWithValue("$pid", c);
                        cmd.Parameters.AddWithValue("$tid", id);
                        cmd.ExecuteNonQuery();


                        ulong captainid = Convert.ToUInt64(c.Substring(1, c.Length - 3));
                        var user = Context.Client.GetUser(captainid);

                        capNicks += "`" + user.Username + "'\n";

                        var ch = await (user as SocketGuildUser).CreateDMChannelAsync();
                        await ch.SendMessageAsync("You have been chosen as a captain in the tournament!");
                    }

                    await ReplyAsync(capNicks);

                }

            }
            await ReplyAsync("\u200B" + "Captains chosen.");
        }

        [Command("admin tournament draft")]
        [Remarks("Begin the draft process.")]
        [MinPermissions(AccessLevel.ServerMod)]
        public async Task AdminTournamentDraft(int id, int secondsPerTurn)
        {

            draftPlayers.Clear();
            draftCaptains.Clear();

            currentDraftingTournament = id;


            Random random = new Random();

            using (SQLiteConnection conn = new SQLiteConnection("data source=players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = "SELECT UID FROM TournamentPlayers WHERE $tid = TOURNAMENTID, CAPTAIN = 1;";
                    cmd.Parameters.AddWithValue("$tid", id);

                    using (SQLiteDataReader r = cmd.ExecuteReader())
                    {

                        while (r.Read())
                        {
                            if (Convert.ToInt32(r["CAPTAIN"]) == 1)
                            {
                                draftCaptains.Add(Convert.ToString(r["UID"]));
                            }
                            else
                            {
                                draftPlayers.Add(Convert.ToString(r["UID"]));
                            }

                        }

                    }

                    #region drafting
                    #region round1
                    currentDraftingRound = 0;

                    for (int i = 0; i < draftCaptains.Count; i++)
                    {
                        await ReplyAsync("\u200B" + draftCaptains[i] as string + " it is now your turn to pick. `!recruit @player` or `!playersleft` to see remaining players.");

                        currentPickingCaptain = draftCaptains[i] as string;
                        currentCaptainHasPicked = false;

                        int count = 0;
                        while (count < secondsPerTurn && !currentCaptainHasPicked)
                        {
                            await Task.Delay(1000);
                            count += 1;
                        }
                        if (!currentCaptainHasPicked)
                        {
                            //assign random player
                        }

                    }
                    #endregion
                    #region round2
                    currentDraftingRound = 1;

                    for (int i = draftCaptains.Count - 1; i > 0; i--)
                    {
                        await ReplyAsync("\u200B" + draftCaptains[i] as string + " it is now your turn to pick. `!recruit @player` or `!tournament players left` to see remaining players.");

                        currentPickingCaptain = draftCaptains[i] as string;
                        currentCaptainHasPicked = false;

                        int count = 0;
                        while (count < secondsPerTurn && !currentCaptainHasPicked)
                        {
                            await Task.Delay(1000);
                            count += 1;
                        }
                        if (!currentCaptainHasPicked)
                        {
                            //assign random player
                        }

                    }
                    #endregion
                    #endregion


                }

            }
            await ReplyAsync("\u200B" + "Tournament draft complete.");
        }


        //TODO make sure you cant register for a closed tournament or a tournament that doesnt exist

        [Command("tournament register")]
        [Remarks("Register for a tournament.")]
        [MinPermissions(AccessLevel.User)]
        public async Task JoinTournament(int tournamentID)
        {
            using (SQLiteConnection conn = new SQLiteConnection("data source=players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = "INSERT INTO TournamentPlayers (PID, TOURNAMENTID, CAPTAIN, PICKED) VALUES ($pid, $tid, 0, 0);";
                    cmd.Parameters.AddWithValue("$pid", "<@" + Context.User.Id + ">");
                    cmd.Parameters.AddWithValue("$tid", tournamentID);
                    cmd.ExecuteNonQuery();

                }

            }
            await ReplyAsync("\u200B" + "Registered " + Context.User.Username + " for tournament: " + tournamentID);
        }


        [Command("tournament unregister")]
        [Remarks("Unregister from a tournament.")]
        [MinPermissions(AccessLevel.User)]
        public async Task LeaveTournament(int tournamentID)
        {
            using (SQLiteConnection conn = new SQLiteConnection("data source=players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = "DELETE FROM TournamentPlayers WHERE $pid = PID AND $tid = TOURNAMENTID;";
                    cmd.Parameters.AddWithValue("$pid", "<@" + Context.User.Id + ">");
                    cmd.Parameters.AddWithValue("$tid", tournamentID);
                    cmd.ExecuteNonQuery();

                }

            }
            await ReplyAsync("\u200B" + "Unregistered " + Context.User.Username + " from tournament: " + tournamentID);
        }

        [Command("recruit")]
        [Remarks("Recruit a player to your team for the currently drafting tournament.")]
        [MinPermissions(AccessLevel.User)]
        public async Task RecruitPlayer(string player)
        {
            using (SQLiteConnection conn = new SQLiteConnection("data source=players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {

                    if ("<@" + Context.User.Id + ">" != currentPickingCaptain)
                    {
                        await ReplyAsync("\u200B" + "<@" + Context.User.Id + "> it's not your turn!");
                        return;
                    }
                    else
                    {

                        if (draftPlayers.Contains(player))
                        {

                            if (currentDraftingRound == 0)
                            {
                                cmd.CommandText = "UPDATE TEAMS SET P2 = $recruit WHERE $pid = CAPTAIN, $tid = TOURNAMENTID;";
                                cmd.Parameters.AddWithValue("$pid", currentPickingCaptain);
                                cmd.Parameters.AddWithValue("$tid", currentDraftingTournament);
                                cmd.Parameters.AddWithValue("$recruit", player);
                                cmd.ExecuteNonQuery();

                                cmd.CommandText = "UPDATE TournamentPlayers SET PICKED = 1 WHERE $pid = PID;";
                                cmd.Parameters.Clear();
                                cmd.Parameters.AddWithValue("$pid", player);

                                draftPlayers.Remove(player);
                                currentCaptainHasPicked = true;
                            }
                            else if (currentDraftingRound == 1)
                            {
                                cmd.CommandText = "UPDATE TEAMS SET P3 = $recruit WHERE $pid = CAPTAIN, $tid = TOURNAMENTID;";
                                cmd.Parameters.AddWithValue("$pid", currentPickingCaptain);
                                cmd.Parameters.AddWithValue("$tid", currentDraftingTournament);
                                cmd.Parameters.AddWithValue("$recruit", player);
                                cmd.ExecuteNonQuery();

                                cmd.CommandText = "UPDATE TournamentPlayers SET PICKED = 1 WHERE $pid = PID;";
                                cmd.Parameters.Clear();
                                cmd.Parameters.AddWithValue("$pid", player);

                                draftPlayers.Remove(player);
                                currentCaptainHasPicked = true;
                            }


                        }
                        else
                        {
                            await ReplyAsync("\u200B" + "That player has either already been picked or was not registered!");
                            return;
                        }

                    }

                    //cmd.CommandText = "INSERT INTO TournamentPlayers (PID, TOURNAMENTID, CAPTAIN, PICKED) VALUES ($pid, $tid, 0, 0);";
                    //cmd.Parameters.AddWithValue("$pid", "<@" + Context.User.Id + ">");
                    //cmd.Parameters.AddWithValue("$tid", tournamentID);
                    //cmd.ExecuteNonQuery();

                }

            }
            await ReplyAsync("\u200B" + "Something weird happened");
        }


        [Command("tournament list")]
        [Remarks("List available tournaments.")]
        [MinPermissions(AccessLevel.User)]
        public async Task ListOpenTournaments()
        {

            string tournaments = "";

            using (SQLiteConnection conn = new SQLiteConnection("data source=players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = "SELECT TOURNAMENTID, NAME FROM Tournaments WHERE REGISTEROPEN = 1;";
                    using (SQLiteDataReader r = cmd.ExecuteReader())
                    {

                        while (r.Read())
                        {
                            tournaments += "`" + Convert.ToString(r["TOURNAMENTID"]) + " - " + Convert.ToString(r["NAME"]) + "`\n";
                        }
                    }

                }

            }
            await ReplyAsync("\u200B" + tournaments);
        }

        [Command("tournament team players")]
        [Remarks("List the players on your team.")]
        [MinPermissions(AccessLevel.User)]
        public async Task ListMyTeamPlayers(int id)
        {
            string players = "";

            using (SQLiteConnection conn = new SQLiteConnection("data source=players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = "SELECT CAPTIAN, P2, P3 FROM TEAMS WHERE (CAPTAIN = $pid OR P2 = $pid OR P3 = $pid), $tid = TOURNAMENTID;";
                    cmd.Parameters.AddWithValue("$pid", "<@" + Context.User.Id + ">");
                    cmd.Parameters.AddWithValue("$tid", id);
                    using (SQLiteDataReader r = cmd.ExecuteReader())
                    {

                        while (r.Read())
                        {
                            players += Convert.ToString(r["CAPTAIN"]) + "\n" + Convert.ToString(r["P3"]) + "\n" + Convert.ToString(r["P3"]);
                        }
                    }

                }

            }

            var dm = await (Context.User as SocketGuildUser).CreateDMChannelAsync();
            await dm.SendMessageAsync("\u200B" + players);

        }

        [Command("tournament team players")]
        [Remarks("List the players on your team.")]
        [MinPermissions(AccessLevel.User)]
        public async Task ListMyTeamPlayers()
        {
            string players = "";

            using (SQLiteConnection conn = new SQLiteConnection("data source=players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = "SELECT CAPTIAN, P2, P3 FROM TEAMS WHERE (CAPTAIN = $pid OR P2 = $pid OR P3 = $pid), $tid = TOURNAMENTID;";
                    cmd.Parameters.AddWithValue("$pid", "<@" + Context.User.Id + ">");
                    cmd.Parameters.AddWithValue("$tid", currentDraftingTournament);
                    using (SQLiteDataReader r = cmd.ExecuteReader())
                    {

                        while (r.Read())
                        {
                            players += Convert.ToString(r["CAPTAIN"]) + "\n" + Convert.ToString(r["P3"]) + "\n" + Convert.ToString(r["P3"]);
                        }
                    }

                }

            }

            var dm = await (Context.User as SocketGuildUser).CreateDMChannelAsync();
            await dm.SendMessageAsync("\u200B" + players);

        }

        [Command("tournament players")]
        [Remarks("List the players in the tournament.")]
        [MinPermissions(AccessLevel.User)]
        public async Task ListTournamentPlayers(int id)
        {
            string players = "";
            var dm = await (Context.User as SocketGuildUser).CreateDMChannelAsync();

            using (SQLiteConnection conn = new SQLiteConnection("data source=players.db"))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = "SELECT PID FROM TournamentPlayers WHERE $tid = TOURNAMENTID;";
                    cmd.Parameters.AddWithValue("$tid", id);
                    using (SQLiteDataReader r = cmd.ExecuteReader())
                    {

                        while (r.Read())
                        {
                            players += Convert.ToString(r["PID"] + "\n");


                            if (players.Length > 1000)
                            {
                                await dm.SendMessageAsync("\u200B" + players);
                                players = "";
                            }
                        }



                    }

                }

            }


            await dm.SendMessageAsync("\u200B" + players);

        }



    }





}
