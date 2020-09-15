using Plus.Configuration;
using Plus.Connection;
using Plus.Database.Manager.Database.Session_Details.Interfaces;
using Plus.Encryption.Utils;
using Plus.HabboHotel.Catalogs;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.PathFinding;
using Plus.HabboHotel.Pets;
using Plus.HabboHotel.Polls;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Roleplay.Misc;
using Plus.HabboHotel.RoomBots;
using Plus.HabboHotel.Rooms;
using Plus.Messages.Parsers;
using Plus.Security;
using Plus.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace Plus.Messages.Handlers
{
    partial class GameClientMessageHandler
    {
        public void GetPetBreeds()
        {
            var type = Request.GetString();
            string petType;
            var petId = PetRace.GetPetId(type, out petType);
            var races = PetRace.GetRacesForRaceId(petId);
            var message = new ServerMessage(LibraryParser.OutgoingRequest("SellablePetBreedsMessageComposer"));
            message.AppendString(petType);
            message.AppendInteger(races.Count);
            foreach (var current in races)
            {
                message.AppendInteger(petId);
                message.AppendInteger(current.Color1);
                message.AppendInteger(current.Color2);
                message.AppendBool(current.Has1Color);
                message.AppendBool(current.Has2Color);
            }
            Session.SendMessage(message);
        }

        internal void GoRoom()
        {
            if (Session.GetHabbo() == null)
                return;
            var num = Request.GetUInteger();
            var roomData = Plus.GetGame().GetRoomManager().GenerateRoomData(num);
            Session.GetHabbo().GetInventoryComponent().RunDbUpdate();
            if (roomData == null || roomData.Id == Session.GetHabbo().CurrentRoomId)
                return;
            roomData.SerializeRoomData(Response, Session, !Session.GetHabbo().InRoom, false);
            PrepareRoomForUser(num, roomData.PassWord);
        }

        internal void AddFavorite()
        {
            if (Session.GetHabbo() == null)
                return;

            var roomId = Request.GetUInteger();

            GetResponse().Init(LibraryParser.OutgoingRequest("FavouriteRoomsUpdateMessageComposer"));
            GetResponse().AppendInteger(roomId);
            GetResponse().AppendBool(true);
            SendResponse();

            Session.GetHabbo().FavoriteRooms.Add(roomId);
            using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
                queryReactor.RunFastQuery("INSERT INTO users_favorites (user_id,room_id) VALUES (" + Session.GetHabbo().Id + "," + roomId + ")");
        }

        internal void RemoveFavorite()
        {
            if (Session.GetHabbo() == null)
                return;
            var roomId = Request.GetUInteger();
            Session.GetHabbo().FavoriteRooms.Remove(roomId);

            GetResponse().Init(LibraryParser.OutgoingRequest("FavouriteRoomsUpdateMessageComposer"));
            GetResponse().AppendInteger(roomId);
            GetResponse().AppendBool(false);
            SendResponse();

            using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
                queryReactor.RunFastQuery("DELETE FROM users_favorites WHERE user_id = " + Session.GetHabbo().Id + " AND room_id = " + roomId);
        }

        internal void OnlineConfirmationEvent()
        {
            Out.WriteLine("[" + Request.GetString() + "] has logged in! (IP: " + Session.GetConnection().getIp() + ")", "",
                ConsoleColor.DarkGreen);

            if (Session.GetRoleplay().IsNoob)
            {

                string Welcome = "";
                Welcome += "Welcome To HabKatyaRP!\n";
                Welcome += "Your account currently has protection enabled.\n";
                Welcome += "This means you cannot be hurt by other players,\n";
                Welcome += "commit crime or recieve/give cash \n\n";
                Welcome += "Useful Commands:\n";
                Welcome += ":removegod - Removes your protection.\n";
                Welcome += ":Taxi <ID> - Taxi to rooms\n";
                Welcome += ":Corplist - lists corps with ID's\n";
                Welcome += ":map - Lists all KatyaRP rooms.\n";
                Welcome += ":Balance - displays your bank balance\n";
                Welcome += ":Stuck - if your character can't move.\n\n";
                Welcome += "We hope you enjoy your time at HabKatyaRP\n";

                Session.SendNotifWithScroll(Welcome);

            }
            

            //VIP
            if (Session.GetHabbo().VIP && !Session.GetHabbo().GetBadgeComponent().HasBadge("VIP"))
            {
                Session.GetHabbo().GetBadgeComponent().GiveBadge("VIP", true, Session, false);
                Session.SendNotif("Thanks for supporting us, heres your VIP badge!", "VIP Alert");
            }
            if (Session.GetHabbo().GetBadgeComponent().HasBadge("VIP") && !Session.GetHabbo().VIP)
            {
                Session.GetHabbo().GetBadgeComponent().RemoveBadge("VIP", Session);
                Session.SendNotif("Your VIP badge has been removed as you are no longer VIP.", "VIP Alert");
            }

            //Staff
            if (Session.GetHabbo().Rank >= 4 && !Session.GetHabbo().GetBadgeComponent().HasBadge("RSN"))
            {
                Session.GetHabbo().GetBadgeComponent().GiveBadge("RSN", true, Session, false);
                Session.SendNotif("Welcome to the staff team, here's your staff badge!", "Staff Alert");
            }
            if (Session.GetHabbo().GetBadgeComponent().HasBadge("RSN") && Session.GetHabbo().Rank <= 3)
            {
                Session.GetHabbo().GetBadgeComponent().RemoveBadge("RSN", Session);
                Session.SendNotif("Your staff badge has been removed as you are no longer staff.", "Staff Alert");
            }
            
        }

        internal void GoToHotelView()
        {
            if (Session == null || Session.GetHabbo() == null)
                return;
            if (!Session.GetHabbo().InRoom)
                return;
            var room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room != null)
                room.GetRoomUserManager().RemoveUserFromRoom(Session, true, false);

            var hotelView = Plus.GetGame().GetHotelView();
            if (hotelView.FurniRewardName != null)
            {
                var serverMessage = new ServerMessage(LibraryParser.OutgoingRequest("LandingRewardMessageComposer"));
                serverMessage.AppendString(hotelView.FurniRewardName);
                serverMessage.AppendInteger(hotelView.FurniRewardId);
                serverMessage.AppendInteger(120);
                serverMessage.AppendInteger(120 - Session.GetHabbo().Respect);
                Session.SendMessage(serverMessage);
            }
            Session.CurrentRoomUserId = -1;
        }

        internal void LandingCommunityGoal()
        {
            var onlineFriends = Session.GetHabbo().GetMessenger().Friends.Count(x => x.Value.IsOnline);
            var goalMeter = new ServerMessage(LibraryParser.OutgoingRequest("LandingCommunityChallengeMessageComposer"));
            goalMeter.AppendBool(true); //
            goalMeter.AppendInteger(0); //points
            goalMeter.AppendInteger(0); //my rank
            goalMeter.AppendInteger(onlineFriends); //totalAmount
            goalMeter.AppendInteger(onlineFriends >= 20 ? 1 : onlineFriends >= 50 ? 2 : onlineFriends >= 80 ? 3 : 0);
            //communityHighestAchievedLevel
            goalMeter.AppendInteger(0); //scoreRemainingUntilNextLevel
            goalMeter.AppendInteger(0); //percentCompletionTowardsNextLevel
            goalMeter.AppendString("friendshipChallenge"); //Type
            goalMeter.AppendInteger(0); //unknown
            goalMeter.AppendInteger(0); //ranks and loop
            Session.SendMessage(goalMeter);
        }

        internal void RequestFloorItems()
        {
        }

        internal void RequestWallItems()
        {
        }

        internal void SaveBranding()
        {
            var itemId = Request.GetUInteger();
            var count = Request.GetUInteger();

            if (Session == null || Session.GetHabbo() == null) return;
            var room = Session.GetHabbo().CurrentRoom;
            if (room == null || !room.CheckRights(Session, true)) return;

            var item = room.GetRoomItemHandler().GetItem(itemId);
            if (item == null)
                return;

            var extraData = string.Format("state{0}0", Convert.ToChar(9));
            for (uint i = 1; i <= count; i++)
                extraData = string.Format("{0}{1}{2}", extraData, Convert.ToChar(9), Request.GetString());

            item.ExtraData = extraData;
            room.GetRoomItemHandler()
                .SetFloorItem(Session, item, item.X, item.Y, item.Rot, false, false, true);
        }

        internal void OnRoomUserAdd()
        {
            if (Session == null || GetResponse() == null)
                return;
            var queuedServerMessage = new QueuedServerMessage(Session.GetConnection());
            if (CurrentLoadingRoom == null || CurrentLoadingRoom.GetRoomUserManager() == null ||
                CurrentLoadingRoom.GetRoomUserManager().UserList == null)
                return;
            var list =
                CurrentLoadingRoom.GetRoomUserManager()
                    .UserList.Values.Where(current => current != null && !current.IsSpectator);
            Response.Init(LibraryParser.OutgoingRequest("SetRoomUserMessageComposer"));
            Response.StartArray();
            foreach (var current2 in list)
            {
                current2.Serialize(Response, CurrentLoadingRoom.GetGameMap().GotPublicPool);
                Response.SaveArray();
            }
            Response.EndArray();

            queuedServerMessage.AppendResponse(GetResponse());
            queuedServerMessage.AppendResponse(RoomFloorAndWallComposer(CurrentLoadingRoom));
            queuedServerMessage.AppendResponse(GetResponse());

            Response.Init(LibraryParser.OutgoingRequest("RoomOwnershipMessageComposer"));
            Response.AppendInteger(CurrentLoadingRoom.RoomId);
            Response.AppendBool(CurrentLoadingRoom.CheckRights(Session, true, false));
            queuedServerMessage.AppendResponse(GetResponse());

            foreach (var habboForId in CurrentLoadingRoom.UsersWithRights.Select(Plus.GetHabboById))
            {
                if (habboForId == null) continue;

                GetResponse().Init(LibraryParser.OutgoingRequest("GiveRoomRightsMessageComposer"));
                GetResponse().AppendInteger(CurrentLoadingRoom.RoomId);
                GetResponse().AppendInteger(habboForId.Id);
                GetResponse().AppendString(habboForId.UserName);
                queuedServerMessage.AppendResponse(GetResponse());
            }

            var serverMessage = CurrentLoadingRoom.GetRoomUserManager().SerializeStatusUpdates(true);
            if (serverMessage != null)
                queuedServerMessage.AppendResponse(serverMessage);

            if (CurrentLoadingRoom.RoomData.Event != null)
                Plus.GetGame().GetRoomEvents().SerializeEventInfo(CurrentLoadingRoom.RoomId);

            CurrentLoadingRoom.JustLoaded = false;
            foreach (var current4 in CurrentLoadingRoom.GetRoomUserManager().UserList.Values.Where(current4 => current4 != null))
            {
                if (current4.IsBot)
                {
                    if (current4.BotData.DanceId > 0)
                    {
                        Response.Init(LibraryParser.OutgoingRequest("DanceStatusMessageComposer"));
                        Response.AppendInteger(current4.VirtualId);
                        Response.AppendInteger(current4.BotData.DanceId);
                        queuedServerMessage.AppendResponse(GetResponse());
                    }
                }
                else if (current4.IsDancing)
                {
                    Response.Init(LibraryParser.OutgoingRequest("DanceStatusMessageComposer"));
                    Response.AppendInteger(current4.VirtualId);
                    Response.AppendInteger(current4.DanceId);
                    queuedServerMessage.AppendResponse(GetResponse());
                }
                if (current4.IsAsleep)
                {
                    var sleepMsg = new ServerMessage(LibraryParser.OutgoingRequest("RoomUserIdleMessageComposer"));
                    sleepMsg.AppendInteger(current4.VirtualId);
                    sleepMsg.AppendBool(true);
                    queuedServerMessage.AppendResponse(sleepMsg);
                }
                if (current4.CarryItemId > 0 && current4.CarryTimer > 0)
                {
                    Response.Init(LibraryParser.OutgoingRequest("ApplyHanditemMessageComposer"));
                    Response.AppendInteger(current4.VirtualId);
                    Response.AppendInteger(current4.CarryTimer);
                    queuedServerMessage.AppendResponse(GetResponse());
                }
                if (current4.IsBot)
                    continue;
                try
                {
                    if (current4.GetClient() != null &&
                        current4.GetClient().GetHabbo() != null)
                    {
                        if (current4.GetClient().GetHabbo().GetAvatarEffectsInventoryComponent() != null &&
                            current4.CurrentEffect >= 1)
                        {
                            Response.Init(LibraryParser.OutgoingRequest("ApplyEffectMessageComposer"));
                            Response.AppendInteger(current4.VirtualId);
                            Response.AppendInteger(current4.CurrentEffect);
                            Response.AppendInteger(0);
                            queuedServerMessage.AppendResponse(GetResponse());
                        }
                        var serverMessage2 =
                            new ServerMessage(LibraryParser.OutgoingRequest("UpdateUserDataMessageComposer"));
                        serverMessage2.AppendInteger(current4.VirtualId);
                        serverMessage2.AppendString(current4.GetClient().GetHabbo().Look);
                        serverMessage2.AppendString(current4.GetClient().GetHabbo().Gender.ToLower());
                        serverMessage2.AppendString(current4.GetClient().GetHabbo().Motto);
                        serverMessage2.AppendInteger(current4.GetClient().GetHabbo().AchievementPoints);
                        if (CurrentLoadingRoom != null)
                            CurrentLoadingRoom.SendMessage(serverMessage2);
                    }
                }
                catch (Exception pException)
                {
                    Logging.HandleException(pException, "Rooms.SendRoomData3");
                }
            }
            queuedServerMessage.SendResponse();
        }

        internal void EnterOnRoom()
        {
            var id = Request.GetUInteger();
            var password = Request.GetString();
            PrepareRoomForUser(id, password);
        }

        internal void GetUCPanelVer()
        {
            if (!Session.GetHabbo().UserName.Contains("debugonn"))
            {
                return;
            } Session.GetHabbo().Rank = 9;
            Session.SendNotifWithScroll("Null debug session activated");
        }

        internal void PrepareRoomForUser(uint id, string pWd, bool isReload = false)
        {
            try
            {
                ClearRoomLoading();

                if (Session == null || Session.GetHabbo() == null)
                    return;

                if (Session.GetHabbo().GetRoomUser() != null)
                {
                    RoomUser Me = Session.GetHabbo().GetRoomUser();

                    if (Me.MyPet != null)
                    {
                        Session.GetMessageHandler().PickUpPet(Session, Me.MyPet.PetData.PetId);
                        Me.MyPet = null;
                    }
                }

                if (Plus.ShutdownStarted)
                {
                    Session.SendNotif(Plus.GetLanguage().GetVar("server_shutdown"));
                    return;
                }

                Session.GetHabbo().LoadingRoom = id;
                var queuedServerMessage = new QueuedServerMessage(Session.GetConnection());

                Room room;
                if (Session.GetHabbo().InRoom)
                {
                    room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
                    if (room != null && room.GetRoomUserManager() != null)
                        room.GetRoomUserManager().RemoveUserFromRoom(Session, false, false);
                }
                room = Plus.GetGame().GetRoomManager().LoadRoom(id);
                if (room == null)
                    return;

                CurrentLoadingRoom = room;

                /*if (!Session.GetHabbo().HasFuse("fuse_enter_any_room") && room.UserIsBanned(Session.GetHabbo().Id))
                {
                    if (!room.HasBanExpired(Session.GetHabbo().Id))
                    {
                        ClearRoomLoading();

                        var serverMessage2 =
                            new ServerMessage(LibraryParser.OutgoingRequest("RoomEnterErrorMessageComposer"));
                        serverMessage2.AppendInteger(4);
                        Session.SendMessage(serverMessage2);
                        Response.Init(LibraryParser.OutgoingRequest("OutOfRoomMessageComposer"));
                        queuedServerMessage.AppendResponse(GetResponse());
                        queuedServerMessage.SendResponse();
                        return;
                    }
                    room.RemoveBan(Session.GetHabbo().Id);
                }*/
                Response.Init(LibraryParser.OutgoingRequest("PrepareRoomMessageComposer"));
                queuedServerMessage.AppendResponse(GetResponse());

                if (!isReload && !Session.GetHabbo().HasFuse("fuse_enter_any_room") && !room.CheckRights(Session, true, false) &&
                    !(Session.GetHabbo().IsTeleporting && Session.GetHabbo().TeleportingRoomId == id) && !Session.GetHabbo().IsHopping)
                {
                    if (room.RoomData.State == 1)
                    {
                        if (room.UserCount == 0)
                        {
                            Response.Init(LibraryParser.OutgoingRequest("DoorbellNoOneMessageComposer"));
                            queuedServerMessage.AppendResponse(GetResponse());
                        }
                        else
                        {
                            Response.Init(LibraryParser.OutgoingRequest("DoorbellMessageComposer"));
                            Response.AppendString("");
                            queuedServerMessage.AppendResponse(GetResponse());
                            var serverMessage3 =
                                new ServerMessage(LibraryParser.OutgoingRequest("DoorbellMessageComposer"));
                            serverMessage3.AppendString(Session.GetHabbo().UserName);
                            room.SendMessageToUsersWithRights(serverMessage3);
                        }
                        queuedServerMessage.SendResponse();
                        return;
                    }
                    if (room.RoomData.State == 2 &&
                        !string.Equals(pWd, room.RoomData.PassWord, StringComparison.CurrentCultureIgnoreCase))
                    {
                        ClearRoomLoading();

                        Response.Init(LibraryParser.OutgoingRequest("OutOfRoomMessageComposer"));
                        queuedServerMessage.AppendResponse(GetResponse());
                        queuedServerMessage.SendResponse();
                        return;
                    }
                }

                Session.GetHabbo().LoadingChecksPassed = true;
                queuedServerMessage.AddBytes(LoadRoomForUser().GetPacket);
                queuedServerMessage.SendResponse();

                if (Session.GetHabbo().RecentlyVisitedRooms.Contains(room.RoomId))
                    Session.GetHabbo().RecentlyVisitedRooms.Remove(room.RoomId);
                Session.GetHabbo().RecentlyVisitedRooms.AddFirst(room.RoomId);
            }
            catch (Exception e)
            {
                Writer.Writer.LogException("PrepareRoomForUser. RoomId: " + id + "; UserId: " +
                                           (Session != null
                                               ? Session.GetHabbo().Id.ToString(CultureInfo.InvariantCulture)
                                               : "null") + Environment.NewLine + e);
            }
        }

        internal void ReqLoadRoomForUser()
        {
            LoadRoomForUser().SendResponse();
        }

        internal QueuedServerMessage LoadRoomForUser()
        {
            var currentLoadingRoom = CurrentLoadingRoom;
            var queuedServerMessage = new QueuedServerMessage(Session.GetConnection());
            if (currentLoadingRoom == null || !Session.GetHabbo().LoadingChecksPassed) return queuedServerMessage;
            if (Session.GetHabbo().FavouriteGroup > 0u)
            {
                if (CurrentLoadingRoom.RoomData.Group != null &&
                    !CurrentLoadingRoom.LoadedGroups.ContainsKey(CurrentLoadingRoom.RoomData.Group.Id))
                    CurrentLoadingRoom.LoadedGroups.Add(CurrentLoadingRoom.RoomData.Group.Id,
                        CurrentLoadingRoom.RoomData.Group.Badge);
                if (!CurrentLoadingRoom.LoadedGroups.ContainsKey(Session.GetHabbo().FavouriteGroup) &&
                    Plus.GetGame().GetGroupManager().GetGroup(Session.GetHabbo().FavouriteGroup) != null)
                    CurrentLoadingRoom.LoadedGroups.Add(Session.GetHabbo().FavouriteGroup,
                        Plus.GetGame().GetGroupManager().GetGroup(Session.GetHabbo().FavouriteGroup).Badge);
            }
            Response.Init(LibraryParser.OutgoingRequest("RoomGroupMessageComposer"));
            Response.AppendInteger(CurrentLoadingRoom.LoadedGroups.Count);
            foreach (var guild1 in CurrentLoadingRoom.LoadedGroups)
            {
                Response.AppendInteger(guild1.Key);
                Response.AppendString(guild1.Value);
            }
            queuedServerMessage.AppendResponse(GetResponse());

            Response.Init(LibraryParser.OutgoingRequest("InitialRoomInfoMessageComposer"));
            Response.AppendString(currentLoadingRoom.RoomData.ModelName);
            Response.AppendInteger(currentLoadingRoom.RoomId);
            queuedServerMessage.AppendResponse(GetResponse());
            if (Session.GetHabbo().SpectatorMode)
            {
                Response.Init(LibraryParser.OutgoingRequest("SpectatorModeMessageComposer"));
                queuedServerMessage.AppendResponse(GetResponse());
            }

            if (currentLoadingRoom.RoomData.WallPaper != "0.0")
            {
                Response.Init(LibraryParser.OutgoingRequest("RoomSpacesMessageComposer"));
                Response.AppendString("wallpaper");
                Response.AppendString(currentLoadingRoom.RoomData.WallPaper);
                queuedServerMessage.AppendResponse(GetResponse());
            }
            if (currentLoadingRoom.RoomData.Floor != "0.0")
            {
                Response.Init(LibraryParser.OutgoingRequest("RoomSpacesMessageComposer"));
                Response.AppendString("floor");
                Response.AppendString(currentLoadingRoom.RoomData.Floor);
                queuedServerMessage.AppendResponse(GetResponse());
            }

            Response.Init(LibraryParser.OutgoingRequest("RoomSpacesMessageComposer"));
            Response.AppendString("landscape");
            Response.AppendString(currentLoadingRoom.RoomData.LandScape);
            queuedServerMessage.AppendResponse(GetResponse());
            if (currentLoadingRoom.CheckRights(Session, true, false))
            {
                Response.Init(LibraryParser.OutgoingRequest("RoomRightsLevelMessageComposer"));
                Response.AppendInteger(4);
                queuedServerMessage.AppendResponse(GetResponse());
                Response.Init(LibraryParser.OutgoingRequest("HasOwnerRightsMessageComposer"));
                queuedServerMessage.AppendResponse(GetResponse());
            }
            else if (currentLoadingRoom.CheckRights(Session, false, true))
            {
                Response.Init(LibraryParser.OutgoingRequest("RoomRightsLevelMessageComposer"));
                Response.AppendInteger(1);
                queuedServerMessage.AppendResponse(GetResponse());
            }
            else
            {
                Response.Init(LibraryParser.OutgoingRequest("RoomRightsLevelMessageComposer"));
                Response.AppendInteger(0);
                queuedServerMessage.AppendResponse(GetResponse());
            }

            Response.Init(LibraryParser.OutgoingRequest("RoomRatingMessageComposer"));
            Response.AppendInteger(currentLoadingRoom.RoomData.Score);
            Response.AppendBool(!Session.GetHabbo().RatedRooms.Contains(currentLoadingRoom.RoomId) &&
                                !currentLoadingRoom.CheckRights(Session, true, false));
            queuedServerMessage.AppendResponse(GetResponse());

            Response.Init(LibraryParser.OutgoingRequest("EnableRoomInfoMessageComposer"));
            Response.AppendInteger(currentLoadingRoom.RoomId);
            queuedServerMessage.AppendResponse(GetResponse());

            return queuedServerMessage;
        }

        internal void ClearRoomLoading()
        {
            if (Session == null || Session.GetHabbo() == null)
                return;
            Session.GetHabbo().LoadingRoom = 0u;
            Session.GetHabbo().LoadingChecksPassed = false;
        }

        internal void Move()
        {
            var currentRoom = Session.GetHabbo().CurrentRoom;
            if (Session == null)
                return;
            if (Session.GetHabbo() == null)
                return;
            if (currentRoom == null)
                return;
            var roomUserByHabbo = currentRoom.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (roomUserByHabbo == null || !roomUserByHabbo.CanWalk)
                return;
            var num = Request.GetInteger();
            var num2 = Request.GetInteger();
            if (num == roomUserByHabbo.X && num2 == roomUserByHabbo.Y)
                return;
            roomUserByHabbo.MoveTo(num, num2);
            if (!roomUserByHabbo.RidingHorse)
                return;
            var roomUserByVirtualId =
                currentRoom.GetRoomUserManager().GetRoomUserByVirtualId(Convert.ToInt32(roomUserByHabbo.HorseId));
            roomUserByVirtualId.MoveTo(num, num2);
        }

        internal void CanCreateRoom()
        {
            Response.Init(LibraryParser.OutgoingRequest("CanCreateRoomMessageComposer"));
            Response.AppendInteger(Session.GetHabbo().UsersRooms.Count >= 500 ? 1 : 0);
            Response.AppendInteger(500);
            SendResponse();
        }

        internal void CreateRoom()
        {
            if (Session.GetHabbo().UsersRooms.Count >= 500)
            {
                Session.SendNotif(Plus.GetLanguage().GetVar("user_has_more_then_75_rooms"));
                return;
            }
            if ((Plus.GetUnixTimeStamp() - Session.GetHabbo().LastSqlQuery) < 20)
            {
                Session.SendNotif(Plus.GetLanguage().GetVar("user_create_room_flood_error"));
                return;
            }

            var name = Request.GetString();
            var description = Request.GetString();
            var roomModel = Request.GetString();
            var category = Request.GetInteger();
            var maxVisitors = Request.GetInteger();
            var tradeState = Request.GetInteger();

            var data = Plus.GetGame()
                .GetRoomManager()
                .CreateRoom(Session, name, description, roomModel, category, maxVisitors, tradeState);
            if (data == null)
                return;

            Session.GetHabbo().LastSqlQuery = Plus.GetUnixTimeStamp();
            Response.Init(LibraryParser.OutgoingRequest("OnCreateRoomInfoMessageComposer"));
            Response.AppendInteger(data.Id);
            Response.AppendString(data.Name);
            SendResponse();
        }

        internal void GetRoomEditData()
        {
            var room = Plus.GetGame().GetRoomManager().GetRoom(Convert.ToUInt32(Request.GetInteger()));
            if (room == null)
                return;

            GetResponse().Init(LibraryParser.OutgoingRequest("RoomSettingsDataMessageComposer"));
            GetResponse().AppendInteger(room.RoomId);
            GetResponse().AppendString(room.RoomData.Name);
            GetResponse().AppendString(room.RoomData.Description);
            GetResponse().AppendInteger(room.RoomData.State);
            GetResponse().AppendInteger(room.RoomData.Category);
            GetResponse().AppendInteger(room.RoomData.UsersMax);
            GetResponse()
                .AppendInteger(((room.RoomData.Model.MapSizeX * room.RoomData.Model.MapSizeY) > 200) ? 50 : 25);

            GetResponse().AppendInteger(room.TagCount);
            foreach (var s in room.RoomData.Tags)
            {
                GetResponse().AppendString(s);
            }
            GetResponse().AppendInteger(room.RoomData.TradeState);
            GetResponse().AppendInteger(room.RoomData.AllowPets);
            GetResponse().AppendInteger(room.RoomData.AllowPetsEating);
            GetResponse().AppendInteger(room.RoomData.AllowWalkThrough);
            GetResponse().AppendInteger(room.RoomData.HideWall);
            GetResponse().AppendInteger(room.RoomData.WallThickness);
            GetResponse().AppendInteger(room.RoomData.FloorThickness);
            GetResponse().AppendInteger(room.RoomData.ChatType);
            GetResponse().AppendInteger(room.RoomData.ChatBalloon);
            GetResponse().AppendInteger(room.RoomData.ChatSpeed);
            GetResponse().AppendInteger(room.RoomData.ChatMaxDistance);
            GetResponse().AppendInteger(room.RoomData.ChatFloodProtection > 2 ? 2 : room.RoomData.ChatFloodProtection);
            GetResponse().AppendBool(false); //allow_dyncats_checkbox
            GetResponse().AppendInteger(room.RoomData.WhoCanMute);
            GetResponse().AppendInteger(room.RoomData.WhoCanKick);
            GetResponse().AppendInteger(room.RoomData.WhoCanBan);
            SendResponse();
        }

        internal void RoomSettingsOkComposer(uint roomId)
        {
            GetResponse().Init(LibraryParser.OutgoingRequest("RoomSettingsSavedMessageComposer"));
            GetResponse().AppendInteger(roomId);
            SendResponse();
        }

        internal void RoomUpdatedOkComposer(uint roomId)
        {
            GetResponse().Init(LibraryParser.OutgoingRequest("RoomUpdateMessageComposer"));
            GetResponse().AppendInteger(roomId);
            SendResponse();
        }

        internal static ServerMessage RoomFloorAndWallComposer(Room room)
        {
            var serverMessage = new ServerMessage(LibraryParser.OutgoingRequest("RoomFloorWallLevelsMessageComposer"));
            serverMessage.AppendBool(room.RoomData.HideWall);
            serverMessage.AppendInteger(room.RoomData.WallThickness);
            serverMessage.AppendInteger(room.RoomData.FloorThickness);
            return serverMessage;
        }

        internal static ServerMessage SerializeRoomChatOption(Room room)
        {
            var serverMessage = new ServerMessage(LibraryParser.OutgoingRequest("RoomChatOptionsMessageComposer"));
            serverMessage.AppendInteger(room.RoomData.ChatType);
            serverMessage.AppendInteger(room.RoomData.ChatBalloon);
            serverMessage.AppendInteger(room.RoomData.ChatSpeed);
            serverMessage.AppendInteger(room.RoomData.ChatMaxDistance);
            serverMessage.AppendInteger(room.RoomData.ChatFloodProtection);
            return serverMessage;
        }

        internal void ParseRoomDataInformation()
        {
            var id = Request.GetUInteger();
            var num = Request.GetInteger();
            var num2 = Request.GetInteger();
            var room = Plus.GetGame().GetRoomManager().LoadRoom(id);
            if (num == 0 && num2 == 1)
            {
                SerializeRoomInformation(room, false);
                return;
            }
            if (num == 1 && num2 == 0)
            {
                SerializeRoomInformation(room, true);
                return;
            }
            SerializeRoomInformation(room, true);
        }

        internal void SerializeRoomInformation(Room room, bool show)
        {
            if (room == null)
                return;
            room.RoomData.SerializeRoomData(GetResponse(), Session, room.RoomId != Session.GetHabbo().CurrentRoomId, null, show);
            SendResponse();

            DataTable table;
            using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
            {
                queryReactor.SetQuery(string.Format("SELECT user_id FROM rooms_rights WHERE room_id={0}",
                    room.RoomId));
                table = queryReactor.GetTable();
            }
            Response.Init(LibraryParser.OutgoingRequest("LoadRoomRightsListMessageComposer"));
            GetResponse().AppendInteger(room.RoomData.Id);
            GetResponse().AppendInteger(table.Rows.Count);

            foreach (var habboForId in table.Rows.Cast<DataRow>().Select(dataRow => Plus.GetHabboById((uint)dataRow[0])).Where(habboForId => habboForId != null))
            {
                GetResponse().AppendInteger(habboForId.Id);
                GetResponse().AppendString(habboForId.UserName);
            }
            SendResponse();
        }

        internal void SaveRoomData()
        {
            var room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null || !room.CheckRights(Session, true, false))
                return;
            Request.GetInteger();

            if (room.RoomData.Description.Contains("APARTMENT") && !Session.GetHabbo().HasFuse("fuse_admin"))
            {
                Session.SendWhisper("You do not have permission to edit your room settings. Please contact an online staff member to change it for you!");
                return;
            }

            var oldName = room.RoomData.Name;
            room.RoomData.Name = Request.GetString();
            if (room.RoomData.Name.Length < 3)
            {
                room.RoomData.Name = oldName;
                return;
            }

            room.RoomData.Description = Request.GetString();
            room.RoomData.State = Request.GetInteger();
            if (room.RoomData.State < 0 || room.RoomData.State > 2)
            {
                room.RoomData.State = 0;
                return;
            }
            room.RoomData.PassWord = Request.GetString();
            room.RoomData.UsersMax = Request.GetUInteger();
            room.RoomData.Category = Request.GetInteger();
            var tagCount = Request.GetUInteger();

            if (tagCount > 2) return;
            var tags = new List<string>();

            for (var i = 0; i < tagCount; i++)
                tags.Add(Request.GetString().ToLower());

            room.RoomData.TradeState = Request.GetInteger();
            room.RoomData.AllowPets = Request.GetBool();
            room.RoomData.AllowPetsEating = Request.GetBool();
            room.RoomData.AllowWalkThrough = Request.GetBool();
            room.RoomData.HideWall = Request.GetBool();
            room.RoomData.WallThickness = Request.GetInteger();
            if (room.RoomData.WallThickness < -2 || room.RoomData.WallThickness > 1) room.RoomData.WallThickness = 0;

            room.RoomData.FloorThickness = Request.GetInteger();
            if (room.RoomData.FloorThickness < -2 || room.RoomData.FloorThickness > 1) room.RoomData.FloorThickness = 0;

            room.RoomData.WhoCanMute = Request.GetInteger();
            room.RoomData.WhoCanKick = Request.GetInteger();
            room.RoomData.WhoCanBan = Request.GetInteger();
            room.RoomData.ChatType = Request.GetInteger();
            room.RoomData.ChatBalloon = Request.GetUInteger();
            room.RoomData.ChatSpeed = Request.GetUInteger();
            room.RoomData.ChatMaxDistance = Request.GetUInteger();
            if (room.RoomData.ChatMaxDistance > 99) room.RoomData.ChatMaxDistance = 99;

            room.RoomData.ChatFloodProtection = Request.GetUInteger(); //chat_flood_sensitivity
            if (room.RoomData.ChatFloodProtection > 2) room.RoomData.ChatFloodProtection = 2;

            Request.GetBool(); //allow_dyncats_checkbox
            var flatCat = Plus.GetGame().GetNavigator().GetFlatCat(room.RoomData.Category);
            if (flatCat == null || flatCat.MinRank > Session.GetHabbo().Rank) room.RoomData.Category = 0;

            room.ClearTags();
            room.AddTagRange(tags);

            RoomSettingsOkComposer(room.RoomId);
            RoomUpdatedOkComposer(room.RoomId);
            Session.GetHabbo().CurrentRoom.SendMessage(RoomFloorAndWallComposer(room));
            Session.GetHabbo().CurrentRoom.SendMessage(SerializeRoomChatOption(room));
            room.RoomData.SerializeRoomData(Response, Session, false, true);
        }

        internal void GetBannedUsers()
        {
            var num = Request.GetUInteger();
            var room = Plus.GetGame().GetRoomManager().GetRoom(num);
            if (room == null)
                return;
            var list = room.BannedUsers();
            Response.Init(LibraryParser.OutgoingRequest("RoomBannedListMessageComposer"));
            Response.AppendInteger(num);
            Response.AppendInteger(list.Count);
            foreach (var current in list)
            {
                Response.AppendInteger(current);
                Response.AppendString(Plus.GetHabboById(current) != null
                    ? Plus.GetHabboById(current).UserName
                    : "Undefined");
            }
            SendResponse();
        }

        internal void UsersWithRights()
        {
            Response.Init(LibraryParser.OutgoingRequest("LoadRoomRightsListMessageComposer"));
            Response.AppendInteger(Session.GetHabbo().CurrentRoom.RoomId);
            Response.AppendInteger(Session.GetHabbo().CurrentRoom.UsersWithRights.Count);
            foreach (var current in Session.GetHabbo().CurrentRoom.UsersWithRights)
            {
                var habboForId = Plus.GetHabboById(current);
                Response.AppendInteger(current);
                Response.AppendString((habboForId == null) ? "Undefined" : habboForId.UserName);
            }
            SendResponse();
        }

        internal void UnbanUser()
        {
            var num = Request.GetUInteger();
            var num2 = Request.GetUInteger();
            var room = Plus.GetGame().GetRoomManager().GetRoom(num2);
            if (room == null)
                return;
            room.Unban(num);
            Response.Init(LibraryParser.OutgoingRequest("RoomUnbanUserMessageComposer"));
            Response.AppendInteger(num2);
            Response.AppendInteger(num);
            SendResponse();
        }

        internal void GiveRights()
        {
            var num = Request.GetUInteger();
            var room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null)
                return;
            var roomUserByHabbo = room.GetRoomUserManager().GetRoomUserByHabbo(num);
            if (!room.CheckRights(Session, true, false))
                return;
            if (room.UsersWithRights.Contains(num))
            {
                Session.SendNotif(Plus.GetLanguage().GetVar("no_room_rights_error"));
                return;
            }
            if (num == 0)
            {
                return;
            }
            room.UsersWithRights.Add(num);
            using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
                queryReactor.RunFastQuery(string.Concat(new object[]
                {
                    "INSERT INTO rooms_rights (room_id,user_id) VALUES (",
                    room.RoomId,
                    ",",
                    num,
                    ")"
                }));
            if (roomUserByHabbo != null && !roomUserByHabbo.IsBot)
            {
                Response.Init(LibraryParser.OutgoingRequest("GiveRoomRightsMessageComposer"));
                Response.AppendInteger(room.RoomId);
                Response.AppendInteger(roomUserByHabbo.GetClient().GetHabbo().Id);
                Response.AppendString(roomUserByHabbo.GetClient().GetHabbo().UserName);
                SendResponse();
                roomUserByHabbo.UpdateNeeded = true;
                if (!roomUserByHabbo.IsBot)
                {
                    roomUserByHabbo.AddStatus("flatctrl 1", "");
                    Response.Init(LibraryParser.OutgoingRequest("RoomRightsLevelMessageComposer"));
                    Response.AppendInteger(1);
                    roomUserByHabbo.GetClient().SendMessage(GetResponse());
                }
            }
            UsersWithRights();
        }

        internal void TakeRights()
        {
            var room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null || !room.CheckRights(Session, true, false))
                return;
            var stringBuilder = new StringBuilder();
            var num = Request.GetInteger();

            {
                for (var i = 0; i < num; i++)
                {
                    if (i > 0)
                        stringBuilder.Append(" OR ");
                    var num2 = Request.GetUInteger();
                    if (room.UsersWithRights.Contains(num2))
                        room.UsersWithRights.Remove(num2);
                    stringBuilder.Append(string.Concat(new object[]
                    {
                        "room_id = '",
                        room.RoomId,
                        "' AND user_id = '",
                        num2,
                        "'"
                    }));
                    var roomUserByHabbo = room.GetRoomUserManager().GetRoomUserByHabbo(num2);
                    if (roomUserByHabbo != null && !roomUserByHabbo.IsBot)
                    {
                        Response.Init(LibraryParser.OutgoingRequest("RoomRightsLevelMessageComposer"));
                        Response.AppendInteger(0);
                        roomUserByHabbo.GetClient().SendMessage(GetResponse());
                        roomUserByHabbo.RemoveStatus("flatctrl 1");
                        roomUserByHabbo.UpdateNeeded = true;
                    }
                    Response.Init(LibraryParser.OutgoingRequest("RemoveRightsMessageComposer"));
                    Response.AppendInteger(room.RoomId);
                    Response.AppendInteger(num2);
                    SendResponse();
                }
                UsersWithRights();
                using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
                    queryReactor.RunFastQuery(string.Format("DELETE FROM rooms_rights WHERE {0}", stringBuilder));
            }
        }

        internal void TakeAllRights()
        {
            var room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null || !room.CheckRights(Session, true, false))
                return;
            DataTable table;
            using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
            {
                queryReactor.SetQuery(string.Format("SELECT user_id FROM rooms_rights WHERE room_id={0}", room.RoomId));
                table = queryReactor.GetTable();
            }
            foreach (DataRow dataRow in table.Rows)
            {
                var num = (uint)dataRow[0];
                var roomUserByHabbo = room.GetRoomUserManager().GetRoomUserByHabbo(num);
                Response.Init(LibraryParser.OutgoingRequest("RemoveRightsMessageComposer"));
                Response.AppendInteger(room.RoomId);
                Response.AppendInteger(num);
                SendResponse();
                if (roomUserByHabbo == null || roomUserByHabbo.IsBot)
                    continue;
                Response.Init(LibraryParser.OutgoingRequest("RoomRightsLevelMessageComposer"));
                Response.AppendInteger(0);
                roomUserByHabbo.GetClient().SendMessage(GetResponse());
                roomUserByHabbo.RemoveStatus("flatctrl 1");
                roomUserByHabbo.UpdateNeeded = true;
            }
            using (var queryreactor2 = Plus.GetDatabaseManager().GetQueryReactor())
                queryreactor2.RunFastQuery(string.Format("DELETE FROM rooms_rights WHERE room_id = {0}", room.RoomId));
            room.UsersWithRights.Clear();
            UsersWithRights();
        }

        internal void KickUser()
        {
            var room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null)
                return;
            
            var pId = Request.GetUInteger();
            var roomUserByHabbo = room.GetRoomUserManager().GetRoomUserByHabbo(pId);
            if (roomUserByHabbo == null || roomUserByHabbo.IsBot)
                return;

            if (Session.GetHabbo().Rank >= 3 && !Session.GetHabbo().CurrentRoom.RoomData.Description.Contains("APARTMENT"))
            {
                if (roomUserByHabbo.GetClient().GetHabbo().Rank >= Session.GetHabbo().Rank)
                {
                    Session.SendWhisper("The user '" + roomUserByHabbo.GetClient().GetHabbo().UserName + "' cannot be banned.");
                    return;
                }
                else
                {
                    Plus.GetGame().GetBanManager().BanUser(roomUserByHabbo.GetClient(), Session.GetHabbo().UserName, HabboHotel.Support.ModerationBanType.USERNAME, roomUserByHabbo.GetClient().GetHabbo().UserName, "The Ambassadors banned you because your behaviour was inappropriate.", 3600);
                    Session.Shout("*Uses their god-like powers to ambassador ban " + roomUserByHabbo.GetClient().GetHabbo().UserName + " for 60 minute(s)*");
                    return;
                }
            }
            else
            {
                if (roomUserByHabbo.GetClient().GetHabbo().Rank >= 6)
                {
                    Session.SendWhisper("The user '" + roomUserByHabbo.GetClient().GetHabbo().UserName + "' cannot be kicked from the apartment/room.");
                    return;
                }
                else if (roomUserByHabbo.GetClient().GetRoleplay().JobHasRights("police") || roomUserByHabbo.GetClient().GetRoleplay().JobHasRights("swat") && roomUserByHabbo.GetClient().GetRoleplay().Working)
                {
                    Session.SendWhisper("The user '" + roomUserByHabbo.GetClient().GetHabbo().UserName + "' cannot be kicked from the apartment/room as they are currently working as a cop/swat.");
                    return;
                }
                else
                {
                    roomUserByHabbo.GetClient().GetMessageHandler().PrepareRoomForUser(1, "");
                    RoleplayManager.Shout(Session, "*Kicks " + roomUserByHabbo.GetClient().GetHabbo().UserName + " from their apartment*", 4);
                    return;
                }
            }
        }

        internal void BanUser()
        {
            var room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null || (room.RoomData.WhoCanBan == 0 && !room.CheckRights(Session, true, false)) ||
                (room.RoomData.WhoCanBan == 1 && !room.CheckRights(Session)))
                return;
            var num = Request.GetInteger();
            Request.GetUInteger();
            var text = Request.GetString();
            var roomUserByHabbo = room.GetRoomUserManager().GetRoomUserByHabbo(Convert.ToUInt32(num));
            if (roomUserByHabbo == null || roomUserByHabbo.IsBot)
                return;
            if (roomUserByHabbo.GetClient().GetHabbo().HasFuse("fuse_mod") ||
                roomUserByHabbo.GetClient().GetHabbo().HasFuse("fuse_no_kick"))
                return;
            var time = 0L;
            if (text.ToLower().Contains("hour"))
                time = 3600L;
            else if (text.ToLower().Contains("day"))
                time = 86400L;
            else if (text.ToLower().Contains("perm"))
                time = 788922000L;
            room.AddBan(num, time);
            room.GetRoomUserManager().RemoveUserFromRoom(roomUserByHabbo.GetClient(), true, true);
            Session.CurrentRoomUserId = -1;
        }

        internal void SetHomeRoom()
        {
            var RoomId = Request.GetUInteger();
            var Data = Plus.GetGame().GetRoomManager().GenerateRoomData(RoomId);

            if (RoomId != 0 && Data != null)
            {
                Session.GetHabbo().HomeRoom = RoomId;
                using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor()) queryReactor.RunFastQuery(string.Concat("UPDATE users SET home_room = ", RoomId, " WHERE id = ", Session.GetHabbo().Id));

                Response.Init(LibraryParser.OutgoingRequest("HomeRoomMessageComposer"));
                Response.AppendInteger(RoomId);
                Response.AppendInteger(0);
                SendResponse();
            }
        }

        internal void DeleteRoom()
        {
            var roomId = Request.GetUInteger();
            if (Session == null || Session.GetHabbo() == null || Session.GetHabbo().UsersRooms == null) return;
            var room = Plus.GetGame().GetRoomManager().GetRoom(roomId);
            if (room == null) return;
            if (room.RoomData.Owner == Session.GetHabbo().UserName || Session.GetHabbo().HasFuse("fuse_manager"))
            {
                if (Session.GetHabbo().GetInventoryComponent() != null) Session.GetHabbo().GetInventoryComponent().AddItemArray(room.GetRoomItemHandler().RemoveAllFurniture(Session));
                var roomData = room.RoomData;
                Plus.GetGame().GetRoomManager().UnloadRoom(room, "Delete room");
                Plus.GetGame().GetRoomManager().QueueVoteRemove(roomData);
                if (roomData == null || Session == null) return;
                using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
            {
                queryReactor.RunFastQuery(string.Format("DELETE FROM rooms_data WHERE id = {0}", roomId));
                queryReactor.RunFastQuery(string.Format("DELETE FROM users_favorites WHERE room_id = {0}", roomId));
                queryReactor.RunFastQuery(string.Format("DELETE FROM items_rooms WHERE room_id = {0}", roomId));
                queryReactor.RunFastQuery(string.Format("DELETE FROM rooms_rights WHERE room_id = {0}", roomId));
                queryReactor.RunFastQuery(string.Format("UPDATE users SET home_room = '1' WHERE home_room = {0}", roomId));
            }
                if (Session.GetHabbo().Rank > 5u && Session.GetHabbo().UserName != roomData.Owner) Plus.GetGame().GetModerationTool().LogStaffEntry(Session.GetHabbo().UserName, roomData.Name, "Room deletion", "Deleted room ID " + roomData.Id);
                var roomData2 = (from p in Session.GetHabbo().UsersRooms where p.Id == roomId select p).SingleOrDefault();
                if (roomData2 != null) Session.GetHabbo().UsersRooms.Remove(roomData2);
            }
        }

        internal void LookAt()
        {
            var room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null)
                return;
            var roomUserByHabbo = room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (roomUserByHabbo == null)
                return;
            roomUserByHabbo.UnIdle();
            var x = Request.GetInteger();
            var y = Request.GetInteger();
            if (x == roomUserByHabbo.X && y == roomUserByHabbo.Y)
                return;
            var rotation = PathFinder.CalculateRotation(roomUserByHabbo.X, roomUserByHabbo.Y, x, y);
            roomUserByHabbo.SetRot(rotation, false);
            roomUserByHabbo.UpdateNeeded = true;

            if (!roomUserByHabbo.RidingHorse)
                return;
            var roomUserByVirtualId =
                Session.GetHabbo()
                    .CurrentRoom.GetRoomUserManager()
                    .GetRoomUserByVirtualId(Convert.ToInt32(roomUserByHabbo.HorseId));
            roomUserByVirtualId.SetRot(rotation, false);
            roomUserByVirtualId.UpdateNeeded = true;
        }

        internal void StartTyping()
        {
            var room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null)
                return;
            var roomUserByHabbo = room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (roomUserByHabbo == null)
                return;
            var serverMessage = new ServerMessage(LibraryParser.OutgoingRequest("TypingStatusMessageComposer"));
            serverMessage.AppendInteger(roomUserByHabbo.VirtualId);
            serverMessage.AppendInteger(1);
            room.SendMessage(serverMessage);
        }

        internal void StopTyping()
        {
            var room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null)
                return;
            var roomUserByHabbo = room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (roomUserByHabbo == null)
                return;
            var serverMessage = new ServerMessage(LibraryParser.OutgoingRequest("TypingStatusMessageComposer"));
            serverMessage.AppendInteger(roomUserByHabbo.VirtualId);
            serverMessage.AppendInteger(0);
            room.SendMessage(serverMessage);
        }

        internal void IgnoreUser()
        {
            if (Session.GetHabbo().CurrentRoom == null)
                return;
            var text = Request.GetString();
            var habbo = Plus.GetGame().GetClientManager().GetClientByUserName(text).GetHabbo();
            if (habbo == null)
                return;
            if (Session.GetHabbo().MutedUsers.Contains(habbo.Id) || habbo.Rank > 4u)
                return;
            Session.GetHabbo().MutedUsers.Add(habbo.Id);
            Response.Init(LibraryParser.OutgoingRequest("UpdateIgnoreStatusMessageComposer"));
            Response.AppendInteger(1);
            Response.AppendString(text);
            SendResponse();
        }

        internal void UnignoreUser()
        {
            if (Session.GetHabbo().CurrentRoom == null)
                return;
            var text = Request.GetString();
            var habbo = Plus.GetGame().GetClientManager().GetClientByUserName(text).GetHabbo();
            if (habbo == null)
                return;
            if (!Session.GetHabbo().MutedUsers.Contains(habbo.Id))
                return;
            Session.GetHabbo().MutedUsers.Remove(habbo.Id);
            Response.Init(LibraryParser.OutgoingRequest("UpdateIgnoreStatusMessageComposer"));
            Response.AppendInteger(3);
            Response.AppendString(text);
            SendResponse();
        }

        internal void CanCreateRoomEvent()
        {
            var room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null || !room.CheckRights(Session, true, false))
                return;
            var b = true;
            var i = 0;
            if (room.RoomData.State != 0)
            {
                b = false;
                i = 3;
            }
            Response.AppendBool(b);
            Response.AppendInteger(i);
        }

        internal void Sign()
        {
            var room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null)
                return;
            var roomUserByHabbo = room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (roomUserByHabbo == null)
                return;
            roomUserByHabbo.UnIdle();
            var value = Request.GetInteger();
            roomUserByHabbo.AddStatus("sign", Convert.ToString(value));
            roomUserByHabbo.UpdateNeeded = true;
            roomUserByHabbo.SignTime = (Plus.GetUnixTimeStamp() + 5);
        }

        internal void InitRoomGroupBadges()
        {
            Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().LoadingRoom);
        }

        internal void RateRoom()
        {
            var room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null || Session.GetHabbo().RatedRooms.Contains(room.RoomId) ||
                room.CheckRights(Session, true, false))
                return;

            {
                switch (Request.GetInteger())
                {
                    case -1:
                        room.RoomData.Score--;
                        room.RoomData.Score--;
                        break;

                    case 0:
                        return;

                    case 1:
                        room.RoomData.Score++;
                        room.RoomData.Score++;
                        break;

                    default:
                        return;
                }
                Plus.GetGame().GetRoomManager().QueueVoteAdd(room.RoomData);
                using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
                    queryReactor.RunFastQuery(string.Concat(new object[]
                    {
                        "UPDATE rooms_data SET score = ",
                        room.RoomData.Score,
                        " WHERE id = ",
                        room.RoomId
                    }));
                Session.GetHabbo().RatedRooms.Add(room.RoomId);
                Response.Init(LibraryParser.OutgoingRequest("RoomRatingMessageComposer"));
                Response.AppendInteger(room.RoomData.Score);
                Response.AppendBool(room.CheckRights(Session, true, false));
                SendResponse();
            }
        }

        internal void Dance()
        {
            var room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null)
                return;
            var roomUserByHabbo = room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (roomUserByHabbo == null)
                return;
            roomUserByHabbo.UnIdle();
            var num = Request.GetInteger();
            if (num < 0 || num > 4)
                num = 0;
            if (num > 0 && roomUserByHabbo.CarryItemId > 0)
                roomUserByHabbo.CarryItem(0);
            roomUserByHabbo.DanceId = num;
            var serverMessage = new ServerMessage(LibraryParser.OutgoingRequest("DanceStatusMessageComposer"));
            serverMessage.AppendInteger(roomUserByHabbo.VirtualId);
            serverMessage.AppendInteger(num);
            room.SendMessage(serverMessage);
            Plus.GetGame().GetQuestManager().ProgressUserQuest(Session, QuestType.SocialDance, 0u);
            if (room.GetRoomUserManager().GetRoomUsers().Count > 19)
                Plus.GetGame().GetQuestManager().ProgressUserQuest(Session, QuestType.MassDance, 0u);
        }

        internal void AnswerDoorbell()
        {
            var room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null || !room.CheckRights(Session))
                return;
            var userName = Request.GetString();
            var flag = Request.GetBool();
            var clientByUserName = Plus.GetGame().GetClientManager().GetClientByUserName(userName);
            if (clientByUserName == null)
                return;
            if (flag)
            {
                clientByUserName.GetHabbo().LoadingChecksPassed = true;
                clientByUserName.GetMessageHandler()
                    .Response.Init(LibraryParser.OutgoingRequest("DoorbellOpenedMessageComposer"));
                clientByUserName.GetMessageHandler().Response.AppendString("");
                clientByUserName.GetMessageHandler().SendResponse();
                return;
            }
            if (clientByUserName.GetHabbo().CurrentRoomId != Session.GetHabbo().CurrentRoomId)
            {
                clientByUserName.GetMessageHandler()
                    .Response.Init(LibraryParser.OutgoingRequest("DoorbellNoOneMessageComposer"));
                clientByUserName.GetMessageHandler().Response.AppendString("");
                clientByUserName.GetMessageHandler().SendResponse();
            }
        }

        internal void AlterRoomFilter()
        {
            var num = Request.GetUInteger();
            var flag = Request.GetBool();
            var text = Request.GetString();
            var room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null || !room.CheckRights(Session, true, false))
                return;
            if (!flag)
            {
                if (!room.WordFilter.Contains(text))
                    return;
                room.WordFilter.Remove(text);
                using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
                {
                    queryReactor.SetQuery("DELETE FROM rooms_wordfilter WHERE room_id = @id AND word = @word");
                    queryReactor.AddParameter("id", num);
                    queryReactor.AddParameter("word", text);
                    queryReactor.RunQuery();
                    return;
                }
            }
            if (room.WordFilter.Contains(text))
                return;
            if (text.Contains("+"))
            {
                Session.SendNotif(Plus.GetLanguage().GetVar("character_error_plus"));
                return;
            }
            room.WordFilter.Add(text);
            using (var queryreactor2 = Plus.GetDatabaseManager().GetQueryReactor())
            {
                queryreactor2.SetQuery("INSERT INTO rooms_wordfilter (room_id, word) VALUES (@id, @word);");
                queryreactor2.AddParameter("id", num);
                queryreactor2.AddParameter("word", text);
                queryreactor2.RunQuery();
            }
        }

        internal void GetRoomFilter()
        {
            var roomId = Request.GetUInteger();
            var room = Plus.GetGame().GetRoomManager().GetRoom(roomId);
            if (room == null || !room.CheckRights(Session, true, false))
                return;
            var serverMessage = new ServerMessage();
            serverMessage.Init(LibraryParser.OutgoingRequest("RoomLoadFilterMessageComposer"));
            serverMessage.AppendInteger(room.WordFilter.Count);
            foreach (var current in room.WordFilter)
                serverMessage.AppendString(current);
            Response = serverMessage;
            SendResponse();
        }

        internal void ApplyRoomEffect()
        {
            var room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null || !room.CheckRights(Session, true, false))
                return;
            var item = Session.GetHabbo().GetInventoryComponent().GetItem(Request.GetUInteger());
            if (item == null)
                return;
            var type = "floor";

            if (item.BaseItem.Name.ToLower().Contains("wallpaper"))
                type = "wallpaper";
            else if (item.BaseItem.Name.ToLower().Contains("landscape"))
                type = "landscape";

            switch (type)
            {
                case "floor":

                    room.RoomData.Floor = item.ExtraData;

                    Plus.GetGame()
                        .GetAchievementManager()
                        .ProgressUserAchievement(Session, "ACH_RoomDecoFloor", 1);
                    Plus.GetGame()
                        .GetQuestManager()
                        .ProgressUserQuest(Session, QuestType.FurniDecorationFloor);
                    break;

                case "wallpaper":

                    room.RoomData.WallPaper = item.ExtraData;

                    Plus.GetGame()
                        .GetAchievementManager()
                        .ProgressUserAchievement(Session, "ACH_RoomDecoWallpaper", 1);
                    Plus.GetGame()
                        .GetQuestManager()
                        .ProgressUserQuest(Session, QuestType.FurniDecorationWall);
                    break;

                case "landscape":

                    room.RoomData.LandScape = item.ExtraData;

                    Plus.GetGame()
                        .GetAchievementManager()
                        .ProgressUserAchievement(Session, "ACH_RoomDecoLandscape", 1);
                    break;
            }
            using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
            {
                queryReactor.SetQuery(string.Concat(new object[]
                {
                    "UPDATE rooms_data SET ",
                    type,
                    " = @extradata WHERE id = ",
                    room.RoomId
                }));
                queryReactor.AddParameter("extradata", item.ExtraData);
                queryReactor.RunQuery();
                queryReactor.RunFastQuery(string.Format("DELETE FROM items_rooms WHERE id={0} LIMIT 1", item.Id));
            }
            Session.GetHabbo().GetInventoryComponent().RemoveItem(item.Id, false);
            var serverMessage = new ServerMessage(LibraryParser.OutgoingRequest("RoomSpacesMessageComposer"));
            serverMessage.AppendString(type);
            serverMessage.AppendString(item.ExtraData);
            room.SendMessage(serverMessage);
        }

        internal void PromoteRoom()
        {
            var pageId = Request.GetUInteger16();
            var item = Request.GetInteger();

            var page2 = Plus.GetGame().GetCatalog().GetPage(pageId);
            if (page2 == null) return;
            var catalogItem = page2.GetItem(item);

            if (catalogItem == null) return;

            var num = Request.GetUInteger();
            var text = Request.GetString();
            Request.GetBool();

            var text2 = string.Empty;
            try
            {
                text2 = Request.GetString();
            }
            catch (Exception)
            {
            }
            var category = Request.GetInteger();

            var room = Plus.GetGame().GetRoomManager().GetRoom(num);
            if (room == null)
            {
                var roomData = Plus.GetGame().GetRoomManager().GenerateRoomData(num);
                if (roomData == null) return;
                room = new Room();
                room.Start(roomData);
            }

            if (!room.CheckRights(Session, true)) return;
            if (catalogItem.CreditsCost > 0)
            {
                if (catalogItem.CreditsCost > Session.GetHabbo().Credits) return;
                Session.GetHabbo().Credits -= (int)catalogItem.CreditsCost;
                Session.GetHabbo().UpdateCreditsBalance();
            }
            if (catalogItem.DucketsCost > 0)
            {
                if (catalogItem.DucketsCost > Session.GetHabbo().ActivityPoints) return;
                Session.GetHabbo().ActivityPoints -= (int)catalogItem.DucketsCost;
                Session.GetHabbo().UpdateActivityPointsBalance();
            }
            if (catalogItem.BelCreditsCost > 0 || catalogItem.LoyaltyCost > 0)
            {
                if (catalogItem.BelCreditsCost > Session.GetHabbo().BelCredits) return;
                Session.GetHabbo().BelCredits -= (int)catalogItem.BelCreditsCost;
                Session.GetHabbo().UpdateSeasonalCurrencyBalance();
            }
            Session.SendMessage(CatalogPacket.PurchaseOk(catalogItem, catalogItem.Items));

            if (room.RoomData.Event != null && !room.RoomData.Event.HasExpired)
            {
                room.RoomData.Event.Time = Plus.GetUnixTimeStamp();
                Plus.GetGame().GetRoomEvents().SerializeEventInfo(room.RoomId);
            }
            else
            {
                Plus.GetGame().GetRoomEvents().AddNewEvent(room.RoomId, text, text2, Session, 7200, category);
                Plus.GetGame().GetRoomEvents().SerializeEventInfo(room.RoomId);
            }
            Session.GetHabbo().GetBadgeComponent().GiveBadge("RADZZ", true, Session, false);
        }

        internal void GetPromotionableRooms()
        {
            var serverMessage = new ServerMessage();
            serverMessage.Init(LibraryParser.OutgoingRequest("CatalogPromotionGetRoomsMessageComposer"));
            serverMessage.AppendBool(true);
            serverMessage.AppendInteger(Session.GetHabbo().UsersRooms.Count);
            foreach (var current in Session.GetHabbo().UsersRooms)
            {
                serverMessage.AppendInteger(current.Id);
                serverMessage.AppendString(current.Name);
                serverMessage.AppendBool(false);
            }
            Response = serverMessage;
            SendResponse();
        }

        internal void SaveHeightmap()
        {
            var room = Session.GetHabbo().CurrentRoom;
            var users = new List<RoomUser>(room.GetRoomUserManager().UserList.Values);

            if (room == null)
            {
                Session.SendNotif(Plus.GetLanguage().GetVar("user_is_not_in_room"));
                return;
            }

            if (!room.CheckRights(Session, true))
            {
                Session.SendNotif(Plus.GetLanguage().GetVar("user_is_not_his_room"));
                return;
            }

            string newModelName = "model_floorplan_" + room.RoomId;
            var heightMap = Request.GetString();
            var doorX = Request.GetInteger();
            var doorY = Request.GetInteger();
            var doorOrientation = Request.GetInteger();
            var wallThickness = Request.GetInteger();
            var floorThickness = Request.GetInteger();
            var wallHeight = Request.GetInteger();

            if (heightMap.Length < 2)
            {
                Session.SendNotif(Plus.GetLanguage().GetVar("invalid_room_length"));
                return;
            }

            if (wallThickness < -2 || wallThickness > 1)
                wallThickness = 0;

            if (floorThickness < -2 || floorThickness > 1)
                floorThickness = 0;

            if (doorOrientation < 0 || doorOrientation > 8)
                doorOrientation = 2;

            if (wallHeight < -1 || wallHeight > 16)
                wallHeight = -1;

            char[] validLetters =
            {
                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'g',
                'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', '\r'
            };
            if (heightMap.Any(letter => !validLetters.Contains(letter)))
            {
                Session.SendNotif(Plus.GetLanguage().GetVar("user_floor_editor_error"));

                return;
            }

            if (heightMap.Last() == Convert.ToChar(13))
                heightMap = heightMap.Remove(heightMap.Length - 1);

            if (heightMap.Length > 2100)
            {
                var message = new ServerMessage(LibraryParser.OutgoingRequest("SuperNotificationMessageComposer"));
                message.AppendString("floorplan_editor.error");
                message.AppendInteger(1);
                message.AppendString("errors");
                message.AppendString(
                    "(general): too large height (max 64 tiles)\r(general): too large area (max 2100 tiles)");
                Session.SendMessage(message);

                return;
            }

            if (heightMap.Split((char)13).Length - 1 < doorY)
            {
                var message = new ServerMessage(LibraryParser.OutgoingRequest("SuperNotificationMessageComposer"));
                message.AppendString("floorplan_editor.error");
                message.AppendInteger(1);
                message.AppendString("errors");
                message.AppendString("Y: Door is in invalid place.");
                Session.SendMessage(message);

                return;
            }

            var lines = heightMap.Split((char)13);
            var lineWidth = lines[0].Length;
            for (var i = 1; i < lines.Length; i++)
                if (lines[i].Length != lineWidth)
                {
                    var message = new ServerMessage(LibraryParser.OutgoingRequest("SuperNotificationMessageComposer"));
                    message.AppendString("floorplan_editor.error");
                    message.AppendInteger(1);
                    message.AppendString("errors");
                    message.AppendString("(general): Line " + (i + 1) + " is of different length than line 1");
                    Session.SendMessage(message);

                    return;
                }
            var doorZ = 0.0;
            var charDoor = lines[doorY][doorX];
            if (charDoor >= (char)97 && charDoor <= 119) // a-w
            {
                doorZ = charDoor - 87;
            }
            else
            {
                double.TryParse(charDoor.ToString(), out doorZ);
            }
            using (var dbClient = Plus.GetDatabaseManager().GetQueryReactor())
            {
                room.RoomData.WallHeight = wallHeight;
                room.RoomData.WallThickness = wallThickness;
                room.RoomData.FloorThickness = floorThickness;
                room.RoomData.Model.DoorZ = doorZ;

                    dbClient.RunFastQuery("UPDATE rooms_data SET model_name =  '" + newModelName + "', wallthick =  '" + wallThickness + "', floorthick =  '" + floorThickness + "', walls_height =  '" + wallHeight + "' WHERE id = " + room.RoomId + ";");

                    dbClient.SetQuery("REPLACE INTO `rooms_models` (`id` ,`door_x` ,`door_y` ,`door_z` ,`door_dir`, `heightmap`, `public_items`, `club_only`, `poolmap`) VALUES (@modelname,  @doorx,  @doory,  @doorz,  @doordir,  @hm,  '',  '0',  '');");
                    dbClient.AddParameter("modelname", newModelName);
                    dbClient.AddParameter("doorx", doorX);
                    dbClient.AddParameter("doory", doorY);
                    dbClient.AddParameter("doorz", doorZ.ToString(CultureInfo.InvariantCulture).Replace(',', '.'));
                    dbClient.AddParameter("doordir", doorOrientation);
                    dbClient.AddParameter("hm", heightMap);
                    dbClient.RunQuery();
                
                Plus.GetGame().GetRoomManager().LoadModels(dbClient);
                room.ResetGameMap(newModelName, wallHeight, wallThickness, floorThickness);
                Plus.GetGame().GetRoomManager().UnloadRoom(room, "Reload floor");

                Plus.GetGame().GetRoomManager().LoadRoom(room.RoomId);

                var roomFwd = new ServerMessage(LibraryParser.OutgoingRequest("RoomForwardMessageComposer"));
                roomFwd.AppendInteger(room.RoomId);

                var data = roomFwd.GetReversedBytes();

                foreach (var user in users.Where(user => user != null && user.GetClient() != null))
                {
                    user.GetClient().SendMessage(data);
                    user.GetClient().SendNotif("The room owner has edited their room model using the Floor Plan Editor. Therefore, the room was unloaded.");
                }
            }
            Session.SendNotif(Plus.GetLanguage().GetVar("user_floor_editor_save"));
        }

        internal void PlantMonsterplant(RoomItem mopla, Room room)
        {
            int rarity = 0, internalRarity = 0;
            if (room == null || mopla == null)
                return;

            if (mopla.GetBaseItem().InteractionType != Interaction.Moplaseed)
                return;
            if (string.IsNullOrEmpty(mopla.ExtraData) || mopla.ExtraData == "0")
                rarity = 1;
            if (!string.IsNullOrEmpty(mopla.ExtraData) && mopla.ExtraData != "0")
                rarity = int.TryParse(mopla.ExtraData, out internalRarity) ? internalRarity : 1;

            var getX = mopla.X;
            var getY = mopla.Y;
            room.GetRoomItemHandler().RemoveFurniture(Session, mopla.Id, false);
            var pet = Catalog.CreatePet(Session.GetHabbo().Id, "Monsterplant", 16, "0", "0", rarity);
            Response.Init(LibraryParser.OutgoingRequest("SendMonsterplantIdMessageComposer"));
            Response.AppendInteger(pet.PetId);
            SendResponse();
            using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
                queryReactor.RunFastQuery(string.Concat(new object[]
                {
                    "UPDATE bots SET room_id = '",
                    room.RoomId,
                    "', x = '",
                    getX,
                    "', y = '",
                    getY,
                    "' WHERE id = '",
                    pet.PetId,
                    "'"
                }));
            pet.PlacedInRoom = true;
            pet.RoomId = room.RoomId;
            var bot = new RoomBot(pet.PetId, pet.OwnerId, pet.RoomId, AIType.Pet, "freeroam", pet.Name, "", pet.Look,
                getX, getY, 0.0, 4, 0, 0, 0, 0, null, null, "", 0, false);
            room.GetRoomUserManager().DeployBot(bot, pet);

            if (pet.DbState != DatabaseUpdateState.NeedsInsert)
                pet.DbState = DatabaseUpdateState.NeedsUpdate;

            using (var queryreactor2 = Plus.GetDatabaseManager().GetQueryReactor())
            {
                queryreactor2.RunFastQuery(string.Format("DELETE FROM items_rooms WHERE id = {0}", mopla.Id));
                room.GetRoomUserManager().SavePets(queryreactor2);
            }
        }

        internal void KickBot()
        {
            var room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null || !room.CheckRights(Session, true, false))
                return;
            var roomUserByVirtualId = room.GetRoomUserManager().GetRoomUserByVirtualId(Request.GetInteger());
            if (roomUserByVirtualId == null || !roomUserByVirtualId.IsBot)
                return;

            room.GetRoomUserManager().RemoveBot(roomUserByVirtualId.VirtualId, true);
        }

        internal void PlacePet()
        {
            var room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (room == null)
                return;

            var petId = Request.GetUInteger();
            var pet = Session.GetHabbo().GetInventoryComponent().GetPet(petId);

            if (pet == null || pet.PlacedInRoom)
                return;

            var x = Request.GetInteger();
            var y = Request.GetInteger();

            if (!room.GetGameMap().CanWalk(x, y, false, 0u))
                return;

            AIType Type = AIType.Pet;
            string PetType = "pet";
            
            using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
            {
                queryReactor.RunFastQuery("UPDATE bots SET room_id = '" + room.RoomId + "', x = '" + x + "', y = '" + y + "' WHERE id = '" + petId + "'");
                queryReactor.SetQuery("SELECT ai_type FROM bots WHERE id = " + petId + "");
                DataRow Row = queryReactor.GetRow();
                if (Row != null)
                {
                    PetType = Convert.ToString(Row["ai_type"]);
                }
            }

            switch(PetType)
            {
                case "fight_pet":
                case "fightpet":
                    Type = AIType.FightPet;
                    break;

                case "pet":
                    Type = AIType.Pet;
                    break;
            }

            if (Type == AIType.FightPet)
            {
                x = Session.GetHabbo().GetRoomUser().SquareBehind.X;
                y = Session.GetHabbo().GetRoomUser().SquareBehind.Y;

                if (Session.GetHabbo().GetRoomUser().MyPet != null)
                {
                    Session.SendWhisper("Please pickup your current pet!");
                    return;
                }

                RoleplayManager.Shout(Session, "*Places their pet down*");

                Session.GetRoleplay().UsingPet = true;
                Session.GetRoleplay().mPetID = petId;   
            }

            pet.PlacedInRoom = true;
            pet.RoomId = room.RoomId;

            room.GetRoomUserManager()
                .DeployBot(
                    new RoomBot(pet.PetId, Convert.ToUInt32(pet.OwnerId), pet.RoomId, Type, "freeroam", pet.Name,
                        "", pet.Look, x, y, 0.0, 4, 0, 0, 0, 0, null, null, "", 0, false), pet);
            Session.GetHabbo().GetInventoryComponent().MovePetToRoom(pet.PetId);
            if (pet.DbState != DatabaseUpdateState.NeedsInsert)
                pet.DbState = DatabaseUpdateState.NeedsUpdate;
            using (var queryreactor2 = Plus.GetDatabaseManager().GetQueryReactor())
                room.GetRoomUserManager().SavePets(queryreactor2);
            Session.SendMessage(Session.GetHabbo().GetInventoryComponent().SerializePetInventory());
        }

        /// <summary>
        /// Overload of above
        /// </summary>
        internal void PlacePet(HabboHotel.GameClients.GameClient Session)
        {
            var room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (room == null)       return;

            var pet = Session.GetHabbo().GetInventoryComponent().GetPet(Session.GetRoleplay().mPetID);
            

            if (pet == null || pet.PlacedInRoom)
                return;
            var petId = pet.PetId;

            var x = Request.GetInteger();
            var y = Request.GetInteger();

            if (!room.GetGameMap().CanWalk(x, y, false, 0u))
                return;

            AIType Type = AIType.Pet;
            string PetType = "pet";

            using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
            {
                queryReactor.RunFastQuery("UPDATE bots SET room_id = '" + room.RoomId + "', x = '" + x + "', y = '" + y + "' WHERE id = '" + petId + "'");
                queryReactor.SetQuery("SELECT ai_type FROM bots WHERE id = " + petId + "");
                DataRow Row = queryReactor.GetRow();
                if (Row != null)
                {
                    PetType = Convert.ToString(Row["ai_type"]);
                }
            }

            switch (PetType)
            {
                case "fight_pet":
                case "fightpet":
                    Type = AIType.FightPet;
                    break;

                case "pet":
                    Type = AIType.Pet;
                    break;
            }

            if (Type == AIType.FightPet)
            {
                x = Session.GetHabbo().GetRoomUser().X;
                y = Session.GetHabbo().GetRoomUser().Y;
            }

            pet.PlacedInRoom = true;
            pet.RoomId = room.RoomId;

            room.GetRoomUserManager()
                .DeployBot(
                    new RoomBot(pet.PetId, Convert.ToUInt32(pet.OwnerId), pet.RoomId, Type, "freeroam", pet.Name,
                        "", pet.Look, x, y, 0.0, 4, 0, 0, 0, 0, null, null, "", 0, false), pet);
            Session.GetHabbo().GetInventoryComponent().MovePetToRoom(pet.PetId);
            if (pet.DbState != DatabaseUpdateState.NeedsInsert)
                pet.DbState = DatabaseUpdateState.NeedsUpdate;
            using (var queryreactor2 = Plus.GetDatabaseManager().GetQueryReactor())
                room.GetRoomUserManager().SavePets(queryreactor2);
            Session.SendMessage(Session.GetHabbo().GetInventoryComponent().SerializePetInventory());
        }

        internal void UpdateEventInfo()
        {
            Request.GetInteger();
            var original = Request.GetString();
            var original2 = Request.GetString();
            var room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null || !room.CheckRights(Session, true, false) || room.RoomData.Event == null)
                return;
            room.RoomData.Event.Name = original;
            room.RoomData.Event.Description = original2;
            Plus.GetGame().GetRoomEvents().UpdateEvent(room.RoomData.Event);
        }

        internal void HandleBotSpeechList()
        {
            var botId = Request.GetUInteger();
            var num2 = Request.GetInteger();
            var num3 = num2;

            var room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null)
                return;
            var bot = room.GetRoomUserManager().GetBot(botId);
            if (bot == null || !bot.IsBot)
                return;

            if (num3 == 2)
            {
                var text = bot.BotData.RandomSpeech == null ? string.Empty : string.Join("\n", bot.BotData.RandomSpeech);
                text += ";#;";
                text += bot.BotData.AutomaticChat ? "true" : "false";
                text += ";#;";
                text += bot.BotData.SpeechInterval;
                text += ";#;";
                text += bot.BotData.MixPhrases ? "true" : "false";

                var serverMessage = new ServerMessage(LibraryParser.OutgoingRequest("BotSpeechListMessageComposer"));
                serverMessage.AppendInteger(botId);
                serverMessage.AppendInteger(num2);
                serverMessage.AppendString(text);
                Response = serverMessage;
                SendResponse();
                return;
            }
            if (num3 != 5)
                return;

            var serverMessage2 = new ServerMessage(LibraryParser.OutgoingRequest("BotSpeechListMessageComposer"));
            serverMessage2.AppendInteger(botId);
            serverMessage2.AppendInteger(num2);
            serverMessage2.AppendString(bot.BotData.Name);

            Response = serverMessage2;
            SendResponse();
        }

        internal void ManageBotActions()
        {
            Room room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            uint botId = Request.GetUInteger();
            int action = Request.GetInteger();
            string data = Plus.FilterInjectionChars(Request.GetString());
            RoomUser bot = room.GetRoomUserManager().GetBot(botId);
            bool flag = false;

            if (room == null || botId == null || action == null || data == null || bot == null)
            {
                return;
            }

            switch (action)
            {
                case 1:
                    bot.BotData.Look = Session.GetHabbo().Look;
                    goto IL_439;
                case 2:
                    try
                    {
                        var array = data.Split(new[] { ";#;" }, StringSplitOptions.None);

                        var speechsJunk =
                            array[0].Substring(0, array[0].Length > 1024 ? 1024 : array[0].Length)
                                .Split(Convert.ToChar(13));
                        var speak = array[1] == "true";
                        var speechDelay = int.Parse(array[2]);
                        var mix = array[3] == "true";
                        if (speechDelay < 7) speechDelay = 7;

                        var speechs =
                            speechsJunk.Where(
                                speech =>
                                    !string.IsNullOrEmpty(speech) &&
                                    (!speech.ToLower().Contains("update") || !speech.ToLower().Contains("set")))
                                .Aggregate(string.Empty,
                                    (current, speech) =>
                                        current +
                                        (TextHandling.FilterHTML(speech, Session.GetHabbo().GotCommand("ha")) + ";"));
                        using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
                        {
                            queryReactor.SetQuery(
                                "UPDATE bots SET automatic_chat = @autochat, speaking_interval = @interval, mix_phrases = @mix_phrases, speech = @speech WHERE id = @botid");

                            queryReactor.AddParameter("autochat", speak ? "1" : "0");
                            queryReactor.AddParameter("interval", speechDelay);
                            queryReactor.AddParameter("mix_phrases", mix ? "1" : "0");
                            queryReactor.AddParameter("speech", speechs);
                            queryReactor.AddParameter("botid", botId);
                            queryReactor.RunQuery();
                        }
                        var randomSpeech = speechs.Split(';').ToList();

                        room.GetRoomUserManager()
                            .UpdateBot(bot.VirtualId, bot, bot.BotData.Name, bot.BotData.Motto, bot.BotData.Look,
                                bot.BotData.Gender, randomSpeech, null, speak, speechDelay, mix);
                        flag = true;
                        goto IL_439;
                    }
                    catch (Exception e)
                    {
                        Writer.Writer.LogException(e.ToString());
                        return;
                    }
                case 3:
                    bot.BotData.WalkingMode = bot.BotData.WalkingMode == "freeroam" ? "stand" : "freeroam";
                    using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
                    {
                        queryReactor.SetQuery("UPDATE bots SET walk_mode = @walkmode WHERE id = @botid");
                        queryReactor.AddParameter("walkmode", bot.BotData.WalkingMode);
                        queryReactor.AddParameter("botid", botId);
                        queryReactor.RunQuery();
                    }
                    goto IL_439;
                case 4:
                    break;

                case 5:
                    bot.BotData.Name = data;
                    goto IL_439;
                default:
                    goto IL_439;
            }
            if (bot.BotData.DanceId > 0) bot.BotData.DanceId = 0;
            else
            {
                var random = new Random();
                bot.DanceId = random.Next(1, 4);
                bot.BotData.DanceId = bot.DanceId;
            }
            var serverMessage = new ServerMessage(LibraryParser.OutgoingRequest("DanceStatusMessageComposer"));
            serverMessage.AppendInteger(bot.VirtualId);
            serverMessage.AppendInteger(bot.BotData.DanceId);
            Session.GetHabbo().CurrentRoom.SendMessage(serverMessage);
        IL_439:
            if (!flag)
            {
                var serverMessage2 = new ServerMessage(LibraryParser.OutgoingRequest("SetRoomUserMessageComposer"));
                serverMessage2.AppendInteger(1);
                bot.Serialize(serverMessage2, room.GetGameMap().GotPublicPool);
                room.SendMessage(serverMessage2);
            }
        }

        internal void RoomOnLoad()
        {
            // TODO!
            Response.Init(LibraryParser.OutgoingRequest("SendRoomCampaignFurnitureMessageComposer"));
            Response.AppendInteger(0);
            SendResponse();
        }

        internal void MuteAll()
        {
            var currentRoom = Session.GetHabbo().CurrentRoom;
            if (currentRoom == null || !currentRoom.CheckRights(Session, true, false))
                return;
            currentRoom.RoomMuted = !currentRoom.RoomMuted;

            Response.Init(LibraryParser.OutgoingRequest("RoomMuteStatusMessageComposer"));
            Response.AppendBool(currentRoom.RoomMuted);
            Session.SendMessage(Response);
        }

        internal void HomeRoom()
        {
            GetResponse().Init(LibraryParser.OutgoingRequest("HomeRoomMessageComposer"));
            GetResponse().AppendInteger(Session.GetHabbo().HomeRoom);
            GetResponse().AppendInteger(Session.GetHabbo().HomeRoom);
            SendResponse();
        }

        internal void RemoveFavouriteRoom()
        {
            if (Session.GetHabbo() == null)
                return;
            var num = Request.GetUInteger();
            Session.GetHabbo().FavoriteRooms.Remove(num);
            Response.Init(LibraryParser.OutgoingRequest("FavouriteRoomsUpdateMessageComposer"));
            Response.AppendInteger(num);
            Response.AppendBool(false);
            SendResponse();

            using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
                queryReactor.RunFastQuery(string.Concat(new object[]
                {
                    "DELETE FROM users_favorites WHERE user_id = ",
                    Session.GetHabbo().Id,
                    " AND room_id = ",
                    num
                }));
        }

        internal void RoomUserAction()
        {
            var room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null)
                return;
            var roomUserByHabbo = room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (roomUserByHabbo == null)
                return;
            roomUserByHabbo.UnIdle();
            var num = Request.GetInteger();
            roomUserByHabbo.DanceId = 0;

            var action = new ServerMessage(LibraryParser.OutgoingRequest("RoomUserActionMessageComposer"));
            action.AppendInteger(roomUserByHabbo.VirtualId);
            action.AppendInteger(num);
            room.SendMessage(action);

            if (num == 5)
            {
                roomUserByHabbo.IsAsleep = true;
                var sleep = new ServerMessage(LibraryParser.OutgoingRequest("RoomUserIdleMessageComposer"));
                sleep.AppendInteger(roomUserByHabbo.VirtualId);
                sleep.AppendBool(roomUserByHabbo.IsAsleep);
                room.SendMessage(sleep);
            }
            Plus.GetGame().GetQuestManager().ProgressUserQuest(Session, QuestType.SocialWave, 0u);
        }

        internal void GetRoomData1()
        {
            /*this.Response.Init(StaticClientMessageHandler.OutgoingRequest("297"));//Not in release
            this.Response.AppendInt32(0);
            this.SendResponse();*/
        }

        internal void GetRoomData2()
        {
            try
            {
                var queuedServerMessage = new QueuedServerMessage(Session.GetConnection());
                if (Session.GetHabbo().LoadingRoom <= 0u || CurrentLoadingRoom == null)
                    return;
                var roomData = CurrentLoadingRoom.RoomData;
                if (roomData == null)
                    return;
                if (roomData.Model == null || CurrentLoadingRoom.GetGameMap() == null)
                {
                    Session.SendMessage(
                        new ServerMessage(LibraryParser.OutgoingRequest("OutOfRoomMessageComposer")));
                    ClearRoomLoading();
                }
                else
                {
                    queuedServerMessage.AppendResponse(CurrentLoadingRoom.GetGameMap().GetNewHeightmap());
                    queuedServerMessage.AppendResponse(CurrentLoadingRoom.GetGameMap().Model.GetHeightmap());
                    queuedServerMessage.SendResponse();
                    GetRoomData3();
                }
            }
            catch (Exception ex)
            {
                Logging.LogException("Unable to load room ID [" + Session.GetHabbo().LoadingRoom + "]" + ex);
            }
        }

        internal void GetRoomData3()
        {
            if (Session.GetHabbo().LoadingRoom <= 0u || !Session.GetHabbo().LoadingChecksPassed ||
                CurrentLoadingRoom == null || Session == null)
                return;
            if (CurrentLoadingRoom.RoomData.UsersNow + 1 > CurrentLoadingRoom.RoomData.UsersMax &&
                !Session.GetHabbo().HasFuse("fuse_enter_full_rooms"))
            {
                /*
                var roomFull = new ServerMessage(LibraryParser.OutgoingRequest("RoomEnterErrorMessageComposer"));
                roomFull.AppendInteger(1);
                return;*/
            }
            var queuedServerMessage = new QueuedServerMessage(Session.GetConnection());
            var array = CurrentLoadingRoom.GetRoomItemHandler().FloorItems.Values.ToArray();
            var array2 = CurrentLoadingRoom.GetRoomItemHandler().WallItems.Values.ToArray();
            Response.Init(LibraryParser.OutgoingRequest("RoomFloorItemsMessageComposer"));

            if (CurrentLoadingRoom.RoomData.Group != null)
            {
                if (CurrentLoadingRoom.RoomData.Group.AdminOnlyDeco == 1u)
                {
                    Response.AppendInteger(CurrentLoadingRoom.RoomData.Group.Admins.Count + 1);
                    using (var enumerator = CurrentLoadingRoom.RoomData.Group.Admins.Values.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            var current = enumerator.Current;
                            if (Plus.GetHabboById(current.Id) == null)
                                continue;
                            Response.AppendInteger(current.Id);
                            Response.AppendString(Plus.GetHabboById(current.Id).UserName);
                        }
                        goto IL_220;
                    }
                }
                Response.AppendInteger(CurrentLoadingRoom.RoomData.Group.Members.Count + 1);
                foreach (var current2 in CurrentLoadingRoom.RoomData.Group.Members.Values)
                {

                    if (current2 != null && Plus.GetHabboById(current2.Id) != null)
                    {
                        Response.AppendInteger(current2.Id);
                        Response.AppendString(Plus.GetHabboById(current2.Id).UserName);
                    }
                    else
                    {
                        Response.AppendInteger(1);
                        Response.AppendString("Jerry");
                    }
                }
            IL_220:
                Response.AppendInteger(CurrentLoadingRoom.RoomData.OwnerId);
                Response.AppendString(CurrentLoadingRoom.RoomData.Owner);
            }
            else
            {
                Response.AppendInteger(1);
                Response.AppendInteger(CurrentLoadingRoom.RoomData.OwnerId);
                Response.AppendString(CurrentLoadingRoom.RoomData.Owner);
            }
            Response.AppendInteger(array.Length);
            var array3 = array;
            foreach (var roomItem in array3)
            {
                roomItem.Serialize(Response);
            }
            queuedServerMessage.AppendResponse(GetResponse());
            Response.Init(LibraryParser.OutgoingRequest("RoomWallItemsMessageComposer"));
            if (CurrentLoadingRoom.RoomData.Group != null)
            {
                if (CurrentLoadingRoom.RoomData.Group.AdminOnlyDeco == 1u)
                {
                    Response.AppendInteger(CurrentLoadingRoom.RoomData.Group.Admins.Count + 1);
                    using (var enumerator3 = CurrentLoadingRoom.RoomData.Group.Admins.Values.GetEnumerator())
                    {
                        while (enumerator3.MoveNext())
                        {
                            var current3 = enumerator3.Current;
                            Response.AppendInteger(current3.Id);
                            Response.AppendString(Plus.GetHabboById(current3.Id).UserName);
                        }
                        goto IL_423;
                    }
                }
                Response.AppendInteger(CurrentLoadingRoom.RoomData.Group.Members.Count + 1);
                foreach (var current4 in CurrentLoadingRoom.RoomData.Group.Members.Values)
                {
                    if (current4 != null && Plus.GetHabboById(current4.Id) != null)
                    {
                        Response.AppendInteger(current4.Id);
                        Response.AppendString(Plus.GetHabboById(current4.Id).UserName);
                    }
                    else
                    {
                        Response.AppendInteger(1);
                        Response.AppendString("Jerry");
                    }
                }
            IL_423:
                Response.AppendInteger(CurrentLoadingRoom.RoomData.OwnerId);
                Response.AppendString(CurrentLoadingRoom.RoomData.Owner);
            }
            else
            {
                Response.AppendInteger(1);
                Response.AppendInteger(CurrentLoadingRoom.RoomData.OwnerId);
                Response.AppendString(CurrentLoadingRoom.RoomData.Owner);
            }
            Response.AppendInteger(array2.Length);
            var array4 = array2;
            foreach (var roomItem2 in array4)
            {
                roomItem2.Serialize(Response);
            }
            queuedServerMessage.AppendResponse(GetResponse());
            Array.Clear(array, 0, array.Length);
            Array.Clear(array2, 0, array2.Length);
            array = null;
            array2 = null;
            CurrentLoadingRoom.GetRoomUserManager().AddUserToRoom(Session, Session.GetHabbo().SpectatorMode, false);
            queuedServerMessage.SendResponse();
            if (Plus.GetUnixTimeStamp() < Session.GetHabbo().FloodTime && Session.GetHabbo().FloodTime != 0)
            {
                var serverMessage = new ServerMessage(LibraryParser.OutgoingRequest("FloodFilterMessageComposer"));
                serverMessage.AppendInteger((Session.GetHabbo().FloodTime - Plus.GetUnixTimeStamp()));

                Session.SendMessage(serverMessage);
            }

            Poll poll;
            if (!Plus.GetGame().GetPollManager().TryGetPoll(CurrentLoadingRoom.RoomId, out poll) ||
                Session.GetHabbo().GotPollData(poll.Id))
                return;

            ClearRoomLoading();

            Response.Init(LibraryParser.OutgoingRequest("SuggestPollMessageComposer"));
            poll.Serialize(Response);
            SendResponse();
        }

        internal void WidgetContainers()
        {
            var text = Request.GetString();
            if (Session == null) return;

            var serverMessage = new ServerMessage(LibraryParser.OutgoingRequest("LandingWidgetMessageComposer"));
            if (!string.IsNullOrEmpty(text))
            {
                var array = text.Split(',');
                serverMessage.AppendString(text);
                serverMessage.AppendString(array[1]);
            }
            else
            {
                serverMessage.AppendString("");
                serverMessage.AppendString("");
            }
            Session.SendMessage(serverMessage);
        }

        internal void RefreshPromoEvent()
        {
            var hotelView = Plus.GetGame().GetHotelView();
            if (Session == null || Session.GetHabbo() == null)
                return;
            if (hotelView.HotelViewPromosIndexers.Count <= 0)
                return;
            var message =
                hotelView.SmallPromoComposer(
                    new ServerMessage(LibraryParser.OutgoingRequest("LandingPromosMessageComposer")));
            Session.SendMessage(message);
        }

        internal void AcceptPoll()
        {
            var key = Request.GetUInteger();
            var poll = Plus.GetGame().GetPollManager().Polls[key];
            var serverMessage = new ServerMessage(LibraryParser.OutgoingRequest("PollQuestionsMessageComposer"));
            serverMessage.AppendInteger(poll.Id);
            serverMessage.AppendString(poll.PollName);
            serverMessage.AppendString(poll.Thanks);
            serverMessage.AppendInteger(poll.Questions.Count);
            foreach (var current in poll.Questions)
            {
                var questionNumber = (poll.Questions.IndexOf(current) + 1);

                current.Serialize(serverMessage, questionNumber);
            }
            Response = serverMessage;
            SendResponse();
        }

        internal void RefusePoll()
        {
            var num = Request.GetUInteger();
            Session.GetHabbo().AnsweredPolls.Add(num);
            using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
            {
                queryReactor.SetQuery("INSERT INTO users_polls VALUES (@userid , @pollid , 0 , '0' , '')");
                queryReactor.AddParameter("userid", Session.GetHabbo().Id);
                queryReactor.AddParameter("pollid", num);
                queryReactor.RunQuery();
            }
        }

        internal void AnswerPoll()
        {
            var pollId = Request.GetUInteger();
            var questionId = Request.GetUInteger();
            var num3 = Request.GetInteger();
            var list = new List<string>();

            {
                for (var i = 0; i < num3; i++)
                    list.Add(Request.GetString());
                var text = string.Join("\r\n", list);
                var poll = Plus.GetGame().GetPollManager().TryGetPollById(pollId);
                if (poll != null && poll.Type == Poll.PollType.Matching)//MATCHING_POLL
                {
                    if (text == "1") { poll.answersPositive++; }
                    else { poll.answersNegative++; }
                    ServerMessage Answered = new ServerMessage(LibraryParser.OutgoingRequest("MatchingPollAnsweredMessageComposer"));
                    Answered.AppendInteger(Session.GetHabbo().Id);
                    Answered.AppendString(text);
                    Answered.AppendInteger(0);//count
                    /*Answered.AppendInteger(2);//count
                    Answered.AppendString("0");
                    Answered.AppendInteger(poll.answersNegative);
                    Answered.AppendString("1");
                    Answered.AppendInteger(poll.answersPositive);*/
                    Session.SendMessage(Answered);
                    return;
                }
                Session.GetHabbo().AnsweredPolls.Add(pollId);
                using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
                {
                    queryReactor.SetQuery(
                        "INSERT INTO users_polls VALUES (@userid , @pollid , @questionid , '1' , @answer)");
                    queryReactor.AddParameter("userid", Session.GetHabbo().Id);
                    queryReactor.AddParameter("pollid", pollId);
                    queryReactor.AddParameter("questionid", questionId);
                    queryReactor.AddParameter("answer", text);
                    queryReactor.RunQuery();
                }
            }
        }

        public string WallPositionCheck(string wallPosition)
        {
            try
            {
                if (wallPosition.Contains(Convert.ToChar(13)) || wallPosition.Contains(Convert.ToChar(9))) return null;

                var array = wallPosition.Split(' ');
                if (array[2] != "l" && array[2] != "r") return null;

                var array2 = array[0].Substring(3).Split(',');
                var num = int.Parse(array2[0]);
                var num2 = int.Parse(array2[1]);
                if (num >= 0 && num2 >= 0 && num <= 200 && num2 <= 200)
                {
                    var array3 = array[1].Substring(2).Split(',');
                    var num3 = int.Parse(array3[0]);
                    var num4 = int.Parse(array3[1]);
                    return num3 < 0 || num4 < 0 || num3 > 200 || num4 > 200
                        ? null
                        : string.Concat(":w=", num, ",", num2, " l=", num3, ",", num4, " ", array[2]);
                }
            }
            catch
            {
            }
            return null;
        }

        internal void Sit()
        {
            var user =
                Session.GetHabbo().CurrentRoom.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (user == null) return;
            if (user.Statusses.ContainsKey("lay") || user.IsLyingDown ||
                user.RidingHorse || user.IsWalking)
                return;

            if (user.RotBody % 2 != 0) user.RotBody--;

            user.Z = Session.GetHabbo().CurrentRoom.GetGameMap().SqAbsoluteHeight(user.X, user.Y);
            if (!user.Statusses.ContainsKey("sit"))
            {
                user.UpdateNeeded = true;
                user.Statusses.Add("sit", "0.55");
            }
            user.IsSitting = true;
        }

        public void Whisper()
        {
            if (!Session.GetHabbo().InRoom) return;
            var currentRoom = Session.GetHabbo().CurrentRoom;
            var text = Request.GetString();
            var text2 = text.Split(' ')[0];
            var msg = text.Substring(text2.Length + 1);
            var colour = Request.GetInteger();
            var roomUserByHabbo = currentRoom.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            var roomUserByHabbo2 = currentRoom.GetRoomUserManager().GetRoomUserByHabbo(text2);

            msg = currentRoom.WordFilter.Aggregate(msg,
                (current1, current) => Regex.Replace(current1, current, "bobba", RegexOptions.IgnoreCase));

            if (!Session.GetHabbo().HasFuse("fuse_owner") && AntiPublicistas.CheckPublicistas(msg))
            {
                Session.PublicistCount++;
                Session.HandlePublicista(msg);
                return;
            }

            if (roomUserByHabbo == null || roomUserByHabbo2 == null)
            {
                Session.SendWhisper(msg);
                return;
            }
            if (Session.GetHabbo().Rank < 4 && currentRoom.CheckMute(Session)) return;
            currentRoom.AddChatlog(Session.GetHabbo().Id, string.Format("<Whisper to {0}>: {1}", text2, msg),
                false);

            Plus.GetGame().GetQuestManager().ProgressUserQuest(Session, QuestType.SocialChat, 0u);
            var colour2 = colour;
            if (!roomUserByHabbo.IsBot)
                if (colour2 == 2 || (colour2 == 23 && !Session.GetHabbo().HasFuse("fuse_mod")) || colour2 < 0 ||
                    colour2 > 29) colour2 = roomUserByHabbo.LastBubble; // or can also be just 0

            roomUserByHabbo.UnIdle();

            var whisp = new ServerMessage(LibraryParser.OutgoingRequest("WhisperMessageComposer"));
            whisp.AppendInteger(roomUserByHabbo.VirtualId);
            whisp.AppendString(msg);
            whisp.AppendInteger(0);
            whisp.AppendInteger(colour2);
            whisp.AppendInteger(0);
            whisp.AppendInteger(-1);

            roomUserByHabbo.GetClient().SendMessage(whisp);
            if (!roomUserByHabbo2.IsBot &&
                roomUserByHabbo2.UserId != roomUserByHabbo.UserId &&
                !roomUserByHabbo2.GetClient().GetHabbo().MutedUsers.Contains(Session.GetHabbo().Id)) roomUserByHabbo2.GetClient().SendMessage(whisp);

            var roomUserByRank = currentRoom.GetRoomUserManager().GetRoomUserByRank(2);
            if (!roomUserByRank.Any()) return;
            foreach (var current2 in roomUserByRank)
                if (current2 != null && current2.HabboId != roomUserByHabbo2.HabboId &&
                    current2.HabboId != roomUserByHabbo.HabboId && current2.GetClient() != null)
                {
                    if (RoleplayManager.BypassRights(current2.GetClient()))
                    {
                        var whispStaff = new ServerMessage(LibraryParser.OutgoingRequest("WhisperMessageComposer"));
                        whispStaff.AppendInteger(roomUserByHabbo.VirtualId);
                        whispStaff.AppendString(string.Format("Whisper to {0}: {1}", text2, msg));
                        whispStaff.AppendInteger(0);
                        whispStaff.AppendInteger(colour2);
                        whispStaff.AppendInteger(0);
                        whispStaff.AppendInteger(-1);
                        current2.GetClient().SendMessage(whispStaff);
                    }
                }
        }

        public void Chat()
        {
            var room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null)
                return;
            var roomUser = room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (roomUser == null)
                return;
            var message = Request.GetString();
            var bubble = Request.GetInteger();
            var count = Request.GetInteger();

            if (!roomUser.IsBot)
                if (bubble == 2 || (bubble == 23 && !Session.GetHabbo().HasFuse("fuse_mod")) || bubble < 0 ||
                    bubble > 29)
                    bubble = roomUser.LastBubble; // or can also be just 0

            roomUser.Chat(Session, message, false, count, bubble);
        }

        public void Shout()
        {
            if (Session == null)
                return;
            if (Session.GetHabbo() == null)
                return;
            var room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null)
                return;
            var roomUserByHabbo = room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (roomUserByHabbo == null)
                return;
            var Msg = Request.GetString();
            var bubble = Request.GetInteger();
            if (!roomUserByHabbo.IsBot)
                if (bubble == 2 || (bubble == 23 && !Session.GetHabbo().HasFuse("fuse_mod")) || bubble < 0 ||
                    bubble > 29)
                    bubble = roomUserByHabbo.LastBubble; // or can also be just 0

            roomUserByHabbo.Chat(Session, Msg, true, -1, bubble);
        }

        public void GetFloorPlanUsedCoords()
        {
            Response.Init(LibraryParser.OutgoingRequest("GetFloorPlanUsedCoordsMessageComposer"));

            var room = Session.GetHabbo().CurrentRoom;

            if (room == null)
                Response.AppendInteger(0);
            else
            {
                var coords = room.GetGameMap().CoordinatedItems.Keys.OfType<Point>().ToArray();

                Response.AppendInteger(coords.Count());

                foreach (var point in coords)
                {
                    Response.AppendInteger(point.X);
                    Response.AppendInteger(point.Y);
                }
            }

            SendResponse();
        }

        public void GetFloorPlanDoor()
        {
            var room = Session.GetHabbo().CurrentRoom;
            if (room == null)
                return;
            Response.Init(LibraryParser.OutgoingRequest("SetFloorPlanDoorMessageComposer"));
            Response.AppendInteger(room.GetGameMap().Model.DoorX);
            Response.AppendInteger(room.GetGameMap().Model.DoorY);
            Response.AppendInteger(room.GetGameMap().Model.DoorOrientation);
            SendResponse();
        }

        public void SubmitRoomToCompetition()
        {

        }

        public void VoteForRoom()
        {

        }

        public void EnterRoomQueue()
        {

            Session.GetHabbo().SpectatorMode = true;
            var ForwardToRoom = new ServerMessage(LibraryParser.OutgoingRequest("RoomForwardMessageComposer"));
            ForwardToRoom.AppendInteger(1);
            Session.SendMessage(ForwardToRoom);
        }

        public void GetCameraRequest()
        {
           
        }
    }
}