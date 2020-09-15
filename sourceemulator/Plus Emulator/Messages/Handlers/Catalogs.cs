using System;
using Plus.HabboHotel.Catalogs;
using Plus.HabboHotel.Groups.Structs;
using Plus.Messages.Parsers;
using System.Linq;
using System.Resources;

namespace Plus.Messages.Handlers
{
    /// <summary>
    /// Class GameClientMessageHandler.
    /// </summary>
    partial class GameClientMessageHandler
    {
        /// <summary>
        /// Catalogues the index.
        /// </summary>
        public void CatalogueIndex()
        {
            var rank = Session.GetHabbo().Rank;
            if (rank < 1)
                rank = 1;
            Session.SendMessage(CatalogPacket.ComposeIndex(rank, Request.GetString().ToUpper()));
        }

        /// <summary>
        /// Catalogues the page.
        /// </summary>
        public void CataloguePage()
        {
            var pageId = Request.GetUInteger16();
            int Num = Request.GetInteger();
            var cPage = Plus.GetGame().GetCatalog().GetPage(pageId);
            if (cPage == null || !cPage.Enabled || !cPage.Visible || cPage.MinRank > Session.GetHabbo().Rank)
                return;
            Session.SendMessage(cPage.CachedContentsMessage);
        }

        /// <summary>
        /// Catalogues the club page.
        /// </summary>
        public void CatalogueClubPage()
        {
            var requestType = Request.GetInteger();
            Session.SendMessage(CatalogPacket.ComposeClubPurchasePage(Session, requestType));
        }

        /// <summary>
        /// Reloads the ecotron.
        /// </summary>
        public void ReloadEcotron()
        {
            Response.Init(LibraryParser.OutgoingRequest("ReloadEcotronMessageComposer"));
            Response.AppendInteger(1);
            Response.AppendInteger(0);
            SendResponse();
        }

        /// <summary>
        /// Gifts the wrapping configuration.
        /// </summary>
        public void GiftWrappingConfig()
        {
            Response.Init(LibraryParser.OutgoingRequest("GiftWrappingConfigurationMessageComposer"));
            Response.AppendBool(true); //enabled
            Response.AppendInteger(1); //cost
            Response.AppendInteger(GiftWrappers.GiftWrappersList.Count);
            foreach (var i in GiftWrappers.GiftWrappersList)
                Response.AppendInteger(i);

            Response.AppendInteger(8);
            for(var i = 0u; i != 8; i++)
                Response.AppendInteger(i);

            Response.AppendInteger(11);
            for (var i = 0u; i != 11; i++)
                Response.AppendInteger(i);

            Response.AppendInteger(GiftWrappers.OldGiftWrappers.Count);
            foreach (var i in GiftWrappers.OldGiftWrappers)
                Response.AppendInteger(i);
            SendResponse();
        }

        /// <summary>
        /// Gets the recycler rewards.
        /// </summary>
        public void GetRecyclerRewards()
        {
            Response.Init(LibraryParser.OutgoingRequest("RecyclerRewardsMessageComposer"));
            var ecotronRewardsLevels = Plus.GetGame().GetCatalog().GetEcotronRewardsLevels();
            Response.AppendInteger(ecotronRewardsLevels.Count);
            foreach (var current in ecotronRewardsLevels)
            {
                Response.AppendInteger(current);
                Response.AppendInteger(current);
                var ecotronRewardsForLevel =
                    Plus.GetGame().GetCatalog().GetEcotronRewardsForLevel(uint.Parse(current.ToString()));
                Response.AppendInteger(ecotronRewardsForLevel.Count);
                foreach (var current2 in ecotronRewardsForLevel)
                {
                    Response.AppendString(current2.GetBaseItem().PublicName);
                    Response.AppendInteger(1);
                    Response.AppendString(current2.GetBaseItem().Type.ToString());
                    Response.AppendInteger(current2.GetBaseItem().SpriteId);
                }
            }
            SendResponse();
        }

        /// <summary>
        /// Purchases the item.
        /// </summary>
        public void PurchaseItem()
        {
            if (Session == null || Session.GetHabbo() == null) return;
            if (Session.GetHabbo().GetInventoryComponent().TotalItems >= 2799)
            {
                Session.SendMessage(CatalogPacket.PurchaseOk(0, string.Empty, 0));
                Session.SendMessage(StaticMessage.AdvicePurchaseMaxItems);
                return;
            }
            var pageId = Request.GetUInteger16();
            var itemId = Request.GetInteger();
            var extraData = Request.GetString();
            var priceAmount = Request.GetInteger();
            Plus.GetGame().GetCatalog().HandlePurchase(Session, pageId, itemId, extraData, priceAmount, false, "", "", 0, 0, 0, false, 0u);
        }

