using Plus.Database.Manager.Database.Session_Details.Interfaces;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.SoundMachine;
using Plus.HabboHotel.SoundMachine.Composers;
using Plus.Messages.Parsers;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Plus.Messages.Handlers
{
    /// <summary>
    /// Class GameClientMessageHandler.
    /// </summary>
    internal partial class GameClientMessageHandler
    {
        /// <summary>
        /// Retrieves the song identifier.
        /// </summary>
        internal void RetrieveSongID()
        {
            string text = this.Request.GetString();
            uint songId = SongManager.GetSongId(text);
            if (songId != 0u)
            {
                var serverMessage = new ServerMessage(LibraryParser.OutgoingRequest("RetrieveSongIDMessageComposer"));
                serverMessage.AppendString(text);
                serverMessage.AppendInteger(songId);
                this.Session.SendMessage(serverMessage);
            }
        }

        /// <summary>
        /// Gets the music data.
        /// </summary>
        internal void GetMusicData()
        {
            
            int num = this.Request.GetInteger();
            var list = new List<SongData>();

            {
                for (int i = 0; i < num; i++)
                {
                    SongData song = null;
                    try
                    {
                        song = SongManager.GetSong(this.Request.GetUInteger());
                    }
                    catch(Exception e)
                    {
                        song = null;
                    }

                    if (song != null)
                        list.Add(song);
                }
                this.Session.SendMessage(JukeboxComposer.Compose(list));
                list.Clear();
            }
        }

        /// <summary>
        /// Adds the playlist item.
        /// </summary>
        internal void AddPlaylistItem()
        {
            if (this.Session == null || this.Session.GetHabbo() == null || this.Session.GetHabbo().CurrentRoom == null)
                return;
            Room currentRoom = this.Session.GetHabbo().CurrentRoom;
            if (!currentRoom.CheckRights(this.Session, true, false))
                return;
            RoomMusicController roomMusicController = currentRoom.GetRoomMusicController();
            if (roomMusicController.PlaylistSize >= roomMusicController.PlaylistCapacity)
                return;
            uint num = this.Request.GetUInteger();
            UserItem item = this.Session.GetHabbo().GetInventoryComponent().GetItem(num);
            if (item == null || item.BaseItem.InteractionType != Interaction.MusicDisc)
                return;
            var songItem = new SongItem(item);
            int num2 = roomMusicController.AddDisk(songItem);
            if (num2 < 0)
                return;
            songItem.SaveToDatabase(currentRoom.RoomId);
            this.Session.GetHabbo().GetInventoryComponent().RemoveItem(num, true);
            using (IQueryAdapter queryReactor = global::Plus.Plus.GetDatabaseManager().GetQueryReactor())
                queryReactor.RunFastQuery(string.Format("UPDATE items_rooms SET user_id='0' WHERE id={0} LIMIT 1", num));
            this.Session.SendMessage(JukeboxComposer.Compose(roomMusicController.PlaylistCapacity, roomMusicController.Playlist.Values.ToList<SongInstance>()));
        }

        /// <summary>
        /// Removes the playlist item.
        /// </summary>
        internal void RemovePlaylistItem()
        {
            if (this.Session == null || this.Session.GetHabbo() == null || this.Session.GetHabbo().CurrentRoom == null)
                return;
            Room currentRoom = this.Session.GetHabbo().CurrentRoom;
            if (!currentRoom.GotMusicController())
                return;
            RoomMusicController roomMusicController = currentRoom.GetRoomMusicController();
            SongItem songItem = roomMusicController.RemoveDisk(this.Request.GetInteger());
            if (songItem == null)
                return;
            songItem.RemoveFromDatabase();
            this.Session.GetHabbo().GetInventoryComponent().AddNewItem(songItem.ItemId, songItem.BaseItem.ItemId, songItem.ExtraData, 0u, false, true, 0, 0, songItem.SongCode);
            this.Session.GetHabbo().GetInventoryComponent().UpdateItems(false);
            using (IQueryAdapter queryReactor = global::Plus.Plus.GetDatabaseManager().GetQueryReactor())
            {
                queryReactor.RunFastQuery(string.Format("UPDATE items_rooms SET user_id='{0}' WHERE id='{1}' LIMIT 1;", Session.GetHabbo().Id, songItem.ItemId));
            }
            this.Session.SendMessage(JukeboxComposer.SerializeSongInventory(this.Session.GetHabbo().GetInventoryComponent().SongDisks));
            this.Session.SendMessage(JukeboxComposer.Compose(roomMusicController.PlaylistCapacity, roomMusicController.Playlist.Values.ToList<SongInstance>()));
        }

        /// <summary>
        /// Gets the disks.
        /// </summary>
        internal void GetDisks()
        {
            if (this.Session == null || this.Session.GetHabbo() == null || this.Session.GetHabbo().GetInventoryComponent() == null)
                return;
            if (this.Session.GetHabbo().GetInventoryComponent().SongDisks.Count == 0)
                return;
            this.Session.SendMessage(JukeboxComposer.SerializeSongInventory(this.Session.GetHabbo().GetInventoryComponent().SongDisks));
        }

        /// <summary>
        /// Gets the playlists.
        /// </summary>
        internal void GetPlaylists()
        {
            if (this.Session == null || this.Session.GetHabbo() == null || this.Session.GetHabbo().CurrentRoom == null)
                return;
            Room currentRoom = this.Session.GetHabbo().CurrentRoom;
            if (!currentRoom.GotMusicController())
                return;
            RoomMusicController roomMusicController = currentRoom.GetRoomMusicController();
            this.Session.SendMessage(JukeboxComposer.Compose(roomMusicController.PlaylistCapacity, roomMusicController.Playlist.Values.ToList<SongInstance>()));
        }
    }
}