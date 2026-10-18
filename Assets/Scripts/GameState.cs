#nullable enable

using UnityEngine;

public class GameState : MonoBehaviour
{
    private static GameState? instance;

    public static GameState Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject(nameof(GameState));
                instance = go.AddComponent<GameState>();
                instance.saveData = SaveGame.Load();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    public SaveGameData saveData = new();

    public void Save()
    {
        SaveGame.Save(saveData);
    }

    public bool IsCollectibleFound(string collectibleId)
    {
        return saveData.foundCollectibles.Contains(collectibleId);
    }

    public void MarkCollectibleFound(string collectibleId)
    {
        if (saveData.foundCollectibles.Contains(collectibleId))
        {
            return;
        }

        saveData.foundCollectibles.Add(collectibleId);
        Save();
    }
}
