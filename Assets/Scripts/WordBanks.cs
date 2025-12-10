using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WordBanks", menuName = "Localization/Word Banks")]
public class WordBanks : ScriptableObject
{
    public List<LocalizationData> localizationBanks = new List<LocalizationData>();
}
