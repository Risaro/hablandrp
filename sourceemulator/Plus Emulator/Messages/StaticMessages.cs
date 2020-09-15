using Plus.Messages.Parsers;
using System.Collections.Generic;

namespace Plus.Messages
{
    /// <summary>
    /// Enum StaticMessage
    /// </summary>
    internal enum StaticMessage
    {
        /// <summary>
        /// The error cant set item
        /// </summary>
        ErrorCantSetItem,

        /// <summary>
        /// The error cant set not owner
        /// </summary>
        ErrorCantSetNotOwner,

        /// <summary>
        /// The kicked
        /// </summary>
        Kicked,

        /// <summary>
        /// The new way to open commands list
        /// </summary>
        NewWayToOpenCommandsList,

        /// <summary>
        /// The user not found
        /// </summary>
        UserNotFound,

        AdviceMaxItems,
        AdvicePurchaseMaxItems
    }

    /// <summary>
    /// Class StaticMessagesManager.
    /// </summary>
    internal static class StaticMessagesManager
    {
        /// <summary>
        /// The cache
        /// </summary>
        private static readonly Dictionary<StaticMessage, byte[]> Cache = new Dictionary<StaticMessage, byte[]>();

        /// <summary>
        /// Loads this instance.
        /// </summary>
        public static void Load()
        {
            Cache.Clear();

            var message = new ServerMessage(LibraryParser.OutgoingRequest("SuperNotificationMessageComposer"));
            message.AppendString("furni_placement_error");
            message.AppendInteger(1);
            message.AppendString("message");
            message.AppendString("${room.error.cant_set_item}");
            Cache.Add(StaticMessage.ErrorCantSetItem, message.GetReversedBytes());

            message = new ServerMessage(LibraryParser.OutgoingRequest("SuperNotificationMessageComposer"));
            message.AppendString("furni_placement_error");
            message.AppendInteger(1);
            message.AppendString("message");
            message.AppendString("${room.error.cant_set_not_owner}");
            Cache.Add(StaticMessage.ErrorCantSetNotOwner, message.GetReversedBytes());

            message = new ServerMessage(LibraryParser.OutgoingRequest("SuperNotificationMessageComposer"));
            message.AppendString("game_promo_small");
            message.AppendInteger(4);
            message.AppendString("title");
            message.AppendString("${generic.notice}");
            message.AppendString("message");
            message.AppendString("Now, the commands page opens in a different way");
            message.AppendString("linkUrl");
            message.AppendString("event:habbopages/chat/newway");
            message.AppendString("linkTitle");
            message.AppendString("${mod.alert.link}");
            Cache.Add(StaticMessage.NewWayToOpenCommandsList, message.GetReversedBytes());

            message = new ServerMessage(LibraryParser.OutgoingRequest("SuperNotificationMessageComposer"));
            message.AppendString("");
            message.AppendInteger(4);
            message.AppendString("title");
            message.AppendString("${generic.notice}");
            message.AppendString("message");
            message.AppendString("${catalog.gift_wrapping.receiver_not_found.title}");
            message.AppendString("linkUrl");
            message.AppendString("event:");
            message.AppendString("linkTitle");
            message.AppendString("ok");
            Cache.Add(StaticMessage.UserNotFound, message.GetReversedBytes());

            message = new ServerMessage(LibraryParser.OutgoingRequest("SuperNotificationMessageComposer"));
            message.AppendString("");
            message.AppendInteger(4);
            message.AppendString("title");
            message.AppendString("${generic.notice}");
            message.AppendString("message");
            message.AppendString("You've exceeded the maximum furnis in inventory. Only furnis 2800 will show if you want to see the others, places some Furni in your rooms.");
            message.AppendString("linkUrl");
            message.AppendString("event:");
            message.AppendString("linkTitle");
            message.AppendString("ok");
            Cache.Add(StaticMessage.AdviceMaxItems, message.GetReversedBytes());

            message = new ServerMessage(LibraryParser.OutgoingRequest("SuperNotificationMessageComposer"));
            message.AppendString("");
            message.AppendInteger(4);
            message.AppendString("title");
            message.AppendString("${generic.notice}");
            message.AppendString("message");
            message.AppendString("You've exceeded the maximum furnis in inventory. You can not buy more until you desagas some furnis.");
            message.AppendString("linkUrl");
            message.AppendString("event:");
            message.AppendString("linkTitle");
            message.AppendString("ok");
            Cache.Add(StaticMessage.AdvicePurchaseMaxItems, message.GetReversedBytes());
        }

        /// <summary>
        /// Gets the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>System.Byte[].</returns>
        public static byte[] Get(StaticMessage type)
        {
            return Cache[type];
        }
    }
}