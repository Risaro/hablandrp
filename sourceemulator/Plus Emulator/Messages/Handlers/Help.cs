using Plus.HabboHotel.Support;
using Plus.Messages.Parsers;
using System.Collections.Generic;

namespace Plus.Messages.Handlers
{
    /// <summary>
    /// Class GameClientMessageHandler.
    /// </summary>
    partial class GameClientMessageHandler
    {
        /// <summary>
        /// Initializes the help tool.
        /// </summary>
        internal void InitHelpTool()
        {
            var errorOccured =
                Plus.GetGame().GetModerationTool().UsersHasPendingTicket(Session.GetHabbo().Id);
            if (errorOccured)
            {
                var Ticket = Plus.GetGame().GetModerationTool().GetPendingTicketForUser(Session.GetHabbo().Id);
                Response.Init(LibraryParser.OutgoingRequest("OpenHelpToolMessageComposer"));
                Response.AppendInteger(1);
                Response.AppendString(Ticket.TicketId.ToString());
                Response.AppendString(Ticket.Timestamp.ToString());
                Response.AppendString(Ticket.Message);
                SendResponse();
                return;
            }
            Response.Init(LibraryParser.OutgoingRequest("OpenHelpToolMessageComposer"));
            Response.AppendInteger(0);
            SendResponse();
        }

        /// <summary>
        /// Submits the help ticket.
        /// </summary>
        internal void SubmitHelpTicket()
        {
            if (Plus.GetGame().GetModerationTool().UsersHasPendingTicket(Session.GetHabbo().Id))
            {
                var ticket = Plus.GetGame().GetModerationTool().GetPendingTicketForUser(Session.GetHabbo().Id);
                Response.Init(LibraryParser.OutgoingRequest("OpenHelpToolMessageComposer"));
                Response.AppendInteger(1);
                Response.AppendString(ticket.TicketId.ToString());
                Response.AppendString(ticket.Timestamp.ToString());
                Response.AppendString(ticket.Message);
                SendResponse();
                return;
            }
            var message = Request.GetString();
            var category = Request.GetInteger();
            var reportedUser = Request.GetUInteger();
            Request.GetUInteger(); // roomId

            var messageCount = Request.GetInteger();

            var chats = new List<string>();

            for (var i = 0; i < messageCount; i++)
            {
                Request.GetInteger();
                chats.Add(Request.GetString());
            }
            Plus.GetGame()
                .GetModerationTool()
                .SendNewTicket(Session, category, 7, reportedUser, message, chats);
            Response.Init(LibraryParser.OutgoingRequest("TicketUserAlert"));
            Response.AppendInteger(0);
            SendResponse();
        }

        /// <summary>
        /// Deletes the pending CFH.
        /// </summary>
        internal void DeletePendingCfh()
        {
            if (!Plus.GetGame().GetModerationTool().UsersHasPendingTicket(Session.GetHabbo().Id))
                return;
            Plus.GetGame().GetModerationTool().DeletePendingTicketForUser(Session.GetHabbo().Id);

            Response.Init(LibraryParser.OutgoingRequest("OpenHelpToolMessageComposer"));
            Response.AppendInteger(0);
            SendResponse();
        }

        /// <summary>
        /// Mods the get user information.
        /// </summary>
        internal void ModGetUserInfo()
        {
            if (!Session.GetHabbo().HasFuse("fuse_mod"))
                return;
            var num = Request.GetUInteger();
            if (Plus.GetGame().GetClientManager().GetNameById(num) != "Unknown User")
            {
                Session.SendMessage(ModerationTool.SerializeUserInfo(num));
                return;
            }
            this.Session.SendNotif(Plus.GetLanguage().GetVar("help_information_error"));
        }

        /// <summary>
        /// Mods the get user chatlog.
        /// </summary>
        internal void ModGetUserChatlog()
        {
            if (!Session.GetHabbo().HasFuse("fuse_chatlogs"))
                return;
            Session.SendMessage(ModerationTool.SerializeUserChatlog(Request.GetUInteger()));
        }

        /// <summary>
        /// Mods the get room chatlog.
        /// </summary>
        internal void ModGetRoomChatlog()
        {
            if (!Session.GetHabbo().HasFuse("fuse_chatlogs"))
            {
                this.Session.SendNotif(Plus.GetLanguage().GetVar("help_information_error_rank_low"));
                return;
            }
            Request.GetInteger();
            var roomId = Request.GetUInteger();
            if (Plus.GetGame().GetRoomManager().GetRoom(roomId) != null)
                Session.SendMessage(ModerationTool.SerializeRoomChatlog(roomId));
        }

        /// <summary>
        /// Mods the get room tool.
        /// </summary>
        internal void ModGetRoomTool()
        {
            if (!Session.GetHabbo().HasFuse("fuse_mod"))
                return;
            var roomId = Request.GetUInteger();
            var data = Plus.GetGame().GetRoomManager().GenerateNullableRoomData(roomId);
            Session.SendMessage(ModerationTool.SerializeRoomTool(data));
        }

        /// <summary>
        /// Mods the pick ticket.
        /// </summary>
        internal void ModPickTicket()
        {
            if (!Session.GetHabbo().HasFuse("fuse_mod"))
                return;
            Request.GetInteger();
            var ticketId = Request.GetUInteger();
            Plus.GetGame().GetModerationTool().PickTicket(Session, ticketId);
        }

