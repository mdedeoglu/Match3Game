using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveableDrop : MonoBehaviour
{
    private Drop drop;
    void Awake()
    {
        drop = GetComponent<Drop>();   
    }

    public void Move(int newX, int newY, float time)
    {
        drop.X = newX;
        drop.Y = newY;
        if (time != 0f) {
            LeanTween.move(drop.gameObject, drop.GridRef.GetWorldPosition(newX, newY), time);

        } else
        {
            drop.transform.position = drop.GridRef.GetWorldPosition(newX, newY);
        }
   
    }

   
}
