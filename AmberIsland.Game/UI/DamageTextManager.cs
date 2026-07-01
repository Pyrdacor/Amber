using Amber.Common;
using AmberIsland.GameData;

namespace AmberIsland.Game.UI;

internal class DamageTextManager(Game game)
{
    private record TextWithRaiseAmount
    {
        public int RaiseAmount { get; set; } = 0;
        public required RenderText Text { get; init; }

        public void Delete() => Text.Delete();
    }

    private const int MaxTexts = 20;
    private const int DisplayLayerGap = 255 / MaxTexts;
    private const double TicksPerRaisePixel = 0.75;
    private const int FadeBeginRaiseAmount = 12;
    private const int DeleteRaiseAmount = 30;
    private const int FadeDurationDivisor = DeleteRaiseAmount - FadeBeginRaiseAmount;
    private readonly Queue<TextWithRaiseAmount> damageTexts = [];
    private long updateTicks = 0;
    private long lastRaisedTicks = 0;

    public void Spawn(Position position, string text, TextColor color)
    {
        if (damageTexts.Count == MaxTexts)
        {
            damageTexts.Dequeue().Text.Visible = false;
            int index = 0;

            foreach (var damageText in damageTexts)
                damageText.Text.DisplayLayer = (byte)(index++ * DisplayLayerGap);
        }

        var newDamageText = new RenderText(game, FontIndex.DamageFont, text, 24, position, TextAlignment.Center)
        {
            DisplayLayer = (byte)(damageTexts.Count * DisplayLayerGap),
            Shadow = true,
            Visible = true,
            Color = color
        };

        damageTexts.Enqueue(new() { Text = newDamageText });
    }

    public void Update(long elapsedTicks)
    {
        if (elapsedTicks == 0)
            return;

        updateTicks += elapsedTicks;

        long diff = updateTicks - lastRaisedTicks;

        if (diff > TicksPerRaisePixel)
        {
            int amount = MathUtil.Round(diff / TicksPerRaisePixel);
            lastRaisedTicks += (long)Math.Round(amount * TicksPerRaisePixel);

            foreach (var damageText in damageTexts.ToArray())
            {
                damageText.RaiseAmount += amount;

                if (damageText.RaiseAmount >= DeleteRaiseAmount)
                {
                    // Hacky but it will be the first in the queue.
                    damageTexts.Dequeue().Delete();
                    continue;
                }
                
                if (damageText.RaiseAmount >= FadeBeginRaiseAmount)
                {
                    float fadeFactor = FadeDurationDivisor <= 0 ? 0 : (float)(damageText.RaiseAmount - FadeBeginRaiseAmount) / FadeDurationDivisor;
                    byte alpha = (byte)Math.Clamp(255 - MathUtil.Round(fadeFactor * 255), 0, 255);
                    damageText.Text.Alpha = alpha;
                }

                damageText.Text.AnchorPosition -= new Position(0, amount);
            }
        }
    }
}
