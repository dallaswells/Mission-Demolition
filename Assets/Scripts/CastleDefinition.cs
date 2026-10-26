using UnityEngine;
[CreateAssetMenu(menuName = "Mission Demolition/Castle")]
public class CastleDefinition : ScriptableObject {
    public string castleName;
    [TextArea] public string hint;
    public GameObject prefab;
    public int goldShots = 1;
    public int silverShots = 3;
}
