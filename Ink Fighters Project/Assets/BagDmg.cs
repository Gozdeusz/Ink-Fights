using UnityEngine;

public class BagDmg : MonoBehaviour
{

    private float hp = 100f;

    public void takeDmg(float damage)
    {
        hp -= damage;
        Debug.Log("Pozostalo hp: " + hp);
    }
    
}
