using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Type = System.Type;

/// <summary>
/// SaveManager provides utility methods for saving and loading game data using ES3.
/// </summary>
public static class SaveManager
{
    // Static constructor to initialize ES3 settings
    static SaveManager()
    {
        es3settings = new ES3Settings
        {
            referenceMode = ES3.ReferenceMode.ByValue,
            directory = ES3.Directory.PersistentDataPath
        };
    }

    // Global settings for ES3
    public static readonly ES3Settings es3settings;
    public static readonly string saveFile = "GameData";
    public static readonly string saveExtension = ".zll";
    private static readonly SaveSettings defaultSS = new();

    /// <summary>
    /// Combines multiple path segments into a single valid path.
    /// Filters out null or empty segments and normalizes path separators.
    /// </summary>
    public static string SafeCombine(params string[] paths)
    {
        return Path.Combine(paths
            .Where(p => !string.IsNullOrEmpty(p))
            .Select(GameManager.NormalizePathSeparators)
            .ToArray());
    }

    /// <summary>
    /// Builds a unique key for a given type and ID.
    /// </summary>
    private static string KeyBuilder(Type type, uint id)
        => $"{type} : {id}";

    /// <summary>
    /// Constructs the file path for saving or loading data.
    /// </summary>
    public static string BuildPath(string key, Type type, SaveSettings settings = null)
    {
        settings ??= defaultSS;
        string end = key + (settings.Extension ? saveExtension : "");
        string before = settings.BeforePath ?? string.Empty;
        string after = settings.AfterPath ?? string.Empty;
        string path = SafeCombine(SavePath(), saveFile, before, type.Name, after, end);
        return GameManager.NormalizePathSeparators(path);
    }

    /// <summary>
    /// Determines the base save path based on the directory type in ES3 settings.
    /// </summary>
    public static string SavePath()
    {
        return es3settings.directory switch
        {
            ES3.Directory.PersistentDataPath => Application.persistentDataPath,
            ES3.Directory.DataPath => Application.dataPath,
            _ => throw new System.InvalidOperationException("Unknown directory type")
        };
    }

    /// <summary>
    /// Saves an object with a unique ID.
    /// </summary>
    public static void SaveWithID<T>(T element, SaveSettings settings = null) where T : IHasID
    {
        settings ??= defaultSS;
        string path = BuildPath(element.ID.ToString(), typeof(T), settings);
        ES3.Save(KeyBuilder(typeof(T), element.ID), element, path, es3settings);
    }

    /// <summary>
    /// Loads an object using its unique ID.
    /// </summary>
    public static T LoadWithID<T>(uint id, SaveSettings settings = null) where T : IHasID
    {
        settings ??= defaultSS;
        string path = BuildPath(id.ToString(), typeof(T), settings);
        T load = ES3.Load<T>(KeyBuilder(typeof(T), id), path);
        load.ID = id;
        load.Initialize();
        return load;
    }

    /// <summary>
    /// Saves an object using a unique name.
    /// </summary>
    public static void SaveWithName<T>(string key, T element, SaveSettings settings = null) where T : IHasName
    {
        settings ??= defaultSS;
        string path = BuildPath(element.Name, typeof(T), settings);
        ES3.Save(key, element, path, es3settings);
    }

    /// <summary>
    /// Loads an object using a unique name and file path.
    /// </summary>
    public static T LoadWithName<T>(string key, string file, SaveSettings settings = null)
    {
        settings ??= defaultSS;
        string path = BuildPath(file, typeof(T), settings);
        T load = ES3.Load<T>(key, path);

        if (load is IHasID hasID)
            hasID.Initialize();

        return load;
    }

    /// <summary>
    /// Loads all files from a folder corresponding to a specific type.
    /// </summary>
    public static List<T> LoadFolder<T>(SaveSettings settings = null) where T : class
    {
        settings ??= defaultSS;
        List<T> loadedFiles = new();
        settings.Extension = false;

        string folderPath = BuildPath(string.Empty, typeof(T), settings);
        IEnumerable<string> files = GetFiles(folderPath);
        settings.Extension = true;

        foreach (string file in files)
        {
            string name = file.Remove(file.Length - saveExtension.Length);
            string key = uint.TryParse(name, out uint id) ? KeyBuilder(typeof(T), id) : name;

            T load = LoadWithName<T>(key, name, settings);

            if (load is IHasID hasID)
                hasID.ID = id;

            loadedFiles.Add(load);
        }

        return loadedFiles;
    }

    /// <summary>
    /// Retrieves all files from a specified directory that match the save file extension.
    /// </summary>
    public static IEnumerable<string> GetFiles(string path)
    {
        if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
            path += Path.DirectorySeparatorChar;

        if (!ES3.DirectoryExists(path))
            return Enumerable.Empty<string>();

        return ES3.GetFiles(path)
            .Where(file => file.EndsWith(saveExtension, System.StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Configuration for save path construction.
/// </summary>
public class SaveSettings
{
    public string BeforePath { get; set; } = string.Empty;
    public string AfterPath { get; set; } = string.Empty;
    public bool Extension { get; set; } = true;

    public SaveSettings() { }

    public SaveSettings(string beforePath, string afterPath, bool extension)
    {
        BeforePath = beforePath;
        AfterPath = afterPath;
        Extension = extension;
    }
}

/// <summary>
/// Interface for objects with a unique ID.
/// </summary>
public interface IHasID
{
    [SerializeField] uint ID { get; set; }
    void Initialize();
}

/// <summary>
/// Interface for objects with a unique name.
/// </summary>
public interface IHasName
{
    [SerializeField] string Name { get; set; }
}

/// <summary>
/// Interface for objects with multiple values.
/// </summary>
public interface IMVF
{
    string[] GetValues();
}
