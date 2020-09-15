using Plus.HabboHotel.GameClients;
using Plus.Messages.Parsers;
using Plus.HabboHotel.Roleplay.Jobs;
using Plus.HabboHotel.Roleplay.Jobs.Space;
using Plus.HabboHotel.Roleplay.Jobs.Farming;
using Plus.HabboHotel.Roleplay.Casino.Slots;
using Plus.HabboHotel.Roleplay.Gangs;
using Plus.HabboHotel.Roleplay.Combat;
using Plus.HabboHotel.Roleplay.Apartments;
using Plus.HabboHotel.Roleplay;
using Plus.HabboHotel.Roleplay.Misc;
using Plus.HabboHotel.Roleplay.Radio;
using Plus.HabboHotel.Roleplay.Timers;
using System;

namespace Plus.Messages.Handlers
{
    /// <summary>
    /// Class GameClientMessageHandler.
    /// </summary>
    partial class GameClientMessageHandler
    {
        /// <summary>
        /// Calls the guide.
        /// </summary>
        internal void CallGuide()
        {
            Request.GetBool(); //false
            var userId = Request.GetIntegerFromString();
            var message = Request.GetString();
            var guideManager = Plus.GetGame().GetGuideManager();
            if (guideManager.GuidesCount <= 0)
            {
                Response.Init(LibraryParser.OutgoingRequest("OnGuideSessionError")); //onGuideSessionError
                Response.AppendInteger(0); //Errorcode
                SendResponse();
                return;
            }
            var guide = guideManager.GetRandomGuide();
            // Message pour la personne qui demande
            var onGuideSessionAttached =
                new ServerMessage(LibraryParser.OutgoingRequest("OnGuideSessionAttachedMessageComposer"));
            onGuideSessionAttached.AppendBool(false); //false
            onGuideSessionAttached.AppendInteger(userId);
            onGuideSessionAttached.AppendString(message);
            onGuideSessionAttached.AppendInteger(30); //Temps moyen
            Session.SendMessage(onGuideSessionAttached);
            // Message pour le guide
            var onGuideSessionAttached2 =
                new ServerMessage(LibraryParser.OutgoingRequest("OnGuideSessionAttachedMessageComposer"));
            onGuideSessionAttached2.AppendBool(true); //false
            onGuideSessionAttached2.AppendInteger(userId);
            onGuideSessionAttached2.AppendString(message);
            onGuideSessionAttached2.AppendInteger(15); //Temps moyen
            guide.SendMessage(onGuideSessionAttached2);
            guide.GetHabbo().GuideOtherUser = Session;
            Session.GetHabbo().GuideOtherUser = guide;
        }

        /// <summary>
        /// Answers the guide request.
        /// </summary>
        internal void AnswerGuideRequest()
        {
            var state = Request.GetBool();
            // Accept button : true
            // Reject button : false
            if (!state)
                return;
            var requester = Session.GetHabbo().GuideOtherUser;
            var message = new ServerMessage(LibraryParser.OutgoingRequest("OnGuideSessionStartedMessageComposer"));
            message.AppendInteger(requester.GetHabbo().Id); //userid
            message.AppendString(requester.GetHabbo().UserName); //Username
            message.AppendString(requester.GetHabbo().Look); //look 1
            message.AppendInteger(Session.GetHabbo().Id); //Id du guide ?
            message.AppendString(Session.GetHabbo().UserName);
            message.AppendString(Session.GetHabbo().Look);
            requester.SendMessage(message);
            Session.SendMessage(message);
        }

        /// <summary>
        /// Cancels the call guide.
        /// </summary>
        internal void CancelCallGuide()
        {
            /*bool Unknown = Request.PopWiredBoolean();
            int UserId = Request.PopFixedInt32();
            string Message = Request.PopFixedString();
            Console.WriteLine(Message);
            GuideManager GuideManager = MercuryEnvironment.GetGame().GetGuideManager();
            //Habbo Guide = GuideManager.GetRandomGuide();*/
            Response.Init(3485);
            SendResponse();
        }

