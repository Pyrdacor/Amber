namespace Amberstar.GameData.Serialization;

public interface IPersonLoader
{
	IPerson LoadPerson(int index);

    IReadOnlyDictionary<int, IPerson> LoadAllPersons();

    Dictionary<int, IPartyMember> GetPartyMemberCopies();
    /// <summary>
    /// Writes the character record of a party member (like CHARDATA.AMB of the original).
    /// </summary>
    byte[] WritePartyMember(IPartyMember partyMember);
    /// <summary>
    /// Reads a character record which was written by <see cref="WritePartyMember"/>.
    /// </summary>
    IPartyMember ReadPartyMember(int index, byte[] data);
}
