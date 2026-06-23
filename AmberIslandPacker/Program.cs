// TODO: For now we just pack a single file into a container :>

using Amber.IO.FileFormats.Compression;
using AmberIsland.GameData;

//string filename = @"D:\Projects\Amber\AmberIsland\assets\tilesets\tileset_data";
string filename = @"D:\Projects\Amber\AmberIsland\assets\maps\map_data";
//string containerPath = @"D:\Projects\Amber\AmberIsland\assets\tileset.aifc";
string containerPath = @"D:\Projects\Amber\AmberIsland\assets\map.aifc";

//var fileData = File.ReadAllBytes(filename);
var fileData = Deflate.Compress(File.ReadAllBytes(filename));
using var stream = File.Create(containerPath);
FileContainer.Write(stream, new() { { 1u, fileData } });