        /// <summary>
        /// Mods the release ticket.
        /// </summary>
        internal void ModReleaseTicket()
        {
            if (!Session.GetHabbo().HasFuse("fuse_mod"))
                return;
            var num = Request.GetInteger();

            {
                for (var i = 0; i < num; i++)
                {
                    var ticketId = Request.GetUInteger();
                    Plus.GetGame().GetModerationTool().ReleaseTicket(Session, ticketId);
                }
            }
        }

        /// <summary>
        /// Mods the close ticket.
        /// </summary>
        internal void ModCloseTicket()
        {
            if (!Session.GetHabbo().HasFuse("fuse_mod"))
                return;
            var result = Request.GetInteger();
            Request.GetInteger();
            var ticketId = Request.GetUInteger();
            Plus.GetGame().GetModerationTool().CloseTicket(Session, ticketId, result);
        }

        /// <summary>
        /// Mods the get ticket chatlog.
        /// </summary>
        internal void ModGetTicketChatlog()
        {
            if (!Session.GetHabbo().HasFuse("fuse_mod"))
                return;
            var ticket = Plus.GetGame().GetModerationTool().GetTicket(Request.GetUInteger());
            if (ticket == null)
                return;
            var roomData = Plus.GetGame().GetRoomManager().GenerateNullableRoomData(ticket.RoomId);
            if (roomData == null)
                return;
            Session.SendMessage(ModerationTool.SerializeTicketChatlog(ticket, roomData, ticket.Timestamp));
        }

        /// <summary>
        /// Mods the get room visits.
        /// </summary>
        internal void ModGetRoomVisits()
        {
            if (!Session.GetHabbo().HasFuse("fuse_mod"))
                return;
            var userId = Request.GetUInteger();
            Session.SendMessage(ModerationTool.SerializeRoomVisits(userId));
        }

        /// <summary>
        /// Mods the send room alert.
        /// </summary>
        internal void ModSendRoomAlert()
        {
            if (!Session.GetHabbo().HasFuse("fuse_alert"))
                return;
            Request.GetInteger();
            var str = Request.GetString();
            var serverMessage = new ServerMessage(LibraryParser.OutgoingRequest("SuperNotificationMessageComposer"));
            serverMessage.AppendString("admin");
            serverMessage.AppendInteger(3);
            serverMessage.AppendString("message");
            serverMessage.AppendString(string.Format("{0}\r\n\r\n- {1}", str, Session.GetHabbo().UserName));
            serverMessage.AppendString("link");
            serverMessage.AppendString("event:");
            serverMessage.AppendString("linkTitle");
            serverMessage.AppendString("ok");
            Session.GetHabbo().CurrentRoom.SendMessage(serverMessage);
        }

        /// <summary>
        /// Mods the perform room action.
        /// </summary>
        internal void ModPerformRoomAction()
        {
            if (!Session.GetHabbo().HasFuse("fuse_mod"))
                return;
            var roomId = Request.GetUInteger();
            var lockRoom = Request.GetInteger() == 1;
            var inappropriateRoom = Request.GetInteger() == 1;
            var kickUsers = Request.GetInteger() == 1;
            ModerationTool.PerformRoomAction(Session, roomId, kickUsers, lockRoom, inappropriateRoom, Response);
        }

        /// <summary>
        /// Mods the send user caution.
        /// </summary>
        internal void ModSendUserCaution()
        {
            if (!Session.GetHabbo().HasFuse("fuse_alert"))
                return;
            var userId = Request.GetUInteger();
            var message = Request.GetString();
            ModerationTool.AlertUser(Session, userId, message, true);
        }

        /// <summary>
        /// Mods the send user message.
        /// </summary>
        internal void ModSendUserMessage()
        {
            if (!Session.GetHabbo().HasFuse("fuse_alert"))
                return;
            var userId = Request.GetUInteger();
            var message = Request.GetString();
            ModerationTool.AlertUser(Session, userId, message, false);
        }

        /// <summary>
        /// Mods the mute user.
        /// </summary>
        internal void ModMuteUser()
        {
            if (!Session.GetHabbo().HasFuse("fuse_mute"))
                return;
            var userId = Request.GetUInteger();
            var message = Request.GetString();
            var clientByUserId = Plus.GetGame().GetClientManager().GetClientByUserId(userId);
            clientByUserId.GetHabbo().Mute();
            clientByUserId.SendNotif(message);
        }

        /// <summary>
        /// Mods the lock trade.
        /// </summary>
        internal void ModLockTrade()
        {
            if (!Session.GetHabbo().HasFuse("fuse_lock_trade"))
                return;
            var userId = Request.GetUInteger();
            var message = Request.GetString();
            var length = (Request.GetInteger() * 3600);

            ModerationTool.LockTrade(Session, userId, message, length);
        }

        /// <summary>
        /// Mods the kick user.
        /// </summary>
        internal void ModKickUser()
        {
            if (!Session.GetHabbo().HasFuse("fuse_kick"))
                return;
            var userId = Request.GetUInteger();
            var message = Request.GetString();
            ModerationTool.KickUser(Session, userId, message, false);
        }

        /// <summary>
        /// Mods the ban user.
        /// </summary>
        internal void ModBanUser()
        {
            if (!Session.GetHabbo().HasFuse("fuse_ban"))
                return;
            var userId = Request.GetUInteger();
            var message = Request.GetString();
            var length = (Request.GetInteger() * 3600);

            ModerationTool.BanUser(Session, userId, length, message);
        }
    }
}