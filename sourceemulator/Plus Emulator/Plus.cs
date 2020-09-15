using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Timers;
using System.Windows.Forms;
using Plus.Configuration;
using Plus.Database;
using Plus.Encryption;
using Plus.HabboHotel;
using Plus.HabboHotel.Groups;
using Plus.HabboHotel.Misc;
using Plus.HabboHotel.Pets;
using Plus.HabboHotel.Users;
using Plus.HabboHotel.Users.Messenger;
using Plus.HabboHotel.Users.UserDataManagement;
using Plus.HabboHotel.Roleplay;
using Plus.Manager;
using Plus.Messages;
using Plus.Messages.Parsers;
using Plus.Util;
using MySql.Data.MySqlClient;
using Timer = System.Timers.Timer;
using Plus.Connection.Net;

namespace Plus
{
    /// <summary>
    /// Class Plus.
    /// </summary>
    public static class Plus
    {
        /// <summary>
        /// Plus Environment: Main Thread of Plus Emulator, SetUp's the Emulator
        /// Contains Initialize: Responsible of the Emulator Loadings
        /// </summary>

        internal static string DatabaseConnectionType = "MySQL", ServerLanguage = "english";

        /// <summary>
        /// The build
        /// </summary>
        internal static readonly string Build = "0180", Version = "1.0";

        /// <summary>
        /// The live currency type
        /// </summary>
        internal static int LiveCurrencyType = 105, ConsoleTimer = 2000;

        /// <summary>
        /// The is live
        /// </summary>
        internal static bool IsLive,
                             SeparatedTasksInGameClientManager,
                             SeparatedTasksInMainLoops,
                             DebugMode,
                             ConsoleTimerOn;

        /// <summary>
        /// The staff alert minimum rank
        /// </summary>
        internal static uint StaffAlertMinRank = 4, FriendRequestLimit = 1000;

        /// <summary>
        /// The manager
        /// </summary>
        internal static DatabaseManager Manager;

        /// <summary>
        /// The configuration data
        /// </summary>
        internal static ConfigData ConfigData;

        /// <summary>
        /// The server started
        /// </summary>
        internal static DateTime ServerStarted;

        /// <summary>
        /// The offline messages
        /// </summary>
        internal static Dictionary<uint, List<OfflineMessage>> OfflineMessages;

        /// <summary>
        /// The timer
        /// </summary>
        internal static Timer Timer;

        /// <summary>
        /// The culture information
        /// </summary>
        internal static CultureInfo CultureInfo;

        /// <summary>
        /// The users cached
        /// </summary>
        public static readonly ConcurrentDictionary<uint, Habbo> UsersCached = new ConcurrentDictionary<uint, Habbo>();

        /// <summary>
        /// The _connection manager
        /// </summary>
        private static ConnectionHandling _connectionManager;

        /// <summary>
        /// The _default encoding
        /// </summary>
        private static Encoding _defaultEncoding;

        /// <summary>
        /// The _game
        /// </summary>
        private static Game _game;

        /// <summary>
        /// The _languages
        /// </summary>
        private static Languages _languages;

        /// <summary>
        /// The allowed special chars
        /// </summary>
        private static readonly HashSet<char> AllowedSpecialChars = new HashSet<char>(new[]
        {
            '-', '.', ' ', 'Ã', '©', '¡', '­', 'º', '³', 'Ã', '‰', '_'
        });

        /// <summary>
        /// Check's if the Shutdown Has Started
        /// </summary>
        /// <value><c>true</c> if [shutdown started]; otherwise, <c>false</c>.</value>
        internal static bool ShutdownStarted { get; set; }

        public static bool ContainsAny(this string haystack, params string[] needles)
        {
            return needles.Any(haystack.Contains);
        }
        public static double GetUnixTimestamp()
        {
            TimeSpan ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
            return ts.TotalSeconds;
        }
        /// <summary>
        /// Get's Habbo By The User Id
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>Habbo.</returns>
        /// Table: users.id
        internal static Habbo GetHabboById(uint userId)
        {
            try
            {
                var clientByUserId = GetGame().GetClientManager().GetClientByUserId(userId);
                if (clientByUserId != null)
                {
                    var habbo = clientByUserId.GetHabbo();
                    if (habbo != null && habbo.Id > 0)
                    {
                        UsersCached.AddOrUpdate(userId, habbo, (key, value) => habbo);
                        return habbo;
                    }
                }
                else
                {
                    var userData = UserDataFactory.GetUserData((int)userId);
                    if (UsersCached.ContainsKey(userId)) return UsersCached[userId];

                    if (userData == null || userData.User == null) return null;

                    UsersCached.TryAdd(userId, userData.User);
                    userData.User.InitInformation(userData);
                    return userData.User;
                }
            }
            catch (Exception e)
            {
                Writer.Writer.LogException("Habbo GetHabboForId: " + e);
            }
            return null;
        }

