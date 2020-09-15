using Plus.Configuration;
using Plus.Encryption;
using Plus.Encryption.Hurlant.Crypto.Prng;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.Messages.Parsers;
using System;
using Plus.HabboHotel.Roleplay.Components;
using System.Globalization;
using System.IO;
using System.Text;
using Plus.Encryption.Utils;
using System.Web.Script.Serialization;
using Plus.Connection;
using System.Data;

namespace Plus.Messages.Handlers
{
    /// <summary>
    /// Class GameClientMessageHandler.
    /// </summary>
    partial class GameClientMessageHandler
    {
        /// <summary>
        /// The current loading room
        /// </summary>
        internal Room CurrentLoadingRoom;

        /// <summary>
        /// The session
        /// </summary>
        protected GameClient Session;
    
        /// <summary>
        /// The request
        /// </summary>
        protected ClientMessage Request;

        /// <summary>
        /// The response
        /// </summary>
        protected ServerMessage Response;

        /// <summary>
        /// The _photo data
        /// </summary>
        private string _photoData;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameClientMessageHandler"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        internal GameClientMessageHandler(GameClient session)
        {
            Session = session;
            Response = new ServerMessage();
        }

        /// <summary>
        /// Gets the session.
        /// </summary>
        /// <returns>GameClient.</returns>
        internal GameClient GetSession()
        {
            return Session;
        }

        /// <summary>
        /// Gets the response.
        /// </summary>
        /// <returns>ServerMessage.</returns>
        internal ServerMessage GetResponse()
        {
            return Response;
        }

        /// <summary>
        /// Destroys this instance.
        /// </summary>
        internal void Destroy()
        {
            Session = null;
        }

        /// <summary>
        /// Handles the request.
        /// </summary>
        /// <param name="request">The request.</param>
        internal void HandleRequest(ClientMessage request)
        {
            Request = request;
            LibraryParser.HandlePacket(this, request);
        }

        /// <summary>
        /// Sends the response.
        /// </summary>
        internal void SendResponse()
        {
            if (Response != null && Response.Id > 0 && Session.GetConnection() != null)
                Session.GetConnection().SendData(Response.GetReversedBytes());
        }

        /// <summary>
        /// Adds the staff pick.
        /// </summary>
        internal void AddStaffPick()
        {
            this.Session.SendNotif(Plus.GetLanguage().GetVar("addstaffpick_error_1"));
        }

        /// <summary>
        /// Gets the client version message event.
        /// </summary>
        internal void GetClientVersionMessageEvent()
        {
            var release = Request.GetString();
            if (release.Contains("201409222303-304766480"))
            {
                Session.GetHabbo().ReleaseName = "304766480";
                Console.WriteLine("[Handled] Release Id: RELEASE63-201409222303-304766480");
            }
            else if (release.Contains("201411201226-580134750"))
            {
                Session.GetHabbo().ReleaseName = "304766480";
                Console.WriteLine("[Handled] Release Id: RELEASE63-201411201226-580134750");
            }
            else
                LibraryParser.ReleaseName = "Undefined Release";
        }

        /// <summary>
        /// Pongs this instance.
        /// </summary>
        internal void Pong()
        {
            Session.TimePingedReceived = DateTime.Now;
        }

        /// <summary>
        /// Disconnects the event.
        /// </summary>
        internal void DisconnectEvent()
        {
            if(!Session.LoggingOut)
            {
                ProcessLogout Logout = new ProcessLogout(Session);
            }
        }

        /// <summary>
        /// Latencies the test.
        /// </summary>
        internal void LatencyTest()
        {
            if (Session == null)
                return;
            Session.TimePingedReceived = DateTime.Now;
            GetResponse().Init(LibraryParser.OutgoingRequest("LatencyTestResponseMessageComposer"));
            GetResponse().AppendInteger(Request.GetIntegerFromString());
            SendResponse();
            Plus.GetGame().GetAchievementManager().ProgressUserAchievement(Session, "ACH_AllTimeHotelPresence", 1);
        }

