using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using Amber.Common;
using Amber.Renderer;
using Amber.Renderer.Common;
using Amberstar.Game.Screens;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game;

internal enum ButtonLayout
{
    Movement,
    Actions
}

partial class Game
{
    const long TicksPerStatusIconChange = TicksPerSecond; // TODO

    readonly ISprite portraitBackgroundSprite;
    readonly ISprite?[] portraitSprites = new ISprite?[MaxPartyMembers];
    readonly ISprite[] playerStatusIcons = new ISprite[MaxPartyMembers];
    readonly int[] playerStatusIconIndices = new int[MaxPartyMembers];
    readonly List<StatusIcon>[] playerStatusIconTypes = new List<StatusIcon>[MaxPartyMembers];
    readonly ISprite layoutSprite;
    readonly IColoredRect?[] partyMemberNameBackgrounds = new IColoredRect?[MaxPartyMembers];
    readonly IRenderText?[] partyMemberNames = new IRenderText?[MaxPartyMembers];

    internal ButtonLayout ButtonLayout { get; set; } = ButtonLayout.Movement;
    internal Cursor Cursor { get; }
    internal Rect PortraitArea { get; } = new(16, 1, 6 * 48, 34);

    internal void SetLayout(Layout layout, byte? paletteIndex = null)
	{
		var renderLayer = GetRenderLayer(Layer.Layout);
		var textureAtlas = renderLayer.Config.Texture!;
		layoutSprite.TextureOffset = textureAtlas.GetOffset((int)layout);

		if (paletteIndex != null)
			layoutSprite.PaletteIndex = paletteIndex.Value;
	}

    internal int SetHandIconsByItem(IItem item, int count = 1)
    {
        int takerCount = 0;
        int weight = item.Weight * count;

        ForeachPartyMemberSlot((index, partyMember) =>
        {
            if (partyMember == null)
                HideStatusIcon(index);
            else if (partyMember.TotalWeight + weight <= partyMember.MaxWeight())
            {
                SetStatusIcon(index, StatusIcon.HandOpen, true, true);
                takerCount++;
            }
            else
                SetStatusIcon(index, StatusIcon.HandStop, true, true);
        });

        return takerCount;
    }

    internal int SetHandIconsByGold(int amount)
    {
        int takerCount = 0;
        long weight = amount * GoldWeight;

        ForeachPartyMemberSlot((index, partyMember) =>
        {
            if (partyMember == null)
                HideStatusIcon(index);
            else if (partyMember.Gold + amount <= short.MaxValue && partyMember.TotalWeight + weight <= partyMember.MaxWeight())
            {
                SetStatusIcon(index, StatusIcon.HandOpen, true, true);
                takerCount++;
            }
            else
                SetStatusIcon(index, StatusIcon.HandStop, true, true);
        });

        return takerCount;
    }

    internal void ResetStatusIcons()
    {
        ForeachPartyMemberSlot((index, partyMember) =>
        {
            if (partyMember == null)
                HideStatusIcon(index);
            else
            {
                var conditions = partyMember.GetConditions();

                if (conditions == Condition.None)
                    HideStatusIcon(index);
                else
                {
                    var statusIconTypes = playerStatusIconTypes[index];

                    statusIconTypes.Clear();
                    statusIconTypes.AddRange(conditions.ToStatusIcons());

                    SetStatusIcon(index, statusIconTypes[0], true);
                }
            }
        });
    }

    internal void SetStatusIcon(int partyMemberSlot, StatusIcon statusIcon,
        bool resetAnimationIndex = true, bool resetAnimationFrames = false)
    {
        var renderLayer = GetRenderLayer(Layer.UI);
        var textureAtlas = renderLayer.Config.Texture!;

        if (resetAnimationIndex || resetAnimationFrames)
            playerStatusIconIndices[partyMemberSlot] = 0;

        if (resetAnimationFrames)
        {
            playerStatusIconTypes[partyMemberSlot].Clear();
            playerStatusIconTypes[partyMemberSlot].Add(statusIcon);
        }

        playerStatusIcons[partyMemberSlot].TextureOffset = textureAtlas.GetOffset(GraphicIndexProvider.GetStatusIconIndex(statusIcon));
        playerStatusIcons[partyMemberSlot].Visible = true;
    }

