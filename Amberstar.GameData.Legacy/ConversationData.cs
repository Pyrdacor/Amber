using Amber.Assets.Common;
using Amber.Common;
using Amber.Serialization;
using Amberstar.GameData.Serialization;
using System.Collections.ObjectModel;

namespace Amberstar.GameData.Legacy;

internal class ConversationData : IConversationData
{
    private class ConversationInteraction(List<IConversationReaction> reactions) : IConversationInteraction
    {
        public ReadOnlyCollection<IConversationReaction> Reactions { get; } = reactions.AsReadOnly();

        public static IConversationReaction CreateReaction(byte type, byte arg0, byte arg1, word arg2)
        {
            return type switch
            {
                1 => new SayReaction { MessageIndex = arg2 },
                2 => new TeachWordReaction { WordIndex = arg2 },
                3 => new GiveItemReaction { ItemSlotIndex = arg2 },
                4 => new GiveGoldReaction { Amount = arg2 },
                5 => new GiveFoodReaction { Amount = arg2 },
                6 => new CompleteQuestReaction { QuestIndex = (byte)arg2 },
                7 => new ChangeStatReaction { StatOffset = arg0, Action = (ChangeStatAction)arg1, Value = arg2 },
                _ => throw new AmberException(ExceptionScope.Data, $"Unknown conversation reaction type {type}."),
            };
        }
    }

    #region Reactions

    private class SayReaction : ISayReaction
    {
        public int MessageIndex { get; init; }
    }

    private class TeachWordReaction : ITeachWordReaction
    {
        public int WordIndex { get; init; }
    }

    private class GiveItemReaction : IGiveItemReaction
    {
        public int ItemSlotIndex { get; init; }
    }

    private class GiveGoldReaction : IGiveGoldReaction
    {
        public word Amount { get; init; }
    }

    private class GiveFoodReaction : IGiveFoodReaction
    {
        public word Amount { get; init; }
    }

    private class CompleteQuestReaction : ICompleteQuestReaction
    {
        public byte QuestIndex { get; init; }
    }

    private class ChangeStatReaction : IChangeStatReaction
    {
        public byte StatOffset { get; init; }

        public ChangeStatAction Action { get; init; }

        public word Value { get; init; }
    }

    #endregion

    private readonly Dictionary<InteractionTrigger, IConversationInteraction> primaryInteractions;
    private readonly Dictionary<InteractionTrigger, IConversationInteraction> secondaryInteractions;

    private ConversationData(Dictionary<InteractionTrigger, IConversationInteraction> primaryInteractions,
        Dictionary<InteractionTrigger, IConversationInteraction> secondaryInteractions)
    {
        this.primaryInteractions = primaryInteractions;
        this.secondaryInteractions = secondaryInteractions;
    }

    public static ConversationData Load(IDataReader reader, ITextLoader textLoader)
    {
        reader.Position = 0x37;
        var learnedLanguages = (LanguageFlags)reader.ReadByte();

        reader.Position = 0x3c;
        var joinChance = reader.ReadByte();
        var questCompletionIndex = reader.ReadByte();

        reader.Position = 0x58;
        var currentAge = reader.ReadWord();
        reader.Position = 0x6c;
        var maxAge = reader.ReadWord();

        reader.Position = 0x47a;
        var interactionTriggers = reader.ReadBytes(20);
        var interactionTriggerValues = reader.ReadWords(20);
        var reactionTypes = reader.ReadBytes(5 * 20);
        var reactionArguments0 = reader.ReadBytes(5 * 20);
        var reactionArguments1 = reader.ReadBytes(5 * 20);
        var reactionArguments2 = reader.ReadWords(5 * 20);
        var primaryInteractions = new Dictionary<InteractionTrigger, IConversationInteraction>();
        var secondaryInteractions = new Dictionary<InteractionTrigger, IConversationInteraction>();

        void LoadInteractions(int sourceStartIndex, Dictionary<InteractionTrigger, IConversationInteraction> targetCollection)
        {
            for (int i = 0; i < 10; i++)
            {
                var trigger = interactionTriggers[sourceStartIndex + i];

                if (trigger == 0)
                    continue;

                var interactionTrigger = new InteractionTrigger((InteractionTriggerType)trigger, (word)interactionTriggerValues[sourceStartIndex + i]);
                int reactionOffset = (sourceStartIndex + i) * 5;
                var reactions = new List<IConversationReaction>(5);

                for (int r = 0; r < 5; r++)
                {
                    var reactionType = reactionTypes[reactionOffset + r];

                    if (reactionType == 0)
                        continue;

                    var arg0 = reactionArguments0[reactionOffset + r];
                    var arg1 = reactionArguments1[reactionOffset + r];
                    var arg2 = (word)reactionArguments2[reactionOffset + r];

                    reactions.Add(ConversationInteraction.CreateReaction(reactionType, arg0, arg1, arg2));
                }

                targetCollection.Add(interactionTrigger, new ConversationInteraction(reactions));
            }
        }

        LoadInteractions(0, primaryInteractions);
        LoadInteractions(10, secondaryInteractions);

        reader.Position = 0x6aa;
        var portrait = reader.Position > reader.Size - 2 || reader.PeekWord() == 0 ? null : GraphicLoader.LoadGraphicWithHeader(reader);

        if (portrait == null && reader.Position <= reader.Size - 2)
            reader.Position += 2;

        var texts = reader.Position > reader.Size - 2 || reader.PeekWord() == 0 ? null : textLoader.ReadText(reader);

        return new(primaryInteractions, secondaryInteractions)
        {
            Age = new(currentAge, maxAge, 0),
            LearnedLanguages = learnedLanguages,
            JoinChance = joinChance,
            QuestCompletionIndex = questCompletionIndex,
            Portrait = portrait,
            Texts = texts
        };
    }

    public required CharacterValue Age { get; init; }
    public LanguageFlags LearnedLanguages { get; set; }
    public byte JoinChance { get; init; }
    public byte QuestCompletionIndex { get; init; }
    public IGraphic? Portrait { get; init; }
    public IText? Texts { get; init; }
    public ReadOnlyDictionary<InteractionTrigger, IConversationInteraction> PrimaryInteractions => primaryInteractions.AsReadOnly();
    public ReadOnlyDictionary<InteractionTrigger, IConversationInteraction> SecondaryInteractions => secondaryInteractions.AsReadOnly();

    public IConversationData Clone()
    {
        return new ConversationData(primaryInteractions, secondaryInteractions)
        {
            Age = Age.Copy(),
            LearnedLanguages = LearnedLanguages,
            JoinChance = JoinChance,
            QuestCompletionIndex = QuestCompletionIndex,
            Portrait = Portrait,
            Texts = Texts
        };
    }
}
