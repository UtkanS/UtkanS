using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Type = System.Type;


public static class SaveManager
{

    static SaveManager()
    {
        es3settings = new()
        {
            referenceMode = ES3.ReferenceMode.ByValue,
            directory = ES3.Directory.PersistentDataPath
        };
    }

    public static readonly ES3Settings es3settings;
    public static readonly string saveFile = "GameData";
    public static readonly string saveExtension = ".zll";
    private static readonly SaveSettings defaultSS = new();

    private static void OnApplicationPause(bool pause) { }

    private static void OnApplicationQuit() { }

    public static string SafeCombine(params string[] paths)
    {
        return Path.Combine(paths
            .Where(p => !string.IsNullOrEmpty(p))
            .Select(GameManager.NormalizePathSeparators)
            .ToArray());
    }

    private static string KeyBuilder(Type type, uint id)
        => type + " : " + id.ToString();

    public static string BuildPath(string key, Type type, SaveSettings settings = null)
    {
        settings ??= defaultSS;
        string end = key + (settings.Extension ? saveExtension : "");
        string before = settings.BeforePath ?? string.Empty;
        string after = settings.AfterPath ?? string.Empty;
        string path = SafeCombine(SavePath(), saveFile, before, type.Name, after, end);
        return GameManager.NormalizePathSeparators(path);
    }

    public static string SavePath()
    {
        string basePath = es3settings.directory switch
        {
            ES3.Directory.PersistentDataPath => Application.persistentDataPath,
            ES3.Directory.DataPath => Application.dataPath,
            _ => throw new System.InvalidOperationException("Unknown directory type")
        };

        return GameManager.NormalizePathSeparators(basePath);
    }

    public static void SaveWithID<T>(T element, SaveSettings settings = null) where T : IHasID
    {
        settings ??= defaultSS;
        string path = BuildPath(element.ID.ToString(), typeof(T), settings);
        ES3.Save(KeyBuilder(typeof(T), element.ID), element, path, es3settings);
    }

    public static T LoadWithID<T>(uint id, SaveSettings settings = null) where T : IHasID
    {
        settings ??= defaultSS;
        string path = BuildPath(id.ToString(), typeof(T), settings);
        T load = ES3.Load<T>(KeyBuilder(typeof(T), id), path);
        load.ID = id;
        load.Initialize();
        return load;
    }

    public static void SaveWithName<T>(string key, T element, SaveSettings settings = null) where T : IHasName
    {
        settings ??= defaultSS;
        string path = BuildPath(element.Name, typeof(T), settings);
        ES3.Save(key, element, path, es3settings);
    }

    public static T LoadWithName<T>(string key, string file, SaveSettings settings = null)
    {
        settings ??= defaultSS;
        string path = BuildPath(file, typeof(T), settings);
        T load = ES3.Load<T>(key, path);

        if (load is IHasID hasID)
            hasID.Initialize();

        return load;
    }

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

public interface IHasID
{
    [SerializeField] 
    uint ID { get; set; }
    void Initialize();
}

public interface IHasName
{
    [SerializeField]
    string Name { get; set; }
}

public interface IMVF
{
    public string[] GetValues();
}
