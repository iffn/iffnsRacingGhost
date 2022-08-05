
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class GhostSync : UdonSharpBehaviour
{
    [SerializeField] GhostController LinkedController;

    [UdonSynced, HideInInspector] public float[] Times;
    [UdonSynced, HideInInspector] public Vector3[] Positions;
    [UdonSynced, HideInInspector] public Quaternion[] Rotations;
    [UdonSynced, HideInInspector] public int maxSteps = 0;

    public void SyncMe()
    {
        if (!Networking.IsOwner(gameObject)) Networking.SetOwner(player: Networking.LocalPlayer, obj: gameObject);

        RequestSerialization();
    }
}
