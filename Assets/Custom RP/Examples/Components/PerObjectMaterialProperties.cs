using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour
{
    static int
        baseColorId = Shader.PropertyToID("_BaseColor"),
        cutoffId = Shader.PropertyToID("_Cutoff"),
        metallicId = Shader.PropertyToID("_Metallic"),
        smoothnessId = Shader.PropertyToID("_Smoothness");

    static MaterialPropertyBlock block;


    [SerializeField]
    Color baseColor = Color.white;

    [SerializeField]
    float alphaCutoff = 0.5f, metallic = 0f, smoothness = 0.5f;

    void OnValidate()
    {
        if (block == null)
            block = new MaterialPropertyBlock();

        block.SetColor(baseColorId, baseColor);
        block.SetFloat(cutoffId, alphaCutoff);
        block.SetFloat(metallicId, metallic);
        block.SetFloat(smoothnessId, smoothness);
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
