using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SkinColorSlider : MonoBehaviour
{
    public GameObject playerModel;
    public SkinnedMeshRenderer playerSMR;
    public Material material;
    public float r;
    public float b;
    public float g;

    private void Start()
    {
        playerSMR = playerModel.GetComponent<SkinnedMeshRenderer>();
        material = playerSMR.materials[0];
        r = material.color.r;
        b = material.color.b;
        g = material.color.g;
        r /= 2;
        b /= 4;
        b /= 3;
    }
}
