using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class ReflectionExtensions
{
    #region GameObject
    public static T AddComponent<T>(this GameObject game, T duplicate) where T : Component
    {
        T target = game.AddComponent<T>();
        foreach (PropertyInfo x in typeof(T).GetProperties())
        {
            if (x.CanWrite)
                x.SetValue(target, x.GetValue(duplicate));
        }
        return target;
    }
    #endregion

    #region (GET) Fields and Properties
    public static PropertyInfo GetPropertyInfo(this object source, string propName, BindingFlags? flags = null)
        => source.GetType().GetProperty(propName, GetBindingFlags(flags));

    public static object GetPropertyValue(this object source, PropertyInfo property)
        => property?.GetValue(source, null);

    public static object GetPropertyValue(this object source, string propName, BindingFlags? flags = null)
        => GetPropertyInfo(source, propName, flags)?.GetValue(source, null);

    public static FieldInfo GetFieldInfo(this object source, string fieldName, BindingFlags? flags = null)
        => source.GetType().GetField(fieldName, GetBindingFlags(flags));

    public static object GetFieldValue(this object source, FieldInfo field)
        => field?.GetValue(source);

    public static object GetFieldValue(this object source, string fieldName, BindingFlags? flags = null)
        => GetFieldInfo(source, fieldName, flags)?.GetValue(source);

    /// <param name="declaredOnly">True to include only properties declared in the source type, false to include inherited ones as well</param>
    public static PropertyInfo[] GetPropertyInfo(this object source, bool declaredOnly, BindingFlags? flags = null)
        => GetPropertyInfo(source.GetType(), declaredOnly, flags);

    /// <param name="declaredOnly">True to include only properties declared in the source type, false to include inherited ones as well</param>
    public static FieldInfo[] GetFieldInfo(this object source, bool declaredOnly, BindingFlags? flags = null)
        => GetFieldInfo(source.GetType(), declaredOnly, flags);

    private static PropertyInfo[] GetPropertyInfo(this Type source, bool declaredOnly, BindingFlags? flags = null)
    {
        PropertyInfo[] properties = source.GetProperties(GetBindingFlags(flags));

        if (declaredOnly)
            properties = properties.Where(p => p.DeclaringType == source).ToArray();

        return properties;
    }

    private static FieldInfo[] GetFieldInfo(this Type source, bool declaredOnly, BindingFlags? flags = null)
    {
        FieldInfo[] fields = source.GetFields(GetBindingFlags(flags));

        if (declaredOnly)
            fields = fields.Where(f => f.DeclaringType == source).ToArray();

        return fields;
    }

    /// <param name="type">Specify a type to limit selected values</param>
    public static IEnumerable<object> GetPropertiesByType(this object source, Type type = null, bool declaredOnly = false, BindingFlags? flags = null)
        => GetPropertyInfo(source, declaredOnly, flags)
            .Select(item => item.GetValue(source, null))
            .Where(value => type == null || value.GetType() == type);

    /// <param name="type">Specify a type to limit selected values</param>
    public static IEnumerable<object> GetFieldsByType(this object source, Type type = null, bool declaredOnly = false, BindingFlags? flags = null)
        => GetFieldInfo(source, declaredOnly, flags)
            .Select(item => item.GetValue(source))
            .Where(value => type == null || value.GetType() == type);

    public static (bool, T) GetFieldValue<T>(this object obj, string value, BindingFlags? flags = null)
    {
        Type type = obj.GetType();
        FieldInfo fieldInfo = type.GetField(value, GetBindingFlags(flags));

        return fieldInfo != null && typeof(T).IsAssignableFrom(fieldInfo.FieldType) ?
            (true, (T)fieldInfo.GetValue(obj)) : (false, default);
    }

    public static (bool, T) GetPropertyValue<T>(this object obj, string value, BindingFlags? flags = null)
    {
        Type type = obj.GetType();
        PropertyInfo propertyInfo = type.GetProperty(value, GetBindingFlags(flags));

        return propertyInfo != null && typeof(T).IsAssignableFrom(propertyInfo.PropertyType)
            ? (true, (T)propertyInfo.GetValue(obj)) : (false, default);
    }

    public static T GetValue<T>(this object obj, string name, BindingFlags? flags = null)
    {
        Type type = obj.GetType();

        (bool foundField, T fieldValue) = obj.GetFieldValue<T>(name, flags);
        if (foundField) return fieldValue;

        (bool foundProperty, T propertyValue) = obj.GetPropertyValue<T>(name, flags);
        if (foundProperty) return propertyValue;

        throw new MissingMemberException($"Member '{name}' not found in type {type.FullName}.");
    }
    #endregion

    #region (SET) Fields and Properties
    public static bool SetField<T>(this object obj, string fieldName, T value, BindingFlags? flags = null)
    {
        Type type = obj.GetType();
        FieldInfo fieldInfo = type.GetField(fieldName, GetBindingFlags(flags));

        if (fieldInfo == null)
            return false;

        if (fieldInfo.FieldType == typeof(T))
        {
            fieldInfo.SetValue(obj, value);
            return true;
        }

        throw new ArgumentException($"Field '{fieldName}' is of type {fieldInfo.FieldType}, but attempted to set with type {typeof(T)}.");
    }

    public static bool SetProperty<T>(this object obj, string propertyName, T value, BindingFlags? flags = null)
    {
        Type type = obj.GetType();
        PropertyInfo propInfo = type.GetProperty(propertyName, GetBindingFlags(flags));

        if (propInfo == null)
            return false;

        if (propInfo.PropertyType == typeof(T))
        {
            propInfo.SetValue(obj, value);
            return true;
        }

        throw new ArgumentException($"Property '{propertyName}' is of type {propInfo.PropertyType}, but attempted to set with type {typeof(T)}.");
    }

    public static bool SetFieldOrProperty<T>(this object obj, string memberName, T value)
    {
        if (obj.SetField(memberName, value)) return true;
        else if (obj.SetProperty(memberName, value)) return true;
        else return false;
    }

    public static T Clone<T>(this T source, BindingFlags? flags = null) where T : new()
    {
        T clone = new();
        CopyPropertiesFrom(clone, source, flags);
        CopyFieldsFrom(clone, source, flags);
        return clone;
    }

    public static T CopyValuesFrom<T>(this T target, T source, BindingFlags? flags = null)
    {
        CopyPropertiesFrom(target, source, flags);
        CopyFieldsFrom(target, source, flags);
        return target;
    }

    public static void CopyPropertiesFrom<T>(this T target, T source, BindingFlags? flags = null)
    {
        Type type = typeof(T);
        PropertyInfo[] properties = flags.HasValue ? type.GetProperties(GetBindingFlags(flags)) : type.GetProperties();

        foreach (PropertyInfo property in properties)
        {
            if (property.CanRead && property.CanWrite)
            {
                object value = property.GetValue(source);
                property.SetValue(target, value);
            }
        }
    }

    public static void CopyFieldsFrom<T>(this T target, T source, BindingFlags? flags = null)
    {
        Type type = typeof(T);
        FieldInfo[] fields = flags.HasValue ? type.GetFields(GetBindingFlags(flags)) : type.GetFields();

        foreach (FieldInfo field in fields)
        {
            object value = field.GetValue(source);
            field.SetValue(target, value);
        }
    }
    #endregion

    private static BindingFlags GetBindingFlags(BindingFlags? flags = null)
        => flags ?? (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
}

