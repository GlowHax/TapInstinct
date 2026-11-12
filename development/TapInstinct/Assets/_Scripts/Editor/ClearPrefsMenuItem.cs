#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ClearPrefsMenuItem
{
    [MenuItem("Tools/PlayerPrefs zurücksetzen")]
    public static void ClearAllPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[Editor] Alle PlayerPrefs wurden erfolgreich gelöscht!");
    }
}
#endif
