using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class ReflectionExtensions
{
    #region GameObject

    /// <summary>
    /// Adds a component of type T to the GameObject and copies properties from a duplicate instance.
    /// </summary>
    /// <typeparam name="T">Type of the Component to add.</typeparam>
    /// <param name="game">The target GameObject.</param>
    /// <param name="duplicate">The source Component whose properties will be copied.</param>
    /// <returns>The newly added Component.</returns>
    public static T AddComponent<T>(this GameObject game, T duplicate) where T : Component
    {
        T target = game.AddComponent<T>();
        foreach (PropertyInfo property in typeof(T).GetProperties())
        {
            if (property.CanWrite)
                property.SetValue(target, property.GetValue(duplicate)); // Copy writable properties.
        }
        return target;
    }

    #endregion

    #region Fields and Properties

    /// <summary>
    /// Retrieves PropertyInfo for a specific property name.
    /// </summary>
    public static PropertyInfo GetPropertyInfo(this object source, string propName, BindingFlags? flags = null)
        => source.GetType().GetProperty(propName, GetBindingFlags(flags));

    /// <summary>
    /// Retrieves FieldInfo for a specific field name.
    /// </summary>
    public static FieldInfo GetFieldInfo(this object source, string fieldName, BindingFlags? flags = null)
        => source.GetType().GetField(fieldName, GetBindingFlags(flags));

    /// <summary>
    /// Gets the value of a field or property by name.
    /// </summary>
    /// <param name="memberName">The name of the member to retrieve.</param>
    /// <exception cref="MissingMemberException">Thrown if the member is not found.</exception>
    public static object GetMemberValue(this object source, string memberName, BindingFlags? flags = null)
    {
        FieldInfo field = source.GetFieldInfo(memberName, flags);
        if (field != null) return field.GetValue(source);

        PropertyInfo property = source.GetPropertyInfo(memberName, flags);
        if (property != null) return property.GetValue(source);

        throw new MissingMemberException($"Member '{memberName}' not found in type {source.GetType().FullName}.");
    }

    /// <summary>
    /// Retrieves all field and property values of a specific type.
    /// </summary>
    public static IEnumerable<object> GetValuesByType(this object source, Type type = null, bool declaredOnly = false, BindingFlags? flags = null)
    {
        BindingFlags bindingFlags = GetBindingFlags(flags);

        // Get field values.
        var fields = source.GetType().GetFields(bindingFlags)
            .Where(f => !declaredOnly || f.DeclaringType == source.GetType())
            .Select(f => f.GetValue(source));

        // Get property values.
        var properties = source.GetType().GetProperties(bindingFlags)
            .Where(p => (!declaredOnly || p.DeclaringType == source.GetType()) && p.CanRead)
            .Select(p => p.GetValue(source));

        // Combine fields and properties, filtering by type if specified.
        return fields.Concat(properties).Where(value => type == null || value?.GetType() == type);
    }

    /// <summary>
    /// Sets the value of a field or property by name.
    /// </summary>
    /// <typeparam name="T">Type of the value being set.</typeparam>
    /// <returns>True if the value was set successfully, false otherwise.</returns>
    public static bool SetMemberValue<T>(this object obj, string memberName, T value, BindingFlags? flags = null)
    {
        FieldInfo field = obj.GetFieldInfo(memberName, flags);
        if (field != null && field.FieldType == typeof(T))
        {
            field.SetValue(obj, value);
            return true;
        }

        PropertyInfo property = obj.GetPropertyInfo(memberName, flags);
        if (property != null && property.PropertyType == typeof(T))
        {
            property.SetValue(obj, value);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Creates a shallow copy of the source object.
    /// </summary>
    public static T Clone<T>(this T source, BindingFlags? flags = null) where T : new()
    {
        T clone = new();
        CopyMembers(clone, source, flags); // Copy fields and properties.
        return clone;
    }

    /// <summary>
    /// Copies all fields and properties from one object to another.
    /// </summary>
    public static void CopyMembers<T>(this T target, T source, BindingFlags? flags = null)
    {
        BindingFlags bindingFlags = GetBindingFlags(flags);

        // Copy properties.
        foreach (PropertyInfo property in typeof(T).GetProperties(bindingFlags))
        {
            if (property.CanRead && property.CanWrite)
            {
                object value = property.GetValue(source);
                property.SetValue(target, value);
            }
        }

        // Copy fields.
        foreach (FieldInfo field in typeof(T).GetFields(bindingFlags))
        {
            object value = field.GetValue(source);
            field.SetValue(target, value);
        }
    }

    #endregion

    /// <summary>
    /// Determines the BindingFlags to use for reflection.
    /// </summary>
    private static BindingFlags GetBindingFlags(BindingFlags? flags = null)
        => flags ?? (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
}
