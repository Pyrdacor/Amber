using Amber.Assets.Common;
using System.Collections.ObjectModel;

namespace Amberstar.GameData;

public interface IConversationData
{
    CharacterValue Age { get; init; }
    LanguageFlags LearnedLanguages { get; set; }
    byte JoinChance { get; init; }
    byte QuestCompletionIndex { get; init; }
    IGraphic? Portrait { get; init; }
    IText? Texts { get; init; }
    ReadOnlyDictionary<InteractionTrigger, IConversationInteraction> PrimaryInteractions { get; }
    ReadOnlyDictionary<InteractionTrigger, IConversationInteraction> SecondaryInteractions { get; }

    IConversationData Clone();
}    
