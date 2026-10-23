using Amber.Common;
using Amberstar.GameData;

namespace Amberstar.Game;

/// <summary>
/// Listening for persons and monsters nearby (Ear_action in EYE.ASM of the original).
/// </summary>
partial class Game
{
	const int MaxHearingIndex = 4;

	// The direction messages by (sgn(dx) + 1) + 3 * (sgn(dy) + 1) relative to ListenNothingHeard (Hear_table in the original).
	static readonly int[] HearDirectionMessages = [10, 3, 4, 9, 0, 5, 8, 7, 6];

	/// <summary>
	/// The active party member listens. If the listen probe succeeds, the rings around
	/// the party are searched for persons and monsters from the inside to the outside.
	/// The better the skill, the more rings are searched (1 to 5).
	/// </summary>
	internal void Listen()
	{
		var partyMember = State.ActivePartyMember;
		var mapScreen = ScreenHandler.FindMapScreen();

		if (partyMember == null || mapScreen == null)
			return;

		int skill = partyMember.Skills[Skill.Listen].TotalCurrent;

		if (!Probe(skill))
		{
			ShowTextMessage(Message.ListenNothingHeard);
			return;
		}

		int hearing = Math.Min(MaxHearingIndex, skill / 20);
		var characterPositions = mapScreen.GetCharacterPositions().ToHashSet();
		var partyPosition = State.PartyPosition;

		for (int ring = 1; ring <= hearing + 1; ring++)
		{
			foreach (var offset in GetRing(ring))
			{
				if (!characterPositions.Contains(partyPosition + offset))
					continue;

				string Text(Message message) => GetFullText(LoadMessageText(message));

				var direction = (Message)((int)Message.ListenNothingHeard + HearDirectionMessages[Math.Sign(offset.X) + 1 + 3 * (Math.Sign(offset.Y) + 1)]);

				// Note: The original shows the hearing range and not the real distance.
				ShowTextMessage($"{Text(Message.ListenSomethingAt)} {(hearing + 1) * 5} {Text(Message.MetersAway)} {Text(direction)}");
				return;
			}
		}

		ShowTextMessage(Message.ListenNothingHeard);
	}

	/// <summary>
	/// Gets the offsets of a ring around the center clockwise from the upper left corner (like the Spiral_table).
	/// </summary>
	static IEnumerable<Position> GetRing(int distance)
	{
		for (int x = -distance; x < distance; x++)
			yield return new(x, -distance);

		for (int y = -distance; y < distance; y++)
			yield return new(distance, y);

		for (int x = distance; x > -distance; x--)
			yield return new(x, distance);

		for (int y = distance; y > -distance; y--)
			yield return new(-distance, y);
	}
}
