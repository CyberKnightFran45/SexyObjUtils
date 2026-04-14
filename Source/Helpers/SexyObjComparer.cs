using System.Collections.Generic;
using System.Linq;

namespace SexyObjUtils
{
/// <summary> SexyObj Table Comparer </summary>

public static class SexyObjComparer
{
// Compare objs by TypeName

private static bool CompareByTypeName(SexyObj a, SexyObj b)
{

if(a.ObjData is not IDictionary<string, object> dataA ||
   b.ObjData is not IDictionary<string, object> dataB)
{
return true;
}

bool hasA = dataA.TryGetValue("TypeName", out var typeA);
bool hasB = dataB.TryGetValue("TypeName", out var typeB);

if(hasA && hasB)
return string.Equals(typeA as string, typeB as string);

return true;
}

// Compare objs by Aliases

public static bool HasSameAliases(SexyObj a, SexyObj b)
{

if(a.Aliases != null && b.Aliases != null)
return a.Aliases.SequenceEqual(b.Aliases);

return true;
}

// Check obj for matches

private static bool Matches(SexyObj a, SexyObj b, bool strictComparing)
{

if(a.ObjClass != b.ObjClass)
return false;

if(!HasSameAliases(a, b) )
return false;

return !strictComparing || CompareByTypeName(a, b);
}

// Filter for new objects

private static bool NewObjFilter(SexyObj newObj, SexyObjTable oldTable)
{
return !oldTable.Objects.Any(oldObj => Matches(oldObj, newObj, false) );
}

// Find new Objects between two Tables

public static SexyObjTable FindAdded(SexyObjTable a, SexyObjTable b)
{
a.CheckObjs();
b.CheckObjs();

if(b.Objects.Count == 0)
return new();

var addedObjs = b.Objects.Where(n => NewObjFilter(n, a) ).ToList();

return new(a.Comment, a.Version, addedObjs);
}

// Get alias diff

private static List<string> GetAliasDiff(List<string> a, List<string> b)
{
a ??= new();
b ??= new();

return b.Except(a).ToList();
}

// Get comment diff

private static string GetCommentDiff(string a, string b) => string.Equals(a, b) ? a : b;

// Get list of objects changed between tables

private static List<SexyObj> CompareObjects(SexyObjTable a, SexyObjTable b, SexyObjDiffCriteria diffCriteria)
{
bool getChangedProps = (diffCriteria & SexyObjDiffCriteria.ChangedProps) != 0;
bool getNewProps = (diffCriteria & SexyObjDiffCriteria.AddedProps) != 0;

bool compareAlias = (diffCriteria & SexyObjDiffCriteria.AliasChanges) != 0;
bool compareComments = (diffCriteria & SexyObjDiffCriteria.ChangedComments) != 0;

List<SexyObj> changed = new();

foreach(var oldObj in a.Objects)
{
var newObj = b.Objects.FirstOrDefault(n => Matches(n, oldObj, true) );

if(newObj is null)
continue;

List<string> aliasDiff = oldObj.Aliases;

if(compareAlias && newObj.Aliases != null)
aliasDiff = GetAliasDiff(oldObj.Aliases, newObj.Aliases);

string commentDiff = oldObj.Comment;

if(compareComments)
commentDiff = GetCommentDiff(oldObj.Comment, newObj.Comment);

oldObj.ObjData.Compare(newObj.ObjData, getChangedProps, getNewProps, out var propsDiff);

if(propsDiff.Any() )
{
SexyObj obj = new(commentDiff, aliasDiff, oldObj.ObjClass, propsDiff);

changed.Add(obj);
}

}

return changed;
}

// Find objects changed between two Tables

public static SexyObjTable FindChanged(SexyObjTable a, SexyObjTable b, SexyObjDiffCriteria diffCriteria)
{
a.CheckObjs();
b.CheckObjs();

if(b.Objects.Count == 0)
return new();

var changedObjs = CompareObjects(a, b, diffCriteria);

return new(changedObjs);
}

// Get full difference between two Tables

public static SexyObjTable FullDiff(SexyObjTable a, SexyObjTable b, SexyObjDiffCriteria diffCriteria)
{
var diff = FindChanged(a, b, diffCriteria);
var added = FindAdded(a, b);

foreach(var obj in added.Objects)
diff.Objects.Add(obj);

return diff;
}

}

}