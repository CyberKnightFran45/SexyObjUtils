using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;

namespace SexyObjUtils
{
/// <summary> Handles the content of SexyObjTables such as Sorting, Comparing or Spliting </summary>

public static class SexyObjMgr
{
// String comparer for obj names

private static readonly StringComparer comparer = StringComparer.Ordinal;

#region ==============  SORTER   ==============

// Sort by Alias

private static IOrderedEnumerable<SexyObj> SortByAlias(List<SexyObj> objs)
{
return objs.OrderBy(o => SexyObjHelper.GetAlias(o), comparer);
}

// Sort by ClassName

private static IOrderedEnumerable<SexyObj> SortByClassName(List<SexyObj> objs)
{
return objs.OrderBy(o => o.ObjClass, comparer);
}

// Sort by TypeName

private static IOrderedEnumerable<SexyObj> SortByTypeName(List<SexyObj> objs)
{

return objs.OrderBy(o => o.ObjClass, comparer)
           .ThenBy(o => SexyObjHelper.GetAlias(o), comparer);

}

// Get sorted list of objects (by Ascending)

private static List<SexyObj> SortObjs(List<SexyObj> objs, SexyObjSortCriteria criteria)
{
	
var sorted = criteria switch
{
SexyObjSortCriteria.Alias => SortByAlias(objs),
SexyObjSortCriteria.Type => SortByTypeName(objs),
_ => SortByClassName(objs)
};

return sorted.ToList();
}

// Sort properties

private static void SortProps(List<SexyObj> objs)
{

foreach(var o in objs)
{

if (o.ObjData is not IDictionary<string, object> dict)
continue;

var sortedDict = dict.OrderBy(p => p.Key, comparer).ToDictionary(p => p.Key, p => p.Value);

o.ObjData = ExpandObjPlugin.ToExpandoObject(sortedDict);   
}

}

// Sort table

public static void Sort(SexyObjTable srcTable, SexyObjSortCriteria criteria, bool sortProperties)
{

if(SexyObjHelper.IsNullOrEmpty(srcTable) )
return;

var sortedObjs = SortObjs(srcTable.Objects, criteria);

if(sortProperties)
SortProps(sortedObjs);

srcTable.Objects = sortedObjs;
}

// Sort JSON

public static void SortFile(string inputPath, SexyObjSortCriteria criteria, bool sortProperties)
{
TraceLogger.Init();
TraceLogger.WriteLine("SexyObjTable Sort Started");

try
{
string propsFlags = sortProperties ? "- Properties sort" : "";
TraceLogger.WriteDebug($"{inputPath} ({criteria} {propsFlags})");

TraceLogger.WriteActionStart("Loading table...");
var srcTable = SexyObjTable.Read(inputPath);

TraceLogger.WriteActionEnd();

TraceLogger.WriteActionStart("Sorting table...");
Sort(srcTable, criteria, sortProperties);

TraceLogger.WriteActionEnd();

TraceLogger.WriteActionStart("Saving json...");

string outputPath = SexyObjHelper.BuildPath(inputPath, "sorted");
using var outFile = FileManager.OpenWrite(outputPath);

JsonSerializer.SerializeObject(srcTable, outFile);

TraceLogger.WriteActionEnd();
}

catch(Exception error)
{
TraceLogger.WriteError(error, "Failed to Sort file");
}

TraceLogger.WriteLine("SexyObjTable Sort Finished");
}

#endregion


#region ==============  COMPARER  ==============

// Compare tables

public static void Compare(SexyObjTable a, SexyObjTable b,
                           SexyTableCompareMode compareMode,
						   SexyObjDiffCriteria diffCriteria,
                           out SexyObjTable oldDiff, out SexyObjTable newDiff)
{

switch(compareMode)
{
case SexyTableCompareMode.Changed:

oldDiff = SexyObjComparer.FindChanged(b, a, diffCriteria);
newDiff = SexyObjComparer.FindChanged(a, b, diffCriteria);
break;

case SexyTableCompareMode.FullDiff:

oldDiff = SexyObjComparer.FullDiff(b, a, diffCriteria);
newDiff = SexyObjComparer.FullDiff(a, b, diffCriteria);
break;

default:

oldDiff = null;
newDiff = SexyObjComparer.FindAdded(a, b);
break;
}

}

// Compare JSON

public static void CompareFiles(string oldPath, string newPath,
                                SexyTableCompareMode compareMode,
						        SexyObjDiffCriteria diffCriteria)
{
TraceLogger.Init();
TraceLogger.WriteLine("SexyObjTable Comparison Started");

try
{
TraceLogger.WriteDebug($"{oldPath} vs. {newPath} (Mode: {compareMode} - {diffCriteria})");

TraceLogger.WriteActionStart("Loading tables...");

var tableA = SexyObjTable.Read(oldPath);
var tableB = SexyObjTable.Read(newPath);

TraceLogger.WriteActionEnd();

TraceLogger.WriteActionStart("Comparing tables...");
Compare(tableA, tableB, compareMode, diffCriteria, out var oldDiff, out var newDiff);

TraceLogger.WriteActionEnd();

TraceLogger.WriteActionStart("Saving diff...");

if(oldDiff != null)
{
string pathToOldDif = SexyObjHelper.BuildPath(newPath, "before");
using var jsonDifOld = FileManager.OpenWrite(pathToOldDif);

JsonSerializer.SerializeObject(oldDiff, jsonDifOld);
}

var suffix = compareMode == SexyTableCompareMode.Added ? "new" : "after";
string pathToNewDif = SexyObjHelper.BuildPath(oldPath, suffix);

using var jsonDifNew = FileManager.OpenWrite(pathToNewDif);
JsonSerializer.SerializeObject(newDiff, jsonDifNew);

TraceLogger.WriteActionEnd();
}

catch(Exception error)
{
TraceLogger.WriteError(error, "Failed to Compare files");
}

TraceLogger.WriteLine("SexyObjTable Comparison Finished");
}

#endregion


#region ==============  UPDATER  ==============

// Add Missing Props to Obj

private static void AddMissingProps(ExpandoObject existing, ExpandoObject updated)
{


if(existing is not IDictionary<string, object> existingDict ||
   updated is not IDictionary<string, object> newDict)
{
return;
}

foreach(var prop in newDict)
{

if(!existingDict.ContainsKey(prop.Key) )
existingDict[prop.Key] = prop.Value;

}

}

// Old obj filter

private static bool OldContentFilter(SexyObj oldObj, SexyObj newObj)
{

if(oldObj.ObjClass != newObj.ObjClass)
return false;

return SexyObjComparer.HasSameAliases(oldObj, newObj);
}

// Update table

public static void Update(SexyObjTable oldTable, SexyObjTable newTable)
{

if(SexyObjHelper.IsNullOrEmpty(oldTable) )
return;

var diff = SexyObjComparer.FullDiff(oldTable, newTable, SexyObjDiffCriteria.AddedProps);

foreach(var newObj in diff.Objects)
{
var existingObj = oldTable.Objects.FirstOrDefault(o => OldContentFilter(o, newObj) );

if(existingObj != null)
AddMissingProps(existingObj.ObjData, newObj.ObjData);

else
oldTable.Objects.Add(newObj);

}

}

// Update JSON

public static void UpdateFile(string oldPath, string newPath)
{
TraceLogger.Init();
TraceLogger.WriteLine("SexyObjTable Update Started");

try
{
TraceLogger.WriteDebug($"{oldPath} vs. {newPath}");

TraceLogger.WriteActionStart("Loading tables...");

var oldTable = SexyObjTable.Read(oldPath);
var newTable = SexyObjTable.Read(newPath);

TraceLogger.WriteActionEnd();

TraceLogger.WriteActionStart("Updating table...");
Update(oldTable, newTable);

TraceLogger.WriteActionEnd();

string outPath = SexyObjHelper.BuildPath(oldPath, "updated");

TraceLogger.WriteActionStart("Saving json...");

using var outFile = FileManager.OpenWrite(outPath);
JsonSerializer.SerializeObject(oldTable, outFile);

TraceLogger.WriteActionEnd();
}

catch(Exception error)
{
TraceLogger.WriteError(error, "Failed to Update file");
}

TraceLogger.WriteLine("SexyObjTable Update Finished");
}

#endregion


#region ==============  SPLIT/MERGE  ==============
 
// Get Obj name

private static string GetObjName(SexyObj fragment, bool isBaseObj, bool isUniqueObj)
{
var typeName = SexyObjHelper.HasTypeName(fragment) ? $"_{SexyObjHelper.GetTypeName(fragment)}" : "";
var aliasSuffix = fragment.Aliases?.FirstOrDefault() != null ? $"_{fragment.Aliases[0]}" : typeName;

if(isBaseObj)
return aliasSuffix.Trim('_');

return isUniqueObj ? fragment.ObjClass : fragment.ObjClass + aliasSuffix;
}

/// <summary> Split table into smaller files </summary>

public static void Split(string inputPath)
{
TraceLogger.Init();
TraceLogger.WriteLine("SexyObjTable Split Started");

try
{
string outDir = inputPath;
PathHelper.ChangeExtension(ref outDir, ".split_obj");

TraceLogger.WriteDebug($"{inputPath} --> {outDir}");

TraceLogger.WriteActionStart("Loading table...");
var srcTable = SexyObjTable.Read(inputPath);

TraceLogger.WriteActionEnd();

if(srcTable.Objects.Count == 0) 
{
TraceLogger.WriteError("Table has no objects to split.");

return;
}

if(srcTable.Objects.Count == 1) 
{
TraceLogger.WriteWarn("Single object found in table. Spliting is unnecesary");

return;
}

var classCounts = srcTable.Objects.GroupBy(o => o.ObjClass).ToDictionary(g => g.Key, g => g.Count() );
bool isBaseObj = classCounts.Values.Any(count => count > 1);

TraceLogger.WriteActionStart("Spliting objects...");
Directory.CreateDirectory(outDir);

foreach(var fragment in srcTable.Objects)
{
bool isUniqueObj = classCounts[fragment.ObjClass] == 1;

string objName = GetObjName(fragment, isBaseObj, isUniqueObj);
string filePath = Path.Combine(outDir, objName + ".json");

using var outFile = FileManager.OpenWrite(filePath);
JsonSerializer.SerializeObject(fragment, outFile);
}

TraceLogger.WriteActionEnd();
}

catch(Exception error)
{
TraceLogger.WriteError(error, "Failed to Split files");
}

TraceLogger.WriteLine("SexyObjTable Split Finished");
}

// Merge objs from dir

private static SexyObjTable MergeObjs(string inputDir)
{
var files = Directory.GetFiles(inputDir, "*.json", SearchOption.AllDirectories);
SexyObjTable merged = new();

foreach(string path in files)
{
var obj = SexyObj.Read(path);

if(obj != null)
merged.Objects.Add(obj);

}

return merged;
}

// Build outPath for merging

private static string BuildMergePath(string inputDir)
{

if(inputDir.Contains("Split") )
return inputDir.Replace("Split", "Merge") + ".json";

return SexyObjHelper.BuildPath(inputDir, "Merge");
}

// Merge files into a bigger one

public static void Merge(string inputDir)
{
TraceLogger.Init();
TraceLogger.WriteLine("SexyObjTable Merge Started");

try
{

if(!Directory.Exists(inputDir) )
throw new DirectoryNotFoundException($"Missing folder: <{inputDir}>");

TraceLogger.WriteActionStart("Merging objects...");
var merged = MergeObjs(inputDir);

TraceLogger.WriteActionEnd();

string outPath = BuildMergePath(inputDir);
PathHelper.CheckDuplicatedPath(ref outPath);

TraceLogger.WriteActionStart("Saving json...");

using var outFile = FileManager.OpenWrite(outPath);
JsonSerializer.SerializeObject(merged, outFile);

TraceLogger.WriteActionEnd();
}

catch(Exception error)
{
TraceLogger.WriteError(error, "Failed to Merge files");
}

TraceLogger.WriteLine("SexyObjTable Merge Finished");
}

#endregion
}

}