using System;

namespace SexyObjUtils
{
/// <summary> Diff criterias for Comparing obj changes </summary>

[Flags]

public enum SexyObjDiffCriteria
{
None = 0,

ChangedProps = 1,
AddedProps = 2,
AliasChanges = 4,
ChangedComments = 8,

Default = ChangedProps | AddedProps,
All = ChangedProps | AddedProps | AliasChanges | ChangedComments
}

}