namespace Amberstar.GameData.Serialization;

public interface IPersonLoader
{
	IPerson LoadPerson(int index);

    IReadOnlyDictionary<int, IPerson> LoadAllPersons();

    Dictionary<int, IPartyMember> GetPartyMemberCopies();
}
