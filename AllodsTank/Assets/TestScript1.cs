using Unity.VisualScripting;
using UnityEngine;
using static OneUpdate;

public class TestScript1 : MonoBehaviour, IUpdatable
{

    private bool dddcc = true;

    void IUpdatable.CustomFixedUpdate()
    {
        if (dddcc = true)
        {
            Debug.Log("sdc");
        }
    }
}
