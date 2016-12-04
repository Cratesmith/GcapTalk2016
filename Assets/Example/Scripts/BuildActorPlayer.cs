using UnityEngine;
using System.Collections;

public class BuildActorPlayer : MonoBehaviour 
{
	void Awake () 
    {
        gameObject.AddComponent<ActorPlayer>();	
	}
}
