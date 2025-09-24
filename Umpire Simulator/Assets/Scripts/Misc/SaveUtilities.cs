using System.IO;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class SaveUtilities : MonoBehaviour
{
    [SerializeField] private string fileName = "save.json";

    private string GetSaveDirectory()
    {
        // Unity guarantees this folder exists, but create it just in case.
        var dir = Application.persistentDataPath;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        return dir;
    }

    private string GetSavePath() => Path.Combine(GetSaveDirectory(), fileName);

    // Hook to a UI Button
    public void DeleteSave()
    {
        var path = GetSavePath();
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"Deleted save: {path}");
                LevelLoader.Instance.Load();
                LevelLoader.Instance.LoadLevelMenu();
            }
            else
            {
                Debug.Log($"No save file to delete: {path}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to delete save: {e.Message}");
        }
    }

    // Hook to a UI Button
    public void RevealSave()
    {
        var filePath = GetSavePath();
        var dir = GetSaveDirectory();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.RevealInFinder(File.Exists(filePath) ? filePath : dir);
#else
        try
        {
#if UNITY_STANDALONE_WIN
            if (File.Exists(filePath))
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{filePath}\"") { UseShellExecute = true });
            else
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dir}\"") { UseShellExecute = true });

#elif UNITY_STANDALONE_OSX
            if (File.Exists(filePath))
                Process.Start(new ProcessStartInfo("open", $"-R \"{filePath}\"") { UseShellExecute = true });
            else
                Process.Start(new ProcessStartInfo("open", $"\"{dir}\"") { UseShellExecute = true });

#elif UNITY_STANDALONE_LINUX
            Process.Start(new ProcessStartInfo("xdg-open", $"\"{dir}\"") { UseShellExecute = true });
#else
            Debug.Log($"Save location: {dir}");
#endif
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to open save location: {e.Message}\nPath: {dir}");
        }
#endif
    }
}