        /// <summary>
        /// Fuckyous this instance.
        /// </summary>
        internal void Fuckyou()
        {
        }

        /// <summary>
        /// Initializes the crypto.
        /// </summary>
        internal void InitCrypto()
        {
            if (LibraryParser.Config["Crypto.Enabled"] == "false")
            {
                Response.Init(LibraryParser.OutgoingRequest("InitCryptoMessageComposer"));
                Response.AppendString("Plus");
                Response.AppendString("Disabled Crypto");
                SendResponse();
                return;
            }
            Response.Init(LibraryParser.OutgoingRequest("InitCryptoMessageComposer"));
            Response.AppendString(Handler.GetRsaDiffieHellmanPrimeKey());
            Response.AppendString(Handler.GetRsaDiffieHellmanGeneratorKey());
            SendResponse();
        }

        /// <summary>
        /// Secrets the key.
        /// </summary>
        internal void SecretKey()
        {
            var cipherKey = Request.GetString();
            var sharedKey = Handler.CalculateDiffieHellmanSharedKey(cipherKey);

            if (LibraryParser.Config["Crypto.Enabled"] == "false")
            {
                Response.Init(LibraryParser.OutgoingRequest("SecretKeyMessageComposer"));
                Response.AppendString("Crypto disabled");
                Response.AppendBool(false); //Rc4 clientside.
                SendResponse();
                return;
            }
            if (sharedKey != 0)
            {
                Response.Init(LibraryParser.OutgoingRequest("SecretKeyMessageComposer"));
                Response.AppendString(Handler.GetRsaDiffieHellmanPublicKey());
                Response.AppendBool(ExtraSettings.CryptoClientSide);
                SendResponse();

                var data = sharedKey.ToByteArray();

                if (data[data.Length - 1] == 0)
                    Array.Resize(ref data, data.Length - 1);

                Array.Reverse(data, 0, data.Length);

                Session.GetConnection().ARC4ServerSide = new ARC4(data);
                if (ExtraSettings.CryptoClientSide)
                    Session.GetConnection().ARC4ClientSide = new ARC4(data);
            }
            else
                Session.Disconnect("crypto error");
        }

        /// <summary>
        /// Machines the identifier.
        /// </summary>
        internal void MachineId()
        {
            Request.GetString();
            var machineId = Request.GetString();
            Session.MachineId = machineId;
        }

        /// <summary>
        /// Logins the with ticket.
        /// </summary>
        internal void LoginWithTicket()
        {
            if (Session == null || Session.GetHabbo() != null)
                return;
            Session.TryLogin(Request.GetString());
           

            if (Session != null)
                Session.TimePingedReceived = DateTime.Now;
        }

