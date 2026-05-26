using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TESTSCRIPT : MonoBehaviour
{
    [NaughtyAttributes.Button]
    public void SET()
    {
        GetComponent<SpriteRenderer>().material.SetFloat("_ZLevel", this.transform.position.z);
    }
}
