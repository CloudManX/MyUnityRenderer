using UnityEngine;

[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour
{
    static int baseColorId = Shader.PropertyToID("_BaseColor");
    static int cutoffId = Shader.PropertyToID("_Cutoff");

    static MaterialPropertyBlock block;

    [SerializeField]
    Color baseColor = Color.white;

    [SerializeField]
    float cutoff = 0.5f;

    void OnValidate()
    {
        if (block == null)
            block = new MaterialPropertyBlock();

        block.SetColor(baseColorId, baseColor);
        block.SetFloat(cutoffId, cutoff);
        GetComponent<Renderer>().SetPropertyBlock(block);
    }

    void Awake()
    {
        if (baseColor == Color.white)
        {
            baseColor.r = Random.value;
            baseColor.g = Random.value;
            baseColor.b = Random.value;
            baseColor.a = (float)0.7;
        }
        OnValidate();
    }
}