        /// <summary>
        /// Opens the guide tool.
        /// </summary>
        internal void OpenGuideTool()
        {
            if (Session.GetRoleplay().JobId == 3)
            {
                var guideManager = Plus.GetGame().GetGuideManager();
                var onDuty = Request.GetBool();
                Request.GetBool(); // guide
                Request.GetBool(); // helper
                Request.GetBool(); // guardian

                if (onDuty)
                {
                    if (Session.GetRoleplay().JobHasRights("police") || Session.GetRoleplay().JobHasRights("swat"))
                    {
                        if (RoleplayManager.PurgeTime)
                        {
                            Session.SendWhisperBubble("You cannot work as an officer during a purge!", 1);
                            onDuty = false;
                            return;
                        }
                        if (Session.GetRoleplay().IsNoob)
                        {
                            Session.SendWhisperBubble("You cannot fulfil your duties in Law Enforcement with God Protection Enabled!", 1);
                            onDuty = false;
                            return;
                        }
                        if (Session.GetHabbo().CurrentRoom.RoomData.Description.Contains("NOCOP"))
                        {
                            Session.SendWhisperBubble("Sorry, but this is a NOCOP zone", 1);
                            onDuty = false;
                            return;
                        }
                    }
                    if (Session.GetRoleplay().SentHome)
                    {
                        Session.SendWhisperBubble("You were sent home by a manager. You must wait " + Session.GetRoleplay().SendHomeTimer + " minutes to begin working again", 1);
                        onDuty = false;
                        return;
                    }
                    if (Session.GetRoleplay().Working)
                    {
                        Session.SendWhisperBubble("You are already working!", 1);
                        onDuty = false;
                        return;
                    }
                    if (Session.GetRoleplay().ATMRobbery)
                    {
                        Session.SendWhisperBubble("You can't work while robbing an ATM!", 1);
                        onDuty = false;
                        return;
                    }
                    if (Session.GetRoleplay().Robbery)
                    {
                        Session.SendWhisperBubble("You can't work while robbing the vault!", 1);
                        onDuty = false;
                        return;
                    }
                    if (Session.GetRoleplay().Learning)
                    {
                        Session.SendWhisperBubble("You can't work while learning!", 1);
                        onDuty = false;
                        return;
                    }
                    if (Session.GetRoleplay().WeightLifting)
                    {
                        Session.SendWhisperBubble("You cannot work while weight lifting!", 1);
                        onDuty = false;
                        return;
                    }
                    if (Session.GetRoleplay().WorkingOut)
                    {
                        Session.SendWhisperBubble("You cannot work while working out!", 1);
                        onDuty = false;
                        return;
                    }
                    if (Session.GetRoleplay().Dead)
                    {
                        Session.SendWhisperBubble("You cannot work while you are dead!", 1);
                        onDuty = false;
                        return;
                    }
                    if (Session.GetRoleplay().Jailed)
                    {
                        Session.SendWhisperBubble("You cannot work while you are jailed!", 1);
                        onDuty = false;
                        return;
                    }
                    
                    if (Session.GetRoleplay().JobRank == 1 || Session.GetRoleplay().JobRank == 2 && Session.GetRoleplay().JobId == 3)
                        guideManager.AddGuide(Session);
                    Session.GetHabbo().GetRoomUser().ApplyEffect(178);

                    if (Session.GetRoleplay().JobRank > 2 && Session.GetRoleplay().JobRank < 8 && Session.GetRoleplay().JobId == 3)
                        guideManager.AddOfficer(Session);
                        Session.GetHabbo().GetRoomUser().ApplyEffect(178);

                    if (Session.GetRoleplay().JobRank == 8 || Session.GetRoleplay().JobRank == 9 && Session.GetRoleplay().JobId == 3)
                        guideManager.AddChief(Session);
                        Session.GetHabbo().GetRoomUser().ApplyEffect(178);

                    int JobId = Session.GetRoleplay().JobId;
                    int JobRank = Session.GetRoleplay().JobRank;
                    string JobName = JobManager.JobData[JobId].Name;
                    string RankName = JobManager.JobRankData[JobId, JobRank].Name;

                    Session.GetRoleplay().FigBeforeWork = Session.GetHabbo().Look;
                    Session.GetRoleplay().MottBeforeWork = Session.GetHabbo().Motto;

                    if (Session.GetRoleplay().JobId != 1 && !JobManager.JobRankData[JobId, JobRank].MaleFig.Contains("*") && !JobManager.JobRankData[JobId, JobRank].FemaleFig.Contains("*")) // Set Figure if not Unemployed
                    {
                        if (!JobManager.JobRankData[JobId, JobRank].MaleFig.ToLower().Contains("none") && !JobManager.JobRankData[JobId, JobRank].FemaleFig.ToLower().Contains("none"))
                        {
                            if (Session.GetHabbo().Gender.ToLower().StartsWith("m"))
                            {
                                Session.GetHabbo().Look = RoleplayManager.SplitFigure(Session.GetHabbo().Look) + JobManager.JobRankData[JobId, JobRank].MaleFig;
                                Session.GetRoleplay().FigWork = RoleplayManager.SplitFigure(Session.GetHabbo().Look) + JobManager.JobRankData[JobId, JobRank].MaleFig;
                            }
                            else
                            {
                                Session.GetHabbo().Look = RoleplayManager.SplitFigure(Session.GetHabbo().Look) + JobManager.JobRankData[JobId, JobRank].FemaleFig;
                                Session.GetRoleplay().FigWork = RoleplayManager.SplitFigure(Session.GetHabbo().Look) + JobManager.JobRankData[JobId, JobRank].FemaleFig;
                            }
                        }
                    }
                    Session.GetHabbo().Motto = "[Working] " + JobName + " " + RankName;
                    Session.GetRoleplay().RefreshVals();
                    Session.GetRoleplay().Working = true;
                    Session.GetRoleplay().workingTimer = new workingTimer(Session);
                    Session.GetRoleplay().SaveJobComponents();
                    RoleplayManager.Shout(Session, "*Starts Working*");
                    Session.SendWhisperBubble("You have " + Session.GetRoleplay().workingTimer.getTime() + " minute(s) left until you receive your next paycheck", 1);
                    Session.GetRoleplay().MultiCoolDown["work_cooldown"] = 5;
                    Session.GetRoleplay().CheckingMultiCooldown = true;
                    }
                else
                {
                    if (Session.GetRoleplay().Working)
                    {
                        Session.GetRoleplay().StopWork(true);

                        if (Session.GetRoleplay().Equiped != null)
                        {
                            if (Session.GetRoleplay().Equiped.ToLower().Equals("npa"))
                            {
                                Session.GetHabbo().GetRoomUser().ApplyEffect(0);
                                Session.GetRoleplay().Equiped = null;
                                onDuty = false;
                                Session.GetRoleplay().Working = false;
                            }
                        }
                    }
                    guideManager.RemoveGuide(Session);
                    guideManager.RemoveOfficer(Session);
                    guideManager.RemoveChief(Session);
                    Session.GetHabbo().GetRoomUser().ApplyEffect(0);

                }
                Session.GetHabbo().OnDuty = onDuty;
                Response.Init(LibraryParser.OutgoingRequest("HelperToolConfigurationMessageComposer"));
                Response.AppendBool(onDuty); // on duty
                Response.AppendInteger(guideManager.GuidesCount); // guides
                Response.AppendInteger(guideManager.OfficerCount); // helpers
                Response.AppendInteger(guideManager.ChiefCount); // guardians
                SendResponse();
            }
            else
            {
                Session.SendWhisper("You must be police to use the Police Tools");
            }
        }

