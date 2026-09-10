using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SetBackgoundValue : MonoBehaviour
{
    [SerializeField] private Material material;

    private void OnEnable() {
        if (material != null) {
            material.SetVector("_GetScreenSize", new Vector4(Screen.width, Screen.height, 0, 0));
        }
    }
}
