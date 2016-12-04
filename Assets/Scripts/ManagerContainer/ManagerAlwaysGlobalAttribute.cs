using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;

/// <summary>
/// This manager can only be initialized in the global container.
/// 
/// GetManager calls will auto-instantiate it in the global container
/// and trying to create it via prefab in a non-global container will throw an error.
/// </summary>
public class ManagerAlwaysGlobalAttribute : System.Attribute
{
}
