using Plus.Database.Manager.Database.Session_Details.Interfaces;
using Plus.HabboHotel.Catalogs;
using Plus.HabboHotel.Groups;
using Plus.HabboHotel.Groups.Structs;
using Plus.HabboHotel.Rooms;
using Plus.Messages.Parsers;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Plus.Messages.Handlers
{
    /// <summary>
    /// Class GameClientMessageHandler.
    /// </summary>
    internal partial class GameClientMessageHandler
    {
        /// <summary>
        /// Serializes the group purchase page.
        /// </summary>
        internal void SerializeGroupPurchasePage()
        {
            var list = new HashSet<RoomData>(this.Session.GetHabbo().UsersRooms.Where(x => x.Group == null));

            this.Response.Init(LibraryParser.OutgoingRequest("GroupPurchasePageMessageComposer"));
            this.Response.AppendInteger(10);
            this.Response.AppendInteger(list.Count);
            foreach (RoomData current2 in list)
                this.NewMethod(current2);
            this.Response.AppendInteger(5);
            this.Response.AppendInteger(5);
            this.Response.AppendInteger(11);
            this.Response.AppendInteger(4);
            this.Response.AppendInteger(6);
            this.Response.AppendInteger(11);
            this.Response.AppendInteger(4);
            this.Response.AppendInteger(0);
            this.Response.AppendInteger(0);
            this.Response.AppendInteger(0);
            this.Response.AppendInteger(0);
            this.Response.AppendInteger(0);
            this.Response.AppendInteger(0);
            this.Response.AppendInteger(0);
            this.Response.AppendInteger(0);
            this.Response.AppendInteger(0);
            this.SendResponse();
        }

        /// <summary>
        /// Serializes the group purchase parts.
        /// </summary>
        internal void SerializeGroupPurchaseParts()
        {
            this.Response.Init(LibraryParser.OutgoingRequest("GroupPurchasePartsMessageComposer"));
            this.Response.AppendInteger(global::Plus.Plus.GetGame().GetGroupManager().Bases.Count);
            foreach (GroupBases current in global::Plus.Plus.GetGame().GetGroupManager().Bases)
            {
                this.Response.AppendInteger(current.Id);
                this.Response.AppendString(current.Value1);
                this.Response.AppendString(current.Value2);
            }
            this.Response.AppendInteger(global::Plus.Plus.GetGame().GetGroupManager().Symbols.Count);
            foreach (GroupSymbols current2 in global::Plus.Plus.GetGame().GetGroupManager().Symbols)
            {
                this.Response.AppendInteger(current2.Id);
                this.Response.AppendString(current2.Value1);
                this.Response.AppendString(current2.Value2);
            }
            this.Response.AppendInteger(global::Plus.Plus.GetGame().GetGroupManager().BaseColours.Count);
            foreach (GroupBaseColours current3 in global::Plus.Plus.GetGame().GetGroupManager().BaseColours)
            {
                this.Response.AppendInteger(current3.Id);
                this.Response.AppendString(current3.Colour);
            }
            this.Response.AppendInteger(global::Plus.Plus.GetGame().GetGroupManager().SymbolColours.Count);
            foreach (GroupSymbolColours current4 in global::Plus.Plus.GetGame().GetGroupManager().SymbolColours.Values)
            {
                this.Response.AppendInteger(current4.Id);
                this.Response.AppendString(current4.Colour);
            }
            this.Response.AppendInteger(global::Plus.Plus.GetGame().GetGroupManager().BackGroundColours.Count);
            foreach (GroupBackGroundColours current5 in global::Plus.Plus.GetGame().GetGroupManager().BackGroundColours.Values)
            {
                this.Response.AppendInteger(current5.Id);
                this.Response.AppendString(current5.Colour);
            }
            this.SendResponse();
        }

        /// <summary>
        /// Purchases the group.
        /// </summary>
        internal void PurchaseGroup()
        {
            if (Session == null || Session.GetHabbo().Credits < 10)
                return;
            var gStates = new List<int>();
            var name = Request.GetString();
            var description = Request.GetString();
            var roomid = Request.GetUInteger();
            var color = Request.GetInteger();
            var num3 = Request.GetInteger();
            var unused = Request.GetInteger();
            var guildBase = Request.GetInteger();
            var guildBaseColor = Request.GetInteger();
            var num6 = Request.GetInteger();
            var roomData = Plus.GetGame().GetRoomManager().GenerateRoomData(roomid);
            if (roomData.Owner != Session.GetHabbo().UserName)
                return;
            for (var i = 0; i < (num6 * 3); i++)
            {
                var item = Request.GetInteger();
                gStates.Add(item);
            }

            var image = Plus.GetGame()
                .GetGroupManager()
                .GenerateGuildImage(guildBase, guildBaseColor, gStates);
            Guild group;
            Plus.GetGame()
                .GetGroupManager()
                .CreateGroup(name, description, roomid, image, Session,
                    (!Plus.GetGame().GetGroupManager().SymbolColours.Contains(color)) ? 1 : color,
                    (!Plus.GetGame().GetGroupManager().BackGroundColours.Contains(num3)) ? 1 : num3,
                    out group);

            Session.SendMessage(CatalogPacket.PurchaseOk(0u, "CREATE_GUILD", 10));
            Response.Init(LibraryParser.OutgoingRequest("GroupRoomMessageComposer"));
            Response.AppendInteger(roomid);
            Response.AppendInteger(group.Id);
            SendResponse();
            roomData.Group = group;
            roomData.GroupId = group.Id;
            roomData.SerializeRoomData(Response, Session, true, false);
            if (Session.GetHabbo().CurrentRoom.RoomId != roomData.Id)
            {
                var roomFwd = new ServerMessage(LibraryParser.OutgoingRequest("RoomForwardMessageComposer"));
                roomFwd.AppendInteger(roomData.Id);
                Session.SendMessage(roomFwd);
            }
            if (Session.GetHabbo().CurrentRoom != null &&
                !Session.GetHabbo().CurrentRoom.LoadedGroups.ContainsKey(group.Id))
                Session.GetHabbo().CurrentRoom.LoadedGroups.Add(group.Id, group.Badge);
            if (CurrentLoadingRoom != null && !CurrentLoadingRoom.LoadedGroups.ContainsKey(group.Id))
                CurrentLoadingRoom.LoadedGroups.Add(group.Id, group.Badge);
            var serverMessage = new ServerMessage(LibraryParser.OutgoingRequest("RoomGroupMessageComposer"));
            serverMessage.AppendInteger(Session.GetHabbo().CurrentRoom.LoadedGroups.Count);
            foreach (var current in Session.GetHabbo().CurrentRoom.LoadedGroups)
            {
                serverMessage.AppendInteger(current.Key);
                serverMessage.AppendString(current.Value);
            }
            if (CurrentLoadingRoom != null)
                CurrentLoadingRoom.SendMessage(serverMessage);

            if (CurrentLoadingRoom == null || Session.GetHabbo().FavouriteGroup != @group.Id)
                return;
            var serverMessage2 = new ServerMessage(LibraryParser.OutgoingRequest("ChangeFavouriteGroupMessageComposer"));
            serverMessage2.AppendInteger(
                CurrentLoadingRoom.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id).VirtualId);
            serverMessage2.AppendInteger(@group.Id);
            serverMessage2.AppendInteger(3);
            serverMessage2.AppendString(@group.Name);
            CurrentLoadingRoom.SendMessage(serverMessage2);
        }

        /// <summary>
        /// Serializes the group information.
        /// </summary>
        internal void SerializeGroupInfo()
        {
            uint groupId = this.Request.GetUInteger();
            bool newWindow = this.Request.GetBool();
            Guild group = global::Plus.Plus.GetGame().GetGroupManager().GetGroup(groupId);
            if (group == null)
                return;
            global::Plus.Plus.GetGame().GetGroupManager().SerializeGroupInfo(group, this.Response, this.Session, newWindow);
        }

        /// <summary>
        /// Serializes the group members.
        /// </summary>
        internal void SerializeGroupMembers()
        {
            uint groupId = this.Request.GetUInteger();
            int page = this.Request.GetInteger();
            string searchVal = this.Request.GetString();
            uint reqType = this.Request.GetUInteger();
            Guild group = global::Plus.Plus.GetGame().GetGroupManager().GetGroup(groupId);
            this.Response.Init(LibraryParser.OutgoingRequest("GroupMembersMessageComposer"));
            global::Plus.Plus.GetGame().GetGroupManager().SerializeGroupMembers(this.Response, group, reqType, this.Session, searchVal, page);
            this.SendResponse();
        }

        /// <summary>
        /// Makes the group admin.
        /// </summary>
        internal void MakeGroupAdmin()
        {
            uint num = this.Request.GetUInteger();
            uint num2 = this.Request.GetUInteger();
            Guild group = global::Plus.Plus.GetGame().GetGroupManager().GetGroup(num);
            if (this.Session.GetHabbo().Id != group.CreatorId || !group.Members.ContainsKey(num2) || group.Admins.ContainsKey(num2))
                return;
            group.Members[num2].Rank = 1;
            group.Admins.Add(num2, group.Members[num2]);
            this.Response.Init(LibraryParser.OutgoingRequest("GroupMembersMessageComposer"));
            global::Plus.Plus.GetGame().GetGroupManager().SerializeGroupMembers(this.Response, group, 1u, this.Session, "", 0);
            this.SendResponse();
            Room room = global::Plus.Plus.GetGame().GetRoomManager().GetRoom(group.RoomId);
            if (room != null)
            {
                RoomUser roomUserByHabbo = room.GetRoomUserManager().GetRoomUserByHabbo(global::Plus.Plus.GetHabboById(num2).UserName);
                if (roomUserByHabbo != null)
                {
                    if (!roomUserByHabbo.Statusses.ContainsKey("flatctrl 1"))
                        roomUserByHabbo.AddStatus("flatctrl 1", "");
                    this.Response.Init(LibraryParser.OutgoingRequest("RoomRightsLevelMessageComposer"));
                    this.Response.AppendInteger(1);
                    roomUserByHabbo.GetClient().SendMessage(this.GetResponse());
                    roomUserByHabbo.UpdateNeeded = true;
                }
            }
            using (IQueryAdapter queryReactor = global::Plus.Plus.GetDatabaseManager().GetQueryReactor())
            {
                queryReactor.RunFastQuery(string.Concat(new object[]
                {
                    "UPDATE groups_members SET rank='1' WHERE group_id=",
                    num,
                    " AND user_id=",
                    num2,
                    " LIMIT 1;"
                }));
            }
        }

        /// <summary>
        /// Removes the group admin.
        /// </summary>
        internal void RemoveGroupAdmin()
        {
            uint num = this.Request.GetUInteger();
            uint num2 = this.Request.GetUInteger();
            Guild group = global::Plus.Plus.GetGame().GetGroupManager().GetGroup(num);
            if (this.Session.GetHabbo().Id != group.CreatorId || !group.Members.ContainsKey(num2) || !group.Admins.ContainsKey(num2))
                return;
            group.Members[num2].Rank = 0;
            group.Admins.Remove(num2);
            this.Response.Init(LibraryParser.OutgoingRequest("GroupMembersMessageComposer"));
            global::Plus.Plus.GetGame().GetGroupManager().SerializeGroupMembers(this.Response, group, 0u, this.Session, "", 0);
            this.SendResponse();
            Room room = global::Plus.Plus.GetGame().GetRoomManager().GetRoom(group.RoomId);
            if (room != null)
            {
                RoomUser roomUserByHabbo = room.GetRoomUserManager().GetRoomUserByHabbo(global::Plus.Plus.GetHabboById(num2).UserName);
                if (roomUserByHabbo != null)
                {
                    if (roomUserByHabbo.Statusses.ContainsKey("flatctrl 1"))
                        roomUserByHabbo.RemoveStatus("flatctrl 1");
                    this.Response.Init(LibraryParser.OutgoingRequest("RoomRightsLevelMessageComposer"));
                    this.Response.AppendInteger(0);
                    roomUserByHabbo.GetClient().SendMessage(this.GetResponse());
                    roomUserByHabbo.UpdateNeeded = true;
                }
            }
            using (IQueryAdapter queryReactor = global::Plus.Plus.GetDatabaseManager().GetQueryReactor())
            {
                queryReactor.RunFastQuery(string.Concat(new object[]
                {
                    "UPDATE groups_members SET rank='0' WHERE group_id=",
                    num,
                    " AND user_id=",
                    num2,
                    " LIMIT 1;"
                }));
            }
        }

        /// <summary>
        /// Accepts the membership.
        /// </summary>
        internal void AcceptMembership()
        {
            uint num = this.Request.GetUInteger();
            uint num2 = this.Request.GetUInteger();
            Guild group = global::Plus.Plus.GetGame().GetGroupManager().GetGroup(num);
            if (this.Session.GetHabbo().Id != group.CreatorId && !group.Admins.ContainsKey(this.Session.GetHabbo().Id) && !group.Requests.Contains(num2))
                return;
            if (group.Members.ContainsKey(num2))
            {
                group.Requests.Remove(num2);
                using (IQueryAdapter queryReactor = global::Plus.Plus.GetDatabaseManager().GetQueryReactor())
                {
                    queryReactor.RunFastQuery(string.Concat(new object[]
                    {
                        "DELETE FROM groups_requests WHERE group_id=",
                        num,
                        " AND user_id=",
                        num2,
                        " LIMIT 1;"
                    }));
                }
                return;
            }
            group.Requests.Remove(num2);
            group.Members.Add(num2, new GroupUser(num2, num, 0));
            group.Admins.Add(num2, group.Members[num2]);
            global::Plus.Plus.GetGame().GetGroupManager().SerializeGroupInfo(group, this.Response, this.Session, false);
            this.Response.Init(LibraryParser.OutgoingRequest("GroupMembersMessageComposer"));
            global::Plus.Plus.GetGame().GetGroupManager().SerializeGroupMembers(this.Response, group, 0u, this.Session, "", 0);
            this.SendResponse();
            using (IQueryAdapter queryreactor2 = global::Plus.Plus.GetDatabaseManager().GetQueryReactor())
            {
                queryreactor2.RunFastQuery(string.Concat(new object[]
                {
                    "DELETE FROM groups_requests WHERE group_id=",
                    num,
                    " AND user_id=",
                    num2,
                    " LIMIT 1;INSERT INTO groups_members (group_id, user_id) VALUES (",
                    num,
                    ", ",
                    num2,
                    ")"
                }));
            }
        }

        /// <summary>
        /// Declines the membership.
        /// </summary>
        internal void DeclineMembership()
        {
            var groupId = Request.GetUInteger();
            var userId = Request.GetUInteger();
            var group = Plus.GetGame().GetGroupManager().GetGroup(groupId);

            if (Session.GetHabbo().Id != group.CreatorId && !group.Admins.ContainsKey(Session.GetHabbo().Id) &&
                !group.Requests.Contains(userId)) return;

            group.Requests.Remove(userId);

            Response.Init(LibraryParser.OutgoingRequest("GroupMembersMessageComposer"));
            Plus.GetGame().GetGroupManager().SerializeGroupMembers(Response, group, 2u, Session, "", 0);
            SendResponse();
            var room = Plus.GetGame().GetRoomManager().GetRoom(group.RoomId);
            if (room != null)
            {
                var roomUserByHabbo = room.GetRoomUserManager().GetRoomUserByHabbo(Plus.GetHabboById(userId).UserName);
                if (roomUserByHabbo != null)
                {
                    if (roomUserByHabbo.Statusses.ContainsKey("flatctrl 1")) roomUserByHabbo.RemoveStatus("flatctrl 1");
                    roomUserByHabbo.UpdateNeeded = true;
                }
            }
            Plus.GetGame().GetGroupManager().SerializeGroupInfo(group, Response, Session, false);
            using (var queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
            {
                queryReactor.RunFastQuery("DELETE FROM groups_requests WHERE group_id=" + groupId +
                                          " AND user_id=" + userId);
            }
        }

        /// <summary>
        /// Joins the group.
        /// </summary>
        internal void JoinGroup()
        {
            uint num = this.Request.GetUInteger();
            Guild group = global::Plus.Plus.GetGame().GetGroupManager().GetGroup(num);
            if (group.Members.ContainsKey(this.Session.GetHabbo().Id))
                return;
            if (group.State == 0u)
            {
                using (IQueryAdapter queryReactor = global::Plus.Plus.GetDatabaseManager().GetQueryReactor())
                {
                    queryReactor.RunFastQuery(string.Concat(new object[]
                    {
                        "INSERT INTO groups_members (user_id, group_id) VALUES (",
                        this.Session.GetHabbo().Id,
                        ",",
                        num,
                        ")"
                    }));
                    queryReactor.RunFastQuery(string.Concat(new object[]
                    {
                        "UPDATE users_stats SET favourite_group=",
                        num,
                        " WHERE id= ",
                        this.Session.GetHabbo().Id,
                        " LIMIT 1"
                    }));
                }
                group.Members.Add(Session.GetHabbo().Id, new GroupUser(Session.GetHabbo().Id, group.Id, 0));
                Session.GetHabbo().UserGroups.Add(group.Members[Session.GetHabbo().Id]);
            }
            else
            {
                using (IQueryAdapter queryreactor2 = Plus.GetDatabaseManager().GetQueryReactor())
                {
                    queryreactor2.RunFastQuery(string.Concat("INSERT INTO groups_requests (user_id, group_id) VALUES (", this.Session.GetHabbo().Id, ",", num, ")"));
                }
                group.Requests.Add(this.Session.GetHabbo().Id);
            }
            global::Plus.Plus.GetGame().GetGroupManager().SerializeGroupInfo(group, this.Response, this.Session, false);
        }

        /// <summary>
        /// Removes the member.
        /// </summary>
        internal void RemoveMember()
        {
            uint num = this.Request.GetUInteger();
            uint num2 = this.Request.GetUInteger();
            Guild group = global::Plus.Plus.GetGame().GetGroupManager().GetGroup(num);
            if (num2 == this.Session.GetHabbo().Id)
            {
                if (group.Members.ContainsKey(num2))
                    group.Members.Remove(num2);
                if (group.Admins.ContainsKey(num2))
                    group.Admins.Remove(num2);
                using (IQueryAdapter queryReactor = global::Plus.Plus.GetDatabaseManager().GetQueryReactor())
                {
                    queryReactor.RunFastQuery(string.Concat(new object[]
                    {
                        "DELETE FROM groups_members WHERE user_id=",
                        num2,
                        " AND group_id=",
                        num,
                        " LIMIT 1"
                    }));
                }
                global::Plus.Plus.GetGame().GetGroupManager().SerializeGroupInfo(group, this.Response, this.Session, false);
                if (this.Session.GetHabbo().FavouriteGroup == num)
                {
                    this.Session.GetHabbo().FavouriteGroup = 0u;
                    using (IQueryAdapter queryreactor2 = global::Plus.Plus.GetDatabaseManager().GetQueryReactor())
                        queryreactor2.RunFastQuery(string.Format("UPDATE users_stats SET favourite_group=0 WHERE id={0} LIMIT 1", num2));
                    this.Response.Init(LibraryParser.OutgoingRequest("FavouriteGroupMessageComposer"));
                    this.Response.AppendInteger(this.Session.GetHabbo().Id);
                    this.Session.GetHabbo().CurrentRoom.SendMessage(this.Response);
                    this.Response.Init(LibraryParser.OutgoingRequest("ChangeFavouriteGroupMessageComposer"));
                    this.Response.AppendInteger(0);
                    this.Response.AppendInteger(-1);
                    this.Response.AppendInteger(-1);
                    this.Response.AppendString("");
                    this.Session.GetHabbo().CurrentRoom.SendMessage(this.Response);
                    if (group.AdminOnlyDeco == 0u)
                    {
                        RoomUser roomUserByHabbo = global::Plus.Plus.GetGame().GetRoomManager().GetRoom(group.RoomId).GetRoomUserManager().GetRoomUserByHabbo(global::Plus.Plus.GetHabboById(num2).UserName);
                        if (roomUserByHabbo == null)
                            return;
                        roomUserByHabbo.RemoveStatus("flatctrl 1");
                        this.Response.Init(LibraryParser.OutgoingRequest("RoomRightsLevelMessageComposer"));
                        this.Response.AppendInteger(0);
                        roomUserByHabbo.GetClient().SendMessage(this.GetResponse());
                    }
                }
                return;
            }
            if (this.Session.GetHabbo().Id != group.CreatorId || !group.Members.ContainsKey(num2))
                return;
            group.Members.Remove(num2);
            if (group.Admins.ContainsKey(num2))
                group.Admins.Remove(num2);
            global::Plus.Plus.GetGame().GetGroupManager().SerializeGroupInfo(group, this.Response, this.Session, false);
            this.Response.Init(LibraryParser.OutgoingRequest("GroupMembersMessageComposer"));
            global::Plus.Plus.GetGame().GetGroupManager().SerializeGroupMembers(this.Response, group, 0u, this.Session, "", 0);
            this.SendResponse();
            using (IQueryAdapter queryreactor3 = global::Plus.Plus.GetDatabaseManager().GetQueryReactor())
            {
                queryreactor3.RunFastQuery(string.Concat(new object[]
                {
                    "DELETE FROM groups_members WHERE group_id=",
                    num,
                    " AND user_id=",
                    num2,
                    " LIMIT 1;"
                }));
            }
        }

        /// <summary>
        /// Makes the fav.
        /// </summary>
        internal void MakeFav()
        {
            uint groupId = this.Request.GetUInteger();
            Guild group = global::Plus.Plus.GetGame().GetGroupManager().GetGroup(groupId);
            if (group == null)
                return;
            if (!group.Members.ContainsKey(this.Session.GetHabbo().Id))
                return;
            this.Session.GetHabbo().FavouriteGroup = group.Id;
            global::Plus.Plus.GetGame().GetGroupManager().SerializeGroupInfo(group, this.Response, this.Session, false);
            using (IQueryAdapter queryReactor = global::Plus.Plus.GetDatabaseManager().GetQueryReactor())
            {
                queryReactor.RunFastQuery(string.Concat(new object[]
                {
                    "UPDATE users_stats SET favourite_group =",
                    group.Id,
                    " WHERE id=",
                    this.Session.GetHabbo().Id,
                    " LIMIT 1;"
                }));
            }
            this.Response.Init(LibraryParser.OutgoingRequest("FavouriteGroupMessageComposer"));
            this.Response.AppendInteger(this.Session.GetHabbo().Id);
            this.Session.GetHabbo().CurrentRoom.SendMessage(this.Response);
            if (!this.Session.GetHabbo().CurrentRoom.LoadedGroups.ContainsKey(group.Id))
            {
                this.Session.GetHabbo().CurrentRoom.LoadedGroups.Add(group.Id, group.Badge);
                this.Response.Init(LibraryParser.OutgoingRequest("RoomGroupMessageComposer"));
                this.Response.AppendInteger(this.Session.GetHabbo().CurrentRoom.LoadedGroups.Count);
                foreach (KeyValuePair<uint, string> current in this.Session.GetHabbo().CurrentRoom.LoadedGroups)
                {
                    this.Response.AppendInteger(current.Key);
                    this.Response.AppendString(current.Value);
                }
                this.Session.GetHabbo().CurrentRoom.SendMessage(this.Response);
            }
            this.Response.Init(LibraryParser.OutgoingRequest("ChangeFavouriteGroupMessageComposer"));
            this.Response.AppendInteger(0);
            this.Response.AppendInteger(group.Id);
            this.Response.AppendInteger(3);
            this.Response.AppendString(group.Name);
            this.Session.GetHabbo().CurrentRoom.SendMessage(this.Response);
        }

        /// <summary>
        /// Removes the fav.
        /// </summary>
        internal void RemoveFav()
        {
            this.Request.GetUInteger();
            this.Session.GetHabbo().FavouriteGroup = 0u;
            using (IQueryAdapter queryReactor = global::Plus.Plus.GetDatabaseManager().GetQueryReactor())
                queryReactor.RunFastQuery(string.Format("UPDATE users_stats SET favourite_group=0 WHERE id={0} LIMIT 1;", this.Session.GetHabbo().Id));
            this.Response.Init(LibraryParser.OutgoingRequest("FavouriteGroupMessageComposer"));
            this.Response.AppendInteger(this.Session.GetHabbo().Id);
            if (this.Session.GetHabbo().CurrentRoom != null)
                this.Session.GetHabbo().CurrentRoom.SendMessage(this.Response);
            this.Response.Init(LibraryParser.OutgoingRequest("ChangeFavouriteGroupMessageComposer"));
            this.Response.AppendInteger(0);
            this.Response.AppendInteger(-1);
            this.Response.AppendInteger(-1);
            this.Response.AppendString("");
            if (this.Session.GetHabbo().CurrentRoom != null)
                this.Session.GetHabbo().CurrentRoom.SendMessage(this.Response);
        }

        /// <summary>
        /// Publishes the forum thread.
        /// </summary>
        internal void PublishForumThread()
        {
            if ((global::Plus.Plus.GetUnixTimeStamp() - this.Session.GetHabbo().LastSqlQuery) < 20)
                return;
            uint groupId = this.Request.GetUInteger();
            uint threadId = this.Request.GetUInteger();
            string subject = this.Request.GetString();
            string content = this.Request.GetString();
            Guild group = global::Plus.Plus.GetGame().GetGroupManager().GetGroup(groupId);
            if (group == null || !group.HasForum)
                return;
            int timestamp = global::Plus.Plus.GetUnixTimeStamp();
            using (IQueryAdapter dbClient = global::Plus.Plus.GetDatabaseManager().GetQueryReactor())
            {
                if (threadId != 0)
                {
                    dbClient.SetQuery(string.Format("SELECT * FROM groups_forums_posts WHERE id = {0}", threadId));
                    DataRow Row = dbClient.GetRow();
                    var Post = new GroupForumPost(Row);
                    if (Post.Locked || Post.Hidden)
                    {
                        this.Session.SendNotif(Plus.GetLanguage().GetVar("forums_cancel"));
                        return;
                    }
                }
                this.Session.GetHabbo().LastSqlQuery = global::Plus.Plus.GetUnixTimeStamp();
                dbClient.SetQuery("INSERT INTO groups_forums_posts (group_id, parent_id, timestamp, poster_id, poster_name, poster_look, subject, post_content) VALUES (@gid, @pard, @ts, @pid, @pnm, @plk, @subjc, @content)");
                dbClient.AddParameter("gid", groupId);
                dbClient.AddParameter("pard", threadId);
                dbClient.AddParameter("ts", timestamp);
                dbClient.AddParameter("pid", this.Session.GetHabbo().Id);
                dbClient.AddParameter("pnm", this.Session.GetHabbo().UserName);
                dbClient.AddParameter("plk", this.Session.GetHabbo().Look);
                dbClient.AddParameter("subjc", subject);
                dbClient.AddParameter("content", content);
                threadId = (uint)dbClient.GetInteger();
            }
            group.ForumScore += 0.25;
            group.ForumLastPosterName = this.Session.GetHabbo().UserName;
            group.ForumLastPosterId = this.Session.GetHabbo().Id;
            group.ForumLastPosterTimestamp = timestamp;
            group.ForumMessagesCount++;
            group.UpdateForum();
            if (threadId == 0)
            {
                var Message = new ServerMessage(LibraryParser.OutgoingRequest("GroupForumNewThreadMessageComposer"));
                Message.AppendInteger(groupId);
                Message.AppendInteger(threadId);
                Message.AppendInteger(this.Session.GetHabbo().Id);
                Message.AppendString(subject);
                Message.AppendString(content);
                Message.AppendBool(false);
                Message.AppendBool(false);
                Message.AppendInteger((global::Plus.Plus.GetUnixTimeStamp() - timestamp));
                Message.AppendInteger(1);
                Message.AppendInteger(0);
                Message.AppendInteger(0);
                Message.AppendInteger(1);
                Message.AppendString("");
                Message.AppendInteger((global::Plus.Plus.GetUnixTimeStamp() - timestamp));
                Message.AppendByte(1);
                Message.AppendInteger(1);
                Message.AppendString("");
                Message.AppendInteger(42);//useless
                this.Session.SendMessage(Message);
            }
            else
            {
                var Message = new ServerMessage(LibraryParser.OutgoingRequest("GroupForumNewResponseMessageComposer"));
                Message.AppendInteger(groupId);
                Message.AppendInteger(threadId);
                Message.AppendInteger(group.ForumMessagesCount);
                Message.AppendInteger(0);
                Message.AppendInteger(this.Session.GetHabbo().Id);
                Message.AppendString(this.Session.GetHabbo().UserName);
                Message.AppendString(this.Session.GetHabbo().Look);
                Message.AppendInteger((global::Plus.Plus.GetUnixTimeStamp() - timestamp));
                Message.AppendString(content);
                Message.AppendByte(0);
                Message.AppendInteger(0);
                Message.AppendString("");
                Message.AppendInteger(0);
                this.Session.SendMessage(Message);
            }
        }

        /// <summary>
        /// Updates the state of the thread.
        /// </summary>
        internal void UpdateThreadState()
        {
            uint GroupId = this.Request.GetUInteger();
            uint ThreadId = this.Request.GetUInteger();
            bool Pin = this.Request.GetBool();
            bool Lock = this.Request.GetBool();
            using (IQueryAdapter dbClient = global::Plus.Plus.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery(string.Format("SELECT * FROM groups_forums_posts WHERE group_id = '{0}' AND id = '{1}' LIMIT 1;", GroupId, ThreadId));
                DataRow Row = dbClient.GetRow();
                Guild Group = global::Plus.Plus.GetGame().GetGroupManager().GetGroup(GroupId);
                if (Row != null)
                {
                    if ((uint)Row["poster_id"] == this.Session.GetHabbo().Id || Group.Admins.ContainsKey(this.Session.GetHabbo().Id))
                    {
                        dbClient.SetQuery(string.Format("UPDATE groups_forums_posts SET pinned = @pin , locked = @lock WHERE id = {0};", ThreadId));
                        dbClient.AddParameter("pin", (Pin) ? "1" : "0");
                        dbClient.AddParameter("lock", (Lock) ? "1" : "0");
                        dbClient.RunQuery();
                    }
                }

                var Thread = new GroupForumPost(Row);
                if (Thread.Pinned != Pin)
                {
                    var Notif = new ServerMessage(LibraryParser.OutgoingRequest("SuperNotificationMessageComposer"));
                    Notif.AppendString((Pin) ? "forums.thread.pinned" : "forums.thread.unpinned");
                    Notif.AppendInteger(0);
                    this.Session.SendMessage(Notif);
                }
                if (Thread.Locked != Lock)
                {
                    var Notif2 = new ServerMessage(LibraryParser.OutgoingRequest("SuperNotificationMessageComposer"));
                    Notif2.AppendString((Lock) ? "forums.thread.locked" : "forums.thread.unlocked");
                    Notif2.AppendInteger(0);
                    this.Session.SendMessage(Notif2);
                }
                if (Thread.ParentId != 0)
                    return;
                var Message = new ServerMessage(LibraryParser.OutgoingRequest("GroupForumThreadUpdateMessageComposer"));
                Message.AppendInteger(GroupId);
                Message.AppendInteger(Thread.Id);
                Message.AppendInteger(Thread.PosterId);
                Message.AppendString(Thread.PosterName);
                Message.AppendString(Thread.Subject);
                Message.AppendBool(Pin);
                Message.AppendBool(Lock);
                Message.AppendInteger((global::Plus.Plus.GetUnixTimeStamp() - Thread.Timestamp));
                Message.AppendInteger(Thread.MessageCount + 1);
                Message.AppendInteger(0);
                Message.AppendInteger(0);
                Message.AppendInteger(1);
                Message.AppendString("");
                Message.AppendInteger((global::Plus.Plus.GetUnixTimeStamp() - Thread.Timestamp));
                Message.AppendByte((Thread.Hidden) ? 10 : 1);
                Message.AppendInteger(1);
                Message.AppendString(Thread.Hider);
                Message.AppendInteger(0);
                this.Session.SendMessage(Message);
            }
        }

        /// <summary>
        /// Alters the state of the forum thread.
        /// </summary>
        internal void AlterForumThreadState()
        {
            uint GroupId = this.Request.GetUInteger();
            uint ThreadId = this.Request.GetUInteger();
            int StateToSet = this.Request.GetInteger();
            using (IQueryAdapter dbClient = global::Plus.Plus.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery(string.Format("SELECT * FROM groups_forums_posts WHERE group_id = '{0}' AND id = '{1}' LIMIT 1;", GroupId, ThreadId));
                DataRow Row = dbClient.GetRow();
                Guild Group = global::Plus.Plus.GetGame().GetGroupManager().GetGroup(GroupId);
                if (Row != null)
                {
                    if ((uint)Row["poster_id"] == this.Session.GetHabbo().Id || Group.Admins.ContainsKey(this.Session.GetHabbo().Id))
                    {
                        dbClient.SetQuery(string.Format("UPDATE groups_forums_posts SET hidden = @hid WHERE id = {0};", ThreadId));
                        dbClient.AddParameter("hid", (StateToSet == 10) ? "1" : "0");
                        dbClient.RunQuery();
                    }
                }
                var Thread = new GroupForumPost(Row);
                var Notif = new ServerMessage(LibraryParser.OutgoingRequest("SuperNotificationMessageComposer"));
                Notif.AppendString((StateToSet == 10) ? "forums.thread.hidden" : "forums.thread.restored");
                Notif.AppendInteger(0);
                this.Session.SendMessage(Notif);
                if (Thread.ParentId != 0)
                    return;
                var Message = new ServerMessage(LibraryParser.OutgoingRequest("GroupForumThreadUpdateMessageComposer"));
                Message.AppendInteger(GroupId);
                Message.AppendInteger(Thread.Id);
                Message.AppendInteger(Thread.PosterId);
                Message.AppendString(Thread.PosterName);
                Message.AppendString(Thread.Subject);
                Message.AppendBool(Thread.Pinned);
                Message.AppendBool(Thread.Locked);
                Message.AppendInteger((global::Plus.Plus.GetUnixTimeStamp() - Thread.Timestamp));
                Message.AppendInteger(Thread.MessageCount + 1);
                Message.AppendInteger(0);
                Message.AppendInteger(0);
                Message.AppendInteger(0);
                Message.AppendString("");
                Message.AppendInteger((global::Plus.Plus.GetUnixTimeStamp() - Thread.Timestamp));
                Message.AppendByte(StateToSet);
                Message.AppendInteger(0);
                Message.AppendString(Thread.Hider);
                Message.AppendInteger(0);
                this.Session.SendMessage(Message);
            }
        }

        /// <summary>
        /// Reads the forum thread.
        /// </summary>
        internal void ReadForumThread()
        {
            uint GroupId = this.Request.GetUInteger();
            uint ThreadId = this.Request.GetUInteger();
            int StartIndex = this.Request.GetInteger();
            int EndIndex = this.Request.GetInteger();
            Guild Group = global::Plus.Plus.GetGame().GetGroupManager().GetGroup(GroupId);
            if (Group == null || !Group.HasForum)
                return;
            using (IQueryAdapter dbClient = global::Plus.Plus.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery(string.Format("SELECT * FROM groups_forums_posts WHERE group_id = '{0}' AND parent_id = '{1}' OR id = '{2}' ORDER BY timestamp ASC;", GroupId, ThreadId, ThreadId));
                DataTable Table = dbClient.GetTable();
                if (Table == null)
                    return;
                int b = (Table.Rows.Count <= 20) ? Table.Rows.Count : 20;
                var posts = new List<GroupForumPost>();
                int i = 1;
                while (i <= b)
                {
                    DataRow Row = Table.Rows[i - 1];
                    if (Row == null)
                    {
                        b--;
                        continue;
                    }
                    var thread = new GroupForumPost(Row);
                    if (thread.ParentId == 0 && thread.Hidden)
                        return;
                    posts.Add(thread);
                    i++;
                }

                var Message = new ServerMessage(LibraryParser.OutgoingRequest("GroupForumReadThreadMessageComposer"));
                Message.AppendInteger(GroupId);
                Message.AppendInteger(ThreadId);
                Message.AppendInteger(StartIndex);
                Message.AppendInteger(b);
                int indx = 0;
                foreach (GroupForumPost Post in posts)
                {
                    Message.AppendInteger(indx++ - 1);
                    Message.AppendInteger(indx - 1);
                    Message.AppendInteger(Post.PosterId);
                    Message.AppendString(Post.PosterName);
                    Message.AppendString(Post.PosterLook);
                    Message.AppendInteger((global::Plus.Plus.GetUnixTimeStamp() - Post.Timestamp));
                    Message.AppendString(Post.PostContent);
                    Message.AppendByte(0);
                    Message.AppendInteger(0);
                    Message.AppendString(Post.Hider);
                    Message.AppendInteger(0);
                }
                this.Session.SendMessage(Message);
            }
        }

        /// <summary>
        /// Gets the group forum thread root.
        /// </summary>
        internal void GetGroupForumThreadRoot()
        {
            uint GroupId = this.Request.GetUInteger();
            int StartIndex = this.Request.GetInteger();
            int EndIndex = this.Request.GetInteger();
            Guild Group = global::Plus.Plus.GetGame().GetGroupManager().GetGroup(GroupId);
            if (Group == null || !Group.HasForum)
                return;
            using (IQueryAdapter dbClient = global::Plus.Plus.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery(string.Format("SELECT * FROM groups_forums_posts WHERE group_id = '{0}' AND parent_id = 0 ORDER BY timestamp DESC;", GroupId));
                DataTable Table = dbClient.GetTable();
                if (Table == null)
                {
                    var Messages = new ServerMessage(LibraryParser.OutgoingRequest("GroupForumThreadRootMessageComposer"));
                    Messages.AppendInteger(GroupId);
                    Messages.AppendInteger(0);
                    Messages.AppendInteger(0);
                    this.Session.SendMessage(Messages);
                    return;
                }
                int b = (Table.Rows.Count <= 20) ? Table.Rows.Count : 20;
                var Threads = new List<GroupForumPost>();
                int i = 1;
                while (i <= b)
                {
                    DataRow Row = Table.Rows[i - 1];
                    if (Row == null)
                    {
                        b--;
                        continue;
                    }
                    var thread = new GroupForumPost(Row);
                    Threads.Add(thread);
                    i++;
                }
                Threads = Threads.OrderByDescending(x => x.Pinned).ToList();
                var Message = new ServerMessage(LibraryParser.OutgoingRequest("GroupForumThreadRootMessageComposer"));
                Message.AppendInteger(GroupId);
                Message.AppendInteger(StartIndex);
                Message.AppendInteger(b);
                foreach (GroupForumPost Thread in Threads)
                {
                    Message.AppendInteger(Thread.Id);
                    Message.AppendInteger(Thread.PosterId);
                    Message.AppendString(Thread.PosterName);
                    Message.AppendString(Thread.Subject);
                    Message.AppendBool(Thread.Pinned);
                    Message.AppendBool(Thread.Locked);
                    Message.AppendInteger((global::Plus.Plus.GetUnixTimeStamp() - Thread.Timestamp));
                    Message.AppendInteger(Thread.MessageCount + 1);
                    Message.AppendInteger(0);
                    Message.AppendInteger(0);
                    Message.AppendInteger(0);
                    Message.AppendString("");
                    Message.AppendInteger((global::Plus.Plus.GetUnixTimeStamp() - Thread.Timestamp));
                    Message.AppendByte((Thread.Hidden) ? 10 : 1);
                    Message.AppendInteger(0);
                    Message.AppendString(Thread.Hider);
                    Message.AppendInteger(0);
                }
                this.Session.SendMessage(Message);
            }
        }

        /// <summary>
        /// Gets the group forum data.
        /// </summary>
        internal void GetGroupForumData()
        {
            uint GroupId = this.Request.GetUInteger();
            Guild Group = global::Plus.Plus.GetGame().GetGroupManager().GetGroup(GroupId);
            if (Group == null || !Group.HasForum)
                return;
            this.Session.SendMessage(Group.ForumDataMessage(this.Session.GetHabbo().Id));
        }

        /// <summary>
        /// Gets the group forums.
        /// </summary>
        internal void GetGroupForums()
        {
            int SelectType = this.Request.GetInteger();
            int StartIndex = this.Request.GetInteger();
            int EndIndex = this.Request.GetInteger();
            var Message = new ServerMessage(LibraryParser.OutgoingRequest("GroupForumListingsMessageComposer"));
            Message.AppendInteger(SelectType);
            if (SelectType < 0 || SelectType > 2)
            {
                Message.AppendInteger(0);
                Message.AppendInteger(0);
                Message.AppendInteger(0);
                this.Session.SendMessage(Message);
                return;
            }
            var GroupList = new List<Guild>();
            var FinalGroupList = new List<Guild>();
            switch (SelectType)
            {
                case 0:
                case 1:
                    using (IQueryAdapter dbClient = global::Plus.Plus.GetDatabaseManager().GetQueryReactor())
                    {
                        dbClient.SetQuery("SELECT id FROM groups_data WHERE has_forum = '1' AND forum_Messages_count > 0 ORDER BY forum_Messages_count DESC LIMIT 75;");
                        DataTable Table = dbClient.GetTable();
                        if (Table == null)
                            return;
                        Message.AppendInteger(Table.Rows.Count);
                        Message.AppendInteger(StartIndex);
                        int b = (Table.Rows.Count <= 20) ? Table.Rows.Count : 20;
                        var Groups = new List<Guild>();
                        int i = 1;
                        while (i <= b)
                        {
                            DataRow Row = Table.Rows[i - 1];
                            if (Row == null)
                            {
                                b--;
                                continue;
                            }
                            uint GroupId = uint.Parse(Row["id"].ToString());
                            Guild Guild = global::Plus.Plus.GetGame().GetGroupManager().GetGroup(GroupId);
                            if (Guild == null || !Guild.HasForum)
                            {
                                b--;
                                continue;
                            }
                            Groups.Add(Guild);
                            i++;
                        }
                        Message.AppendInteger(b);
                        foreach (Guild Group in Groups)
                            Group.SerializeForumRoot(Message);
                        this.Session.SendMessage(Message);
                    }
                    break;

                case 2:
                    foreach (GroupUser GU in this.Session.GetHabbo().UserGroups)
                    {
                        Guild AGroup = global::Plus.Plus.GetGame().GetGroupManager().GetGroup(GU.GroupId);
                        if (AGroup == null)
                            continue;
                        else if (AGroup.HasForum)
                            GroupList.Add(AGroup);
                    }
                    try
                    {
                        FinalGroupList = GroupList.OrderByDescending(x => x.ForumMessagesCount).Skip(StartIndex).Take(20).ToList();
                        Message.AppendInteger(GroupList.Count);
                        Message.AppendInteger(StartIndex);
                        Message.AppendInteger(FinalGroupList.Count);
                    }
                    catch
                    {
                        Message.AppendInteger(0);
                        Message.AppendInteger(0);
                        Message.AppendInteger(0);
                        this.Session.SendMessage(Message);
                        return;
                    }
                    foreach (Guild Group in FinalGroupList)
                        Group.SerializeForumRoot(Message);
                    this.Session.SendMessage(Message);
                    break;
            }
        }

        /// <summary>
        /// Manages the group.
        /// </summary>
        internal void ManageGroup()
        {
            var groupId = Request.GetUInteger();
            var group = Plus.GetGame().GetGroupManager().GetGroup(groupId);
            if (group == null)
                return;
            if (!@group.Admins.ContainsKey(Session.GetHabbo().Id) && @group.CreatorId != Session.GetHabbo().Id &&
                Session.GetHabbo().Rank < 7)
                return;
            Response.Init(LibraryParser.OutgoingRequest("GroupDataEditMessageComposer"));
            Response.AppendInteger(0);
            Response.AppendBool(true);
            Response.AppendInteger(@group.Id);
            Response.AppendString(@group.Name);
            Response.AppendString(@group.Description);
            Response.AppendInteger(@group.RoomId);
            Response.AppendInteger(@group.Colour1);
            Response.AppendInteger(@group.Colour2);
            Response.AppendInteger(@group.State);
            Response.AppendInteger(@group.AdminOnlyDeco);
            Response.AppendBool(false);
            Response.AppendString("");
            var array = @group.Badge.Replace("b", "").Split('s');
            Response.AppendInteger(5);
            var num = (5 - array.Length);

            var num2 = 0;
            var array2 = array;
            foreach (var text in array2)
            {
                Response.AppendInteger((text.Length >= 6)
                    ? uint.Parse(text.Substring(0, 3))
                    : uint.Parse(text.Substring(0, 2)));
                Response.AppendInteger((text.Length >= 6)
                    ? uint.Parse(text.Substring(3, 2))
                    : uint.Parse(text.Substring(2, 2)));
                if (text.Length < 5)
                    Response.AppendInteger(0);
                else if (text.Length >= 6)
                    Response.AppendInteger(uint.Parse(text.Substring(5, 1)));
                else
                    Response.AppendInteger(uint.Parse(text.Substring(4, 1)));
            }
            while (num2 != num)
            {
                Response.AppendInteger(0);
                Response.AppendInteger(0);
                Response.AppendInteger(0);
                num2++;
            }
            Response.AppendString(@group.Badge);
            Response.AppendInteger(@group.Members.Count);
            SendResponse();
        }

        /// <summary>
        /// Updates the name of the group.
        /// </summary>
        internal void UpdateGroupName()
        {
            uint num = this.Request.GetUInteger();
            string text = this.Request.GetString();
            string text2 = this.Request.GetString();
            Guild group = global::Plus.Plus.GetGame().GetGroupManager().GetGroup(num);
            if (group == null)
                return;
            if (group.CreatorId != this.Session.GetHabbo().Id)
                return;
            using (IQueryAdapter queryReactor = global::Plus.Plus.GetDatabaseManager().GetQueryReactor())
            {
                queryReactor.SetQuery(string.Format("UPDATE groups_data SET `name`=@name, `desc`=@desc WHERE id={0} LIMIT 1", num));
                queryReactor.AddParameter("name", text);
                queryReactor.AddParameter("desc", text2);
                queryReactor.RunQuery();
            }
            group.Name = text;
            group.Description = text2;
            global::Plus.Plus.GetGame().GetGroupManager().SerializeGroupInfo(group, this.Response, this.Session, this.Session.GetHabbo().CurrentRoom, false);
        }

        /// <summary>
        /// Updates the group badge.
        /// </summary>
        internal void UpdateGroupBadge()
        {
            uint guildId = this.Request.GetUInteger();
            Guild guild = global::Plus.Plus.GetGame().GetGroupManager().GetGroup(guildId);
            if (guild != null)
            {
                Room room = global::Plus.Plus.GetGame().GetRoomManager().GetRoom(guild.RoomId);
                if (room != null)
                {
                    this.Request.GetInteger();
                    int Base = this.Request.GetInteger();
                    int baseColor = this.Request.GetInteger();
                    this.Request.GetInteger();
                    var guildStates = new List<int>();

                    for (int i = 0; i < 12; i++)
                    {
                        int item = this.Request.GetInteger();
                        guildStates.Add(item);
                    }
                    string badge = global::Plus.Plus.GetGame().GetGroupManager().GenerateGuildImage(Base, baseColor, guildStates);
                    guild.Badge = badge;
                    this.Response.Init(LibraryParser.OutgoingRequest("RoomGroupMessageComposer"));
                    this.Response.AppendInteger(room.LoadedGroups.Count);
                    foreach (KeyValuePair<uint, string> current2 in room.LoadedGroups)
                    {
                        this.Response.AppendInteger(current2.Key);
                        this.Response.AppendString(current2.Value);
                    }
                    room.SendMessage(this.Response);
                    global::Plus.Plus.GetGame().GetGroupManager().SerializeGroupInfo(guild, this.Response, this.Session, room, false);
                    using (IQueryAdapter queryReactor = global::Plus.Plus.GetDatabaseManager().GetQueryReactor())
                    {
                        queryReactor.SetQuery(string.Format("UPDATE groups_data SET badge = @badgi WHERE id = {0}", guildId));
                        queryReactor.AddParameter("badgi", badge);
                        queryReactor.RunQuery();
                    }

                    if (this.Session.GetHabbo().CurrentRoom != null)
                    {
                        this.Session.GetHabbo().CurrentRoom.LoadedGroups[guildId] = guild.Badge;
                        this.Response.Init(LibraryParser.OutgoingRequest("RoomGroupMessageComposer"));
                        this.Response.AppendInteger(this.Session.GetHabbo().CurrentRoom.LoadedGroups.Count);
                        foreach (KeyValuePair<uint, string> current in this.Session.GetHabbo().CurrentRoom.LoadedGroups)
                        {
                            this.Response.AppendInteger(current.Key);
                            this.Response.AppendString(current.Value);
                        }
                        this.Session.GetHabbo().CurrentRoom.SendMessage(this.Response);
                        global::Plus.Plus.GetGame().GetGroupManager().SerializeGroupInfo(guild, this.Response, this.Session, this.Session.GetHabbo().CurrentRoom, false);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the group colours.
        /// </summary>
        internal void UpdateGroupColours()
        {
            uint groupId = this.Request.GetUInteger();
            int num = this.Request.GetInteger();
            int num2 = this.Request.GetInteger();
            Guild group = global::Plus.Plus.GetGame().GetGroupManager().GetGroup(groupId);
            if (group == null)
                return;
            if (group.CreatorId != this.Session.GetHabbo().Id)
                return;
            using (IQueryAdapter queryReactor = global::Plus.Plus.GetDatabaseManager().GetQueryReactor())
            {
                queryReactor.RunFastQuery(string.Concat(new object[]
                {
                    "UPDATE groups_data SET colour1= ",
                    num,
                    ", colour2=",
                    num2,
                    " WHERE id=",
                    group.Id,
                    " LIMIT 1"
                }));
            }
            group.Colour1 = num;
            group.Colour2 = num2;
            global::Plus.Plus.GetGame().GetGroupManager().SerializeGroupInfo(group, this.Response, this.Session, this.Session.GetHabbo().CurrentRoom, false);
        }

        /// <summary>
        /// Updates the group settings.
        /// </summary>
        internal void UpdateGroupSettings()
        {
            uint groupId = this.Request.GetUInteger();
            uint num = this.Request.GetUInteger();
            uint num2 = this.Request.GetUInteger();
            Guild group = global::Plus.Plus.GetGame().GetGroupManager().GetGroup(groupId);
            if (group == null)
                return;
            if (group.CreatorId != this.Session.GetHabbo().Id)
                return;
            using (IQueryAdapter queryReactor = global::Plus.Plus.GetDatabaseManager().GetQueryReactor())
            {
                queryReactor.RunFastQuery(string.Concat(new object[]
                {
                    "UPDATE groups_data SET state='",
                    num,
                    "', admindeco='",
                    num2,
                    "' WHERE id=",
                    group.Id,
                    " LIMIT 1"
                }));
            }
            group.State = num;
            group.AdminOnlyDeco = num2;
            Room room = global::Plus.Plus.GetGame().GetRoomManager().GetRoom(group.RoomId);
            if (room != null)
            {
                foreach (RoomUser current in room.GetRoomUserManager().GetRoomUsers())
                {
                    if (room.RoomData.OwnerId != current.UserId && !group.Admins.ContainsKey(current.UserId) && group.Members.ContainsKey(current.UserId))
                    {
                        if (num2 == 1u)
                        {
                            current.RemoveStatus("flatctrl 1");
                            this.Response.Init(LibraryParser.OutgoingRequest("RoomRightsLevelMessageComposer"));
                            this.Response.AppendInteger(0);
                            current.GetClient().SendMessage(this.GetResponse());
                        }
                        else
                        {
                            if (num2 == 0u && !current.Statusses.ContainsKey("flatctrl 1"))
                            {
                                current.AddStatus("flatctrl 1", "");
                                this.Response.Init(LibraryParser.OutgoingRequest("RoomRightsLevelMessageComposer"));
                                this.Response.AppendInteger(1);
                                current.GetClient().SendMessage(this.GetResponse());
                            }
                        }
                        current.UpdateNeeded = true;
                    }
                }
            }
            global::Plus.Plus.GetGame().GetGroupManager().SerializeGroupInfo(group, this.Response, this.Session, this.Session.GetHabbo().CurrentRoom, false);
        }

        /// <summary>
        /// Requests the leave group.
        /// </summary>
        internal void RequestLeaveGroup()
        {
            uint GroupId = this.Request.GetUInteger();
            uint UserId = this.Request.GetUInteger();
            Guild Guild = global::Plus.Plus.GetGame().GetGroupManager().GetGroup(GroupId);
            if (Guild == null || Guild.CreatorId == UserId)
                return;
            if (UserId == this.Session.GetHabbo().Id || Guild.Admins.ContainsKey(this.Session.GetHabbo().Id))
            {
                this.Response.Init(LibraryParser.OutgoingRequest("GroupAreYouSureMessageComposer"));
                this.Response.AppendInteger(UserId);
                this.Response.AppendInteger(0);
                this.SendResponse();
            }
        }

        /// <summary>
        /// Confirms the leave group.
        /// </summary>
        internal void ConfirmLeaveGroup()
        {
            uint Guild = this.Request.GetUInteger();
            uint UserId = this.Request.GetUInteger();
            Guild byeGuild = global::Plus.Plus.GetGame().GetGroupManager().GetGroup(Guild);
            if (byeGuild == null)
                return;
            else if (byeGuild.CreatorId == UserId)
            {
                this.Session.SendNotif(Plus.GetLanguage().GetVar("user_room_video_true"));
                return;
            }
            int type = 3;
            if (UserId == this.Session.GetHabbo().Id || byeGuild.Admins.ContainsKey(this.Session.GetHabbo().Id))
            {
                GroupUser memberShip;
                if (byeGuild.Members.ContainsKey(UserId))
                {
                    memberShip = byeGuild.Members[UserId];
                    type = 3;
                    this.Session.GetHabbo().UserGroups.Remove(memberShip);
                    byeGuild.Members.Remove(UserId);
                }
                else if (byeGuild.Admins.ContainsKey(UserId))
                {
                    memberShip = byeGuild.Admins[UserId];
                    type = 1;
                    this.Session.GetHabbo().UserGroups.Remove(memberShip);
                    byeGuild.Admins.Remove(UserId);
                }
                else
                    return;
                using (IQueryAdapter queryReactor = global::Plus.Plus.GetDatabaseManager().GetQueryReactor())
                {
                    queryReactor.RunFastQuery(string.Concat(new object[]
                    {
                        "DELETE FROM groups_members WHERE user_id=",
                        UserId,
                        " AND group_id=",
                        Guild,
                        " LIMIT 1"
                    }));
                }
                HabboHotel.Users.Habbo byeUser = global::Plus.Plus.GetHabboById(UserId);
                if (byeUser != null)
                {
                    this.Response.Init(LibraryParser.OutgoingRequest("GroupConfirmLeaveMessageComposer"));
                    this.Response.AppendInteger(Guild);
                    this.Response.AppendInteger(type);
                    this.Response.AppendInteger(byeUser.Id);
                    this.Response.AppendString(byeUser.UserName);
                    this.Response.AppendString(byeUser.Look);
                    this.Response.AppendString("");
                    this.SendResponse();
                }
                if (byeUser != null && byeUser.FavouriteGroup == Guild)
                {
                    byeUser.FavouriteGroup = 0;
                    using (IQueryAdapter queryreactor2 = global::Plus.Plus.GetDatabaseManager().GetQueryReactor())
                        queryreactor2.RunFastQuery(string.Format("UPDATE users_stats SET favourite_group=0 WHERE id={0} LIMIT 1", UserId));
                    Room Room = this.Session.GetHabbo().CurrentRoom;

                    this.Response.Init(LibraryParser.OutgoingRequest("FavouriteGroupMessageComposer"));
                    this.Response.AppendInteger(byeUser.Id);
                    if (Room != null)
                        Room.SendMessage(this.Response);
                    else
                        this.SendResponse();
                }

                this.Response.Init(LibraryParser.OutgoingRequest("GroupRequestReloadMessageComposer"));
                this.Response.AppendInteger(Guild);
                this.SendResponse();
            }
        }

        /// <summary>
        /// News the method.
        /// </summary>
        /// <param name="current2">The current2.</param>
        private void NewMethod(RoomData current2)
        {
            this.Response.AppendInteger(current2.Id);
            this.Response.AppendString(current2.Name);
            this.Response.AppendBool(false);
        }

        internal void UpdateForumSettings()
        {
            /*uint guild = Request.GetUInteger();
            int whoCanRead = Request.GetInteger();
            int whoCanPost = Request.GetInteger();
            int whoCanThread = Request.GetInteger();
            int whoCanMod = Request.GetInteger();
            Guild group = Plus.GetGame().GetGroupManager().GetGroup(guild);
            if (group == null) return;
            group.WhoCanRead = whoCanRead;
            group.WhoCanPost = whoCanPost;
            group.WhoCanThread = whoCanThread;
            group.WhoCanMod = whoCanMod;
            using (IQueryAdapter queryReactor = Plus.GetDatabaseManager().GetQueryReactor())
            {
                queryReactor.SetQuery("UPDATE rp_jobs SET who_can_read = @who_can_read, who_can_post = @who_can_post, who_can_thread = @who_can_thread, who_can_mod = @who_can_mod WHERE id = @group_id");
                queryReactor.AddParameter("group_id", group.Id);
                queryReactor.AddParameter("who_can_read", whoCanRead);
                queryReactor.AddParameter("who_can_post", whoCanPost);
                queryReactor.AddParameter("who_can_thread", whoCanThread);
                queryReactor.AddParameter("who_can_mod", whoCanMod);
                queryReactor.RunQuery();
            }
            Session.SendMessage(group.ForumDataMessage(Session.GetHabbo().Id));*/
        }
        internal void DeleteGroup()
        {

        }
    }
}