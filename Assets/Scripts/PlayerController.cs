using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
   public static PlayerController instance;
   private void Awake()
   {
    instance = this;
   }
}
