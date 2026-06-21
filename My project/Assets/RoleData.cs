using UnityEngine;

[CreateAssetMenu(fileName = "Role", menuName = "Game/Role")]
public class RoleData : ScriptableObject
{
    public string roleName;

    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 7f;

    [Header("UI")]
    public GameObject roleUIPrefab;
}