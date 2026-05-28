using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

// CharacterIndex = Index of person or monster
// MapCharacterIndex = Index inside the characters on the map (0..23)
internal record ConversationCharacter(int CharacterIndex, IMap Map, int MapCharacterIndex);

// TODO: Silk has no gold and food (but he has in original)
// TODO: When talking to Silk, the gold and food display is broken
internal sealed class ConversationScreen : ItemGridScreen
{
    const int ItemSlotCount = 12;
    const int MessageBaseY = 63;
    readonly static Position[] itemSlotPositions = new Position[ItemSlotCount];
    readonly static Rect itemArea;
    readonly static Rect textDisplayArea = new(16, 49, 174, 77);
    readonly static Rect itemTooltipArea = new(16, 72, 174, 7);
    readonly TextScrollHandler textScrollHandler = new();
    readonly ItemContainer[] items = new ItemContainer[ItemSlotCount];    
    readonly List<ItemSlot> receivedItems = [];
    IPerson? person;
    IMap? map;
    int characterIndex = 0;
    int mapCharacterIndex = 0;
    PersonInfoView? personInfoView;
    Label? conversationText;
    Action? afterWaitClickAction = null;
    bool waitForClick = false;
    bool closeAfterClick = false;
    bool insideParty = false;
    int? dragSourceItemSlot = null;
    Image? goldIcon = null;
    Image? foodIcon = null;
    Image? goldIconBackground = null;
    Image? foodIconBackground = null;
    Label? goldValue = null;
    Label? foodValue = null;

    static ConversationScreen()
    {
        const int slotsPerRow = 6;

        for (int i = 0; i < itemSlotPositions.Length; i++)
        {
            int column = i % slotsPerRow;
            int row = i / slotsPerRow;
            var position = new Position(16 + column * 32, 37 + 108 + row * 32);

            itemSlotPositions[i] = position;
        }

        var firstSlot = itemSlotPositions[0];
        var lastSlot = itemSlotPositions[^1];

        itemArea = new(firstSlot.X, firstSlot.Y, lastSlot.X + 16 - firstSlot.X, lastSlot.Y + 16 - firstSlot.Y);
    }

    public sealed override ScreenType Type { get; } = ScreenType.Conversation;

