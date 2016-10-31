using UnityEngine;
using System.Collections;
    
public class PlayerManager : Manager, IManagerDependency<ExampleDependency>
{
    public ActorPlayer currentPlayer {get;set;}
}