        /// <summary>
        /// Informations the retrieve.
        /// </summary>
        internal void InfoRetrieve()
        {
            if (Session == null || Session.GetHabbo() == null)
                return;
            var habbo = Session.GetHabbo();
            Response.Init(LibraryParser.OutgoingRequest("UserObjectMessageComposer"));
            Response.AppendInteger(habbo.Id);
            Response.AppendString(habbo.UserName);
            Response.AppendString(habbo.Look);
            Response.AppendString(habbo.Gender.ToUpper());
            Response.AppendString(habbo.Motto);
            Response.AppendString("");
            Response.AppendBool(false);
            Response.AppendInteger(habbo.Respect);
            Response.AppendInteger(habbo.DailyRespectPoints);
            Response.AppendInteger(habbo.DailyPetRespectPoints);
            Response.AppendBool(true);
            Response.AppendString(habbo.LastOnline.ToString(CultureInfo.InvariantCulture));
            Response.AppendBool(habbo.CanChangeName);
            Response.AppendBool(false);
            SendResponse();
            Response.Init(LibraryParser.OutgoingRequest("BuildersClubMembershipMessageComposer"));
            Response.AppendInteger(Session.GetHabbo().BuildersExpire);
            Response.AppendInteger(Session.GetHabbo().BuildersItemsMax);
            Response.AppendInteger(2);
            SendResponse();
            var tradeLocked = Session.GetHabbo().CheckTrading();
            var canUseFloorEditor = (ExtraSettings.EVERYONE_USE_FLOOR || Session.GetHabbo().VIP || Session.GetHabbo().Rank >= 4);
            Response.Init(LibraryParser.OutgoingRequest("SendPerkAllowancesMessageComposer"));
            Response.AppendInteger(11);
            Response.AppendString("BUILDER_AT_WORK");
            Response.AppendString("");
            Response.AppendBool(canUseFloorEditor);
            Response.AppendString("VOTE_IN_COMPETITIONS");
            Response.AppendString("requirement.unfulfilled.helper_level_2");
            Response.AppendBool(false);
            Response.AppendString("USE_GUIDE_TOOL");
            Response.AppendString("requirement.unfulfilled.helper_level_4");
            Response.AppendBool(false);
            Response.AppendString("JUDGE_CHAT_REVIEWS");
            Response.AppendString("requirement.unfulfilled.helper_level_6");
            Response.AppendBool(false);
            Response.AppendString("NAVIGATOR_ROOM_THUMBNAIL_CAMERA");
            Response.AppendString("");
            Response.AppendBool(true);
            Response.AppendString("CALL_ON_HELPERS");
            Response.AppendString("");
            Response.AppendBool(true);
            Response.AppendString("CITIZEN");
            Response.AppendString("");
            Response.AppendBool(Session.GetHabbo().TalentStatus == "helper" ||
                                Session.GetHabbo().CurrentTalentLevel >= 4);
            Response.AppendString("MOUSE_ZOOM");
            Response.AppendString("");
            Response.AppendBool(false);
            Response.AppendString("TRADE");
            Response.AppendString(tradeLocked ? "" : "requirement.unfulfilled.no_trade_lock");
            Response.AppendBool(tradeLocked);
            Response.AppendString("CAMERA");
            Response.AppendString("");
            Response.AppendBool(ExtraSettings.ENABLE_BETA_CAMERA);
            Response.AppendString("NAVIGATOR_PHASE_TWO_2014");
            Response.AppendString("");
            Response.AppendBool(true);
            SendResponse();

            Session.GetHabbo().InitMessenger();

            GetResponse().Init(LibraryParser.OutgoingRequest("CitizenshipStatusMessageComposer"));
            GetResponse().AppendString("citizenship");
            GetResponse().AppendInteger(1);
            GetResponse().AppendInteger(4);
            SendResponse();

            GetResponse().Init(LibraryParser.OutgoingRequest("GameCenterGamesListMessageComposer"));
            GetResponse().AppendInteger(1);
            GetResponse().AppendInteger(18);
            GetResponse().AppendString("elisa_habbo_stories");
            GetResponse().AppendString("000000");
            GetResponse().AppendString("ffffff");
            GetResponse().AppendString("");
            GetResponse().AppendString("");
            SendResponse();
            GetResponse().Init(LibraryParser.OutgoingRequest("AchievementPointsMessageComposer"));
            GetResponse().AppendInteger(Session.GetHabbo().AchievementPoints);
            SendResponse();
            GetResponse().Init(LibraryParser.OutgoingRequest("FigureSetIdsMessageComposer"));
            Session.GetHabbo()._clothingManager.Serialize(GetResponse());
            SendResponse();
            /*Response.Init(LibraryParser.OutgoingRequest("NewbieStatusMessageComposer"));
            Response.AppendInteger(2);// 2 = new - 1 = nothing - 0 = not new
            SendResponse();*/
            Session.SendMessage(Plus.GetGame().GetNavigator().SerializePromotionCategories());
        }

