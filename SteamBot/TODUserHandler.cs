/*
==========KeyUserHandler.cs==========

Keybanking bot written by waylaidwanderer (http://steamcommunity.com/id/waylaidwanderer/)

## Keybanking Bot Instructions ##

This keybanking bot is the first of its kind. I finished coding it on 12/12/12 (before scrap.tf) for my Steam Community group http://steamcommunity.com/groups/CTSCommunity and since then it has only gotten better. I am releasing the source here for anyone to use due to the fact that another group (who had previously agreed not to share it) has taken my code and bot and is redistributing it without permission or proper credit.

### Setup ###

You will need to add a new code file to the SteamBot solution, named KeyUserHandler.cs. Paste the contents of this code into the file and save.

In `settings.json` you will need to change the `BotControlClass` parameter to `KeyUserHandler` instead of the default `SimpleUserHandler`, if it has not been already done.

### Commands ###

I've written some commands for the bot so that I wouldn't have to keep recompiling and run the bot with new parameters. If you need to change other parameters like certain things the bot says, you will need to recompile the bot yourself.

These commands are all typed into chat, unless otherwise specified.

#### `.buy xx` ####
You can change the price that the bot buys keys for with this command. The prices are calculated in scraps, so `.buy 29` would be `29 / 9 = 3.22 ref`.

#### `.sell xx` ####
Change the price the bot sells keys at. The prices are calculated in scraps, so `.sell 30` would be `30 / 9 = 3.33 ref`.

#### `.join groupID` ####
Tell the bot to join a group's chatroom. e.g. `.join 103582791433582049` or `.join cts`. The latter is coded into the bot so you will need to change it if you want your own shortcuts.

#### `.leave groupID` ####
Tell the bot to leave a group's chatroom. e.g. `.leave 103582791433582049` or `.leave cts`. The latter is coded into the bot so you will need to change it if you want your own shortcuts.

#### `.gmessage string` ####
Send a message to the last chatroom that the bot joined. For example typing `.gmessage Hello!` into your bot's chat will make it say `Hello!` into the group's chatroom.

#### `.advertise` ####
This will advertise a predefined message into the last group chatroom that the bot joined, changeable only via the bot's code. Currently it tells the chatroom that it is buying and selling keys at whatever price you set it at every 2 minutes and 50 seconds. This interval can only be changed from within the bot's code, of which its current iteration is `static int TimerInterval = 170000;`.

#### `.canceltrade` ####
Cancels the bot's trade and sends the user a warning message. Occasionally the message will be sent to YOU instead of the current user. I haven't fixed that yet.

#### `.removeall` ####
Removes everyone from the bot's friends list. Useful if it is full.

### Trade Commands ###

These commands must be typed into the trade window and not the chat window.

#### `donate` ####
This tells the bot you want to donate keys/metal. Useful for replenishing its stock.

The bot will also allow you to overpay for keys with metal - it will first tell you how much you will be overpaying by, and then tell you how much you need to add for another key. If you choose to type `continue`, it will note that you are overpaying.

==Credit where credit is due - thanks to:==

Jessecar96 and Geel9 for coding SteamBot, allowing me to have hours and hours of fun
*/

using SteamKit2;
using System.Collections.Generic;
using SteamTrade;
using System;
using System.Timers;

namespace SteamBot
{
    public class TODUserHandler : UserHandler
    {
        static string BotVersion = "2.4.1";
        static int BuyPricePerTOD = 21; // price in scrap, e.g. 29 / 9 = 3.33 ref
        static float SellPricePerTOD = BuyPricePerTOD + 0.5F; // price in scrap, e.g. 31 / 9 = 3.55 ref

        static int BuyPricePerKey = 53;
        static float SellPricePerKey = BuyPricePerKey + 0.5F;
        
        static int TimerInterval = 170000;
        static int InviteTimerInterval = 2000;

        int UserWeapAdded,UserScrapAdded,UserRecAdded,UserRefAdded,UserKeysAdded,UserTODAdded,BotTODsAdded,BotKeysAdded,BotScrapAdded,BotRecAdded,BotRefAdded,InventoryMetal,InventoryScrap,InventoryRec,InventoryRef,InventoryKeys,InventoryTOD,PreviousTODs,PreviousKeys,WhileLoop,InvalidItem = 0;

        float UserMetalAdded, BotMetalAdded, OverpayNumKeys, OverpayNumTOD, ExcessInScrapKey, ExcessInScrapTOD = 0.0F;
        double ExcessRefinedKey, ExcessRefinedTOD = 0.0;

        bool InGroupChat,TimerEnabled,HasRun,HasErrorRun,ChooseDonate,AskOverpay,IsOverpayingKey,IsOverpayingTOD,HasCounted = false;
        bool TimerDisabled = true;
        bool tradetod, tradekey = false;

        ulong uid;
        SteamID currentSID;

        Timer adTimer = new System.Timers.Timer(TimerInterval);
        Timer inviteMsgTimer = new System.Timers.Timer(InviteTimerInterval);

        public TODUserHandler(Bot bot, SteamID sid) : base(bot, sid)
        {
            
        }
        public override void OnLoginCompleted()
        {
        }

        public override bool OnFriendAdd()
        {
            Bot.log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " (" + OtherSID.ToString() + ") added me!");
            // Using a timer here because the message will fail to send if you do it too quickly
            inviteMsgTimer.Interval = InviteTimerInterval;
            inviteMsgTimer.Elapsed += (sender, e) => OnInviteTimerElapsed(sender, e, EChatEntryType.ChatMsg);
            inviteMsgTimer.Enabled = true;
            return true;
        }

