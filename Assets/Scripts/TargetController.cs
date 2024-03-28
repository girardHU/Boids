using UnityEngine;
using UnityEngine.Events;


public class TargetController : MonoBehaviour
{
    [SerializeField] private float start = 0f;
    [SerializeField] private float repeat = 3f;
    public Vector3 dest;
    public static UnityAction<Vector3> NewDestinationEvent;
    // Start is called before the first frame update
    void Start()
    {
        InvokeRepeating(nameof(SelectRandomDestination), start, repeat);
    }

    public void SelectRandomDestination()
    {

        float x = Random.Range(-25f + 5, 25f - 5);
        float y = Random.Range(-7.5f + 2, 7.5f - 2);
        float z = Random.Range(-12.5f + 4, 12.5f - 4);

        dest = new Vector3(x, y, z);

        NewDestinationEvent?.Invoke(dest);
    }

}