        /// <summary>
        /// Console Clear Thread
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ElapsedEventArgs"/> instance containing the event data.</param>
        internal static void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            Console.Clear();
            Out.WriteLine("Console Cleared  in: " + DateTime.Now + " Next Time on: " + ConsoleTimer + " Seconds ");
            GC.Collect();
            Timer.Start();
        }

        /// <summary>
        /// Main Void, Initializes the Emulator.
        /// </summary>
        internal static void Initialize()
        {
            #region Precheck

            Console.Title = "Plus (KatyaRP) is initializing..";
            ServerStarted = DateTime.Now;
            _defaultEncoding = Encoding.Default;

            #endregion Precheck

            #region Database Connection

            CultureInfo = CultureInfo.CreateSpecificCulture("en-GB");
            try
            {
                ConfigurationData.Load(Path.Combine(Application.StartupPath, "Settings/main.ini"), false);
                RoleplayData.Load(Path.Combine(Application.StartupPath, "Settings/Roleplay/settings.ini"), true);

                DatabaseConnectionType = ConfigurationData.Data["db.type"];
                var mySqlConnectionStringBuilder = new MySqlConnectionStringBuilder
                {
                    Server = (ConfigurationData.Data["db.hostname"]),
                    Port = (uint.Parse(ConfigurationData.Data["db.port"])),
                    UserID = (ConfigurationData.Data["db.username"]),
                    Password = (ConfigurationData.Data["db.password"]),
                    Database = (ConfigurationData.Data["db.name"]),
                    MinimumPoolSize = (uint.Parse(ConfigurationData.Data["db.pool.minsize"])),
                    MaximumPoolSize = (uint.Parse(ConfigurationData.Data["db.pool.maxsize"])),
                    Pooling = (true),
                    AllowZeroDateTime = (true),
                    ConvertZeroDateTime = (true),
                    DefaultCommandTimeout = (300),
                    ConnectionTimeout = (10)
                };
                var mySqlConnectionStringBuilder2 = mySqlConnectionStringBuilder;
                Manager = new DatabaseManager(mySqlConnectionStringBuilder2.ToString(), DatabaseConnectionType);
                using (var queryReactor = GetDatabaseManager().GetQueryReactor())
                {
                    ConfigData = new ConfigData(queryReactor);
                    PetCommandHandler.Init(queryReactor);
                    PetLocale.Init(queryReactor);
                    OfflineMessages = new Dictionary<uint, List<OfflineMessage>>();
                    OfflineMessage.InitOfflineMessages(queryReactor);
                }

                #endregion Database Connection
                
            #region Packets Registering

                ConsoleTimer = (int.Parse(ConfigurationData.Data["console.clear.time"]));
                ConsoleTimerOn = (bool.Parse(ConfigurationData.Data["console.clear.enabled"]));
                FriendRequestLimit = ((uint)int.Parse(ConfigurationData.Data["client.maxrequests"]));


                LibraryParser.Incoming = new Dictionary<int, LibraryParser.StaticRequestHandler>();
                LibraryParser.Library = new Dictionary<string, string>();
                LibraryParser.Outgoing = new Dictionary<string, int>();
                LibraryParser.Config = new Dictionary<string, string>();

                LibraryParser.RegisterLibrary();
                LibraryParser.RegisterOutgoing();
                LibraryParser.RegisterIncoming();
                LibraryParser.RegisterConfig();
            
            #endregion Packets Registering
                
            #region Game Initalizer
                
                ExtraSettings.RunExtraSettings();
                CrossDomainPolicy.Set();
                _game = new Game(int.Parse(ConfigurationData.Data["game.tcp.conlimit"]));
                _game.GetNavigator().LoadNewPublicRooms();
                _game.ContinueLoading();
            
            #endregion Game Initalizer

            #region Languages Parser

                ServerLanguage = (Convert.ToString(ConfigurationData.Data["system.lang"]));
                _languages = new Languages(ServerLanguage);
            
            #endregion Languages Parser
                
            #region Environment SetUp

                if (ConsoleTimerOn) Out.WriteLine("Console Clear Timer is enable with " + ConsoleTimer + " seconds.");

                _connectionManager = new ConnectionHandling(int.Parse(ConfigurationData.Data["game.tcp.port"]),
                    int.Parse(ConfigurationData.Data["game.tcp.conlimit"]),
                    int.Parse(ConfigurationData.Data["game.tcp.conperip"]),
                    ConfigurationData.Data["game.tcp.enablenagles"].ToLower() == "true");

                if (LibraryParser.Config["Crypto.Enabled"] == "true")
                {
                    Handler.Initialize(LibraryParser.Config["Crypto.RSA.N"], LibraryParser.Config["Crypto.RSA.D"], LibraryParser.Config["Crypto.RSA.E"]);
                }

                _connectionManager.init();

                LibraryParser.Initialize();
            
            #endregion Environment SetUp

                #region Tasks and MusSystem

                if (ConsoleTimerOn)
                {
                    Timer = new Timer { Interval = ConsoleTimer };
                    Timer.Elapsed += TimerElapsed;
                    Timer.Start();
                }

                if (ConfigurationData.Data.ContainsKey("StaffAlert.MinRank")) StaffAlertMinRank = uint.Parse(ConfigurationData.Data["StaffAlert.MinRank"]);

                if (ConfigurationData.Data.ContainsKey("SeparatedTasksInMainLoops.enabled") &&
                    ConfigurationData.Data["SeparatedTasksInMainLoops.enabled"] == "true") SeparatedTasksInMainLoops = true;
                if (ConfigurationData.Data.ContainsKey("SeparatedTasksInGameClientManager.enabled") &&
                    ConfigurationData.Data["SeparatedTasksInGameClientManager.enabled"] == "true") SeparatedTasksInGameClientManager = true;
                if (ConfigurationData.Data.ContainsKey("Debug")) if (ConfigurationData.Data["Debug"] == "true") DebugMode = true;

                TimeSpan TimeUsed = DateTime.Now - ServerStarted;

                Out.WriteLine("KatyaRP >> Started (" + TimeUsed.Seconds + "s, " + TimeUsed.Milliseconds + "ms)", "", ConsoleColor.Green);
                IsLive = true;

                using (var queryReactor = GetDatabaseManager().GetQueryReactor())
                {
                    queryReactor.RunFastQuery("UPDATE server_settings SET value = '1' WHERE variable = 'status'");
                }
            }
            catch (Exception e)
            {
                Out.WriteLine("Error in main.ini: Configuration file is invalid" + Environment.NewLine + e.Message, "", ConsoleColor.Red);
                Out.WriteLine("Please press Y to get more details or press other Key to Exit", "", ConsoleColor.Red);

                var key = Console.ReadKey();
                if (key.Key == ConsoleKey.Y)
                {
                    Console.WriteLine();
                    Out.WriteLine(
                        Environment.NewLine + "[Message] Error Details: " + Environment.NewLine + e.StackTrace +
                        Environment.NewLine + e.InnerException + Environment.NewLine + e.TargetSite +
                        Environment.NewLine + "[Message ]Press Any Key To Exit", "", ConsoleColor.Red);
                    Console.ReadKey();
                    Environment.Exit(1);
                }
                else
                {
                    Environment.Exit(1);
                }
            }

            #endregion Tasks and MusSystem
        }

