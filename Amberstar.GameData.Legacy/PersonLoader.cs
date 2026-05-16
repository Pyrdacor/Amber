using Amber.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy
{
	public class PersonLoader(Amber.Assets.Common.IAssetProvider assetProvider, Lazy<ITextLoader> textLoader, ICollection<int> personKeys) : IPersonLoader
    {
		readonly Dictionary<int, IPerson> persons = [];

		public IPerson LoadPerson(int index)
		{
			if (!persons.TryGetValue(index, out var person))
			{
				var asset = assetProvider.GetAsset(new(AssetType.Person, index));

				if (asset == null)
					throw new AmberException(ExceptionScope.Data, $"Person {index} not found.");

                // Normally the join chance determines if it is a party member or NPC.
                // But for the hero (index 1) this is also 0, so we need to check the index here.
                person = index == 1 ? PartyMember.Load(asset, textLoader.Value) : Person.Load(asset, textLoader.Value);
				persons.Add(index, person);
			}

			return person;
		}

        public Dictionary<int, IPartyMember> GetPartyMemberCopies()
        {
            IEnumerable<(int Index, IPartyMember PartyMember)> GetCopies()
            {
                foreach (var key in personKeys)
                {
                    var person = LoadPerson(key);

                    if (person is IPartyMember partyMember)
                        yield return (key, partyMember.Clone());
                }
            }

            return GetCopies().ToDictionary(x => x.Index, x => x.PartyMember);
        }
    }
}