        public override void OnFriendRemove()
        {
            Bot.log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " (" + OtherSID.ToString() + ") removed me!");
        }

        public override void OnMessage(string message, EChatEntryType type)
        {
            message = message.ToLower();

            //REGULAR chat commands
            if (message.Contains("buying") || message.Contains("what") || message.Contains("how many") || message.Contains("how much") || message.Contains("price") || message.Contains("selling"))
            {
                if ( message.Contains("key") ){
                    Bot.SteamFriends.SendChatMessage(OtherSID, type, "I buy Keys for " + String.Format("{0:0.00}", (BuyPricePerKey / 9.0)) + " ref, and sell keys for " + String.Format("{0:0.000}", (SellPricePerKey / 9.0)) + " ref or " + String.Format("{0:0.00}", (BuyPricePerKey / 9.0)) + " + a regular weapon. Weapons are valued at 0.055 ref.");
                }
                if ( message.Contains("ticket") || message.Contains("tod") || message.Contains("tour") || message.Contains("duty") ){
                    Bot.SteamFriends.SendChatMessage(OtherSID, type, "I buy TODs for " + String.Format("{0:0.00}", (BuyPricePerTOD / 9.0)) + " ref, and sell TODs for " + String.Format("{0:0.000}", (SellPricePerTOD / 9.0)) + " ref or " + String.Format("{0:0.00}", (BuyPricePerTOD / 9.0)) + " + a regular weapon. Weapons are valued at 0.055 ref.");
                }
            }
            else if (message.Contains("<3"))
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "<3");
            }

            else if (message.Contains("thank") || message.Contains("ty") )
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "You're welcome!");
            }
            else if (message == "donate")
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "If you wish to donate type that command into the TRADE WINDOW, and thanks. <3");
            }
            else if (message == "buy")
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "That's an old command, and is unnecessary. In the TRADE WINDOW specify what items you are buying with \"key\" or \"TOD\".");
            }
            else if (message == "sell")
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "That's an old command, and is unnecessary. Just trade me to begin!");
            }
            else if (message.Contains("help"))
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "To begin just trade me, then simply put up your keys or tods or metal or other items and I will add my items or metal automatically. I also accept donations of either keys or metal. To donate, type \"donate\" in the trade window!");
            }
            else if (message.Contains("stock"))
            {
                TradeCountInventory(true);
            // ADMIN commands
            else if (IsAdmin)
            {
                if (message.StartsWith(".pricekey"))
                {
                    // Usage: .buy newprice "e.g. .buy 24"
                    int NewKeyBuyPrice = 0;
                    if (message.Length >= 10)
                    {
                        Bot.SteamFriends.SendChatMessage(OtherSID, type, "Current buying price per Key: " + BuyPricePerKey + " scrap.");
                        int.TryParse(message.Substring(4), out NewKeyBuyPrice);
                        Bot.log.Success("Admin has requested that I set the new selling price per Key from " + BuyPricePerKey + " scrap to " + NewKeyBuyPrice + " scrap.");
                        BuyPricePerKey = NewKeyBuyPrice;
                        SellPricePerKey = BuyPricePerKey + 0.5F;
                        Bot.SteamFriends.SendChatMessage(OtherSID, type, "Setting new buying price per Key to: " + BuyPricePerKey + " scrap and selling price to: " + SellPricePerKey + " .");
                        Bot.log.Success("Successfully set new price for Keys.");
                    }
                    else
                    {
                        Bot.SteamFriends.SendChatMessage(OtherSID, type, "I need more arguments. Current buying price per Key: " + BuyPricePerKey + " scrap.");
                    }
                }
                else if (message.StartsWith(".pricetod"))
                {
                    // Usage: .buy newprice "e.g. .buy 24"
                    int NewTODBuyPrice = 0;
                    if (message.Length >= 10)
                    {
                        Bot.SteamFriends.SendChatMessage(OtherSID, type, "Current buying priceper TOD: " + BuyPricePerTOD + " scrap.");
                        int.TryParse(message.Substring(4), out NewTODBuyPrice);
                        Bot.log.Success("Admin has requested that I set the new selling price per TOD from " + BuyPricePerTOD + " scrap to " + NewTODBuyPrice + " scrap.");
                        BuyPricePerKey = NewTODBuyPrice;
                        SellPricePerTOD = BuyPricePerTOD + 0.5F;
                        Bot.SteamFriends.SendChatMessage(OtherSID, type, "Setting new buying priceper TOD to: " + BuyPricePerTOD + " scrap and selling price to: " + SellPricePerTOD + " .");
                        Bot.log.Success("Successfully set new price for TODs.");
                    }
                    else
                    {
                        Bot.SteamFriends.SendChatMessage(OtherSID, type, "I need more arguments. Current buying price: " + BuyPricePerTOD + " scrap.");
                    }
                }
                else if (message == ".canceltrade")
                {
                    // Cancels the trade. Occasionally the message will be sent to YOU instead of the current user. Oops.
                    Trade.CancelTrade();
                    Bot.SteamFriends.SendChatMessage(currentSID, EChatEntryType.ChatMsg, "My creator has forcefully cancelled the trade. Whatever you were doing, he probably wants you to stop.");
                }
                else if (message == ".removeall")
                {
                    // Commenting this out because RemoveAllFriends is a custom function I wrote.
                    //Bot.SteamFriends.RemoveAllFriends();
                    //Bot.log.Warn("Removed all friends from my friends list.");
                }
            }
            else
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "Available commands: help, stock, <3");
            }
        }

        public override bool OnTradeRequest()
        {
            Bot.log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " (" + OtherSID.ToString() + ") has requested to trade with me!");
            return true;
        }

        public override void OnTradeError(string error)
        {
            Bot.SteamFriends.SendChatMessage(OtherSID,
                                              EChatEntryType.ChatMsg,
                                              "Error: " + error + "."
                                              );
            Bot.log.Warn(error);
            if (!HasErrorRun)
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "Did something go horribly wrong? If you have found a bug or something that you think wasn't supposed to happen, please leave a message on my owner's profile!");
                HasErrorRun = true;
            }
            Bot.SteamFriends.SetPersonaState(EPersonaState.Online);
        }

        public override void OnTradeTimeout()
        {
            Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg,
                                              "Sorry, but you were either AFK or took too long and the trade was canceled.");
            Bot.log.Info("User was kicked because he was AFK.");
            Bot.SteamFriends.SetPersonaState(EPersonaState.Online);
        }

        public override void OnTradeInit()
        {
            ReInit();
            TradeCountInventory(true);
            Trade.SendMessage("Welcome the TOD and Key banking bot. To use this bot, just add your keys or tods or items, and the bot will automatically add metal. To buy please put your metal in the TRADE WINDOW and type \"key\" or \"tod\" respectively.");
            Trade.SendMessage("Please trade TODs and Keys separately. Buy commands should be written in the TRADE WINDOW.");
            //CHECK TODS INVENTORY
            if (InventoryTOD == 0)
            {
                Trade.SendMessage("I don't have any TODs to sell right now! I am currently buying TOD for " + String.Format("{0:0.00}", (BuyPricePerTOD / 9.0)) + " ref.");
            }
            else if (InventoryMetal < BuyPricePerTOD)
            {
                Trade.SendMessage("I don't have enough metal to buy TODs! I am selling TODs for " + String.Format("{0:0.000}", (SellPricePerTOD / 9.0)) + " ref or " + String.Format("{0:0.00}", (BuyPricePerTOD / 9.0)) + " + a regular weapon. Weapons are valued at 0.055 ref.");
            }
            else
            {
                Trade.SendMessage("I am currently buying TODs for " + String.Format("{0:0.00}", (BuyPricePerTOD / 9.0)) + " ref, and selling TODs for " + String.Format("{0:0.000}", (SellPricePerTOD / 9.0)) + " ref or " + String.Format("{0:0.00}", (BuyPricePerTOD / 9.0)) + " + a regular weapon. Weapons are valued at 0.055 ref.");
            }
            //CHECK KEYS INVENTORY
            if (InventoryKeys == 0)
            {
                Trade.SendMessage("I don't have any TODs to sell right now! I am currently buying TOD for " + String.Format("{0:0.00}", (BuyPricePerKey / 9.0)) + " ref.");
            }
            else if (InventoryMetal < BuyPricePerKey)
            {
                Trade.SendMessage("I don't have enough metal to buy TODs! I am selling TODs for " + String.Format("{0:0.000}", (SellPricePerKey / 9.0)) + " ref or " + String.Format("{0:0.00}", (BuyPricePerKey / 9.0)) + " + a regular weapon. Weapons are valued at 0.055 ref.");
            }
            else
            {
                Trade.SendMessage("I am currently buying TODs for " + String.Format("{0:0.00}", (BuyPricePerKey / 9.0)) + " ref, and selling TODs for " + String.Format("{0:0.000}", (SellPricePerKey / 9.0)) + " ref or " + String.Format("{0:0.00}", (BuyPricePerKey / 9.0)) + " + a regular weapon. Weapons are valued at 0.055 ref.");
            }

            Bot.SteamFriends.SetPersonaState(EPersonaState.Busy);
        }

        public override void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
        {
            var item = Trade.CurrentSchema.GetItem(schemaItem.Defindex);
            if (!HasCounted)
            {
                Trade.SendMessage("ERROR: I haven't finished counting my inventory yet! Please remove any items you added, and then re-add them or there could be errors.");
            }
            else if (InvalidItem >= 4)
            {
                Trade.CancelTrade();
                Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "Please stop messing around. I am used for buying and selling keys only. I can only accept metal or keys as payment.");
                Bot.log.Warn("Booted user for messing around.");
                Bot.SteamFriends.SetPersonaState(EPersonaState.Online);
            }
                //add weapons
            else if ( (item.CraftClass == "weapon" || item.CraftMaterialType == "weapon") && !inventoryItem.IsNotCraftable)
            {
                if (inventoryItem.Quality.ToString() != "6" || item.Name.Contains("Festive"))
                {
                    Trade.SendMessage(schemaItem.ItemName + " is not a valid item! Please remove it from the trade.");
                    InvalidItem++;
                    Bot.log.Warn("This is a special weapon and will not be accepted.");
                }
                else
                {
                    UserWeapAdded++;
                    UserMetalAdded += 0.5F;
                }
            }
            else if (item.Defindex == 5000)
            {
                // Put up scrap metal
                UserMetalAdded++;
                UserScrapAdded++;
                Bot.log.Success("User added: " + item.ItemName);
            }
            else if (item.Defindex == 5001)
            {
                // Put up reclaimed metal
                UserMetalAdded += 3;
                UserRecAdded++;
                Bot.log.Success("User added: " + item.ItemName);
            }
            else if (item.Defindex == 5002)
            {
                // Put up refined metal
                UserMetalAdded += 9;
                UserRefAdded++;
                Bot.log.Success("User added: " + item.ItemName);
            }
            else if (item.Defindex == 725)
            {
                //Put up TOD
                UserTODAdded++;
                Bot.log.Success("User adder: " + item.ItemName);
                //USER IS SELLING TOD
                if(!ChooseDonate)

                {
                    //BOT ADDS METAL
                    int TODtoScrap = UserTODAdded * BuyPricePerTOD;
                    if (InventoryMetal < TODtoScrap)
                    Trade.SendMessage("How embarrasing, I only have " + InventoryMetal + " scrap. You need to remove some TODs.");
                        Bot.log.Warn("I don't have enough metal for the user.");
                    }
                else
                {
                    int TODtoScrap = UserTODAdded * BuyPricePerTOD;
                    int KeysToScrap = UserKeysAdded * BuyPricePerKey;
                    Trade.SendMessage("You have given me " + UserTODAdded + " TODs. I will give you " + TODtoScrap + " scrap.");
                    Bot.log.Success("User gave me " + UserKeysAdded + " TODs. I will now give him " + TODtoScrap + " scrap.");
                    // Put up required metal
                    bool DoneAddingMetal = false;
                    while (!DoneAddingMetal)
                    {
                        if (InventoryRef > 0 && BotMetalAdded + 9 <= TODtoScrap)
                        {
                            Trade.AddItemByDefindex(5002);
                            Bot.log.Warn("I added Refined Metal.");
                            BotMetalAdded += 9;
                            BotRefAdded++;
                            InventoryRef--;
                        }
                        else if (InventoryRec > 0 && BotMetalAdded + 3 <= TODtoScrap)
                        {
                            Trade.AddItemByDefindex(5001);
                            Bot.log.Warn("I added Reclaimed Metal.");
                            BotMetalAdded += 3;
                            BotRecAdded++;
                            InventoryRec--;
                        }
                        else if (InventoryScrap > 0 && BotMetalAdded + 1 <= TODtoScrap)
                        {
                            Trade.AddItemByDefindex(5000);
                            Bot.log.Warn("I added Scrap Metal.");
                            BotMetalAdded++;
                            BotScrapAdded++;
                            InventoryScrap--;
                        }
                        else if (InventoryScrap == 0 && BotMetalAdded + 2 == TODtoScrap)
                        {
                            Trade.SendMessage("Sorry, but I don't have enough scrap metal to give you! Please remove some TODs or add two TODs.");
                            Bot.log.Warn("Couldn't add enough metal for the user!");
                            DoneAddingMetal = true;
                        }
                        else if (InventoryScrap == 0 && BotMetalAdded + 1 == KeysToScrap)
                        {
                            Trade.SendMessage("Sorry, but I don't have enough scrap metal to give you! Please remove some TODs or add a TOD.");
                            Bot.log.Warn("Couldn't add enough metal for the user!");
                            DoneAddingMetal = true;
                        }
                        else if (BotMetalAdded == TODtoScrap)
                        {
                            Trade.SendMessage("Added enough metal. " + BotRefAdded + " ref, " + BotRecAdded + " rec, " + BotScrapAdded + " scrap.");
                            Bot.log.Success("Gave user enough metal!");
                            DoneAddingMetal = true;
                        }
                    }
                }
            }
            else if (item.Defindex == 5021)
            {
                // Put up keys
                UserKeysAdded++;
                Bot.log.Success("User added: " + item.ItemName);
                // USER IS SELLING KEYS
                if (!ChooseDonate)
                {
                    // BOT ADDS METAL
                    int KeysToScrap = UserKeysAdded * BuyPricePerKey;
                    if (InventoryMetal < KeysToScrap)
                    {
                        Trade.SendMessage("I only have " + InventoryMetal + " scrap. You need to remove some keys.");
                        Bot.log.Warn("I don't have enough metal for the user.");
                    }
                    else
                    {
                        Trade.SendMessage("You have given me " + UserKeysAdded + " key(s). I will give you " + KeysToScrap + " scrap.");
                        Bot.log.Success("User gave me " + UserKeysAdded + " key(s). I will now give him " + KeysToScrap + " scrap.");
                        // Put up required metal
                        bool DoneAddingMetal = false;
                        while (!DoneAddingMetal)
                        {
                            if (InventoryRef > 0 && BotMetalAdded + 9 <= KeysToScrap)
                            {
                                Trade.AddItemByDefindex(5002);
                                Bot.log.Warn("I added Refined Metal.");
                                BotMetalAdded += 9;
                                BotRefAdded++;
                                InventoryRef--;
                            }
                            else if (InventoryRec > 0 && BotMetalAdded + 3 <= KeysToScrap)
                            {
                                Trade.AddItemByDefindex(5001);
                                Bot.log.Warn("I added Reclaimed Metal.");
                                BotMetalAdded += 3;
                                BotRecAdded++;
                                InventoryRec--;
                            }
                            else if (InventoryScrap > 0 && BotMetalAdded + 1 <= KeysToScrap)
                            {
                                Trade.AddItemByDefindex(5000);
                                Bot.log.Warn("I added Scrap Metal.");
                                BotMetalAdded++;
                                BotScrapAdded++;
                                InventoryScrap--;
                            }
                            else if (InventoryScrap == 0 && BotMetalAdded + 2 == KeysToScrap)
                            {
                                Trade.SendMessage("Sorry, but I don't have enough scrap metal to give you! Please remove some keys or add two keys.");
                                Bot.log.Warn("Couldn't add enough metal for the user!");
                                DoneAddingMetal = true;
                            }
                            else if (InventoryScrap == 0 && BotMetalAdded + 1 == KeysToScrap)
                            {
                                Trade.SendMessage("Sorry, but I don't have enough scrap metal to give you! Please remove some keys or add a key.");
                                Bot.log.Warn("Couldn't add enough metal for the user!");
                                DoneAddingMetal = true;
                            }
                            else if (BotMetalAdded == KeysToScrap)
                            {
                                Trade.SendMessage("Added enough metal. " + BotRefAdded + " ref, " + BotRecAdded + " rec, " + BotScrapAdded + " scrap.");
                                Bot.log.Success("Gave user enough metal!");
                                DoneAddingMetal = true;
                            }
                        }
                    }
                }
            }
            else if (item.Defindex == 5049 || item.Defindex == 5067 || item.Defindex == 5072 || item.Defindex == 5073 || item.Defindex == 5079 || item.Defindex == 5081 || item.Defindex == 5628 || item.Defindex == 5631 || item.Defindex == 5632)
            {
                Trade.SendMessage("I'm really sorry but I can't accept " + item.ItemName + "! This key used to be a special key such as a winter key that has turned back into a normal one. Unfortunately the ID is different and I am not coded to handle them at the moment. Please remove it and exchange it for a normal key!");
                Bot.log.Warn("User added a special key, but I cannot accept it.");
            }
            else
            {
                // Put up other items
                Trade.SendMessage("Sorry, I don't accept " + item.ItemName + "! I only accept metal/keys/tods/reg-weaps. Please remove it from the trade to continue.");
                Bot.log.Warn("User added:  " + item.ItemName);
                InvalidItem++;
            }
            // USER IS BUYING KEYS
            if (!ChooseDonate)
            {
                if (UserMetalAdded % SellPricePerKey >= 0 && UserMetalAdded > 0)
                {
                    // Count refined and convert to keys -- X scrap per key
                    int NumKeys = (int)(UserMetalAdded / SellPricePerKey);
                    if (NumKeys > 0 && NumKeys != PreviousKeys)
                    {
                        Trade.SendMessage("You put up enough metal for " + NumKeys + " key(s). Adding your keys now...");
                        Bot.log.Success("User put up enough metal for " + NumKeys + " key(s).");
                        if (NumKeys > InventoryKeys)
                        {
                            double excess = ((NumKeys - BotKeysAdded) * SellPricePerKey) / 9.0;
                            string refined = string.Format("{0:N2}", excess);
                            Trade.SendMessage("I only have " + InventoryKeys + " in my backpack. :(");
                            Bot.log.Warn("User wanted to buy " + NumKeys + " key(s), but I only have " + InventoryKeys + " key(s).");
                            Trade.SendMessage("Please remove " + refined + " ref.");
                            NumKeys = InventoryKeys;
                        }
                        // Add the keys to the trade window
                        for (int count = BotKeysAdded; count < NumKeys; count++)
                        {
                            Trade.AddItemByDefindex(5021);
                            Bot.log.Warn("I am adding Mann Co. Supply Crate Key.");
                            BotKeysAdded++;
                            InventoryKeys--;
                        }
                        Trade.SendMessage("I have added " + BotKeysAdded + " key(s) for you.");
                        Bot.log.Success("I have added " + BotKeysAdded + " key(s) for the user.");
                        PreviousKeys = NumKeys;
                    }
                }
                // USER IS BUYING TODS
                else if (UserMetalAdded % SellPricePerTOD >= 0 && UserMetalAdded > 0)
                {
                    // Count refined and convert to keys -- X scrap per key
                    int NumTODs = (int)(UserMetalAdded / SellPricePerTOD);
                    if (NumTODs > 0 && NumTODs != PreviousTODs)
                    {
                        Trade.SendMessage("You put up enough metal for " + NumTODs + " TODs. Adding your TODs now...");
                        Bot.log.Success("User put up enough metal for " + NumTODs + " TODs.");
                        if (NumTODs > InventoryTOD)
                        {
                            double excess = ((NumTODs - BotTODsAdded) * SellPricePerTOD) / 9.0;
                            string refined = string.Format("{0:N2}", excess);
                            Trade.SendMessage("I only have " + InventoryTOD + " in my backpack. :(");
                            Bot.log.Warn("User wanted to buy " + NumTODs + " TODs, but I only have " + InventoryTOD + " TODs.");
                            Trade.SendMessage("Please remove " + refined + " ref.");
                            NumTODs = InventoryTOD;
                        }
                        // Add the keys to the trade window
                        for (int count = BotKeysAdded; count < NumTODs; count++)
                        {
                            Trade.AddItemByDefindex(725);
                            Bot.log.Warn("I am adding Tour of Duty ticket.");
                            BotTODsAdded++;
                            InventoryTOD--;
                        }
                        Trade.SendMessage("I have added " + BotTODsAdded + " key(s) for you.");
                        Bot.log.Success("I have added " + BotTODsAdded + " key(s) for the user.");
                        PreviousTODs = NumTODs;
                    }
                }
            }
        }

        public override void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
        {
            var item = Trade.CurrentSchema.GetItem(schemaItem.Defindex);
            if ((item.CraftClass == "weapon" || item.CraftMaterialType == "weapon") && !inventoryItem.IsNotCraftable)
            {
                if (inventoryItem.Quality.ToString() != "6" || item.Name.Contains("Festive"))
                {
                    InvalidItem--;
                    Bot.log.Warn("User removed special item.");
                }
    			else
				{
					UserWeapAdded--;
                    UserMetalAdded -= 0.5F;
				}
            }
            else if (item.Defindex == 5000)
            {
                // Removed scrap metal
                UserMetalAdded -= 1;
                UserScrapAdded--;
                Bot.log.Success("User removed: " + item.ItemName);
            }
            else if (item.Defindex == 5001)
            {
                // Removed reclaimed metal
                UserMetalAdded -= 3;
                UserRecAdded--;
                Bot.log.Success("User removed: " + item.ItemName);
            }
            else if (item.Defindex == 5002)
            {
                // Removed refined metal
                UserMetalAdded -= 9;
                UserRefAdded--;
                Bot.log.Success("User removed: " + item.ItemName);
            }
            else if (item.Defindex == 5021)
            {
                // Removed keys
                UserKeysAdded--;
                Bot.log.Success("User removed: " + item.ItemName);
            }
            else if (item.Defindex == 5049 || item.Defindex == 5067 || item.Defindex == 5072 || item.Defindex == 5073 || item.Defindex == 5079 || item.Defindex == 5081 || item.Defindex == 5628 || item.Defindex == 5631 || item.Defindex == 5632)
            {
                Bot.log.Warn("User removed special key.");
            }
            else
            {
                // Removed other items
                Bot.log.Warn("User removed: " + item.ItemName);
            }
            // User removes key from trade
            if (UserKeysAdded < (float) BotMetalAdded / BuyPricePerKey)
            {
                int KeysToScrap = UserKeysAdded * BuyPricePerKey;
                bool DoneAddingMetal = false;
                while (!DoneAddingMetal)
                {
                    WhileLoop++;
                    if (BotRefAdded > 0 && BotMetalAdded - 9 >= KeysToScrap)
                    {
                        Trade.RemoveItemByDefindex(5002);
                        Bot.log.Warn("I removed Refined Metal.");
                        BotMetalAdded -= 9;
                        BotRefAdded--;
                        InventoryRef++;
                    }
                    else if (BotRecAdded > 0 && BotMetalAdded - 3 >= KeysToScrap)
                    {
                        Trade.RemoveItemByDefindex(5001);
                        Bot.log.Warn("I removed Reclaimed Metal.");
                        BotMetalAdded -= 3;
                        BotRecAdded--;
                        InventoryRec++;
                    }
                    else if (BotScrapAdded > 0 && BotMetalAdded - 1 >= KeysToScrap)
                    {
                        Trade.RemoveItemByDefindex(5000);
                        Bot.log.Warn("I removed Scrap Metal.");
                        BotMetalAdded--;
                        BotScrapAdded--;
                        InventoryScrap++;
                    }
                    else if (BotMetalAdded == KeysToScrap)
                    {
                        DoneAddingMetal = true;
                    }
                    else if (WhileLoop > 50)
                    {
                        Trade.SendMessage("Error: I could not remove the proper amounts of metal from the trade. I might be out of scrap metal - try adding more keys if possible, or remove a few keys.");
                        WhileLoop = 0;
                        DoneAddingMetal = true;
                    }
                }
            }
            // User removes metal from trade
            while ((float) UserMetalAdded / SellPricePerKey < BotKeysAdded && tradekey)
            {
                Trade.RemoveItemByDefindex(5021);
                Bot.log.Warn("I removed Mann Co. Supply Crate Key.");
                BotKeysAdded--;
                InventoryKeys++;
                PreviousKeys = BotKeysAdded;
                IsOverpayingKey = false;
            }
            while ((float)UserMetalAdded / SellPricePerTOD < BotTODsAdded && tradetod)
            {
                Trade.RemoveItemByDefindex(725);
                Bot.log.Warn("I removed a TOur of Duty ticket.");
                BotTODsAdded--;
                InventoryTOD++;
                PreviousTODs = BotTODsAdded;
                IsOverpayingTOD = false;
            }
        }

        public override void OnTradeMessage(string message)
        {
            Bot.log.Info("[TRADE MESSAGE] " + message);            
            message = message.ToLower();

            if (message == "buy")
            {
                Trade.SendMessage("This is an old command and is unnecessary. Simply add your metal and I will add keys for you when you've put up enough. I am currently selling keys for " + String.Format("{0:0.00}", (SellPricePerKey / 9.0)) + " ref each.");
                Bot.log.Warn("User tried to use old BUY command.");
            }

            if (message == "sell")
            {
                Trade.SendMessage("This is an old command and is unnecessary. Simply add your keys and I will automatically add the metal for you. I am currently buying keys for " + String.Format("{0:0.00}", (BuyPricePerKey / 9.0)) + " ref each.");
                Bot.log.Warn("User tried to use old SELL  command.");
            }

            if (message == "donate")
            {
                ChooseDonate = true;
                Trade.SendMessage("Oh, you want to donate metal or keys? Thank you so much! Please put up your items and simply click \"Ready to Trade\" when done! If you want to buy or sell keys again you need to start a new trade with me.");
                Bot.log.Success("User wants to donate!");
            }

            if (message == "ready")
            {
                Trade.SendMessage("That is an old command and is unnecessary. I will automatically add keys and metal when you've put up enough.");
                Bot.log.Warn("User tried to use an old command.");
            }
            if (message == "key")
            {
                tradekey = true;
                tradetod = false;
                Trade.SendMessage("You have selected to trade for KEYs and not in TODs. Please insert items.");
                Bot.log.Warn("User selected key trade.");
            }
            if (message == "tod")
            {
                tradetod = true;
                tradekey = false;
                Trade.SendMessage("You have selected to trade for TODs and not in KEYs. Please insert items.");
                Bot.log.Warn("User selected tod trade.");
            }
            if (message == "continue")
            {
                if (AskOverpay)
                {
                    IsOverpayingTOD = true;
                    IsOverpayingKey = true;
                    Trade.SendMessage("You have chosen to continue overpaying. Click \"Ready to Trade\" again to complete the trade.");
                    Bot.log.Warn("User has chosen to continue overpaying!");
                }
                else
                {
                    Trade.SendMessage("You cannot use this command right now!");
                    Bot.log.Warn("User typed \"continue\" for no reason.");
                }
            }

        }

        public override void OnTradeReady(bool ready)
        {
            if (!ready)
            {
                Trade.SetReady(false);
            }
            else
            {
                Bot.log.Success("User is ready to trade!");
                if (Validate())
                {
                    Trade.SetReady(true);
                }
                else
                {
                    if (AskOverpay && OverpayNumKeys != 0 && !ChooseDonate)
                    {
                        if (tradekey && !tradetod)
                        {
                            double AdditionalRefined = (SellPricePerKey / 9.0) - ExcessRefinedKey;
                            string addRef = string.Format("{0:N2}", AdditionalRefined);
                            string refined = string.Format("{0:N2}", ExcessRefinedKey);
                            Trade.SendMessage("WARNING: You will be overpaying. If you'd like to continue, type \"continue\", otherwise remove " + refined + " ref, or add " + addRef + " ref. You cannot complete the trade unless you do so.");
                            Bot.log.Warn("User has added an excess of " + refined + " ref. He can add " + addRef + " ref for another key. Asking user if they want to continue.");
                        }
                        if(tradetod && !tradekey)
                        {
                            double AdditionalRefined = (SellPricePerTOD / 9.0) - ExcessRefinedTOD;
                            string addRef = string.Format("{0:N2}", AdditionalRefined);
                            string refined = string.Format("{0:N2}", ExcessRefinedTOD);
                            Trade.SendMessage("WARNING: You will be overpaying. If you'd like to continue, type \"continue\", otherwise remove " + refined + " ref, or add " + addRef + " ref. You cannot complete the trade unless you do so.");
                            Bot.log.Warn("User has added an excess of " + refined + " ref. He can add " + addRef + " ref for another key. Asking user if they want to continue.");
                        }
                    }
                    else
                    {
                        ResetTrade(false);
                    }
                }
            }
        }

        public override void OnTradeAccept()
        {
            if (Validate() || IsAdmin)
            {
                bool success = Trade.AcceptTrade();
                if (success)
                {
                    Log.Success("Trade was successful!");
                    Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "Thanks for a successful trade! Please leave a +rep on my profile if you'd like. :) You're the best!");
                    Bot.SteamFriends.SetPersonaState(EPersonaState.Online);
                }
                else
                {
                    Log.Warn("Trade might have failed.");
                    Bot.SteamFriends.SetPersonaState(EPersonaState.Online);
                }
            }
            OnTradeClose();
        }

        public override void OnTradeClose()
        {
            Bot.SteamFriends.SetPersonaState(EPersonaState.Online);
            base.OnTradeClose();
        }

        public bool Validate()
        {
            int WeapCount = 0;
            int ScrapCount = 0;
            int KeyCount = 0;
            int TODcount = 0;

            List<string> errors = new List<string>();

            foreach (ulong id in Trade.OtherOfferedItems)
            {
                var item = Trade.OtherInventory.GetItem(id);
                var itemschema = Trade.CurrentSchema.GetItem(item.Defindex);
                if ( (itemschema.CraftClass == "weapon" || itemschema.CraftMaterialType == "weapon") && !item.IsNotCraftable)
                {
                    if (item.Quality.ToString() != "6" || itemschema.Name.Contains("Festive"))
                    {
                        errors.Add("I can't accept " + itemschema.Name + "! This is a special Weapon.");
                    }
                    else
                    {
                        WeapCount++;
                    }
                }
                else if (item.Defindex == 5000)
                {
                    ScrapCount++;
                }
                else if (item.Defindex == 5001)
                {
                    ScrapCount += 3;
                }
                else if (item.Defindex == 5002)
                {
                    ScrapCount += 9;
                }
                else if (item.Defindex == 5021)
                {
                    KeyCount++;
                }
                else if (item.Defindex == 725)
                {
                    TODcount++;
                }
                else if (item.Defindex == 5049 || item.Defindex == 5067 || item.Defindex == 5072 || item.Defindex == 5073 || item.Defindex == 5079 || item.Defindex == 5081 || item.Defindex == 5628 || item.Defindex == 5631 || item.Defindex == 5632)
                {
                    var schemaItem = Trade.CurrentSchema.GetItem(item.Defindex);
                    errors.Add("I'm sorry, but I cannot accept " + schemaItem.ItemName + " because it is not in my code to process keys that used to be special keys like scorched keys or winter keys. Please exchange your key for a normal one and try again!");
                }
                else
                {
                    var schemaItem = Trade.CurrentSchema.GetItem(item.Defindex);
                    errors.Add("I can't accept " + schemaItem.ItemName + "!");
                }
            }

            if (ChooseDonate)
            {
                foreach (ulong id in Trade.OtherOfferedItems)
                {
                    var item = Trade.OtherInventory.GetItem(id);
                    var itemschema = Trade.CurrentSchema.GetItem(item.Defindex);
                    if (item.Defindex == 725 || item.Defindex == 5021 || item.Defindex == 5049 || item.Defindex == 5067 || item.Defindex == 5072 || item.Defindex == 5073 || item.Defindex == 5079 || item.Defindex == 5081 || item.Defindex == 5628 || item.Defindex == 5631 || item.Defindex == 5632 || item.Defindex == 5000 || item.Defindex == 5001 || item.Defindex == 5002) { }
                    else if ((itemschema.CraftClass == "weapon" || itemschema.CraftMaterialType == "weapon") && !item.IsNotCraftable) { }
                    else
                    {
                        var schemaItem = Trade.CurrentSchema.GetItem(item.Defindex);
                        errors.Add("I'm sorry, but I cannot accept " + itemschema.Name + "!");
                    }
                }
                if (BotMetalAdded > 0 || BotKeysAdded > 0 || BotTODsAdded > 0)
                {
                    errors.Add("You can't do that :( I still have items put up!");
                }
            }
            else if (UserKeysAdded > 0)
            {
                Bot.log.Warn("User has " + KeyCount + " key(s) put up. Verifying if " + (float) BotMetalAdded / BuyPricePerKey + " == " + KeyCount + ".");
                if (KeyCount != (float)BotMetalAdded / BuyPricePerKey)
                {
                    errors.Add("Something went wrong. Either you do not have the correct amount of keys or I don't have the correct amount of metal.");
                }
            }
            else if (UserTODAdded > 0)
            {
                Bot.log.Warn("User has " + TODcount + " TODs put up. Veryfying if " + (float) BotMetalAdded / BuyPricePerTOD + " == " + TODcount + ".");
                if (TODcount != (float)BotMetalAdded / BuyPricePerTOD)
                {
                    errors.Add("Something went wrong. Either you do not have the correct amount of tods or I don't have the correct amount of metal.");
                }
            }
            else if (UserMetalAdded % SellPricePerKey != 0 && !IsOverpayingKey && tradekey)
            {
                // Count refined and convert to keys -- X scrap per key
                OverpayNumKeys = (float) UserMetalAdded / SellPricePerKey;
                ExcessInScrapKey = UserMetalAdded - (OverpayNumKeys * SellPricePerKey);
                ExcessRefinedKey = (ExcessInScrapKey / 9.0);
                string refined = string.Format("{0:N2}", ExcessRefinedKey);
                Trade.SendMessage("You put up enough metal for " + OverpayNumKeys + " key(s), with " + refined + " ref extra.");
                Bot.log.Success("User put up enough metal for " + OverpayNumKeys + " key(s), with " + refined + " ref extra.");
                if (OverpayNumKeys == 0)
                {
                    double AdditionalRefined = (SellPricePerKey / 9.0) - ExcessRefinedKey;
                    string addRef = string.Format("{0:N2}", AdditionalRefined);
                    errors.Add("ERROR: You need to add " + addRef + " ref for a key.");
                    Bot.log.Warn("User doesn't have enough metal added, and needs add " + addRef + " ref for a key.");
                }
                else if (OverpayNumKeys >= 1)
                {
                    errors.Add("You have put up more metal than what I'm asking.");
                    AskOverpay = true;
                }
            }
            else if (UserMetalAdded % SellPricePerTOD != 0 && !IsOverpayingTOD && tradetod)
            {
                // Count refined and convert to keys -- X scrap per tod
                OverpayNumTOD = UserMetalAdded / SellPricePerTOD;
                ExcessInScrapTOD = UserMetalAdded - (OverpayNumTOD * SellPricePerTOD);
                ExcessRefinedTOD = (ExcessInScrapTOD / 9.0);
                string refined = string.Format("{0:N2}", ExcessRefinedTOD);
                Trade.SendMessage("You put up enough metal for " + OverpayNumTOD + " TODs, with " + refined + " ref extra.");
                Bot.log.Success("User put up enough metal for " + OverpayNumTOD + " TODs, with " + refined + " ref extra.");
                if (OverpayNumTOD == 0)
                {
                    double AdditionalRefined = (SellPricePerTOD / 9.0) - ExcessRefinedTOD;
                    string addRef = string.Format("{0:N2}", AdditionalRefined);
                    errors.Add("ERROR: You need to add " + addRef + " ref for a TOD.");
                    Bot.log.Warn("User doesn't have enough metal added, and needs add " + addRef + " ref for a TOD.");
                }
                else if (OverpayNumTOD >= 1)
                {
                    errors.Add("You have put up more metal than what I'm asking.");
                    AskOverpay = true;
                }
            }
            else if (UserMetalAdded > 0 && !IsOverpayingKey)
            {
                if ( ( (ScrapCount < BotKeysAdded * SellPricePerKey) || (ScrapCount > BotKeysAdded * SellPricePerKey) ) && tradekey )
                {
                    errors.Add("You must put up exactly " + String.Format("{0:0.000}", (SellPricePerKey / 9.0)) + " ref per key (Weaps values is : 0.055 ref).");
                }
                if ( ( (ScrapCount < BotTODsAdded * SellPricePerTOD) || (ScrapCount > BotTODsAdded * SellPricePerTOD) ) && tradetod )
                {
                    errors.Add("You must put up exactly " + String.Format("{0:0.000}", (SellPricePerTOD / 9.0)) + " ref per tod (Weaps values is : 0.055 ref).");
                }
            }
            else if (UserMetalAdded > 0 && !tradekey && !tradetod)
            {
                errors.Add("You must select what type of item you are buying. In the TRADE WINDOW type \"tod\" for TODs or type \"key\" for Keys.");
            }

            // send the errors
            if (errors.Count != 0)
                Trade.SendMessage("There were errors in your trade: ");

            foreach (string error in errors)
            {
                Trade.SendMessage(error);
            }

            return errors.Count == 0;
        }

        public void TradeCountInventory(bool message)
        {
            // Let's count our inventory
            Inventory.Item[] inventory = Trade.MyInventory.Items;
            InventoryMetal = 0;
            InventoryKeys = 0;
            InventoryRef = 0;
            InventoryRec = 0;
            InventoryScrap = 0;
            InventoryTOD = 0;
            foreach (Inventory.Item item in inventory)
            {
                if (item.Defindex == 5000)
                {
                    InventoryMetal++;
                    InventoryScrap++;
                }
                else if (item.Defindex == 5001)
                {
                    InventoryMetal += 3;
                    InventoryRec++;
                }
                else if (item.Defindex == 5002)
                {
                    InventoryMetal += 9;
                    InventoryRef++;
                }
                else if (item.Defindex == 5021)
                {
                    InventoryKeys++;
                }
                else if (item.Defindex == 725)
                {
                    InventoryTOD++;
                }
            }
            if (message)
            {
                double MetalToRef = (InventoryMetal / 9.0) - 0.01;
                string refined = string.Format("{0:N2}", MetalToRef);
                Trade.SendMessage("Current stock: I have " + refined + " ref (" + InventoryRef + " ref, " + InventoryRec + " rec, and " + InventoryScrap + " scrap) and " + InventoryTOD + " TODs and " + InventoryKeys + " key(s) in my backpack.");
                Bot.log.Success("Current stock: I have " + refined + " ref (" + InventoryRef + " ref, " + InventoryRec + " rec, and " + InventoryScrap + " scrap) and " + InventoryTOD + " TODs and " + InventoryKeys + " key(s) in my backpack.");
            }
            HasCounted = true;
        }

        public void ReInit()
        {
            UserMetalAdded = 0;
            UserRefAdded = 0;
            UserRecAdded = 0;
            UserScrapAdded = 0;
            UserKeysAdded = 0;
            UserTODAdded = 0;
            BotTODsAdded = 0;
            BotKeysAdded = 0;
            BotMetalAdded = 0;
            BotRefAdded = 0;
            BotRecAdded = 0;
            BotScrapAdded = 0;
            OverpayNumTOD = 0;
            OverpayNumKeys = 0;
            PreviousKeys = 0;
            ExcessInScrapTOD = 0;
            ExcessRefinedTOD = 0.0;
            ExcessInScrapKey = 0;
            ExcessRefinedKey = 0.0;
            WhileLoop = 0;
            InvalidItem = 0;
            HasErrorRun = false;
            ChooseDonate = false;
            AskOverpay = false;
            IsOverpayingKey = false;
            IsOverpayingTOD = false;
            HasCounted = false;
            currentSID = OtherSID;
        }

        public void Advertise(EChatEntryType type)
        {
            if (TimerEnabled == true)
            {
                adTimer.Enabled = true;
                TimerDisabled = false;

            }
            if (TimerEnabled == false)
            {
                adTimer.Enabled = false;
                adTimer.Stop();
                TimerDisabled = true;
            }
        }

        private void OnTimerElapsed(object source, ElapsedEventArgs e, EChatEntryType type)
        {
            var chatid = new SteamID(uid);
            string adMessage;
            //KEY CHAT
            if (InventoryMetal > BuyPricePerKey && InventoryKeys > 0)
            {
                adMessage = "I am a keybanking trade bot! I am buying keys for " + String.Format("{0:0.00}", (BuyPricePerKey / 9.0)) + " ref and selling keys for " + String.Format("{0:0.00}", (SellPricePerKey / 9.0)) + " ref (weap = 0.055). Send me a trade!";
            }
            else if (InventoryMetal < BuyPricePerKey)
            {
                adMessage = "I am a keybanking trade bot! I am only selling keys for " + String.Format("{0:0.00}", (SellPricePerKey / 9.0)) + " ref at the moment (weap = 0.055). Send me a trade!";
            }
            else if (InventoryKeys == 0)
            {
                adMessage = "I am a keybanking trade bot! I am only buying keys for " + String.Format("{0:0.00}", (BuyPricePerKey / 9.0)) + " ref at the moment. Send me a trade!";
            }
            else
            {
                adMessage = "I am a keybanking trade bot! I am buying keys for " + String.Format("{0:0.00}", (BuyPricePerKey / 9.0)) + " ref and selling keys for " + String.Format("{0:0.000}", (SellPricePerKey / 9.0)) + " ref (weap = 0.055). Send me a trade!";
            }
            //TOD CHAT
            if (InventoryMetal > BuyPricePerTOD && InventoryTOD > 0)
            {
                adMessage = "I am a todbanking trade bot! I am buying tods for " + String.Format("{0:0.00}", (BuyPricePerTOD / 9.0)) + " ref and selling tods for " + String.Format("{0:0.000}", (SellPricePerTOD / 9.0)) + " ref (weap = 0.055). Send me a trade!";
            }
            else if (InventoryMetal < BuyPricePerTOD)
            {
                adMessage = "I am a todbanking trade bot! I am only selling tods for " + String.Format("{0:0.00}", (SellPricePerTOD / 9.0)) + " ref at the moment (weap = 0.055). Send me a trade!";
            }
            else if (InventoryTOD == 0)
            {
                adMessage = "I am a todbanking trade bot! I am only buying tods for " + String.Format("{0:0.00}", (BuyPricePerTOD / 9.0)) + " ref at the moment. Send me a trade!";
            }
            else
            {
                adMessage = "I am a todbanking trade bot! I am buying tods for " + String.Format("{0:0.00}", (BuyPricePerTOD / 9.0)) + " ref and selling tods for " + String.Format("{0:0.000}", (SellPricePerTOD / 9.0)) + " ref (weap = 0.055). Send me a trade!";
            }
            Bot.SteamFriends.SendChatRoomMessage(chatid, type, adMessage);
            Bot.log.Success("Advertised into group chat: " + adMessage);
        }

        private void OnInviteTimerElapsed(object source, ElapsedEventArgs e, EChatEntryType type)
        {
            Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "Hi. You have added BrackBot, the key and tod banking bot! Two in one, how exciting! But please trade on type of item at a time, because I'm still in development.");
            Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "Just trade me, and add your keys, tods or metal to begin! I also accept donations of either keys or metal. To donate, type \"donate\" in the trade window!");
            Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "Please read the instructions in the TRADE WINDOW to keep your trade as enjoyable as possible.");
            Bot.log.Success("Sent welcome message.");
            inviteMsgTimer.Enabled = false;
            inviteMsgTimer.Stop();
        }

        public void ResetTrade(bool message)
        {

            for (int count = 0; count < BotKeysAdded; count++)
            {
                Trade.RemoveAllItemsByDefindex(5021);
            }
            for (int count = 0; count < BotScrapAdded; count++)
            {
                Trade.RemoveItemByDefindex(5000);
            }
            for (int count = 0; count < BotRecAdded; count++)
            {
                Trade.RemoveItemByDefindex(5001);
            }
            for (int count = 0; count < BotRefAdded; count++)
            {
                Trade.RemoveItemByDefindex(5002);
            }
            for (int count = 0; count < BotTODsAdded; count++)
            {
                Trade.RemoveItemByDefindex(725);
            }
            BotKeysAdded = 0;
            BotMetalAdded = 0;
            BotRefAdded = 0;
            BotRecAdded = 0;
            BotScrapAdded = 0;
            BotTODsAdded = 0;
            ChooseDonate = false;
            TradeCountInventory(message);
            Trade.SendMessage("Something went wrong! Scroll up to read the errors.");
            Bot.log.Warn("Something went wrong! I am resetting the trade.");
            Trade.SendMessage("I have reset the trade. Please try again. (If you chose to donate, you will need to type \"donate\" again)");
            Bot.log.Success("Reset trade.");
        }
    }
}
