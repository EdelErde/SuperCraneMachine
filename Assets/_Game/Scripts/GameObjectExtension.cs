using UnityEngine;

public static class GameObjectExtensions
{
    public static void ToggleActive(this GameObject go)
    {
        go.SetActive(!go.activeSelf);
    }
}