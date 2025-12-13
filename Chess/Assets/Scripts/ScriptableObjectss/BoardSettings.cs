using UnityEngine;

[CreateAssetMenu(fileName = "BoardSettings", menuName = "Scriptable Objects/BoardSettings")]
public class BoardSettings : ScriptableObject
{
    public Color whiteColor = new Color(1f, 1f, 1f);
    public Color blackColor = new Color(0f, 0f,  0f);
    public Color highlightColor = new Color(0f, 1f, 0f);
}
