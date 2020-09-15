namespace Plus.Messages.Handlers
{
    /// <summary>
    /// Class GameClientMessageHandler.
    /// </summary>
    internal partial class GameClientMessageHandler
    {
        /// <summary>
        /// Gets the inventory.
        /// </summary>
        internal void GetInventory()
        {
            var queuedServerMessage = new QueuedServerMessage(this.Session.GetConnection());
            queuedServerMessage.AppendResponse(this.Session.GetHabbo().GetInventoryComponent().SerializeFloorItemInventory());
            queuedServerMessage.SendResponse();
        }
    }
}