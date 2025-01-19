using Unity.VisualScripting;
using UnityEngine;
using static OneUpdate;

public class TestScript3 : MonoBehaviour, IUpdatable
{


    private bool ddd3 = true;

    void IUpdatable.CustomFixedUpdate()
    {
        if (ddd3 = true)
        {
            Debug.Log("Tesadcst");
        }
    }
}