        /// <summary>
        /// Invites to room.
        /// </summary>
        internal void InviteToRoom()
        {
            var requester = Session.GetHabbo().GuideOtherUser;
            var room = Session.GetHabbo().CurrentRoom;
            var message =
                new ServerMessage(LibraryParser.OutgoingRequest("OnGuideSessionInvitedToGuideRoomMessageComposer"));
            //onGuideSessionInvitedToGuideRoom
            if (room == null)
            {
                message.AppendInteger(0); //id de l'appart
                message.AppendString("");
            }
            else
            {
                message.AppendInteger(room.RoomId); //id de l'appart
                message.AppendString(room.RoomData.Name);
            }
            requester.SendMessage(message);
            Session.SendMessage(message);
        }

        /// <summary>
        /// Visits the room.
        /// </summary>
        internal void VisitRoom()
        {
            var requester = Session.GetHabbo().GuideOtherUser;
            var message = new ServerMessage(3916); //onGuideSessionRequesterRoom
            message.AppendInteger(requester.GetHabbo().CurrentRoomId);
            //requester.SendMessage(Message);
            Session.SendMessage(message);
        }

        /// <summary>
        /// Guides the speak.
        /// </summary>
        internal void GuideSpeak()
        {
            var message = Request.GetString();
            var requester = Session.GetHabbo().GuideOtherUser;
            var messageC = new ServerMessage(LibraryParser.OutgoingRequest("OnGuideSessionMsgMessageComposer"));
            //onGuideSessionMessage
            messageC.AppendString(message);
            messageC.AppendInteger(Session.GetHabbo().Id);
            requester.SendMessage(messageC);
            Session.SendMessage(messageC);
        }

