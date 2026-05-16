namespace Amberstar.GameData.Serialization;

public interface IPersonLoader
{
	IPerson LoadPerson(int index);

	Dictionary<int, IPartyMember> GetPartyMemberCopies();
}
