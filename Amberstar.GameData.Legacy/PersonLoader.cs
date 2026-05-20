using Amber.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy
{
	public class PersonLoader(AssetProvider assetProvider, Lazy<ITextLoader> textLoader) : IPersonLoader
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

        public IReadOnlyDictionary<int, IPerson> LoadAllPersons()
        {
            var keys = assetProvider.GetAssetKeys(AssetType.Person);

            if (persons.Count == keys.Count)
                return persons.AsReadOnly();

            foreach (var key in keys)
            {
                if (persons.ContainsKey(key))
                    continue;

                var asset = assetProvider.GetAsset(new(AssetType.Person, key));

                if (asset == null)
                    throw new AmberException(ExceptionScope.Data, $"Person {key} not found.");

                var person = key == 1 ? PartyMember.Load(asset, textLoader.Value) : Person.Load(asset, textLoader.Value);
                persons.Add(key, person);
            }

            return persons.AsReadOnly();
        }

        public Dictionary<int, IPartyMember> GetPartyMemberCopies()
        {
            var keys = assetProvider.GetAssetKeys(AssetType.Person);

            IEnumerable<(int Index, IPartyMember PartyMember)> GetCopies()
            {
                foreach (var key in keys)
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