        /// <summary>
        /// Closes the guide request.
        /// </summary>
        internal void CloseGuideRequest()
        {
            var requester = Session.GetHabbo().GuideOtherUser;
            var message = new ServerMessage(LibraryParser.OutgoingRequest("OnGuideSessionDetachedMessageComposer"));
            //onGuideSessionEnded
            message.AppendInteger(2); //0,1,2
            /* 0 : Erreur
             * 1 : c'est la personne qui demande qui a fermé
             * 2 : C'est le guide qui a fermé */
            requester.SendMessage(message);
            Session.SendMessage(message);
            requester.GetHabbo().GuideOtherUser = null;
            Session.GetHabbo().GuideOtherUser = null;
        }

        /// <summary>
        /// Guides the feedback.
        /// </summary>
        internal void GuideFeedback()
        {
            Request.GetBool(); // feedback
            //var guide = session.GetHabbo().GuideOtherUser;
            var message = new ServerMessage(LibraryParser.OutgoingRequest("OnGuideSessionDetachedMessageComposer"));
            //onGuideSessionEnded
            //requester.SendMessage(Message);
            Session.SendMessage(message);
        }

        /// <summary>
        /// Ambassadors the alert.
        /// </summary>
        internal void AmbassadorAlert()
        {
            if (Session.GetHabbo().Rank < Convert.ToUInt32(Plus.GetDbConfig().DbData["ambassador.minrank"])) return;
            uint userId = Request.GetUInteger();
            GameClient user = Plus.GetGame().GetClientManager().GetClientByUserId(userId);
            if (user == null) return;
            user.SendNotif("${notification.ambassador.alert.warning.message}", "${notification.ambassador.alert.warning.title}");
            Session.Shout("*Sends " + user.GetHabbo().UserName + " an ambassador alert*");
        }

        internal void BullyRequestAccept()
        {
            bool accepted = Request.GetBool();

        }
        internal void BullyRequestVote()
        {
            int vote = Request.GetInteger();
        }
        internal void BullyRequestNoVote()
        {

        }
    }
}