    internal void HideStatusIcon(int partyMemberSlot)
    {
        playerStatusIconIndices[partyMemberSlot] = 0;
        playerStatusIcons[partyMemberSlot].Visible = false;
    }

    private void UpdateStatusIcons()
    {
        var layer = GetRenderLayer(Layer.UI);
        var textureAtlas = layer.Config.Texture!;

        foreach (var slot in GetValidPartyMemberSlots())
        {
            var statusIconTypes = playerStatusIconTypes[slot];

            if (statusIconTypes.Count <= 1)
                continue;

            long cycleTime = statusIconTypes.Count * TicksPerStatusIconChange;
            int frameIndex = (int)((gameTicks % cycleTime) / TicksPerStatusIconChange);
            var statusIconType = statusIconTypes[frameIndex];

            playerStatusIcons[slot].TextureOffset = textureAtlas.GetOffset(GraphicIndexProvider.GetStatusIconIndex(statusIconType));
        }
    }

    internal int? TestPartyPortraitHit(Position position)
    {
        if (position.X < 16 || position.X >= 304 || position.Y < 1 || position.Y >= 35)
            return null;

        int slotIndex = 1 + (position.X - 16) / 48;

        if (State.HasPartyMemberInSlot(slotIndex))
            return slotIndex;

        return null;
    }

    internal void ShowTextMessage(string text, Action? nextAction = null)
    {
        ShowTextMessage(AssetProvider.TextLoader.FromString(text), nextAction);
    }

    internal void ShowTextMessage(IText text, Action? nextAction = null)
    {
        CurrentText = text;
        EventHandler.CurrentEvent = null;
        ScreenHandler.PushScreen(ScreenType.TextBox, nextAction);
    }

    internal void ShowTextMessage(Message message, Action? nextAction = null)
    {
        var text = AssetProvider.TextLoader.LoadText(new AssetIdentifier(AssetType.Message, (int)message));
        ShowTextMessage(text, nextAction);
    }

    /// <summary>
    /// Amberstar gives some format strings in the form of:
    /// 
    /// G:01234 R:0123
    /// 
    /// Here the 012.. presents a placeholder for a number and
    /// the max digit value is the length of the number minus 1.
    /// The digits mark the digit position of the number so an
    /// inserted number should end at the last digit.
    /// </summary>
    internal static string FormatValueString(string text, params object[] args)
    {
        var regex = NumberRegex();
        int placeholderIndex = 0;

        return regex.Replace(text, (match) => args[placeholderIndex++]?.ToString()?.PadLeft(match.Length, '0') ?? string.Empty);
    }

    /// <summary>
    /// Amberstar gives some format strings in the form of:
    /// 
    /// "EP:      "
    /// "LP :    /    "
    /// 
    /// Here the spaces make room for a number. In contrast to
    /// the 0123.. placeholders, here the numbers are left aligned
    /// inside the spaces. Like "EP:123" instead of "EP:  123".
    /// 
    /// But it also depends and in the original numbers are inserted
    /// manually. We use marker characters like the colon or dash to
    /// determine the insert position so this works for different
    /// input strings and languages.
    /// </summary>
    internal static string InsertNumberIntoString<T>(string text, string marker, bool after, T number, int maxLength, char? padding = null)
        where T : struct
    {
        int markerIndex = text.IndexOf(marker);

        if (markerIndex == -1)
            throw new AmberException(ExceptionScope.Application, $"Marker '{marker}' not found in text \"{text}\".");

        string insertion = number.ToString()!;

        if (insertion.Length > maxLength)
            insertion = new string('*', maxLength);
        else if (insertion.Length < maxLength && padding != null)
            insertion = insertion.PadLeft(maxLength, padding.Value);

        int position = after ? markerIndex + marker.Length : markerIndex - insertion.Length;
        var builder = new StringBuilder(text);

        for (int i = 0; i < insertion.Length; i++)
        {
            if (text[position + i] != ' ')
                throw new AmberException(ExceptionScope.Application, $"Character at position {position + i} is not a space in text \"{text}\".");

            builder[position + i] = insertion[i];
        }

        return builder.ToString();
    }

    [GeneratedRegex("[0-9]+")]
    private static partial Regex NumberRegex();
}