    internal sealed override byte ButtonGridPaletteIndex => Game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];

    internal override ItemContainer[] ItemContainers => items;

    protected override void SetupButtons(ButtonGrid buttonGrid)
    {
        bool itemsAvailable = receivedItems.Count > 0;
        bool partyMemberHasItems = Game.State.ActivePartyMember!.Inventory.Any(itemSlot => itemSlot.Count > 0);
        bool partyMemberHasGold = Game.State.ActivePartyMember.Gold > 0;
        bool partyMemberHasFood = Game.State.ActivePartyMember.Food > 0;
        bool allowInteractions = !insideParty && receivedItems.Count < ItemSlotCount;

        // Upper row
        buttonGrid.SetButton(0, ButtonType.GiveItem);
        buttonGrid.SetButton(1, ButtonType.DropItem);
        buttonGrid.SetButton(2, ButtonType.Exit);
        // Middle row
        buttonGrid.SetButton(3, ButtonType.ExamineItem);
        buttonGrid.SetButton(4, ButtonType.Mouth);
        buttonGrid.SetButton(5, ButtonType.AskToJoin);
        // Lower row
        buttonGrid.SetButton(6, ButtonType.GiveItemToPerson);
        buttonGrid.SetButton(7, ButtonType.GiveGoldToPerson);
        buttonGrid.SetButton(8, ButtonType.GiveFoodToPerson);

        buttonGrid.EnableButton(0, itemsAvailable);
        buttonGrid.EnableButton(1, itemsAvailable);
        buttonGrid.EnableButton(2, !itemsAvailable);

        buttonGrid.EnableButton(3, allowInteractions && partyMemberHasItems);
        buttonGrid.EnableButton(4, allowInteractions);
        buttonGrid.EnableButton(5, allowInteractions);
        buttonGrid.EnableButton(6, allowInteractions && partyMemberHasItems);
        buttonGrid.EnableButton(7, allowInteractions && partyMemberHasGold);
        buttonGrid.EnableButton(8, allowInteractions && partyMemberHasFood);
    }

    public override void Init()
    {
        base.Init();

        var dialogLabel = AddLabel(16, 39, Game.LoadUIText(UIText.Dialog), 176, 7);
        dialogLabel.Alignment = TextAlignment.Center;

        var (x, y, width, height) = textDisplayArea;
        conversationText = AddLabel(x, y, width, height);
        textScrollHandler.ScrollEnded += EndClickWait;

        for (int i = 0; i < items.Length; i++)
        {
            var (slotX, slotY) = itemSlotPositions[i];
            items[i] = AddItem(slotX, slotY, item: null, count: 0, displayLayer: 10);
        }

        var itemPalette = Game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.Item];

        x = 208;
        y = 113;
        goldIconBackground = AddImage(x, y, 16, 16, Game.GraphicIndexProvider.GetUIGraphicIndex(UIGraphic.EmptyItemSlot), displayLayer: 10, true);
        goldIcon = AddImage(ref x, y, 16, 16, Game.GraphicIndexProvider.GetItemGraphicIndex(ItemGraphic.WishingCoins), displayLayer: 15);
        goldIcon.PaletteIndex = itemPalette;
        goldValue = AddLabel(x + 1, y + 5, 30, 7, displayLayer: 15);

        x += 32;
        foodIconBackground = AddImage(x, y, 16, 16, Game.GraphicIndexProvider.GetUIGraphicIndex(UIGraphic.EmptyItemSlot), displayLayer: 10, true);
        foodIcon = AddImage(ref x, y, 16, 16, Game.GraphicIndexProvider.GetItemGraphicIndex(ItemGraphic.Ration), displayLayer: 15);
        foodIcon.PaletteIndex = itemPalette;
        foodValue = AddLabel(x + 1, y + 5, 30, 7, displayLayer: 15);
    }

    public override void Open(Action? closeAction)
	{
		base.Open(closeAction);

        var palette = Game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];

        Game.SetLayout(Layout.Conversation, palette);
        Game.Cursor.CursorType = CursorType.Sword;

        if (Game.State.CurrentConversationCharacter is not ConversationCharacter conversationCharacter)
            throw new AmberException(ExceptionScope.Application, "No conversation character specified.");

        person = Game.AssetProvider.PersonLoader.LoadPerson(conversationCharacter.CharacterIndex);
        personInfoView = new(Game, person, conversationCharacter.CharacterIndex, palette);
        map = conversationCharacter.Map;
        mapCharacterIndex = conversationCharacter.MapCharacterIndex + 1; // They are 1-based for save bits etc
        characterIndex = conversationCharacter.CharacterIndex;
        insideParty = Game.State.PartyCharacterIndices.Any(index => index == characterIndex);

        waitForClick = false;
        closeAfterClick = false;

        var partyMember = Game.State.ActivePartyMember!;
        goldValue!.SetText($"{partyMember.Gold:00000}");
        foodValue!.SetText($"{partyMember.Food:00000}");

        HideText();
        ShowReceivedItems();
        RequestButtonSetup();
    }

    public override void Close()
    {
        personInfoView?.Destroy();
        HideText();

        base.Close();
    }

    public override void ScreenPushed(Screen screen)
    {
        if (!screen.Transparent)
        {
            if (personInfoView != null)
                personInfoView.Visible = false;
        }

        base.ScreenPushed(screen);        
    }

    public override void ScreenPopped(Screen screen)
    {
        base.ScreenPopped(screen);

        if (!screen.Transparent)
        {
            if (personInfoView != null)
                personInfoView.Visible = true;
        }

        ShowReceivedItems();

        if (screen is SelectWordScreen && !string.IsNullOrEmpty(Game.CurrentWord))
        {
            CheckWord(Game.CurrentWord);
        }
        else if (screen is GiveGoldScreen && Game.CurrentAmount > 0 && Game.CurrentAmount <= word.MaxValue)
        {
            if (!TryExecuteReactionsForTrigger(InteractionTriggerType.Pay, (word)Game.CurrentAmount, ShowReceivedItems))
            {
                ShowMessage(Message.KeepYourGold);
            }
        }
        else if (screen is GiveFoodScreen && Game.CurrentAmount > 0 && Game.CurrentAmount <= word.MaxValue)
        {
            if (!TryExecuteReactionsForTrigger(InteractionTriggerType.Feed, (word)Game.CurrentAmount, ShowReceivedItems))
            {
                ShowMessage(Message.KeepYourFood);
            }
        }
    }

    private void EndClickWait()
    {
        waitForClick = false;
        Game.Cursor.CursorType = CursorType.Sword;
        HideText();
        Game.UntrapMouse();

        afterWaitClickAction?.Invoke();
        afterWaitClickAction = null;

        if (closeAfterClick)
            Game.ScreenHandler.PopScreen();
    }

    private void CheckWord(string word)
    {
        word = word.Trim();

        if (word.Length != 0)
        {
            var wordIndex = Game.AssetProvider.TextLoader.FindWord(word);

            if (wordIndex != null && TryExecuteReactionsForTrigger(InteractionTriggerType.Say, wordIndex.Value))
                return;
        }

        ShowMessage(Message.NothingToSayAboutThat);
    }

    private bool TryExecuteReactionsForTrigger(InteractionTriggerType triggerType, word param = 0, Action? finishAction = null)
    {
        var conversationData = person!.ConversationData;
        bool questCompleted = Game.State.IsQuestBitSet(conversationData.QuestCompletionIndex);
        var interactions = questCompleted ? conversationData.SecondaryInteractions : conversationData.PrimaryInteractions;

        var trigger = new InteractionTrigger(triggerType, param);
        var matchingInteraction = interactions.GetValueOrDefault(trigger);
        bool finishActionBoundToText = false;

        if (matchingInteraction != null)
        {
            foreach (var reaction in matchingInteraction.Reactions)
            {
                if (!finishActionBoundToText && reaction is ISayReaction)
                {
                    afterWaitClickAction = finishAction;
                    finishActionBoundToText = true;
                }

                ExecuteReaction(reaction);
            }

            if (!finishActionBoundToText)
                finishAction?.Invoke();

            return true;
        }

        return false;
    }

    private void ExecuteReaction(IConversationReaction reaction)
    {
        if (reaction is ISayReaction sayReaction)
        {
            ShowText(person!.ConversationData.Texts!.GetTextBlock(sayReaction.MessageIndex));
        }
        else if (reaction is ITeachWordReaction teachWordReaction)
        {
            Game.State.LearnWord(teachWordReaction.WordIndex);
        }
        else if (reaction is IGiveItemReaction giveItemReaction)
        {
            int slotIndex = giveItemReaction.ItemSlotIndex - 1;
            ItemSlot itemSlot;

            if (giveItemReaction.ItemSlotIndex < 9)
                itemSlot = person!.Equipment[(EquipmentSlot)slotIndex];
            else
                itemSlot = person!.Inventory[slotIndex - 9];

            if (itemSlot != null && receivedItems.Count < ItemSlotCount)
            {
                receivedItems.Add(new(itemSlot)); // Clone
                RequestButtonSetup();
            }
        }
        else if (reaction is IGiveGoldReaction giveGoldReaction)
        {
            Game.DistributeGold(giveGoldReaction.Amount);
            RequestButtonSetup();
        }
        else if (reaction is IGiveFoodReaction giveFoodReaction)
        {
            Game.DistributeFood(giveFoodReaction.Amount);
            RequestButtonSetup();
        }
        else if (reaction is ICompleteQuestReaction completeQuestReaction)
        {
            Game.State.SetQuestBit(completeQuestReaction.QuestIndex);
        }
        else if (reaction is IChangeStatReaction changeStatReaction)
        {
            void ChangeStat(Func<uint, uint> changeAction)
            {
                var target = Game.State.ActivePartyMember!;

                void ChangeByte(byte oldValue, Action<byte> setter)
                {
                    setter((byte)MathUtil.Limit(byte.MinValue, changeAction(oldValue), byte.MaxValue));
                }

                void ChangeWord(word oldValue, Action<word> setter)
                {
                    setter((word)MathUtil.Limit(word.MinValue, changeAction(oldValue), word.MaxValue));
                }

                void ChangeDWord(dword oldValue, Action<dword> setter)
                {
                    setter(changeAction(oldValue));
                }

                void ChangeCharacterValue(CharacterValue value, bool max)
                {
                    if (max)
                        ChangeWord(value.MaxValue, v => value.MaxValue = v);
                    else
                        ChangeWord(value.CurrentValue, v => value.CurrentValue = v);
                }

                if (changeStatReaction.StatOffset >= 0x06 && changeStatReaction.StatOffset <= 0x19)
                {
                    var skill = (Skill)(1 + (changeStatReaction.StatOffset - 6) % 10);
                    bool max = changeStatReaction.StatOffset >= 0x10;

                    ChangeCharacterValue(target.Skills[skill], max);
                }
                else if (changeStatReaction.StatOffset == 0x1B)
                {
                    ChangeByte(target.Level, v => target.Level = v);
                }
                else if (changeStatReaction.StatOffset == 0x1E)
                {
                    ChangeByte(target.Defense, v => target.Defense = v);
                }
                else if (changeStatReaction.StatOffset == 0x1F)
                {
                    ChangeByte(target.Damage, v => target.Damage = v);
                }
                else if (changeStatReaction.StatOffset == 0x20)
                {
                    ChangeByte(target.MagicBonusWeapon, v => target.MagicBonusWeapon = v);
                }
                else if (changeStatReaction.StatOffset == 0x21)
                {
                    ChangeByte(target.MagicBonusArmor, v => target.MagicBonusArmor = v);
                }
                else if (changeStatReaction.StatOffset == 0x37)
                {
                    ChangeByte((byte)target.ConversationData.LearnedLanguages, v => target.ConversationData.LearnedLanguages = (LanguageFlags)v);
                }
                else if (changeStatReaction.StatOffset == 0x3A)
                {
                    ChangeByte((byte)target.PhysicalConditions, v => target.PhysicalConditions = (PhysicalCondition)v);
                }
                else if (changeStatReaction.StatOffset == 0x3B)
                {
                    ChangeByte((byte)target.MentalConditions, v => target.MentalConditions = (MentalCondition)v);
                }
                else if (changeStatReaction.StatOffset == 0x43)
                {
                    ChangeByte(target.AttacksPerRound, v => target.AttacksPerRound = v);
                }
                else if (changeStatReaction.StatOffset == 0x46)
                {
                    ChangeWord((word)target.PossibleClasses, v => target.PossibleClasses = (ClassFlags)v);
                }
                else if (changeStatReaction.StatOffset >= 0x48 && changeStatReaction.StatOffset <= 0x6A)
                {
                    // This will fail for Age and the unused attribute but they should not be set anyways.
                    var attribute = (GameData.Attribute)(1 + (changeStatReaction.StatOffset - 0x48) % 10);
                    bool max = changeStatReaction.StatOffset >= 0x5C;

                    ChangeCharacterValue(target.Attributes[attribute], max);
                }
                else if (changeStatReaction.StatOffset == 0x86)
                {
                    ChangeCharacterValue(target.HitPoints, false);
                }
                else if (changeStatReaction.StatOffset == 0x88)
                {
                    ChangeCharacterValue(target.HitPoints, true);
                }
                else if (changeStatReaction.StatOffset == 0x8A)
                {
                    ChangeCharacterValue(target.SpellPoints, false);
                }
                else if (changeStatReaction.StatOffset == 0x8C)
                {
                    ChangeCharacterValue(target.SpellPoints, true);
                }
                else if (changeStatReaction.StatOffset == 0x8E)
                {
                    ChangeWord(target.SpellLearningPoints, v => target.SpellLearningPoints = v);
                }
                else if (changeStatReaction.StatOffset == 0x90)
                {
                    ChangeWord(target.Gold, v => target.Gold = v);
                }
                else if (changeStatReaction.StatOffset == 0x92)
                {
                    ChangeWord(target.Food, v => target.Food = v);
                }
                else if (changeStatReaction.StatOffset == 0xCC)
                {
                    ChangeDWord(target.ExperiencePoints, v => target.ExperiencePoints = v);
                }
                else if (changeStatReaction.StatOffset == 0xD0)
                {
                    ChangeDWord(target.LearnedWhiteSpells, v => target.LearnedWhiteSpells = v);
                }
                else if (changeStatReaction.StatOffset == 0xD4)
                {
                    ChangeDWord(target.LearnedGraySpells, v => target.LearnedGraySpells = v);
                }
                else if (changeStatReaction.StatOffset == 0xD8)
                {
                    ChangeDWord(target.LearnedBlackSpells, v => target.LearnedBlackSpells = v);
                }
                else if (changeStatReaction.StatOffset == 0xD8)
                {
                    ChangeDWord(target.LearnedSpecialSpells, v => target.LearnedSpecialSpells = v);
                }
                // Ignore the rest
            }

            switch (changeStatReaction.Action)
            {
                case ChangeStatAction.Increase:
                    ChangeStat(value => value + changeStatReaction.Value);
                    break;
                case ChangeStatAction.Decrease:
                    ChangeStat(value => value - changeStatReaction.Value);
                    break;
                case ChangeStatAction.ClearBit:
                    ChangeStat(value => value & ~(1u << changeStatReaction.Value));
                    break;
                case ChangeStatAction.SetBit:
                    ChangeStat(value => value | (1u << changeStatReaction.Value));
                    break;
                case ChangeStatAction.ToggleBit:
                    ChangeStat(value => value ^ (1u << changeStatReaction.Value));
                    break;
                default:
                    throw new NotSupportedException("Invalid change stat reaction type.");
            }
        }
        else
        {
            throw new NotSupportedException("Invalid person reaction type.");
        }
    }

    private void ShowMessage(Message message) => ShowText(Game.LoadMessageText(message));

    private void ShowText(IText text, bool waitForClick = true, bool closeAfterClick = false, int yOffset = 0, bool center = false)
    {
        int y = textDisplayArea.Top + yOffset;
        conversationText!.Position = new(textDisplayArea.Left, y);
        conversationText.Alignment = center ? TextAlignment.Center : TextAlignment.Left;

        conversationText!.SetText(text);
        conversationText.Visible = true;
        textScrollHandler.Attach(conversationText!);
        Game.TrapMouse(conversationText.Area);
        Game.Cursor.CursorType = CursorType.Zzz;
        this.waitForClick = waitForClick;
        this.closeAfterClick = closeAfterClick;
    }

    private void HideText()
    {
        conversationText!.Visible = false;
        textScrollHandler.Detach();
    }

    protected override void ButtonClicked(int index)
    {
        switch (index)
        {
            case 0: // Give item to party
                ShowReceivedItems();
                Game.ScreenHandler.PushScreen(ScreenType.ConversationPickupItem);
                break;
            case 1: // Drop item
                ShowReceivedItems();
                Game.ScreenHandler.PushScreen(ScreenType.ConversationDropItem);
                break;
            case 2:
                // TODO: Check for left items
                Game.ScreenHandler.PopScreen();
                break;
            case 3: // Show item
                ShowInventoryItems();
                Game.ScreenHandler.PushScreen(ScreenType.ConversationShowItem);
                break;
            case 4: // Speak
                Game.ScreenHandler.PushScreen(ScreenType.SelectWord);
                break;
            case 5: // Ask to join
            {
                bool joins = person!.ConversationData.JoinChance == 100 || (
                    person!.ConversationData.JoinChance != 0 && Game.Probe(person.ConversationData.JoinChance));

                if (!joins)
                {
                    ShowMessage(Message.NotInterestedInJoining);
                }
                else
                {
                    if (!Game.State.TryAddPartyMember(characterIndex, out _))
                    {
                        // NOTE: The original just ends silently here.
                        return;
                    }

                    void Joined()
                    {
                        (person as IPartyMember)!.SaveBit = (word)((map!.Index - 1) * IMap.CharacterCount + mapCharacterIndex);
                        Game.State.SetMapCharacterActive(map.Index, mapCharacterIndex, false); // Remove from map
                        Game.UpdatePartyMembers();
                        insideParty = true;
                        ShowReceivedItems();
                        RequestButtonSetup();
                    }

                    TryExecuteReactionsForTrigger(InteractionTriggerType.Join, 0, Joined);
                }

                break;
            }
            case 6: // Give item to person
                ShowInventoryItems();
                Game.ScreenHandler.PushScreen(ScreenType.ConversationGiveItem);
                break;
            case 7: // Give gold to person
                Game.CurrentAmount = 0;
                Game.CurrentMaxAmount = Game.State.ActivePartyMember!.Gold;
                Game.ScreenHandler.PushScreen(ScreenType.ConversationGiveGold);
                break;
            case 8: // Give food to person
                Game.CurrentAmount = 0;
                Game.CurrentMaxAmount = Game.State.ActivePartyMember!.Food;
                Game.ScreenHandler.PushScreen(ScreenType.ConversationGiveFood);
                break;
        }
    }

    public override bool KeyDown(Key key, KeyModifiers keyModifiers)
    {
        if (waitForClick)
        {
            if (textScrollHandler.KeyDown(key, keyModifiers))
                return true;

            if (key == Key.Escape || key == Key.Space || key == Key.Enter || key == Key.Down || key == Key.PageDown)
                EndClickWait();

            return true;
        }

        return base.KeyDown(key, keyModifiers);
    }

    public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
    {
        if (waitForClick)
        {
            if (textScrollHandler.MouseDown(buttons))
                return true;

            EndClickWait();
            return true;
        }

        if (buttons == MouseButtons.Right)
        {
            if (ItemContainer.IsDragging)
            {
                AbortItemDrag();
                return true;
            }

            int? characterSlotIndex = Game.TestPartyPortraitHit(position);

            if (characterSlotIndex != null)
            {
                Game.OpenInventory(characterSlotIndex.Value);
                return true;
            }
        }
        else if (buttons == MouseButtons.Left)
        {
            if (ItemContainer.IsDragging)
            {
                int? characterSlotIndex = Game.TestPartyPortraitHit(position);

                if (characterSlotIndex != null)
                {
                    // TODO: allow many stacked items? use count param
                    if (Game.TryAddItem(characterSlotIndex.Value, Game.CurrentItem!))
                    {
                        var sourceSlot = receivedItems[dragSourceItemSlot!.Value];
                        if (sourceSlot.Count == 1)
                            sourceSlot.ClearItem();
                        else
                            sourceSlot.Count--;
                        ItemContainer.ConsumeDragged(Game);
                        Game.UntrapMouse();
                        Game.ResetStatusIcons();

                        ShowReceivedItems(); // Update items
                        RequestButtonSetup();
                    }
                    else
                    {
                        AbortItemDrag();
                    }

                    return true;
                }
            }
            else
            {
                int? characterSlotIndex = Game.TestPartyPortraitHit(position);

                if (characterSlotIndex != null)
                {
                    SwitchToPartyMember(characterSlotIndex.Value);
                    return true;
                }
            }
        }

        return base.MouseDown(position, buttons, keyModifiers);
    }

    public override void MouseMove(Position position, MouseButtons buttons)
    {
        ItemContainer.UpdateDragPosition(position);

        base.MouseMove(position, buttons);
    }

    private void AbortItemDrag()
    {
        Game.UntrapMouse();
        ItemContainer.AbortDrag(Game);
        Game.ResetStatusIcons();
    }

    /// <summary>
    /// Note: This function is only used by item picker screens.
    /// In this case the message is displayed at an adjusted Y coordinate!
    /// </summary>
    internal override void ShowMessage(Message messageIndex, bool waitForClick = true, bool closeAfterClick = false)
    {
        var text = Game.LoadMessageText(messageIndex);
        bool multiline = text.GetLines(textDisplayArea.Size.Width / 6).Length > 1;
        int yOffset = MessageBaseY - textDisplayArea.Top;
        bool center = true;

        if (multiline)
        {
            yOffset -= 8;
            center = false;
        }

        ShowText(text, waitForClick, closeAfterClick, yOffset, center);
    }

    internal override void HideMessage() => HideText();

    internal override void PickItem(ScreenType sourceScreen, int? index)
    {
        switch (sourceScreen)
        {
            case ScreenType.ConversationPickupItem:
                if (index != null)
                {
                    Game.CurrentItem = ItemContainers[index.Value].Item;
                    if (Game.SetHandIconsByItem(Game.CurrentItem!) > 0) // TODO: item count
                    {
                        Game.CurrentItem = ItemContainers[index.Value].Item;
                        dragSourceItemSlot = index;
                        ItemContainers[index.Value].StartDragging();
                        Game.TrapMouseInPortraitArea();
                    }
                    else
                    {
                        Game.ResetStatusIcons();
                        ShowMessage(Message.NoMemberHasRoomForItem);
                    }
                }
                break;
            case ScreenType.ConversationDropItem:
                if (index != null)
                {
                    // TODO: Show ask message, if yes, remove it from receivedItems and update
                }
                break;
            case ScreenType.ConversationShowItem:
                if (index != null)
                {
                    if (!TryExecuteReactionsForTrigger(InteractionTriggerType.Show, (word)ItemContainers[index.Value].Item!.Index, ShowReceivedItems))
                    {
                        afterWaitClickAction = ShowReceivedItems;
                        ShowMessage(Message.NotInterestedInItem);

                    }
                }
                break;
            case ScreenType.ConversationGiveItem:
                if (index != null)
                {
                    if (!TryExecuteReactionsForTrigger(InteractionTriggerType.Give, (word)ItemContainers[index.Value].Item!.Index, () =>
                    {
                        Game.RemoveInventoryItem(Game.State.ActivePartyMember!, index.Value); // TODO: allow count > 1?
                        RequestButtonSetup();
                        ShowReceivedItems();
                    }))
                    {
                        afterWaitClickAction = ShowReceivedItems;
                        ShowMessage(Message.NotInterestedInItem);
                    }
                }
                break;
        }
    }

    private void ShowItems(List<ItemSlot> itemSlots)
    {
        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];

            if (i < itemSlots.Count)
            {
                var receivedItem = itemSlots[i];
                item.SetItem(receivedItem.Count, receivedItem.Item);
            }
            else
            {
                item.ClearItem();
            }

            item.Visible = true;
        }
    }

    public void SwitchToPartyMember(int index)
    {
        if (Game.State.ActivePartyMemberIndex == index)
            return;

        var partyMember = Game.State.SetActivePartyMember(index)!;

        goldValue!.SetText($"{partyMember.Gold:00000}");
        foodValue!.SetText($"{partyMember.Food:00000}");

        Game.UpdatePartyMembers();
        RequestButtonSetup();
    }

    private void ShowInventoryItems() => ShowItems([.. Game.State.ActivePartyMember!.Inventory]);

    private void ShowReceivedItems() => ShowItems(receivedItems);

    internal abstract class ConversationItemScreen(ScreenType screenType, Message message) : ItemPickerScreen
    {
        public override ScreenType Type { get; } = screenType;

        public override Rect MouseTrapArea { get; } = itemArea;

        public override Message Message { get; } = message;

        public override Rect ItemTooltipArea { get; } = itemTooltipArea;

        public override bool HideItemsAfterPicking { get; } = false;
    }

    internal class PickupItemScreen() : ConversationItemScreen(ScreenType.ConversationPickupItem, Message.TransferWhichItem);

    internal class DropItemScreen() : ConversationItemScreen(ScreenType.ConversationDropItem, Message.DropWhichItem);

    internal class ShowItemScreen() : ConversationItemScreen(ScreenType.ConversationShowItem, Message.ShowWhichItem);

    internal class GiveItemScreen() : ConversationItemScreen(ScreenType.ConversationGiveItem, Message.GiveWhichItem);

    internal class GiveGoldScreen : InputAmountScreen
    {
        IText? inputLabelText;

        public override ScreenType Type { get; } = ScreenType.ConversationGiveGold;
        protected override ItemGraphic Graphic { get; } = ItemGraphic.WishingCoins;
        protected override IText InputLabelText => inputLabelText!;
        protected override Message Message { get; } = Message.GiveHowMuch;

        public override void Init()
        {
            inputLabelText = Game.AssetProvider.TextLoader.LoadText(new(AssetType.UIText, (int)UIText.Gold));

            base.Init();
        }
    }

    internal class GiveFoodScreen : InputAmountScreen
    {
        IText? inputLabelText;

        public override ScreenType Type { get; } = ScreenType.ConversationGiveFood;
        protected override ItemGraphic Graphic { get; } = ItemGraphic.Ration;
        protected override IText InputLabelText => inputLabelText!;
        protected override Message Message { get; } = Message.TransferHowManyFood;

        public override void Init()
        {
            inputLabelText = Game.AssetProvider.TextLoader.LoadText(new(AssetType.UIText, (int)UIText.Food));

            base.Init();
        }
    }
}
