using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshBall : MonoBehaviour
{
    const int numBalls = 1023;

    static int
        baseColorId = Shader.PropertyToID("_BaseColor"),
        metallicId = Shader.PropertyToID("_Metallic"),
        smoothnessId = Shader.PropertyToID("_Smoothness");

    [SerializeField]
    Mesh mesh = default;

    [SerializeField]
    Material material = default;

    Matrix4x4[] matrices = new Matrix4x4[numBalls];
    Vector4[] baseColors = new Vector4[numBalls];
    float[] metallic = new float[numBalls];
    float[] smoothness = new float[numBalls];

    MaterialPropertyBlock block;

    void Awake()
    {
        for (int i = 0; i < matrices.Length; ++i)
        {
            matrices[i] = Matrix4x4.TRS(
                Random.insideUnitSphere * 10f,
                Quaternion.Euler(
                    Random.value * 360f,
                    Random.value * 360f,
                    Random.value * 360f
                ),
                Vector3.one * Random.Range(0.5f, 1.5f)
            );

            baseColors[i] =
                new Vector4(
                    Random.value, 
                    Random.value, 
                    Random.value, 
                    Random.Range(0.5f, 1.0f)
                );
            metallic[i] = Random.value < 0.5f ? 1f : 0f;
            smoothness[i] = Random.Range(0.05f, 0.95f);

        }        
    }

    void Update()
    {
        if (block == null)
        {
            block = new MaterialPropertyBlock();
            block.SetVectorArray(baseColorId, baseColors);
            block.SetFloatArray(metallicId, metallic);
            block.SetFloatArray(smoothnessId, smoothness);
        }
        Graphics.DrawMeshInstanced(mesh, 0, material, matrices, matrices.Length, block);
    }
}