        /// <summary>
        /// Convert's Enum to Boolean
        /// </summary>
        /// <param name="enum">The enum.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool EnumToBool(string @enum)
        {
            return @enum == "1";
        }

        /// <summary>
        /// Convert's Boolean to Integer
        /// </summary>
        /// <param name="bool">if set to <c>true</c> [bool].</param>
        /// <returns>System.Int32.</returns>
        internal static int BoolToInteger(bool @bool)
        {
            return @bool ? 1 : 0;
        }

        /// <summary>
        /// Convert's Boolean to Enum
        /// </summary>
        /// <param name="bool">if set to <c>true</c> [bool].</param>
        /// <returns>System.String.</returns>
        internal static string BoolToEnum(bool @bool)
        {
            return @bool ? "1" : "0";
        }

        /// <summary>
        /// Generates a Random Number in the Interval Min,Max
        /// </summary>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        /// <returns>System.Int32.</returns>
        internal static int GetRandomNumber(int min, int max)
        {
            return RandomNumber.Get(min, max);
        }

        /// <summary>
        /// Get's the Actual Timestamp in Unix Format
        /// </summary>
        /// <returns>System.Int32.</returns>
        internal static int GetUnixTimeStamp()
        {
            var totalSeconds = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            return ((int) totalSeconds);
        }

