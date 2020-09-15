#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Plus.Messages.Handlers;

#endregion

namespace Plus.Messages.Parsers
{
    internal static class LibraryParser
    {
        internal static Dictionary<int, StaticRequestHandler> Incoming;
        internal static Dictionary<string, string> Library;
        internal static Dictionary<string, int> Outgoing;
        internal static Dictionary<string, string> Config;

        private static List<uint> _registeredOutoings;

        internal static int CountReleases;
        internal static string ReleaseName;

        public delegate void ParamLess();

        internal delegate void StaticRequestHandler(GameClientMessageHandler handler);

        public static int OutgoingRequest(string packetName)
        {
            int packetId;

            if (Outgoing.TryGetValue(packetName, out packetId))
                return packetId;

            //Writer.Writer.LogMessage("Outgoing " + packetName + " doesn't exist.");

            return -1;
        }

        public static void Initialize()
        {
            //Out.WriteLine(string.Format("Loaded {0} Habbo Releases", CountReleases), "Plus.Packets");
            //Out.WriteLine(string.Format("Loaded {0} Event Controllers", Incoming.Count), "Plus.Packets");
        }

        public static void HandlePacket(GameClientMessageHandler handler, ClientMessage message)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;

            if (Incoming.ContainsKey(message.Id))
            {
                if (Plus.DebugMode)
                {
                    Console.WriteLine();
                    Console.Write("INCOMING ");
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.Write("HANDLED ");
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write(message.Id + Environment.NewLine + message);
                    if (message.Length > 0)
                        Console.WriteLine();
                    Console.WriteLine();
                }

                StaticRequestHandler staticRequestHandler = Incoming[message.Id];
                staticRequestHandler(handler);
            }
            else if (Plus.DebugMode)
            {
                Console.WriteLine();
                Console.Write("INCOMING ");
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.Write("REFUSED ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(message.Id + Environment.NewLine + message);
                if (message.Length > 0)
                    Console.WriteLine();
                Console.WriteLine();
            }
        }

        internal static void ReloadDictionarys()
        {
            Incoming.Clear();
            Outgoing.Clear();
            Library.Clear();
            Config.Clear();

            RegisterLibrary();
            RegisterConfig();
            RegisterIncoming();
            RegisterOutgoing();
        }

        internal static void RegisterIncoming()
        {
            CountReleases = 0;
            var filePaths = Directory.GetFiles(string.Format("{0}\\Packets", Environment.CurrentDirectory), "*.incoming");
            foreach (var fileContents in filePaths.Select(currentFile => File.ReadAllLines(currentFile, Encoding.UTF8)))
            {
                CountReleases++;
                foreach (var fields in fileContents.Where(line => !string.IsNullOrEmpty(line) && !line.StartsWith("[")).Select(line => line.Replace(" ", string.Empty).Split('=')))
                {
                    var packetName = fields[0];
                    if (fields[1].Contains('/')) // anti comments
                        fields[1] = fields[1].Split('/')[0];

                    var packetId = fields[1].ToLower().Contains('x') ? Convert.ToInt32(fields[1], 16) : Convert.ToInt32(fields[1]);
                    if (!Library.ContainsKey(packetName))
                        continue;
                    var libValue = Library[packetName];
                    var del =
                        (PacketLibrary.GetProperty)
                            Delegate.CreateDelegate(typeof(PacketLibrary.GetProperty), typeof(PacketLibrary),
                                libValue);
                    if (Incoming.ContainsKey(packetId))
                    {
                        if (packetId == -1)
                            continue;

                        //Writer.Writer.LogMessage("An incoming packet with the same ID was encountered. Packet ID: " + packetId);
                    }
                    else
                        Incoming.Add(packetId, new StaticRequestHandler(del));
                }
            }
        }


        internal static void RegisterConfig()
        {
            var filePaths = Directory.GetFiles(string.Format("{0}\\Packets", Environment.CurrentDirectory), "*.inf");
            foreach (var fields in filePaths.Select(File.ReadAllLines).SelectMany(fileContents => fileContents.Where(line => !string.IsNullOrEmpty(line) && !line.StartsWith("[")).Select(line => line.Split('='))))
            {
                if (fields[1].Contains('/')) // anti comments
                    fields[1] = fields[1].Split('/')[0];

                Config.Add(fields[0], fields[1]);
            }
        }

        internal static void RegisterOutgoing()
        {
            _registeredOutoings = new List<uint>();

            var filePaths = Directory.GetFiles(string.Format("{0}\\Packets", Environment.CurrentDirectory), "*.outgoing");
            foreach (var fields in filePaths.Select(File.ReadAllLines).SelectMany(fileContents => fileContents.Where(line => !string.IsNullOrEmpty(line) && !line.StartsWith("[")).Select(line => line.Replace(" ", string.Empty).Split('='))))
            {
                if (fields[1].Contains('/'))
                    fields[1] = fields[1].Split('/')[0];

                var packetName = fields[0];
                var packetId = int.Parse(fields[1]);

                if (packetId != -1)
                {
                    if (!_registeredOutoings.Contains((uint)packetId))
                        _registeredOutoings.Add((uint)packetId);
                }
                Outgoing.Add(packetName, packetId);
            }

            _registeredOutoings.Clear();
            _registeredOutoings = null;
        }

        internal static void RegisterLibrary()
        {
            var filePaths = Directory.GetFiles(string.Format("{0}\\Packets", Environment.CurrentDirectory), "*.library");
            foreach (var fields in filePaths.Select(File.ReadAllLines).SelectMany(fileContents => fileContents.Select(line => line.Split('='))))
            {
                if (fields[1].Contains('/')) // anti comments
                    fields[1] = fields[1].Split('/')[0];

                var incomingName = fields[0];
                var libraryName = fields[1];
                Library.Add(incomingName, libraryName);
            }
        }
    }
}