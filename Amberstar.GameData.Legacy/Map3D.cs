using Amber.Common;
using Amber.IO.Common.Serialization;

namespace Amberstar.GameData.Legacy;

internal class Map3D : Map, IMap3D
{
	private Map3D(MapHeader header, MapCharacter[] characters, PositionList[] characterPositions,
		LabTile[] labTiles, Tile3D[] tiles)
		: base(header, characters, characterPositions)
	{
		Tiles = tiles;
		LabTiles = labTiles;
	}

	public int LabDataIndex => header.LabdataIndex;
	public Tile3D[] Tiles { get; }
	public LabTile[] LabTiles { get; }

	public static unsafe Map3D Load(int id, MapHeader header, IDataReader reader)
	{
		if (header.MapType != MapType.Map3D)
			throw new AmberException(ExceptionScope.Data, $"Map {id} is not a 3D map.");

		int mapSize = header.Width * header.Height;

		int numLabTiles = reader.ReadByte();
		var labTileFlags = new LabTileFlags[numLabTiles];
		var labTiles = new LabTile[numLabTiles];

		for (int i = 0; i < numLabTiles; i++)
			labTileFlags[i] = (LabTileFlags)reader.ReadDword();

		var labTilePrimaryIndices = reader.ReadBytes(numLabTiles);
		var labTileSecondaryIndices = reader.ReadBytes(numLabTiles);
		var labTileColors = reader.ReadBytes(numLabTiles);

		for (int i = 0; i < numLabTiles; i++)
		{
			labTiles[i] = new LabTile()
			{
				Flags = labTileFlags[i],
				PrimaryLabBlockIndex = labTilePrimaryIndices[i],
				SecondaryLabBlockIndex = labTileSecondaryIndices[i],
				MinimapColorIndex = labTileColors[i]
			};
		}

		var labTileIndices = reader.ReadBytes(mapSize);
		var events = reader.ReadBytes(mapSize);

		var tiles = Enumerable.Range(0, mapSize).Select(index => new Tile3D
		{
			LabTileIndex = labTileIndices[index],
			Event = events[index]
		}).ToArray();

		word* index = header.CharacterData;
		byte* icon = header.CharacterIcon;
		byte* move = header.CharacterMove;
		byte* flags = header.CharacterFlags;
		byte* day = header.CharacterDay;
		byte* month = header.CharacterMonth;

		var characters = new MapCharacter[IMap.CharacterCount];
		var positionCountPerCharacters = new int[IMap.CharacterCount];

		for (int i = 0; i < characters.Length; i++)
		{
			var character = new MapCharacter();

			character.Index = *index++;
			character.Icon = *icon++;
			var characterFlags = *flags++;
			character.TravelType = *move++;
			character.Type = (characterFlags & 0x1) != 0 ? MapCharacterType.Monster
				: (characterFlags & 0x10) != 0 ? MapCharacterType.Popup
				: MapCharacterType.Person;
			character.WalkType = character.Type == MapCharacterType.Monster
				? ((characterFlags & 0x04) != 0 ? MapCharacterWalkType.Chase : MapCharacterWalkType.Stationary)
				: ((characterFlags & 0x02) != 0 ? MapCharacterWalkType.Random : MapCharacterWalkType.Path);
			bool hasSpawnDate = (characterFlags & 0x08) != 0;
			var spawnDay = *day++;
			var spawnMonth = *month++;
			character.Day = hasSpawnDate ? spawnDay : (byte)0xff;
			character.Month = hasSpawnDate ? spawnMonth : (byte)0xff;		

			characters[i] = character;

			positionCountPerCharacters[i] = character.Index == 0
				? 0
				: character.WalkType == MapCharacterWalkType.Path
					? 288
					: 1;
		}

		int totalCharacterPositions = positionCountPerCharacters.Sum();
		var coords = reader.ReadBytes(totalCharacterPositions * 2);
		var characterPositions = new PositionList[IMap.CharacterCount];
		int offset = 0;

		for (int i = 0; i < characters.Length; i++)
		{
			int positionCount = positionCountPerCharacters[i];
            var positions = new Position[positionCount];

			if (positionCount == 1)
			{
				positions[0] = new(coords[offset], coords[offset + 1]);
				offset += 2;
			}
			else if (positionCount == 288)
			{
				var x = coords[offset..(offset + 288)];
				offset += 288;
                var y = coords[offset..(offset + 288)];
                offset += 288;

                for (int p = 0; p < positions.Length; p++)
				{
					positions[p] = new Position(x[p], y[p]);
				}
			}

			characterPositions[i] = positions;
		}

		return new Map3D(header, characters, characterPositions, labTiles, tiles);
	}
}
