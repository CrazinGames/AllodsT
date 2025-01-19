using UnityEngine;
using static OneUpdate;

public class TestScript2 : MonoBehaviour, IUpdatable
{

    private bool ddd = true;

    void IUpdatable.CustomFixedUpdate()
    {
        if (ddd = true)
        {
            Debug.Log("scd");
        }
    }
}