        /// <summary>
        /// Convert's a Unix TimeStamp to DateTime
        /// </summary>
        /// <param name="unixTimeStamp">The unix time stamp.</param>
        /// <returns>DateTime.</returns>
        internal static DateTime UnixToDateTime(double unixTimeStamp)
        {
            var result = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Local);
            result = result.AddSeconds(unixTimeStamp).ToLocalTime();
            return result;
        }

        internal static DateTime UnixToDateTime(int unixTimeStamp)
        {
            var result = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Local);
            result = result.AddSeconds(unixTimeStamp).ToLocalTime();
            return result;
        }

        /// <summary>
        /// Convert's a DateTime to Unix TimeStamp
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns>System.Int32.</returns>
        internal static Int64 DateTimeToUnix(DateTime target)
        {
            var d = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((target - d).TotalSeconds);
        }

        /// <summary>
        /// Get the Actual Time
        /// </summary>
        /// <returns>System.Int64.</returns>
        internal static long Now()
        {
            var totalMilliseconds = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds;
            return ((long) totalMilliseconds);
        }

        internal static int DifferenceInMilliSeconds(DateTime time, DateTime from)
        {
            return
                Convert.ToInt32((from.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds -
                                 time.Subtract(
                                     new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds));
        }

        /// <summary>
        /// Filter's the Habbo Avatars Figure
        /// </summary>
        /// <param name="figure">The figure.</param>
        /// <returns>System.String.</returns>
        internal static string FilterFigure(string figure)
        {
            return figure.Any(character => !IsValid(character))
                ? "lg-3023-1335.hr-828-45.sh-295-1332.hd-180-4.ea-3168-89.ca-1813-62.ch-235-1332"
                : figure;
        }

        /// <summary>
        /// Check if is a Valid AlphaNumeric String
        /// </summary>
        /// <param name="inputStr">The input string.</param>
        /// <returns><c>true</c> if [is valid alpha numeric] [the specified input string]; otherwise, <c>false</c>.</returns>
        internal static bool IsValidAlphaNumeric(string inputStr)
        {
            inputStr = inputStr.ToLower();
            if (string.IsNullOrEmpty(inputStr)) return false;

            {
                return inputStr.All(IsValid);
            }
        }

        /// <summary>
        /// Get a Habbo With the Habbo's Username
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <returns>Habbo.</returns>
        /// Table: users.username
        internal static Habbo GetHabboForName(string userName)
        {
            try
            {
                using (var queryReactor = GetDatabaseManager().GetQueryReactor())
                {
                    queryReactor.SetQuery("SELECT id FROM users WHERE username = @user");
                    queryReactor.AddParameter("user", userName);
                    var integer = queryReactor.GetInteger();
                    if (integer > 0)
                    {
                        return GetHabboById((uint)integer);
                    }
                }
            }
            catch
            {
            }
            return null;
        }

        /// <summary>
        /// Check if the Input String is a Integer
        /// </summary>
        /// <param name="int">The int.</param>
        /// <returns><c>true</c> if the specified int is number; otherwise, <c>false</c>.</returns>
        internal static bool IsNum(string @int)
        {
            double num;
            return double.TryParse(@int, out num);
        }

        /// <summary>
        /// Get the Database Configuration Data
        /// </summary>
        /// <returns>ConfigData.</returns>
        internal static ConfigData GetDbConfig()
        {
            return ConfigData;
        }

        /// <summary>
        /// Get's the Default Emulator Encoding
        /// </summary>
        /// <returns>Encoding.</returns>
        internal static Encoding GetDefaultEncoding()
        {
            return _defaultEncoding;
        }

        /// <summary>
        /// Get's the Game Connection Manager Handler
        /// </summary>
        /// <returns>ConnectionHandling.</returns>
        internal static ConnectionHandling GetConnectionManager()
        {
            return _connectionManager;
        }

        /// <summary>
        /// Get's the Game Environment Handler
        /// </summary>
        /// <returns>Game.</returns>
        internal static Game GetGame()
        {
            return _game;
        }

        /// <summary>
        /// Gets the language.
        /// </summary>
        /// <returns>Languages.</returns>
        internal static Languages GetLanguage()
        {
            return _languages;
        }

        /// <summary>
        /// Send a Message to Everyone in the Habbo Client
        /// </summary>
        /// <param name="message">The message.</param>
        internal static void SendMassMessage(string message)
        {
            try
            {
                var serverMessage = new ServerMessage(LibraryParser.OutgoingRequest("BroadcastNotifMessageComposer"));
                serverMessage.AppendString(message);
                GetGame().GetClientManager().QueueBroadcaseMessage(serverMessage);
            }
            catch (Exception pException)
            {
                Logging.HandleException(pException, "PlusEnvironment.SendMassMessage");
            }
        }

        /// <summary>
        /// Filter's SQL Injection Characters
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>System.String.</returns>
        internal static string FilterInjectionChars(string input)
        {
            input = input.Replace('\u0001', ' ');
            input = input.Replace('\u0002', ' ');
            input = input.Replace('\u0003', ' ');
            input = input.Replace('\t', ' ');
            return input;
        }

        /// <summary>
        /// Get's the Database Manager Handler
        /// </summary>
        /// <returns>DatabaseManager.</returns>
        internal static DatabaseManager GetDatabaseManager()
        {
            return Manager;
        }

        /// <summary>
        /// Perform's the Emulator Shutdown
        /// </summary>
        internal static void PerformShutDown()
        {
            PerformShutDown(false);
        }

        /// <summary>
        /// Performs the restart.
        /// </summary>
        internal static void PerformRestart()
        {
            PerformShutDown(true);
        }

        /// <summary>
        /// Shutdown the Emulator
        /// </summary>
        /// <param name="restart">if set to <c>true</c> [restart].</param>
        /// Set a Different Message in Hotel
        internal static void PerformShutDown(bool restart)
        {
            var now = DateTime.Now;
            Cache.StopProcess();

            ShutdownStarted = true;

            var serverMessage = new ServerMessage(LibraryParser.OutgoingRequest("SuperNotificationMessageComposer"));
            serverMessage.AppendString("disconnection");
            serverMessage.AppendInteger(2);
            serverMessage.AppendString("title");
            serverMessage.AppendString("Emulator Shutting down");
            serverMessage.AppendString("message");
            serverMessage.AppendString("<b>A technician has applied new updates / fixes, so the hotel is shutting down for these changes to take effect! It will automatically come back in a few seconds! So Long!");
            GetGame().GetClientManager().QueueBroadcaseMessage(serverMessage);

            System.Threading.Thread.Sleep(3000);

            _game.StopGameLoop();
            _game.GetRoomManager().RemoveAllRooms();
            GetGame().GetClientManager().CloseAll();

            GetConnectionManager().Destroy();

            foreach (Guild group in _game.GetGroupManager().Groups.Values) group.UpdateForum();

            using (var queryReactor = Manager.GetQueryReactor())
            {
                queryReactor.RunFastQuery("UPDATE users SET online = '0'");
                queryReactor.RunFastQuery("UPDATE rooms_data SET users_now = 0");
                queryReactor.RunFastQuery("UPDATE server_settings SET value = '0' WHERE variable = 'status'");
            }

            _connectionManager.Destroy();
            _game.Destroy();

            try
            {
                Manager.Destroy();
                Out.WriteLine("Game Manager destroyed", "", ConsoleColor.Red);
            }
            catch (Exception e)
            {
                Writer.Writer.LogException("Plus.cs PerformShutDown GameManager" + e);
            }

            TimeSpan timeUsedOnShutdown = DateTime.Now - now;
            Console.WriteLine(" >> Katya Emulator Successfully Shutdown <<");
            IsLive = false;

            if (restart)
            {
                Process.Start(Assembly.GetEntryAssembly().Location);
            }
            else
            {
                System.Threading.Thread.Sleep(2000);
            }

            Environment.Exit(0);
        }

        /// <summary>
        /// Convert's a Unix TimeSpan to A String
        /// </summary>
        /// <param name="span">The span.</param>
        /// <returns>System.String.</returns>
        internal static string TimeSpanToString(TimeSpan span)
        {
            return string.Concat(span.Seconds, " s, ", span.Milliseconds, " ms");
        }

        /// <summary>
        /// Check's if Input Data is a Valid AlphaNumeric Character
        /// </summary>
        /// <param name="c">The c.</param>
        /// <returns><c>true</c> if the specified c is valid; otherwise, <c>false</c>.</returns>
        private static bool IsValid(char c)
        {
            return char.IsLetterOrDigit(c) || AllowedSpecialChars.Contains(c);
        }
    }
}