using Plus.Configuration;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Quests.Composer;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Users;
using Plus.HabboHotel.Users.Badges;
using Plus.HabboHotel.Users.Relationships;
using Plus.Messages.Parsers;
using System;
using Plus.HabboHotel.Roleplay.Misc;
using System.Collections.Generic;
using Plus.HabboHotel.Roleplay.Components;
using System.Data;
using System.Linq;

namespace Plus.Messages.Handlers
{
    /// <summary>
    /// Class GameClientMessageHandler.
    /// </summary>
    partial class GameClientMessageHandler
    {
        /// <summary>
        /// Sends the bully report.
        /// </summary>
        public void SendBullyReport()
        {
            var reportedId = Request.GetUInteger();
            Plus.GetGame()
                .GetModerationTool()
                .SendNewTicket(Session, 104, 9, reportedId, "", new List<string>());

            Response.Init(LibraryParser.OutgoingRequest("BullyReportSentMessageComposer"));
            Response.AppendInteger(0);
            SendResponse();
        }

        /// <summary>
        /// Opens the bully reporting.
        /// </summary>
        public void OpenBullyReporting()
        {
            Response.Init(LibraryParser.OutgoingRequest("OpenBullyReportMessageComposer"));
            Response.AppendInteger(0);
            SendResponse();
        }

        /// <summary>
        /// Opens the quests.
        /// </summary>
        public void OpenQuests()
        {
            Plus.GetGame().GetQuestManager().GetList(Session, Request);
        }

        /// <summary>
        /// Retrieves the citizenship.
        /// </summary>
        internal void RetrieveCitizenship()
        {
            GetResponse().Init(LibraryParser.OutgoingRequest("CitizenshipStatusMessageComposer"));
            GetResponse().AppendString(Request.GetString());
            GetResponse().AppendInteger(4);
            GetResponse().AppendInteger(4);
        }

        /// <summary>
        /// Loads the club gifts.
        /// </summary>
        internal void LoadClubGifts()
        {
            if (Session == null || Session.GetHabbo() == null)
                return;
            //var i = 0;
            //var i2 = 0;
            Session.GetHabbo().GetSubscriptionManager().GetSubscription();
            var serverMessage = new ServerMessage();
            serverMessage.Init(LibraryParser.OutgoingRequest("LoadCatalogClubGiftsMessageComposer"));
            serverMessage.AppendInteger(0); // i
            serverMessage.AppendInteger(0); // i2
            serverMessage.AppendInteger(1);
        }

        /// <summary>
        /// Chooses the club gift.
        /// </summary>
        internal void ChooseClubGift()
        {
            if (Session == null || Session.GetHabbo() == null)
                return;
            Request.GetString();
        }

        /// <summary>
        /// Gets the user tags.
        /// </summary>
        internal void GetUserTags()
        {
            var room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null)
                return;
            var roomUserByHabbo = room.GetRoomUserManager().GetRoomUserByHabbo(Request.GetUInteger());
            if (roomUserByHabbo == null || roomUserByHabbo.IsBot)
                return;
            Response.Init(LibraryParser.OutgoingRequest("UserTagsMessageComposer"));
            Response.AppendInteger(roomUserByHabbo.GetClient().GetHabbo().Id);
            Response.AppendInteger(roomUserByHabbo.GetClient().GetHabbo().Tags.Count);
            foreach (string current in roomUserByHabbo.GetClient().GetHabbo().Tags)
                Response.AppendString(current);
            SendResponse();

            if (Session != roomUserByHabbo.GetClient())
                return;
            if (Session.GetHabbo().Tags.Count >= 5)
                Plus.GetGame()
                    .GetAchievementManager()
                    .ProgressUserAchievement(roomUserByHabbo.GetClient(), "ACH_UserTags", 5, false);
        }

        /// <summary>
        /// Gets the user badges.
        /// </summary>
        internal void GetUserBadges()
        {
            var room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null)
                return;
            var roomUserByHabbo = room.GetRoomUserManager().GetRoomUserByHabbo(Request.GetUInteger());
            if (roomUserByHabbo == null || roomUserByHabbo.IsBot)
                return;
            if (roomUserByHabbo.GetClient() == null)
                return;

            Session.GetHabbo().LastSelectedUser = roomUserByHabbo.UserId;
            Response.Init(LibraryParser.OutgoingRequest("UserBadgesMessageComposer"));
            Response.AppendInteger(roomUserByHabbo.GetClient().GetHabbo().Id);

            Response.StartArray();
            foreach (
                var badge in
                    roomUserByHabbo.GetClient()
                        .GetHabbo()
                        .GetBadgeComponent()
                        .BadgeList.Values.Cast<Badge>()
                        .Where(badge => badge.Slot > 0).Take(5))
            {
                Response.AppendInteger(badge.Slot);
                Response.AppendString(badge.Code);

                Response.SaveArray();
            }