        /// <summary>
        /// Habboes the camera.
        /// </summary>
        internal void HabboCamera()
        {
            //string one = this.Request.GetString();
            /*var two = */
            using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
            {
                queryReactor.SetQuery(string.Format("SELECT * FROM cms_stories_photos WHERE user_id = {0} AND type = 'PHOTO' ORDER BY id DESC LIMIT 1", Session.GetHabbo().Id));
                DataTable table = queryReactor.GetTable();
                foreach (DataRow dataRow in table.Rows)
                {
                    var date = dataRow["date"];
                    var room = dataRow["room_id"];
                    var photo = dataRow["id"];
                    var image = dataRow["image_url"];

                    using (var queryReactor2 = Plus.GetDatabaseManager().GetQueryReactor())
                    {
                        queryReactor2.SetQuery("INSERT INTO cms_stories_photos (user_id,user_name,room_id,image_preview_url,image_url,type,date,tags) VALUES (@user_id,@user_name,@room_id,@image_url,@image_url,@type,@date,@tags)");
                        queryReactor2.AddParameter("user_id", Session.GetHabbo().Id);
                        queryReactor2.AddParameter("user_name", Session.GetHabbo().UserName);
                        queryReactor2.AddParameter("room_id", room);
                        queryReactor2.AddParameter("image_url", image);
                        queryReactor2.AddParameter("type", "PHOTO");
                        queryReactor2.AddParameter("date", date);
                        queryReactor2.AddParameter("tags", "");
                        queryReactor2.RunQuery();

                        var new_photo_data = "{\"t\":" + date + ",\"u\":\"" + photo + "\",\"m\":\"\",\"s\":" + room + ",\"w\":\"" + image + "\"}";
                        var item = Session.GetHabbo().GetInventoryComponent().AddNewItem(0, Plus.GetGame().GetItemManager().PhotoId, new_photo_data, 0, true, false, 0, 0);

                        Session.GetHabbo().GetInventoryComponent().UpdateItems(false);
                        Session.GetHabbo().Credits -= 2;
                        Session.GetHabbo().UpdateCreditsBalance();
                        Session.GetHabbo().GetInventoryComponent().SendNewItems(item.Id);

                    }
                }
            }

            var message = new ServerMessage(LibraryParser.OutgoingRequest("CameraPurchaseOk"));
            Session.SendMessage(message);
        }
        /// <summary>
        /// Called when [click].
        /// </summary>
        internal void OnClick()
        {
            // i guess its useless
        }

        /// <summary>
        /// Gets the friends count.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>System.Int32.</returns>
        private static int GetFriendsCount(uint userId)
        {
            int result;
            using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
            {
                queryReactor.SetQuery(
                    "SELECT COUNT(*) FROM messenger_friendships WHERE user_one_id = @id OR user_two_id = @id;");
                queryReactor.AddParameter("id", userId);
                result = queryReactor.GetInteger();
            }
            return result;
        }

        /// <summary>
        /// Targeteds the offer buy.
        /// </summary>
        internal void TargetedOfferBuy()
        {
            int offerId = Request.GetInteger();
        }

        internal void PurchaseTargetedOffer()
        {
            
        }

        /// <summary>
        /// Goes the name of to room by.
        /// </summary>
        internal void GoToRoomByName()
        {
            string name = Request.GetString();
            switch (name)
            {
                case "predefined_noob_lobby":
                    var roomFwd = new ServerMessage(LibraryParser.OutgoingRequest("RoomForwardMessageComposer"));
                    roomFwd.AppendInteger(Convert.ToInt32(Plus.GetDbConfig().DbData["noob.lobby.roomid"]));
                    Session.SendMessage(roomFwd);
                    break;
            }
        }

        /// <summary>
        /// Gets the uc panel.
        /// </summary>
        internal void GetUCPanel()
        {
            string name = Request.GetString();
            switch (name)
            {
                case "new":

                    break;
            }
        }

        

        /// <summary>
        /// Gets the uc panel hotel.
        /// </summary>
        internal void GetUCPanelHotel()
        {
            int id = Request.GetInteger();
        }

        /// <summary>
        /// Saves the room thumbnail.
        /// </summary>
        internal void SaveRoomThumbnail()
        {

        }
    }
}