        /// <summary>
        /// Purchases the gift.
        /// </summary>
        public void PurchaseGift()
        {
            var pageId = Request.GetUInteger16();
            var itemId = Request.GetInteger();
            var extraData = Request.GetString();
            var giftUser = Request.GetString();
            var giftMessage = Request.GetString();
            var giftSpriteId = Request.GetInteger();
            var giftLazo = Request.GetInteger();
            var giftColor = Request.GetInteger();
            var undef = Request.GetBool();
            Plus.GetGame().GetCatalog().HandlePurchase(Session, pageId, itemId, extraData, 1, true, giftUser, giftMessage, giftSpriteId, giftLazo, giftColor, undef, 0u);
        }

        /// <summary>
        /// Checks the name of the pet.
        /// </summary>
        public void CheckPetName()
        {
            var petName = Request.GetString();
            var i = 0;
            if (petName.Length > 15)
                i = 1;
            else if (petName.Length < 3)
                i = 2;
            else if (!Plus.IsValidAlphaNumeric(petName))
                i = 3;
            Response.Init(LibraryParser.OutgoingRequest("CheckPetNameMessageComposer"));
            Response.AppendInteger(i);
            Response.AppendString(petName);
            SendResponse();
        }

        /// <summary>
        /// Catalogues the offer.
        /// </summary>
        public void CatalogueOffer()
        {
            var num = Request.GetInteger();
            var catalogItem = Plus.GetGame().GetCatalog().GetItemFromOffer(num);
            if (catalogItem == null || Catalog.LastSentOffer == num)
                return;
            Catalog.LastSentOffer = num;
            var message = new ServerMessage(LibraryParser.OutgoingRequest("CatalogOfferMessageComposer"));
            CatalogPacket.ComposeItem(catalogItem, message);
            Session.SendMessage(message);
        }

        /// <summary>
        /// Catalogues the offer configuration.
        /// </summary>
        public void CatalogueOfferConfig()
        {
            Response.Init(LibraryParser.OutgoingRequest("CatalogueOfferConfigMessageComposer"));
            Response.AppendInteger(100);
            Response.AppendInteger(6);
            Response.AppendInteger(1);
            Response.AppendInteger(1);
            Response.AppendInteger(2);
            Response.AppendInteger(40);
            Response.AppendInteger(99);
            SendResponse();
        }

        /// <summary>
        /// Serializes the group furni page.
        /// </summary>
        internal void SerializeGroupFurniPage()
        {
            try
            {
                var userGroups = Plus.GetGame().GetGroupManager().GetUserGroups(Session.GetHabbo().Id);
                Response.Init(LibraryParser.OutgoingRequest("GroupFurniturePageMessageComposer"));

                var responseList = new System.Collections.Generic.List<ServerMessage>();
                foreach (
                    var @group in
                        userGroups.Where(current => current != null).Select(
                            current => Plus.GetGame().GetGroupManager().GetGroup(current.GroupId)))
                {
                    if (@group == null)
                        continue;
                    var subResponse = new ServerMessage();
                    subResponse.AppendInteger(@group.Id);
                    subResponse.AppendString(@group.Name);
                    subResponse.AppendString(@group.Badge);
                    subResponse.AppendString(
                        Plus.GetGame().GetGroupManager().SymbolColours.Contains(@group.Colour1)
                            ? ((GroupSymbolColours)
                                Plus.GetGame().GetGroupManager().SymbolColours[@group.Colour1]).Colour
                            : "4f8a00");
                    subResponse.AppendString(
                        Plus.GetGame().GetGroupManager().BackGroundColours.Contains(@group.Colour2)
                            ? ((GroupBackGroundColours)
                                Plus.GetGame().GetGroupManager().BackGroundColours[@group.Colour2]).Colour
                            : "4f8a00");
                    subResponse.AppendBool(@group.CreatorId == Session.GetHabbo().Id);
                    subResponse.AppendInteger(@group.CreatorId);
                    subResponse.AppendBool(@group.HasForum);

                    responseList.Add(subResponse);
                }
                Response.AppendInteger(responseList.Count());
                Response.AppendServerMessages(responseList);

                responseList.Clear();
                responseList = null;

                SendResponse();
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine(e);
            }
        }
    }
}