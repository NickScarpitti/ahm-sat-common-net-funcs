﻿using System.Collections.Concurrent;
using System.Data;
using System.Reflection;
using static System.Convert;

namespace Common_Net_Funcs.Conversion;
public static class DataTableConversion
{
    /// <summary>
    /// Convert datatable to equivalent list of specified class
    /// </summary>
    /// <typeparam name="T">Class to use in table conversion</typeparam>
    /// <param name="table">Table to convert to list</param>
    /// <param name="convertShortToBool">Allow checking for parameters that are short values in the table that correlate to a bool parameter when true</param>
    /// <returns>List containing table values as the specified class</returns>
    public static List<T?> ConvertDataTableToList<T>(DataTable table, bool convertShortToBool = false) where T : class, new()
    {
        List<(DataColumn DataColumn, PropertyInfo PropertyInfo, bool IsShort)> map = [];

        List<T?> list = new(table.Rows.Count);

        if (table.Rows.Count > 0)
        {
            DataRow firstRow = table.Rows[0];
            foreach (PropertyInfo propertyInfo in typeof(T).GetProperties())
            {
                if (table.Columns.Contains(propertyInfo.Name))
                {
                    if (convertShortToBool)
                    {
                        Type colType = firstRow[table.Columns[propertyInfo.Name]!].GetType();
                        map.Add(new(table.Columns[propertyInfo.Name]!, propertyInfo, convertShortToBool && (colType == typeof(short) || colType == typeof(short?))));
                    }
                    else
                    {
                        map.Add((table.Columns[propertyInfo.Name]!, propertyInfo, false));
                    }
                }
            }

            foreach (DataRow row in table.AsEnumerable())
            {
                if (row == null)
                {
                    list.Add(null);
                    continue;
                }
                T item = new();
                foreach ((DataColumn DataColumn, PropertyInfo PropertyInfo, bool IsShort) pair in map)
                {
                    object? value = row[pair.DataColumn!];

                    //Handle issue where DB returns Int16 for boolean values
                    if (pair.IsShort && (pair.PropertyInfo!.PropertyType == typeof(bool) || pair.PropertyInfo!.PropertyType == typeof(bool?)))
                    {
                        pair.PropertyInfo!.SetValue(item, value is not System.DBNull ? ToBoolean(value) : null);
                    }
                    else
                    {
                        pair.PropertyInfo!.SetValue(item, value is not System.DBNull ? value : null);
                    }
                }
                list.Add(item);
            }
        }
        return list;
    }

    /// <summary>
    /// Convert datatable to equivalent list of specified class using a Parallel.Foreach loop to get data from each row
    /// </summary>
    /// <typeparam name="T">Class to use in table conversion</typeparam>
    /// <param name="table">Table to convert to list</param>
    /// <param name="maxDegreeOfParallelism">Parallelism parameter to be used in Parallel.Foreach loop</param>
    /// <param name="convertShortToBool">Allow checking for parameters that are short values in the table that correlate to a bool parameter when true</param>
    /// <returns>List containing table values as the specified class</returns>
    public static List<T?> ConvertDataTableToListParallel<T>(DataTable table, int maxDegreeOfParallelism = -1, bool convertShortToBool = false) where T : class, new()
    {
        ConcurrentBag<(DataColumn DataColumn, PropertyInfo PropertyInfo, bool IsShort)> map = [];

        ConcurrentBag<T?> bag = [];

        if (table.Rows.Count > 0)
        {
            DataRow firstRow = table.Rows[0];
            foreach (PropertyInfo propertyInfo in typeof(T).GetProperties())
            {
                if (table.Columns.Contains(propertyInfo.Name))
                {
                    if (convertShortToBool)
                    {
                        Type colType = firstRow[table.Columns[propertyInfo.Name]!].GetType();
                        map.Add((table.Columns[propertyInfo.Name]!, propertyInfo, convertShortToBool && (colType == typeof(short) || colType == typeof(short?))));
                    }
                    else
                    {
                        map.Add((table.Columns[propertyInfo.Name]!, propertyInfo, false));
                    }
                }
            }

            Parallel.ForEach(table.AsEnumerable(), new() { MaxDegreeOfParallelism = maxDegreeOfParallelism }, row =>
            {
                T? item = new();
                if (row != null)
                {
                    foreach ((DataColumn DataColumn, PropertyInfo PropertyInfo, bool IsShort) pair in map)
                    {
                        object? value = row[pair.DataColumn!];

                        //Handle issue where DB returns Int16 for boolean values
                        if (pair.IsShort && (pair.PropertyInfo!.PropertyType == typeof(bool) || pair.PropertyInfo!.PropertyType == typeof(bool?)))
                        {
                            pair.PropertyInfo!.SetValue(item, value is not System.DBNull ? ToBoolean(value) : null);
                        }
                        else
                        {
                            pair.PropertyInfo!.SetValue(item, value is not System.DBNull ? value : null);
                        }
                    }
                }
                else
                {
                    item = null;
                }
                bag.Add(item);
            });
        }
        return bag.ToList();
    }
}

//sealed class Tuple<T1, T2, T3>
//{
//    public Tuple() { }
//    public Tuple(T1 value1, T2 value2, T3 value3) { DataColumn = value1; PropertyInfo = value2; IsShort = value3; }
//    public T1? DataColumn { get; set; }
//    public T2? PropertyInfo { get; set; }
//    public T3? IsShort { get; set; }
//}

//sealed class Tuple<T1, T2>
//{
//    public Tuple() { }
//    public Tuple(T1 value1, T2 value2) { Value1 = value1; Value2 = value2; }
//    public T1? Value1 { get; set; }
//    public T2? Value2 { get; set; }
//}
