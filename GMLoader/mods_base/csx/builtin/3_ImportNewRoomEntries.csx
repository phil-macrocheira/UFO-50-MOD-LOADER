string[] dirFiles = Directory.GetFiles(roomPath, "*.json")
                             .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                             .ToArray();

if (dirFiles.Length == 0)
    return;

int stubbed = 0;

foreach (string file in dirFiles)
{
    string name = Path.GetFileNameWithoutExtension(file);

    if (Data.Rooms.ByName(name) != null)
        continue;

    UndertaleString nameStr = Data.Strings.MakeString(name);

    UndertaleRoom stub = new UndertaleRoom();
    stub.Name = nameStr;

    Data.Rooms.Add(stub);
    stubbed++;

    Log.Information($"Adding {name}");
}