            Response.EndArray();
            SendResponse();
        }

        /// <summary>
        /// Gives the respect.
        /// </summary>
        internal void GiveRespect()
        {
            var room = Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null || Session.GetHabbo().DailyRespectPoints <= 0)
                return;
            var roomUserByHabbo = room.GetRoomUserManager().GetRoomUserByHabbo(Request.GetUInteger());
            if (roomUserByHabbo == null || roomUserByHabbo.GetClient().GetHabbo().Id == Session.GetHabbo().Id ||
                roomUserByHabbo.IsBot)
                return;
            Plus.GetGame().GetQuestManager().ProgressUserQuest(Session, QuestType.SocialRespect, 0u);
            Plus.GetGame().GetAchievementManager().ProgressUserAchievement(Session, "ACH_RespectGiven", 1, false);
            Plus.GetGame()
                .GetAchievementManager()
                .ProgressUserAchievement(roomUserByHabbo.GetClient(), "ACH_RespectEarned", 1, false);

            {
                Session.GetHabbo().DailyRespectPoints--;
                roomUserByHabbo.GetClient().GetHabbo().Respect++;
                using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
                    queryReactor.RunFastQuery("UPDATE users_stats SET respect = respect + 1 WHERE id = " + roomUserByHabbo.GetClient().GetHabbo().Id + " LIMIT 1;UPDATE users_stats SET daily_respect_points = daily_respect_points - 1 WHERE id= " + Session.GetHabbo().Id + " LIMIT 1");
                var serverMessage = new ServerMessage(LibraryParser.OutgoingRequest("GiveRespectsMessageComposer"));
                serverMessage.AppendInteger(roomUserByHabbo.GetClient().GetHabbo().Id);
                serverMessage.AppendInteger(roomUserByHabbo.GetClient().GetHabbo().Respect);
                room.SendMessage(serverMessage);

                var thumbsUp = new ServerMessage();
                thumbsUp.Init(LibraryParser.OutgoingRequest("RoomUserActionMessageComposer"));
                thumbsUp.AppendInteger(
                    room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().UserName).VirtualId);
                thumbsUp.AppendInteger(7);
                room.SendMessage(thumbsUp);
            }
        }

        /// <summary>
        /// Applies the effect.
        /// </summary>
        internal void ApplyEffect()
        {
            var effectId = Request.GetInteger();
            var roomUserByHabbo =
                Plus.GetGame()
                    .GetRoomManager()
                    .GetRoom(Session.GetHabbo().CurrentRoomId)
                    .GetRoomUserManager()
                    .GetRoomUserByHabbo(Session.GetHabbo().UserName);
            if (!roomUserByHabbo.RidingHorse)
                Session.GetHabbo().GetAvatarEffectsInventoryComponent().ActivateCustomEffect(effectId);
        }

        /// <summary>
        /// Enables the effect.
        /// </summary>
        internal void EnableEffect()
        {
            var currentRoom = Session.GetHabbo().CurrentRoom;
            if (currentRoom == null)
                return;
            var roomUserByHabbo = currentRoom.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (roomUserByHabbo == null)
                return;
            var num = Request.GetInteger();
            if (roomUserByHabbo.RidingHorse)
                return;
            if (num == 0)
            {
                Session.GetHabbo()
                    .GetAvatarEffectsInventoryComponent()
                    .StopEffect(Session.GetHabbo().GetAvatarEffectsInventoryComponent().CurrentEffect);
                return;
            }
            Session.GetHabbo().GetAvatarEffectsInventoryComponent().ActivateEffect(num);
        }

        /// <summary>
        /// Mutes the user.
        /// </summary>
        internal void MuteUser()
        {
            var num = Request.GetUInteger();
            Request.GetUInteger();
            var num2 = Request.GetUInteger();
            var currentRoom = Session.GetHabbo().CurrentRoom;
            if (currentRoom == null || (currentRoom.RoomData.WhoCanBan == 0 && !currentRoom.CheckRights(Session, true, false)) ||
                (currentRoom.RoomData.WhoCanBan == 1 && !currentRoom.CheckRights(Session)) || Session.GetHabbo().Rank < Convert.ToUInt32(Plus.GetDbConfig().DbData["ambassador.minrank"]))
                return;
            var roomUserByHabbo = currentRoom.GetRoomUserManager()
                .GetRoomUserByHabbo(Plus.GetHabboById(num).UserName);
            if (roomUserByHabbo == null)
                return;
            if (roomUserByHabbo.GetClient().GetHabbo().Rank >= Session.GetHabbo().Rank)
                return;
            if (currentRoom.MutedUsers.ContainsKey(num))
            {
                if (currentRoom.MutedUsers[num] >= (ulong)Plus.GetUnixTimeStamp())
                    return;
                currentRoom.MutedUsers.Remove(num);
            }
            currentRoom.MutedUsers.Add(num,
                uint.Parse(
                    ((Plus.GetUnixTimeStamp()) + unchecked(checked(num2 * 60u))).ToString()));

            roomUserByHabbo.GetClient()
                .SendNotif(string.Format(Plus.GetLanguage().GetVar("room_owner_has_mute_user"), num2));
        }

        /// <summary>
        /// Gets the user information.
        /// </summary>
        internal void GetUserInfo()
        {
            GetResponse().Init(LibraryParser.OutgoingRequest("UpdateUserDataMessageComposer"));
            GetResponse().AppendInteger(-1);
            GetResponse().AppendString(Session.GetHabbo().Look);
            GetResponse().AppendString(Session.GetHabbo().Gender.ToLower());
            GetResponse().AppendString(Session.GetHabbo().Motto);
            GetResponse().AppendInteger(Session.GetHabbo().AchievementPoints);
            SendResponse();
            GetResponse().Init(LibraryParser.OutgoingRequest("AchievementPointsMessageComposer"));
            GetResponse().AppendInteger(Session.GetHabbo().AchievementPoints);
            SendResponse();

            using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
            {
                queryReactor.SetQuery(string.Format("SELECT ip_last FROM users WHERE id={0} LIMIT 1",
                    Session.GetHabbo().Id));
                var @string = queryReactor.GetString();
                queryReactor.RunFastQuery(string.Concat(new object[]
                {
                    "REPLACE INTO users_access (user_id, ip, machineid, last_login) VALUES (",
                    Session.GetHabbo().Id,
                    ", '",
                    @string,
                    "', '",
                    Session.MachineId,
                    "', UNIX_TIMESTAMP())"
                }));
            }
        }

        /// <summary>
        /// Gets the balance.
        /// </summary>
        internal void GetBalance()
        {
            if (Session == null || Session.GetHabbo() == null) return;

            Session.GetHabbo().UpdateCreditsBalance();
            Session.GetHabbo().UpdateSeasonalCurrencyBalance();
        }

        /// <summary>
        /// Gets the subscription data.
        /// </summary>
        internal void GetSubscriptionData()
        {
            Session.GetHabbo().SerializeClub();
        }

        /// <summary>
        /// Loads the settings.
        /// </summary>
        internal void LoadSettings()
        {
            var preferences = Session.GetHabbo().Preferences;
            var serverMessage = new ServerMessage(LibraryParser.OutgoingRequest("LoadVolumeMessageComposer"));

            serverMessage.AppendIntegersArray(preferences.Volume, ',', 3, 0, 100);

            serverMessage.AppendBool(preferences.PreferOldChat);
            serverMessage.AppendBool(preferences.IgnoreRoomInvite);
            serverMessage.AppendBool(preferences.DisableCameraFollow);
            serverMessage.AppendInteger(3); // collapse friends (3 = no)
            serverMessage.AppendInteger(0); //bubble
            this.Session.SendMessage(serverMessage);
        }

        /// <summary>
        /// Saves the settings.
        /// </summary>
        internal void SaveSettings()
        {
            var num = Request.GetInteger();
            var num2 = Request.GetInteger();
            var num3 = Request.GetInteger();
            Session.GetHabbo().Preferences.Volume = num + "," + num2 + "," + num3;
            Session.GetHabbo().Preferences.Save();
        }

        /// <summary>
        /// Sets the chat preferrence.
        /// </summary>
        internal void SetChatPreferrence()
        {
            bool enable = Request.GetBool();
            Session.GetHabbo().Preferences.PreferOldChat = enable;
            Session.GetHabbo().Preferences.Save();
        }

        internal void SetInvitationsPreference()
        {
            bool enable = Request.GetBool();
            Session.GetHabbo().Preferences.IgnoreRoomInvite = enable;
            Session.GetHabbo().Preferences.Save();
        }

        internal void SetRoomCameraPreferences()
        {
            bool enable = Request.GetBool();
            Session.GetHabbo().Preferences.DisableCameraFollow = enable;
            Session.GetHabbo().Preferences.Save();
        }

        /// <summary>
        /// Gets the badges.
        /// </summary>
        internal void GetBadges()
        {
            Session.SendMessage(Session.GetHabbo().GetBadgeComponent().Serialize());
        }

        /// <summary>
        /// Updates the badges.
        /// </summary>
        internal void UpdateBadges()
        {
            Session.GetHabbo().GetBadgeComponent().ResetSlots();
            using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
                queryReactor.RunFastQuery(string.Format("UPDATE users_badges SET badge_slot = 0 WHERE user_id = {0}",
                    Session.GetHabbo().Id));
            for (var i = 0; i < 5; i++)
            {
                var slot = Request.GetInteger();
                var code = Request.GetString();
                if (code.Length == 0) continue;
                if (!Session.GetHabbo().GetBadgeComponent().HasBadge(code) || slot < 1 || slot > 5) return;
                Session.GetHabbo().GetBadgeComponent().GetBadge(code).Slot = slot;
                using (var queryreactor2 = Plus.GetDatabaseManager().GetQueryReactor())
                {
                    queryreactor2.SetQuery("UPDATE users_badges SET badge_slot = " + slot +
                                           " WHERE badge_id = @badge AND user_id = " + Session.GetHabbo().Id);
                    queryreactor2.AddParameter("badge", code);
                    queryreactor2.RunQuery();
                }
            }
            Plus.GetGame().GetQuestManager().ProgressUserQuest(Session, QuestType.ProfileBadge, 0u);
            var serverMessage = new ServerMessage(LibraryParser.OutgoingRequest("UserBadgesMessageComposer"));
            serverMessage.AppendInteger(Session.GetHabbo().Id);

            serverMessage.StartArray();
            foreach (
                var badge in
                    Session.GetHabbo()
                        .GetBadgeComponent()
                        .BadgeList.Values.Cast<Badge>()
                        .Where(badge => badge.Slot > 0))
            {
                serverMessage.AppendInteger(badge.Slot);
                serverMessage.AppendString(badge.Code);

                serverMessage.SaveArray();
            }

            serverMessage.EndArray();
            if (Session.GetHabbo().InRoom &&
                Plus.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId) != null)
            {
                Plus.GetGame()
                    .GetRoomManager()
                    .GetRoom(Session.GetHabbo().CurrentRoomId)
                    .SendMessage(serverMessage);
                return;
            }
            Session.SendMessage(serverMessage);
        }

        /// <summary>
        /// Gets the achievements.
        /// </summary>
        internal void GetAchievements()
        {
            Plus.GetGame().GetAchievementManager().GetList(Session, Request);
        }

        /// <summary>
        /// Prepares the campaing.
        /// </summary>
        internal void PrepareCampaing()
        {
            var text = Request.GetString();
            Response.Init(LibraryParser.OutgoingRequest("SendCampaignBadgeMessageComposer"));
            Response.AppendString(text);
            Response.AppendBool(Session.GetHabbo().GetBadgeComponent().HasBadge(text));
            SendResponse();
        }

        /// <summary>
        /// Loads the profile.
        /// </summary>
        internal void LoadProfile()
        {
            var userId = Request.GetUInteger();
            Request.GetBool();

            var habbo = Plus.GetHabboById(userId);
            if (habbo == null)
            {
                this.Session.SendNotif(Plus.GetLanguage().GetVar("user_not_found"));
                return;
            }
            var createTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(habbo.CreateDate);

            Response.Init(LibraryParser.OutgoingRequest("UserProfileMessageComposer"));
            Response.AppendInteger(habbo.Id);
            Response.AppendString(habbo.UserName);
            Response.AppendString(habbo.Look);
            Response.AppendString(habbo.Motto);
            Response.AppendString(createTime.ToString("dd/MM/yyyy"));
            Response.AppendInteger(habbo.AchievementPoints);
            Response.AppendInteger(GetFriendsCount(userId));
            Response.AppendBool(habbo.Id != Session.GetHabbo().Id &&
                                Session.GetHabbo().GetMessenger().FriendshipExists(habbo.Id));
            Response.AppendBool(habbo.Id != Session.GetHabbo().Id &&
                                !Session.GetHabbo().GetMessenger().FriendshipExists(habbo.Id) &&
                                Session.GetHabbo().GetMessenger().RequestExists(habbo.Id));
            Response.AppendBool(Plus.GetGame().GetClientManager().GetClientByUserId(habbo.Id) != null);
            var groups = Plus.GetGame().GetGroupManager().GetUserGroups(habbo.Id);
            Response.AppendInteger(groups.Count);
            foreach (var @group in groups.Select(groupUs => Plus.GetGame().GetGroupManager().GetGroup(groupUs.GroupId))
                )
                if (@group != null)
                {
                    Response.AppendInteger(@group.Id);
                    Response.AppendString(@group.Name);
                    Response.AppendString(@group.Badge);
                    Response.AppendString(Plus.GetGame().GetGroupManager().GetGroupColour(@group.Colour1, true));
                    Response.AppendString(Plus.GetGame().GetGroupManager().GetGroupColour(@group.Colour2, false));
                    Response.AppendBool(@group.Id == habbo.FavouriteGroup);
                    Response.AppendInteger(-1);
                    Response.AppendBool(@group.HasForum);
                }
                else
                {
                    Response.AppendInteger(1);
                    Response.AppendString("THIS GROUP IS INVALID");
                    Response.AppendString("");
                    Response.AppendString("");
                    Response.AppendString("");
                    Response.AppendBool(false);
                    Response.AppendInteger(-1);
                    Response.AppendBool(false);
                }

            if (Plus.GetGame().GetClientManager().GetClientByUserId(habbo.Id) == null)
                Response.AppendInteger((Plus.GetUnixTimeStamp() - habbo.PreviousOnline));
            else
                Response.AppendInteger((Plus.GetUnixTimeStamp() - habbo.LastOnline));

            Response.AppendBool(true);
            SendResponse();
            Response.Init(LibraryParser.OutgoingRequest("UserBadgesMessageComposer"));
            Response.AppendInteger(habbo.Id);
            Response.StartArray();

            foreach (
                var badge in habbo.GetBadgeComponent().BadgeList.Values.Cast<Badge>().Where(badge => badge.Slot > 0))
            {
                Response.AppendInteger(badge.Slot);
                Response.AppendString(badge.Code);

                Response.SaveArray();
            }

            Response.EndArray();
            SendResponse();
        }

        /// <summary>
        /// Changes the look.
        /// </summary>
        internal void ChangeLook()
        {
            if (Session.GetRoleplay().Working)
            {
                Session.SendWhisper("You cannot change your looks while working!");
                return;
            }
            if (Session.GetHabbo().CurrentRoom.RoomData.Description.Contains("CLOTHING_STORE"))
            {
                if (Session.GetHabbo().Credits < 2)
                {
                    Session.SendWhisper("You do not have $2 to change your looks!");
                    return;
                }

                var text = Request.GetString().ToUpper();
                var text2 = Request.GetString();

                text2 = Plus.FilterFigure(text2);

                Session.GetRoleplay().Changing = true;

                string[] separators = { "." };
                string value = Session.GetHabbo().Look;
                string[] clothesParts = value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                string oldHair = "";
                foreach (var str in clothesParts)
                {
                    if (str.StartsWith("hr-"))
                    {
                        oldHair = str;
                    }
                }

                string newHair = "";
                string value2 = text2;
                string[] newclothesparts = value2.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                foreach (var str2 in newclothesparts)
                {
                    if (str2.StartsWith("hr-"))
                    {
                        newHair = str2;
                    }
                }
                text2 = text2.Replace(newHair, oldHair);
                ProcessClothing newProcess = new ProcessClothing(Session, text2, text);
            }
            else if (Session.GetHabbo().CurrentRoom.RoomData.Description.Contains("HAIR_STORE"))
            {
                if (Session.GetHabbo().Credits < 2)
                {
                    Session.SendWhisper("You do not have $2 to change your hair!");
                    return;
                }

                var text = Request.GetString().ToUpper();
                var text2 = Request.GetString();

                text2 = Plus.FilterFigure(text2);

                Session.GetRoleplay().Changing = true;

                string[] separators = { "." };
                string value = Session.GetHabbo().Look;
                string[] clothesParts = value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                string oldHair = "";
                foreach (var str in clothesParts)
                {
                    if (str.StartsWith("hr-"))
                    {
                        oldHair = str;
                    }
                }

                string newHair = "";
                string value2 = text2;
                string[] newclothesparts = value2.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                foreach (var str2 in newclothesparts)
                {
                    if (str2.StartsWith("hr-"))
                    {
                        newHair = str2;
                    }
                }
                text2 = value.Replace(oldHair, newHair);
                ProcessClothing newProcess = new ProcessClothing(Session, text2, text, true);
            }
            else
            {
                Session.SendWhisper("You can not change your clothes [Room ID: 3] or your hair [Room ID: 11] in here");
            }
        }

        /// <summary>
        /// Changes the motto.
        /// </summary>
        internal void ChangeMotto()
        {
            if (Session.GetHabbo().Rank < 4)
            {
                Session.SendWhisper("You cannot change your motto!");
                return;
            }

            var text = Request.GetString();
            if (text == Session.GetHabbo().Motto)
                return;
            Session.GetHabbo().Motto = text;
            using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
            {
                queryReactor.SetQuery(string.Format("UPDATE users SET motto = @motto WHERE id = '{0}'",
                    Session.GetHabbo().Id));
                queryReactor.AddParameter("motto", text);
                queryReactor.RunQuery();
            }
            Plus.GetGame().GetQuestManager().ProgressUserQuest(Session, QuestType.ProfileChangeMotto, 0u);
            if (Session.GetHabbo().InRoom)
            {
                var currentRoom = Session.GetHabbo().CurrentRoom;
                if (currentRoom == null)
                    return;
                var roomUserByHabbo = currentRoom.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
                if (roomUserByHabbo == null)
                    return;
                var serverMessage = new ServerMessage(LibraryParser.OutgoingRequest("UpdateUserDataMessageComposer"));
                serverMessage.AppendInteger(roomUserByHabbo.VirtualId); //BUGG
                //serverMessage.AppendInt32(-1);
                serverMessage.AppendString(Session.GetHabbo().Look);
                serverMessage.AppendString(Session.GetHabbo().Gender.ToLower());
                serverMessage.AppendString(Session.GetHabbo().Motto);
                serverMessage.AppendInteger(Session.GetHabbo().AchievementPoints);
                currentRoom.SendMessage(serverMessage);
            }
            Plus.GetGame().GetAchievementManager().ProgressUserAchievement(Session, "ACH_Motto", 1, false);
        }

        /// <summary>
        /// Gets the wardrobe.
        /// </summary>
        internal void GetWardrobe()
        {
            GetResponse().Init(LibraryParser.OutgoingRequest("LoadWardrobeMessageComposer"));
            GetResponse().AppendInteger(0);
            using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
            {
                queryReactor.SetQuery(
                    string.Format("SELECT slot_id, look, gender FROM users_wardrobe WHERE user_id = {0}",
                        Session.GetHabbo().Id));
                var table = queryReactor.GetTable();
                if (table == null)
                    GetResponse().AppendInteger(0);
                else
                {
                    GetResponse().AppendInteger(table.Rows.Count);
                    foreach (DataRow dataRow in table.Rows)
                    {
                        GetResponse().AppendInteger(Convert.ToUInt32(dataRow["slot_id"]));
                        GetResponse().AppendString((string)dataRow["look"]);
                        GetResponse().AppendString(dataRow["gender"].ToString().ToUpper());
                    }
                }
                SendResponse();
            }
        }

        /// <summary>
        /// Saves the wardrobe.
        /// </summary>
        internal void SaveWardrobe()
        {
            if (Session.GetRoleplay().Working)
            {
                Session.SendModeratorMessage("You cannot steal your work clothes you poor cunt lmao");
            }
            else
            {
                var num = Request.GetUInteger();
                var text = Request.GetString();
                var text2 = Request.GetString();

                text = Plus.FilterFigure(text);

                using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
                {
                    queryReactor.SetQuery(string.Concat(new object[]
                {
                    "SELECT null FROM users_wardrobe WHERE user_id = ",
                    Session.GetHabbo().Id,
                    " AND slot_id = ",
                    num
                }));
                    queryReactor.AddParameter("look", text);
                    queryReactor.AddParameter("gender", text2.ToUpper());
                    if (queryReactor.GetRow() != null)
                    {
                        queryReactor.SetQuery(string.Concat(new object[]
                    {
                        "UPDATE users_wardrobe SET look = @look, gender = @gender WHERE user_id = ",
                        Session.GetHabbo().Id,
                        " AND slot_id = ",
                        num,
                        ";"
                    }));
                        queryReactor.AddParameter("look", text);
                        queryReactor.AddParameter("gender", text2.ToUpper());
                        queryReactor.RunQuery();
                    }
                    else
                    {
                        queryReactor.SetQuery(string.Concat(new object[]
                    {
                        "INSERT INTO users_wardrobe (user_id,slot_id,look,gender) VALUES (",
                        Session.GetHabbo().Id,
                        ",",
                        num,
                        ",@look,@gender)"
                    }));
                        queryReactor.AddParameter("look", text);
                        queryReactor.AddParameter("gender", text2.ToUpper());
                        queryReactor.RunQuery();
                    }
                }
                Plus.GetGame()
                    .GetQuestManager()
                    .ProgressUserQuest(Session, QuestType.ProfileChangeLook);
            }
        }

        /// <summary>
        /// Gets the pets inventory.
        /// </summary>
        internal void GetPetsInventory()
        {
            if (Session.GetHabbo().GetInventoryComponent() == null)
                return;
            Session.SendMessage(Session.GetHabbo().GetInventoryComponent().SerializePetInventory());
        }

        /// <summary>
        /// Gets the bots inventory.
        /// </summary>
        internal void GetBotsInventory()
        {
            Session.SendMessage(Session.GetHabbo().GetInventoryComponent().SerializeBotInventory());
            SendResponse();
        }

        /// <summary>
        /// Checks the name.
        /// </summary>
        internal void CheckName()
        {
            var text = Request.GetString();
            if (text.ToLower() == Session.GetHabbo().UserName.ToLower())
            {
                Response.Init(LibraryParser.OutgoingRequest("NameChangedUpdatesMessageComposer"));
                Response.AppendInteger(0);
                Response.AppendString(text);
                Response.AppendInteger(0);
                SendResponse();
                return;
            }
            using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
            {
                queryReactor.SetQuery("SELECT username FROM users WHERE Username=@name LIMIT 1");
                queryReactor.AddParameter("name", text);
                var @string = queryReactor.GetString();
                var array = text.ToLower().ToCharArray();
                const string source = "abcdefghijklmnopqrstuvwxyz1234567890.,_-;:?!@áéíóúÁÉÍÓÚñÑÜüÝý ";
                var array2 = array;
                if (array2.Any(c => !source.Contains(char.ToLower(c))))
                {
                    Response.Init(LibraryParser.OutgoingRequest("NameChangedUpdatesMessageComposer"));
                    Response.AppendInteger(4);
                    Response.AppendString(text);
                    Response.AppendInteger(0);
                    SendResponse();
                    return;
                }
                if (text.ToLower().Contains("mod") || text.ToLower().Contains("m0d") || text.Contains(" ") ||
                    text.ToLower().Contains("admin"))
                {
                    Response.Init(LibraryParser.OutgoingRequest("NameChangedUpdatesMessageComposer"));
                    Response.AppendInteger(4);
                    Response.AppendString(text);
                    Response.AppendInteger(0);
                    SendResponse();
                }
                else if (text.Length > 15)
                {
                    Response.Init(LibraryParser.OutgoingRequest("NameChangedUpdatesMessageComposer"));
                    Response.AppendInteger(3);
                    Response.AppendString(text);
                    Response.AppendInteger(0);
                    SendResponse();
                }
                else if (text.Length < 3)
                {
                    Response.Init(LibraryParser.OutgoingRequest("NameChangedUpdatesMessageComposer"));
                    Response.AppendInteger(2);
                    Response.AppendString(text);
                    Response.AppendInteger(0);
                    SendResponse();
                }
                else if (string.IsNullOrWhiteSpace(@string))
                {
                    Response.Init(LibraryParser.OutgoingRequest("NameChangedUpdatesMessageComposer"));
                    Response.AppendInteger(0);
                    Response.AppendString(text);
                    Response.AppendInteger(0);
                    SendResponse();
                }
                else
                {
                    queryReactor.SetQuery("SELECT tag FROM users_tags ORDER BY RAND() LIMIT 3");
                    var table = queryReactor.GetTable();
                    Response.Init(LibraryParser.OutgoingRequest("NameChangedUpdatesMessageComposer"));
                    Response.AppendInteger(5);
                    Response.AppendString(text);
                    Response.AppendInteger(table.Rows.Count);
                    foreach (DataRow dataRow in table.Rows)
                        Response.AppendString(string.Format("{0}{1}", text, dataRow[0]));
                    SendResponse();
                }
            }
        }

        /// <summary>
        /// Changes the name.
        /// </summary>
        internal void ChangeName()
        {
            var text = Request.GetString();
            var userName = Session.GetHabbo().UserName;

            {
                using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
                {
                    queryReactor.SetQuery("SELECT username FROM users WHERE Username=@name LIMIT 1");
                    queryReactor.AddParameter("name", text);
                    var @String = queryReactor.GetString();

                    if (!string.IsNullOrWhiteSpace(String) &&
                        !String.Equals(userName, text, StringComparison.CurrentCultureIgnoreCase))
                        return;
                    queryReactor.SetQuery("UPDATE rooms_data SET owner = @newowner WHERE owner = @oldowner");
                    queryReactor.AddParameter("newowner", text);
                    queryReactor.AddParameter("oldowner", Session.GetHabbo().UserName);
                    queryReactor.RunQuery();

                    queryReactor.SetQuery(
                        "UPDATE users SET Username = @newname, last_name_change = @timestamp WHERE id = @userid");
                    queryReactor.AddParameter("newname", text);
                    queryReactor.AddParameter("timestamp", Plus.GetUnixTimeStamp() + 43200);
                    queryReactor.AddParameter("userid", Session.GetHabbo().UserName);
                    queryReactor.RunQuery();

                    Session.GetHabbo().LastChange = Plus.GetUnixTimeStamp() + 43200;
                    Session.GetHabbo().UserName = text;
                    Response.Init(LibraryParser.OutgoingRequest("UpdateUsernameMessageComposer"));
                    Response.AppendInteger(0);
                    Response.AppendString(text);
                    Response.AppendInteger(0);
                    SendResponse();
                    Response.Init(LibraryParser.OutgoingRequest("UpdateUserDataMessageComposer"));
                    Response.AppendInteger(-1);
                    Response.AppendString(Session.GetHabbo().Look);
                    Response.AppendString(Session.GetHabbo().Gender.ToLower());
                    Response.AppendString(Session.GetHabbo().Motto);
                    Response.AppendInteger(Session.GetHabbo().AchievementPoints);
                    SendResponse();
                    Session.GetHabbo().CurrentRoom.GetRoomUserManager().UpdateUser(userName, text);
                    if (Session.GetHabbo().CurrentRoom != null)
                    {
                        Response.Init(LibraryParser.OutgoingRequest("UserUpdateNameInRoomMessageComposer"));
                        Response.AppendInteger(Session.GetHabbo().Id);
                        Response.AppendInteger(Session.GetHabbo().CurrentRoom.RoomId);
                        Response.AppendString(text);
                    }
                    foreach (var current in Session.GetHabbo().UsersRooms)
                    {
                        current.Owner = text;
                        current.SerializeRoomData(Response, Session, false, true);
                        var room = Plus.GetGame().GetRoomManager().GetRoom(current.Id);
                        if (room != null)
                            room.RoomData.Owner = text;
                    }
                    foreach (var current2 in Session.GetHabbo().GetMessenger().Friends.Values)
                        if (current2.Client != null)
                            foreach (
                                var current3 in
                                    current2.Client.GetHabbo()
                                        .GetMessenger()
                                        .Friends.Values.Where(current3 => current3.UserName == userName))
                            {
                                current3.UserName = text;
                                current3.Serialize(Response, Session);
                            }
                }
            }
        }

        /// <summary>
        /// Gets the relationships.
        /// </summary>
        internal void GetRelationships()
        {
            var userId = Request.GetUInteger();
            var habboForId = Plus.GetHabboById(userId);
            if (habboForId == null)
                return;
            var rand = new Random();
            habboForId.Relationships = (
                from x in habboForId.Relationships
                orderby rand.Next()
                select x).ToDictionary(item => item.Key,
                    item => item.Value);
            var num = habboForId.Relationships.Count(x => x.Value.Type == 1);
            var num2 = habboForId.Relationships.Count(x => x.Value.Type == 2);
            var num3 = habboForId.Relationships.Count(x => x.Value.Type == 3);
            Response.Init(LibraryParser.OutgoingRequest("RelationshipMessageComposer"));
            Response.AppendInteger(habboForId.Id);
            Response.AppendInteger(habboForId.Relationships.Count);
            foreach (var current in habboForId.Relationships.Values)
            {
                var habboForId2 = Plus.GetHabboById(Convert.ToUInt32(current.UserId));
                if (habboForId2 == null)
                {
                    Response.AppendInteger(0);
                    Response.AppendInteger(0);
                    Response.AppendInteger(0);
                    Response.AppendString("Placeholder");
                    Response.AppendString("hr-115-42.hd-190-1.ch-215-62.lg-285-91.sh-290-62");
                }
                else
                {
                    Response.AppendInteger(current.Type);
                    Response.AppendInteger((current.Type == 1) ? num : ((current.Type == 2) ? num2 : num3));
                    Response.AppendInteger(current.UserId);
                    Response.AppendString(habboForId2.UserName);
                    Response.AppendString(habboForId2.Look);
                }
            }
            SendResponse();
        }

        /// <summary>
        /// Sets the relationship.
        /// </summary>
        internal void SetRelationship()
        {
            var num = Request.GetUInteger();
            var num2 = Request.GetInteger();

            {
                using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
                {
                    if (num2 == 0)
                    {
                        queryReactor.SetQuery(
                            "SELECT id FROM users_relationships WHERE user_id=@id AND target=@target LIMIT 1");
                        queryReactor.AddParameter("id", Session.GetHabbo().Id);
                        queryReactor.AddParameter("target", num);
                        var integer = queryReactor.GetInteger();
                        queryReactor.SetQuery(
                            "DELETE FROM users_relationships WHERE user_id=@id AND target=@target LIMIT 1");
                        queryReactor.AddParameter("id", Session.GetHabbo().Id);
                        queryReactor.AddParameter("target", num);
                        queryReactor.RunQuery();
                        if (Session.GetHabbo().Relationships.ContainsKey(integer))
                            Session.GetHabbo().Relationships.Remove(integer);
                    }
                    else
                    {
                        queryReactor.SetQuery(
                            "SELECT id FROM users_relationships WHERE user_id=@id AND target=@target LIMIT 1");
                        queryReactor.AddParameter("id", Session.GetHabbo().Id);
                        queryReactor.AddParameter("target", num);
                        var integer2 = queryReactor.GetInteger();
                        if (integer2 > 0)
                        {
                            queryReactor.SetQuery(
                                "DELETE FROM users_relationships WHERE user_id=@id AND target=@target LIMIT 1");
                            queryReactor.AddParameter("id", Session.GetHabbo().Id);
                            queryReactor.AddParameter("target", num);
                            queryReactor.RunQuery();
                            if (Session.GetHabbo().Relationships.ContainsKey(integer2))
                                Session.GetHabbo().Relationships.Remove(integer2);
                        }
                        queryReactor.SetQuery(
                            "INSERT INTO users_relationships (user_id, target, type) VALUES (@id, @target, @type)");
                        queryReactor.AddParameter("id", Session.GetHabbo().Id);
                        queryReactor.AddParameter("target", num);
                        queryReactor.AddParameter("type", num2);
                        var num3 = (int)queryReactor.InsertQuery();
                        Session.GetHabbo().Relationships.Add(num3, new Relationship(num3, (int)num, num2));
                    }
                    var clientByUserId = Plus.GetGame().GetClientManager().GetClientByUserId(num);
                    Session.GetHabbo().GetMessenger().UpdateFriend(num, clientByUserId, true);
                }
            }
        }

        /// <summary>
        /// Starts the quest.
        /// </summary>
        public void StartQuest()
        {
            Plus.GetGame().GetQuestManager().ActivateQuest(Session, Request);
        }

        /// <summary>
        /// Stops the quest.
        /// </summary>
        public void StopQuest()
        {
            Plus.GetGame().GetQuestManager().CancelQuest(Session, Request);
        }

        /// <summary>
        /// Gets the current quest.
        /// </summary>
        public void GetCurrentQuest()
        {
            Plus.GetGame().GetQuestManager().GetCurrentQuest(Session, Request);
        }

        /// <summary>
        /// Starts the seasonal quest.
        /// </summary>
        public void StartSeasonalQuest()
        {
            RoomData roomData;
            using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
            {
                var quest = Plus.GetGame().GetQuestManager().GetQuest(Request.GetUInteger());
                if (quest == null)
                    return;
                queryReactor.RunFastQuery(string.Concat(new object[]
                {
                    "REPLACE INTO users_quests_data(user_id,quest_id) VALUES (",
                    Session.GetHabbo().Id,
                    ", ",
                    quest.Id,
                    ")"
                }));
                queryReactor.RunFastQuery(string.Concat(new object[]
                {
                    "UPDATE users_stats SET quest_id = ",
                    quest.Id,
                    " WHERE id = ",
                    Session.GetHabbo().Id
                }));
                Session.GetHabbo().CurrentQuestId = quest.Id;
                Session.SendMessage(QuestStartedComposer.Compose(Session, quest));
                Plus.GetGame().GetQuestManager().ActivateQuest(Session, Request);
                queryReactor.SetQuery("SELECT id FROM rooms_data WHERE state='open' ORDER BY users_now DESC LIMIT 1");
                var @string = queryReactor.GetString();
                roomData = Plus.GetGame().GetRoomManager().GenerateRoomData(uint.Parse(@string));
            }
            if (roomData != null)
            {
                roomData.SerializeRoomData(Response, Session, true, false);
                Session.GetMessageHandler().PrepareRoomForUser(roomData.Id, "");
                return;
            }
            this.Session.SendNotif(Plus.GetLanguage().GetVar("start_quest_need_room"));
        }

        /// <summary>
        /// Receives the nux gifts.
        /// </summary>
        public void ReceiveNuxGifts()
        {
            if (!ExtraSettings.NEW_users_gifts_ENABLED)
            {
                this.Session.SendNotif(Plus.GetLanguage().GetVar("nieuwe_gebruiker_kado_error_1"));
                return;
            }
            if (Session.GetHabbo().NuxPassed)
            {
                this.Session.SendNotif(Plus.GetLanguage().GetVar("nieuwe_gebruiker_kado_error_2"));
                return;
            }

            var item = Session.GetHabbo().GetInventoryComponent().AddNewItem(0, ExtraSettings.NEW_USER_GIFT_YTTV2_ID, "", 0, true, false, 0, 0);
            Session.GetHabbo().GetInventoryComponent().UpdateItems(false);

            Session.GetHabbo().BelCredits += 25;
            Session.GetHabbo().UpdateSeasonalCurrencyBalance();
            if (item != null)
                Session.GetHabbo().GetInventoryComponent().SendNewItems(item.Id);

            using (var dbClient = Plus.GetDatabaseManager().GetQueryReactor())
                if (Session.GetHabbo().VIP)
                    dbClient.RunFastQuery(
                        string.Format(
                            "UPDATE users SET vip = '1', vip_expire = DATE_ADD(vip_expire, INTERVAL 1 DAY), nux_passed = '1' WHERE id = {0}",
                            Session.GetHabbo().Id));
                else
                    dbClient.RunFastQuery(
                        string.Format(
                            "UPDATE users SET vip = '1', vip_expire = DATE_ADD(NOW(), INTERVAL 1 DAY), nux_passed = '1' WHERE id = {0}",
                            Session.GetHabbo().Id));

            Session.GetHabbo().NuxPassed = true;
            Session.GetHabbo().VIP = true;
        }

        /// <summary>
        /// Accepts the nux gifts.
        /// </summary>
        public void AcceptNuxGifts()
        {
            if (ExtraSettings.NEW_users_gifts_ENABLED == false || Request.GetInteger() != 0)
                return;

            var nuxGifts = new ServerMessage(LibraryParser.OutgoingRequest("NuxListGiftsMessageComposer"));
            nuxGifts.AppendInteger(3); //Cantidad

            nuxGifts.AppendInteger(0);
            nuxGifts.AppendInteger(0);
            nuxGifts.AppendInteger(1); //Cantidad
            // ahora nuevo bucle
            nuxGifts.AppendString("");
            nuxGifts.AppendString("nux/gift_yttv2.png");
            nuxGifts.AppendInteger(1); //cantidad
            //Ahora nuevo bucle...
            nuxGifts.AppendString("yttv2");
            nuxGifts.AppendString("");

            nuxGifts.AppendInteger(2);
            nuxGifts.AppendInteger(1);
            nuxGifts.AppendInteger(1);
            nuxGifts.AppendString("");
            nuxGifts.AppendString("nux/gift_diamonds.png");
            nuxGifts.AppendInteger(1);
            nuxGifts.AppendString("nux_gift_diamonds");
            nuxGifts.AppendString("");

            nuxGifts.AppendInteger(3);
            nuxGifts.AppendInteger(1);
            nuxGifts.AppendInteger(1);
            nuxGifts.AppendString("");
            nuxGifts.AppendString("nux/gift_vip1day.png");
            nuxGifts.AppendInteger(1);
            nuxGifts.AppendString("nux_gift_vip_1_day");
            nuxGifts.AppendString("");

            Session.SendMessage(nuxGifts);
        }

        /// <summary>
        /// Talentses this instance.
        /// </summary>
        /// <exception cref="System.NullReferenceException"></exception>
        internal void Talents()
        {
            var trackType = Request.GetString();
            var talents = Plus.GetGame().GetTalentManager().GetTalents(trackType, -1);
            var failLevel = -1;
            if (talents == null)
                return;
            Response.Init(LibraryParser.OutgoingRequest("TalentsTrackMessageComposer"));
            Response.AppendString(trackType);
            Response.AppendInteger(talents.Count);
            foreach (var current in talents)
            {
                Response.AppendInteger(current.Level);
                var nm = (failLevel == -1) ? 1 : 0;
                Response.AppendInteger(nm);
                var talents2 = Plus.GetGame().GetTalentManager().GetTalents(trackType, current.Id);
                Response.AppendInteger(talents2.Count);
                foreach (var current2 in talents2)
                {
                    if (current2.GetAchievement() == null)
                        throw new NullReferenceException(
                            string.Format("The following talent achievement can't be found: {0}",
                                current2.AchievementGroup));

                    var num = (failLevel != -1 && failLevel < current2.Level)
                        ? 0
                        : (Session.GetHabbo().GetAchievementData(current2.AchievementGroup) == null)
                            ? 1
                            : (Session.GetHabbo().GetAchievementData(current2.AchievementGroup).Level >=
                               current2.AchievementLevel)
                                ? 2
                                : 1;
                    Response.AppendInteger(current2.GetAchievement().Id);
                    Response.AppendInteger(0);
                    Response.AppendString(string.Format("{0}{1}", current2.AchievementGroup, current2.AchievementLevel));
                    Response.AppendInteger(num);
                    Response.AppendInteger((Session.GetHabbo().GetAchievementData(current2.AchievementGroup) != null)
                        ? Session.GetHabbo().GetAchievementData(current2.AchievementGroup).Progress
                        : 0);
                    Response.AppendInteger((current2.GetAchievement() == null)
                        ? 0
                        : current2.GetAchievement().Levels[current2.AchievementLevel].Requirement);
                    if (num != 2 && failLevel == -1)
                        failLevel = current2.Level;
                }
                Response.AppendInteger(0);
                if (current.Type == "citizenship" && current.Level == 4)
                {
                    Response.AppendInteger(2);
                    Response.AppendString("HABBO_CLUB_VIP_7_DAYS");
                    Response.AppendInteger(7);
                    Response.AppendString(current.Prize);
                    Response.AppendInteger(0);
                }
                else
                {
                    Response.AppendInteger(1);
                    Response.AppendString(current.Prize);
                    Response.AppendInteger(0);
                }
            }
            SendResponse();
        }

        /// <summary>
        /// Completes the safety quiz.
        /// </summary>
        internal void CompleteSafetyQuiz()
        {
            Plus.GetGame().GetAchievementManager().ProgressUserAchievement(Session, "ACH_SafetyQuizGraduate", 1, false);
        }

        /// <summary>
        /// Hotels the view countdown.
        /// </summary>
        internal void HotelViewCountdown()
        {
            string time = Request.GetString();
            DateTime date;
            DateTime.TryParse(time, out date);
            TimeSpan diff = date - DateTime.Now;
            Response.Init(LibraryParser.OutgoingRequest("HotelViewCountdownMessageComposer"));
            Response.AppendString(time);
            Response.AppendInteger(Convert.ToInt32(diff.TotalSeconds));
            SendResponse();
            Console.WriteLine(diff.TotalSeconds);
        }

        /// <summary>
        /// Hotels the view dailyquest.
        /// </summary>
        internal void HotelViewDailyquest()
        {
        }

        internal void FindMoreFriends()
        {
            var allRooms = Plus.GetGame().GetRoomManager().GetActiveRooms();
            Random rnd = new Random();
            var randomRoom = allRooms[rnd.Next(allRooms.Length)].Key;
            var success = new ServerMessage(LibraryParser.OutgoingRequest("FindMoreFriendsSuccessMessageComposer"));
            if (randomRoom == null)
            {
                success.AppendBool(false);
                Session.SendMessage(success);
                return;
            }
            success.AppendBool(true);
            Session.SendMessage(success);
            var roomFwd = new ServerMessage(LibraryParser.OutgoingRequest("RoomForwardMessageComposer"));
            roomFwd.AppendInteger(randomRoom.Id);
            Session.SendMessage(roomFwd);
        }
        internal void HotelViewRequestBadge()
        {
            string name = Request.GetString();
            var hotelViewBadges = Plus.GetGame().GetHotelView().HotelViewBadges;
            if (!hotelViewBadges.ContainsKey(name))
                return;
            var badge = hotelViewBadges[name];
            Session.GetHabbo().GetBadgeComponent().GiveBadge(badge, true, Session, true);
        }
        internal void GetCameraPrice()
        {
            GetResponse().Init(LibraryParser.OutgoingRequest("SetCameraPriceMessageComposer"));
            GetResponse().AppendInteger(3);//credits
            GetResponse().AppendInteger(0);//duckets
            SendResponse();
        }
        internal void GetHotelViewHallOfFame()
        {

        }
        internal void FriendRequestListLoad()
        {

        }
    }
}