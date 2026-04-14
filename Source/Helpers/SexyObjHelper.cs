using System.Collections.Generic;
using System.IO;

namespace SexyObjUtils
{
/// <summary> Some useful Tasks for SexyObjs </summary>

internal static class SexyObjHelper
{
// BuildPath

public static string BuildPath(string sourcePath, string suffix)
{
string baseDir = Path.GetDirectoryName(sourcePath);
string fileName = Path.GetFileNameWithoutExtension(sourcePath);

string outputPath = Path.Combine(baseDir, $"{fileName}_{suffix}.json");
PathHelper.CheckDuplicatedPath(ref outputPath);

return outputPath;
}

// Get alias

public static string GetAlias(SexyObj obj)
{
var aliases = obj.Aliases;

return aliases != null && aliases.Count > 0 ? aliases[0] : "";
}

// Check if ObjData has 'TypeName' field

public static bool HasTypeName(SexyObj obj)
{

if(obj.ObjData is not IDictionary<string, object> data)
return false;

return data.ContainsKey("TypeName");
}

// Get type name

public static string GetTypeName(SexyObj obj)
{

if(obj.ObjData is not IDictionary<string, object> data)
return "";

return data.TryGetValue("TypeName", out var type) ? type.ToString() : "";
}

// Check if table is null or empty

public static bool IsNullOrEmpty(SexyObjTable table)
{
return table is null || table.Objects is null || table.Objects.Count == 0;
}

}

}