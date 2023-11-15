﻿using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Common_Net_Funcs.Tools;

/// <summary>
/// Helper methods for complex classes and lists
/// </summary>
public static partial class ObjectHelpers
{
    /// <summary>
    /// Copy properties of the same name from one object to another
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TU"></typeparam>
    /// <param name="source">Object to copy common properties from</param>
    /// <param name="dest">Object to copy common properties to</param>
    public static void CopyPropertiesTo<T, TU>(this T source, TU dest)
    {
        IEnumerable<PropertyInfo> sourceProps = typeof(T).GetProperties().Where(x => x.CanRead);
        IEnumerable<PropertyInfo> destProps = typeof(TU).GetProperties().Where(x => x.CanWrite);

        foreach (PropertyInfo sourceProp in sourceProps)
        {
            if (destProps.Any(x => x.Name == sourceProp.Name))
            {
                PropertyInfo? p = destProps.FirstOrDefault(x => x.Name == sourceProp.Name);
                p?.SetValue(dest, sourceProp.GetValue(source, null), null);
            }
        }
    }

    /// <summary>
    /// Copy properties of the same name from one object to another
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">Object to copy common properties from</param>
    /// <param name="dest">New class of desired output type</param>
    public static T CopyPropertiesToNew<T>(this T source, T dest)
    {
        IEnumerable<PropertyInfo> sourceProps = typeof(T).GetProperties().Where(x => x.CanRead);
        IEnumerable<PropertyInfo> destProps = typeof(T).GetProperties().Where(x => x.CanWrite);

        foreach (PropertyInfo sourceProp in sourceProps)
        {
            if (destProps.Any(x => x.Name == sourceProp.Name))
            {
                PropertyInfo? p = destProps.FirstOrDefault(x => x.Name == sourceProp.Name);
                p?.SetValue(dest, sourceProp.GetValue(source, null), null);
            }
        }
        return dest;
    }

    /// <summary>
    /// Set values in an IEnumerable as an extension of linq
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items">Items to have the updateMethod expression performed on</param>
    /// <param name="updateMethod">Lambda expression of the action to perform</param>
    /// <returns>IEnumerable with values updated according to updateMethod</returns>
    public static IEnumerable<T> SetValue<T>(this IEnumerable<T> items, Action<T> updateMethod)
    {
        foreach (T item in items)
        {
            updateMethod(item);
        }
        return items.ToList();
    }

    /// <summary>
    /// Set values in an IEnumerable as an extension of linq using a Parallel.ForEach loop
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items">Items to have the updateMethod expression performed on</param>
    /// <param name="updateMethod">Lambda expression of the action to perform</param>
    /// <param name="maxDegreeOfParallelism">Integer setting the max number of parallel operations allowed. Default of -1 allows maximum possible.</param>
    /// <returns>IEnumerable with values updated according to updateMethod</returns>
    public static IEnumerable<T> SetValueParallel<T>(this IEnumerable<T> items, Action<T> updateMethod, int maxDegreeOfParallelism = -1)
    {
        ConcurrentBag<T> concurrentBag = new(items);
        Parallel.ForEach(concurrentBag, new() { MaxDegreeOfParallelism = maxDegreeOfParallelism }, item => updateMethod(item));
        return concurrentBag;
    }

    /// <summary>
    /// Removes excess spaces in string properties inside of an object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    public static void TrimObjectStrings<T>(this T obj)
    {
        PropertyInfo[] props = typeof(T).GetProperties();
        if (props != null)
        {
            foreach (PropertyInfo prop in props)
            {
                if (prop.PropertyType == typeof(string))
                {
                    string? value = (string)(prop.GetValue(obj) ?? string.Empty);
                    if (!string.IsNullOrEmpty(value))
                    {
                        value = SpacingRegex().Replace(value.Trim(), " "); //Replaces any multiples of spacing with a single space
                        prop.SetValue(obj, value.Trim());
                    }
                }
            }
        }
    }

    /// <summary>
    /// Adds AddRange functionality to ConcurrentBag similar to a list. Skips null items
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="concurrentBag">ConcurrentBag to add list of items to</param>
    /// <param name="toAdd">Items to add to the ConcurrentBag object</param>
    /// <param name="parallelOptions">ParallelOptions for Parallel.ForEach</param>
    public static void AddRange<T>(this ConcurrentBag<T> concurrentBag, IEnumerable<T?> toAdd, ParallelOptions? parallelOptions = null)
    {
        Parallel.ForEach(toAdd.Where(x => x != null), parallelOptions ?? new(), item => concurrentBag.Add(item!));
    }

    /// <summary>
    /// Create a single item list from an object
    /// </summary>
    /// <typeparam name="T">Type to use in list</typeparam>
    /// <param name="obj">Object to turn into a single item list</param>
    public static List<T> SingleToList<T>(this T obj)
    {
        return new() { obj };
    }

    public static T? GetObjectByPartial<T>(this IQueryable<T> queryable, T partialObject) where T : class
    {
        // Get the properties of the object using reflection
        PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Build the expression tree for the conditions
        ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
        Expression? conditions = null;

        foreach (PropertyInfo property in properties)
        {
            // Get the value of the property from the partial object
            object? partialValue = property.GetValue(partialObject);

            //Only compare non-null values since these are going to be the one's that matter
            if (partialValue != null)
            {
                // Build the condition for this property
                var condition = Expression.Equal(Expression.Property(parameter, property), Expression.Constant(partialValue, property.PropertyType));

                // Combine the conditions using 'AndAlso' if this is not the first condition
                conditions = conditions == null ? condition : Expression.AndAlso(conditions, condition);
            }
        }

        T? model = null;
        if (conditions != null)
        {
            // Build the final lambda expression and execute the query
            Expression<Func<T, bool>> lambda = Expression.Lambda<Func<T, bool>>(conditions, parameter);
            model = queryable.FirstOrDefault(lambda);
        }
        return model;
    }

    /// <summary>
    /// UNTESTED
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="instances"></param>
    public static T MergeInstances<T>(IEnumerable<T> instances) where T : class
    {
        T merged = instances.First();
        foreach (T instance in instances)
        {
            foreach (PropertyInfo property in typeof(T).GetProperties())
            {
                var value = property.GetValue(instance);
                var mergedValue = property.GetValue(merged);

                if (value != default && mergedValue == default)
                {
                    property.SetValue(merged, value);
                }
            }
        }

        return merged;
    }

    /// <summary>
    /// Returns whether a Type has the specified attribute or not
    /// </summary>
    /// <param name="type">The type to check for the specified attribute</param>
    /// <param name="attributeName">The name of the attribute you are checking the provided type for</param>
    /// <returns></returns>
    public static bool ObjectHasProperty(this Type type, string attributeName)
    {
        bool hasAttribute = false;
        foreach (object item in type.GetCustomAttributes(true))
        {
            object? typeIdObject = item.GetType().GetProperty("TypeId")?.GetValue(item, null);

            if (typeIdObject != null)
            {
                string? attrName = typeIdObject.GetType().GetProperty("Name")?.GetValue(typeIdObject, null)?.ToString();
                if (attrName?.StrEq(attributeName) == true)
                {
                    hasAttribute = true;
                    break;
                }
            }
        }
        return hasAttribute;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex SpacingRegex();

    public static bool AnyFast<T>(this IList<T>? list)
    {
        return list?.Count > 0;
    }

    public static bool AnyFast<T>(this T[]? array)
    {
        return array?.Length > 0;
    }

    public static bool AnyFast<T, UT>(this Dictionary<T, UT>? dict) where T : notnull
    {
        return dict?.Count > 0;
    }
}
