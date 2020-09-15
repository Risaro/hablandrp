using Plus.Database.Manager.Database.Session_Details.Interfaces;
using Plus.HabboHotel;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.Messages;
using Plus.Messages.Parsers;
using System;
using System.Collections.Generic;
using Plus.HabboHotel.Roleplay.Misc;
using System.Threading.Tasks;

namespace Plus.Configuration
{
    /// <summary>
    /// Class ConsoleCommandHandling.
    /// </summary>
    internal class ConsoleCommandHandling
    {
        internal static bool IsWaiting = false;

        internal static Game GetGame()
        {
            return Plus.GetGame();
        }

        /// <summary>
        /// Invokes the command.
        /// </summary>
        /// <param name="inputData">The input data.</param>
        internal static void InvokeCommand(string inputData)
        {
            if (string.IsNullOrEmpty(inputData) && Logging.DisabledState)
                return;

            Console.WriteLine();

            if (Logging.DisabledState == false)
            {
                Logging.DisabledState = true;
                Console.WriteLine("Console writing disabled. Waiting for user input.");
                return;
            }

            try
            {
                string[] parameters = inputData.Split(' ');

                switch (parameters[0])
                {

                    #region Roleplay

                    case "wualert":
                        {

                            if (parameters[1] == null)
                            {
                                Console.WriteLine("Please enter the users username!");
                            }

                            string name = parameters[1].ToString();
                            string Notice = HabboHotel.Misc.ChatCommandHandler.MergeParams(parameters, 2);

                            bool dced = false;
                            GameClient User = null;
                            foreach (GameClient mClient in Plus.GetGame().GetClientManager().Clients.Values)
                            {
                                if (mClient == null)
                                    continue;
                                if (mClient.GetHabbo() == null)
                                    continue;
                                if (mClient.GetHabbo().CurrentRoom == null)
                                    continue;
                                if (mClient.GetConnection() == null)
                                    continue;
                                if (mClient.GetHabbo().UserName.ToLower() == name.ToLower())
                                {
                                    User = mClient;
                                    dced = true;
                                }
                            }

                            if (dced)
                            {

                                User.SendWhisper("[Alert][Private]: " + Notice);
                                Console.WriteLine("Successfully alerted " + name.ToLower() + " :: '" + Notice + "'", ConsoleColor.Red);
                            }


                        }
                        break;
                    case "walert":
                        {


                            string Notice = HabboHotel.Misc.ChatCommandHandler.MergeParams(parameters, 1);

                            lock (Plus.GetGame().GetClientManager().Clients.Values)
                            {
                                foreach (GameClient mClient in Plus.GetGame().GetClientManager().Clients.Values)
                                {
                                    if (mClient == null)
                                        continue;
                                    if (mClient.GetHabbo() == null)
                                        continue;
                                    if (mClient.GetHabbo().CurrentRoom == null)
                                        continue;
                                    if (mClient.GetConnection() == null)
                                        continue;
                                    mClient.SendWhisperBubble("[Alert][Global]: " + Notice, 33);
                                }
                            }

                            Console.WriteLine("Whisper Alerted: '" + Notice + "'", ConsoleColor.DarkGreen);
                        }
                        break;
                    case "halert":
                        {


                            string Notice = HabboHotel.Misc.ChatCommandHandler.MergeParams(parameters, 1);
                            ServerMessage HotelAlert = new ServerMessage(LibraryParser.OutgoingRequest("BroadcastNotifMessageComposer"));
                            HotelAlert.AppendString(Notice + "\r\n" + "- KatyaRP");



                            Plus.GetGame().GetClientManager().QueueBroadcaseMessage(HotelAlert);

                            Console.WriteLine("Hotel Alerted: '" + Notice + "'", ConsoleColor.DarkGreen);
                        }
                        break;
                    case "ban":
                        {

                            if (parameters[1] == null)
                            {
                                Console.WriteLine("Please enter the users username!");
                            }

                            string name = parameters[1].ToString();
                            bool dced = false;
                            GameClient User = null;
                            foreach (GameClient mClient in Plus.GetGame().GetClientManager().Clients.Values)
                            {
                                if (mClient == null)
                                    continue;
                                if (mClient.GetHabbo() == null)
                                    continue;
                                if (mClient.GetHabbo().CurrentRoom == null)
                                    continue;
                                if (mClient.GetConnection() == null)
                                    continue;
                                if (mClient.GetHabbo().UserName.ToLower() == name.ToLower())
                                {
                                    User = mClient;
                                    dced = true;
                                }
                            }

                            if (dced)
                            {
                                User.GetConnection().Dispose();

                                Console.WriteLine("Successfully banned " + name.ToLower(), ConsoleColor.Red);

                                using (IQueryAdapter dbClient = Plus.GetDatabaseManager().GetQueryReactor())
                                {
                                    dbClient.RunFastQuery("INSERT INTO users_bans(bantype,value,reason) VALUES('user','" + User.GetHabbo().UserName + "','Automatic Ban via Console [Urgent]')");
                                }
                            }
                            else
                            {

                                using (IQueryAdapter dbClient = Plus.GetDatabaseManager().GetQueryReactor())
                                {
                                    dbClient.RunFastQuery("INSERT INTO bans(bantype,value,reason) VALUES('user','" + name.ToLower() + "','Automatic Ban via Console [Urgent]')");
                                }

                                Console.WriteLine("Successfully banned " + name.ToLower(), ConsoleColor.Red);
                            }


                        }
                        break;

                    case "derank":
                        {

                            if (parameters[1] == null)
                            {
                                Console.WriteLine("Please enter the users username!");
                            }

                            string name = parameters[1].ToString();

                            bool dced = false;
                            GameClient User = null;
                            foreach (GameClient mClient in Plus.GetGame().GetClientManager().Clients.Values)
                            {
                                if (mClient == null)
                                    continue;
                                if (mClient.GetHabbo() == null)
                                    continue;
                                if (mClient.GetHabbo().CurrentRoom == null)
                                    continue;
                                if (mClient.GetConnection() == null)
                                    continue;
                                if (mClient.GetHabbo().UserName.ToLower() == name.ToLower())
                                {
                                    User = mClient;
                                    dced = true;
                                }
                            }

                            if (dced)
                            {
                                User.GetConnection().Dispose();

                                Console.WriteLine("Successfully deranked " + name.ToLower(), ConsoleColor.Red);


                                using (IQueryAdapter dbClient = Plus.GetDatabaseManager().GetQueryReactor())
                                {
                                    dbClient.RunFastQuery("UPDATE users SET rank = '1' WHERE username = '" + name.ToLower() + "'");
                                }

                            }
                            else
                            {

                                using (IQueryAdapter dbClient = Plus.GetDatabaseManager().GetQueryReactor())
                                {
                                    dbClient.RunFastQuery("UPDATE users SET rank = '1' WHERE username = '" + name.ToLower() + "'");
                                }

                                Console.WriteLine("Successfully deranked " + name.ToLower(), ConsoleColor.Red);
                            }


                        }
                        break;
                    case "dc":
                        {

                            if (parameters[1] == null)
                            {
                                Console.WriteLine("Please enter the users username!");
                            }

                            string name = parameters[1].ToString();
                            bool dced = false;
                            GameClient User = null;
                            foreach (GameClient mClient in Plus.GetGame().GetClientManager().Clients.Values)
                            {
                                if (mClient == null)
                                    continue;
                                if (mClient.GetHabbo() == null)
                                    continue;
                                if (mClient.GetHabbo().CurrentRoom == null)
                                    continue;
                                if (mClient.GetConnection() == null)
                                    continue;
                                if (mClient.GetHabbo().UserName.ToLower() == name.ToLower())
                                {
                                    User = mClient;
                                    dced = true;
                                }
                            }

                            if (dced)
                            {
                                User.GetConnection().Dispose();
                                Console.WriteLine("Successfully disconnected " + name.ToLower(), ConsoleColor.Red);
                            }


                        }
                        break;

                    #endregion

                    case "shutdown":
                    case "close":
                        {
                            using (IQueryAdapter dbClient = Plus.GetDatabaseManager().GetQueryReactor())
                            {
                                dbClient.RunFastQuery("UPDATE server_settings SET value = '2' WHERE variable = 'status'");
                            }

                            Logging.DisablePrimaryWriting(true);
                            Out.WriteLine("Shutdown Initalized", "", ConsoleColor.DarkYellow);
                            Plus.PerformShutDown(false);
                            Console.WriteLine();
                            return;
                        }

                    case "restart":
                        {

                            using (IQueryAdapter dbClient = Plus.GetDatabaseManager().GetQueryReactor())
                            {
                                dbClient.RunFastQuery("UPDATE server_status SET status = 2 where id = 1");
                            }

                            Logging.LogMessage(string.Format("Server Restarting at {0}", DateTime.Now));
                            Logging.DisablePrimaryWriting(true);
                            Out.WriteLine("Restart Initialized", "", ConsoleColor.DarkYellow);
                            Plus.PerformShutDown(true);
                            Console.WriteLine();
                            return;
                        }

                    case "fixrooms":
                        {
                            List<Room> roomsToUnload = new List<Room>();
                            foreach (Room room30 in Plus.GetGame().GetRoomManager().LoadedRooms.Values)
                            {
                                if ((room30 != null))
                                {
                                    roomsToUnload.Add(room30);
                                    //Plus.GetGame().GetRoomManager().UnloadRoom(room30);
                                }
                            }
                            foreach (Room roomj in roomsToUnload)
                            {
                                Plus.GetGame().GetRoomManager().UnloadRoom(roomj, "fixrooms");
                            }
                            roomsToUnload = null;

                            var message = new ServerMessage(LibraryParser.OutgoingRequest("MOTDNotificationMessageComposer"));
                            message.AppendInteger(1);
                            message.AppendString("Hey Guys,\n\nAll rooms have been unloaded. This could be because:\n1. Room glitches\n2. Manual room changes\n3. Room migration\n\nPlease reload the client to prevent any errors. Thank you!");
                            GetGame().GetClientManager().QueueBroadcaseMessage(message);

                            break;
                        }

                    case "flush":
                        {
                            if (parameters.Length < 2)
                                Console.WriteLine("You need to specify a parameter within your command. Type help for more information");
                            else
                            {
                                switch (parameters[1])
                                {
                                    case "database":
                                        {
                                            Plus.GetDatabaseManager().Destroy();
                                            Console.WriteLine("Closed old connections");
                                            break;
                                        }

                                    case "settings":
                                        {
                                            if (parameters.Length < 3)
                                                Console.WriteLine("You need to specify a parameter within your command. Type help for more information");
                                            else
                                            {
                                                switch (parameters[2])
                                                {
                                                    case "catalog":
                                                        {
                                                            Console.WriteLine("Flushing catalog settings");

                                                            using (IQueryAdapter dbClient = Plus.GetDatabaseManager().GetQueryReactor())
                                                            {
                                                                GetGame().GetCatalog().Initialize(dbClient);
                                                            }

                                                            GetGame()
                            .GetClientManager()
                            .QueueBroadcaseMessage(
                                new ServerMessage(LibraryParser.OutgoingRequest("PublishShopMessageComposer")));

                                                            Console.WriteLine("Catalog flushed");

                                                            break;
                                                        }

                                                    case "modeldata":
                                                        {
                                                            Console.WriteLine("Flushing modeldata");
                                                            using (IQueryAdapter dbClient = Plus.GetDatabaseManager().GetQueryReactor())
                                                            {
                                                                GetGame().GetRoomManager().LoadModels(dbClient);
                                                            }
                                                            Console.WriteLine("Models flushed");

                                                            break;
                                                        }

                                                    case "bans":
                                                        {
                                                            Console.WriteLine("Flushing bans");
                                                            using (IQueryAdapter dbClient = Plus.GetDatabaseManager().GetQueryReactor())
                                                            {
                                                                GetGame().GetBanManager().Init();
                                                            }
                                                            Console.WriteLine("Bans flushed");

                                                            break;
                                                        }



                                                }
                                            }
                                            break;
                                        }

                                    case "console":
                                        {
                                            Console.Clear();
                                            break;
                                        }

                                    case "memory":
                                        {

                                            GC.Collect();
                                            Console.WriteLine("Memory flushed!");

                                            break;
                                        }

                                    default:
                                        {
                                            UnknownCommand(inputData);
                                            break;
                                        }
                                }
                            }

                            break;
                        }

                    case "view":
                        {
                            switch (parameters[1])
                            {

                                case "console":
                                    {
                                        Console.WriteLine("Press ENTER for disabling console writing");
                                        Logging.DisabledState = false;
                                        break;
                                    }
                            }
                            break;
                        }

                    case "alert":
                        {
                            var str = inputData.Substring(6);
                            var message = new ServerMessage(LibraryParser.OutgoingRequest("BroadcastNotifMessageComposer"));
                            message.AppendString(str);
                            message.AppendString(string.Empty);
                            GetGame().GetClientManager().QueueBroadcaseMessage(message);
                            Console.WriteLine("[{0}] was sent!", str);
                            return;
                        }
                    case "clear":
                        Console.Clear();
                        return;

                    case "help":
                        Console.WriteLine("shutdown/close - for safe shutting down PlusEmulator");
                        Console.WriteLine("clear - Clear all text");
                        Console.WriteLine("alert (msg) - send alert to Every1!");
                        Console.WriteLine("flush/reload");
                        Console.WriteLine("   - catalog");
                        Console.WriteLine("   - modeldata");
                        Console.WriteLine("   - bans");
                        Console.WriteLine("   - packets (reload packets ids)");
                        Console.WriteLine("   - filter");
                        Console.WriteLine();
                        return;

                    default:
                        UnknownCommand(inputData);
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in command [" + inputData + "]: " + e.ToString());
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Unknowns the command.
        /// </summary>
        /// <param name="command">The command.</param>
        private static void UnknownCommand(string command)
        {
            Console.WriteLine(command + " is an unknown or unsupported command. Type help for more information");
        }
    }
}