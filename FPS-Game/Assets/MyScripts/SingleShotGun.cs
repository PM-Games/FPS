using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SingleShotGun : Gun
{
    [SerializeField] Camera cam;

    PhotonView PV;

    //PlayerController victimPlayerController;

    public int playerID;

    private void Awake()
    {
        PV = GetComponent<PhotonView>();
    }

    public override void Use()
    {
        Shoot(PhotonNetwork.LocalPlayer.ActorNumber);
    }

    void Shoot(int playerID)
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));        
        ray.origin = cam.transform.position;
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            hit.collider.gameObject.GetComponent<IDamageable>()?.TakeDamage(((GunInfo)itemInfo).damage);            
            //if (hit.collider.gameObject.GetComponent<PhotonView>() != null)
            //{
            //    victimPlayerController = hit.collider.gameObject.GetComponent<PlayerController>();
            //    VictimDeath();
            //    Debug.Log(victimPlayerController);
            //}            
            PV.RPC("RPC_Shoot", RpcTarget.All, hit.point, hit.normal);
        }

    }

    [PunRPC]
    void RPC_Shoot(Vector3 hitPosition, Vector3 hitNormal)
    {
        Collider[] colliders = Physics.OverlapSphere(hitPosition, 0.3f);
        if (colliders.Length != 0)
        {
            GameObject bulletImpactObject = Instantiate(bulletImpactPrefab, hitPosition + hitNormal * 0.001f, Quaternion.LookRotation(hitNormal, Vector3.up) * bulletImpactPrefab.transform.rotation);
            Destroy((bulletImpactObject), 10f);
            bulletImpactObject.transform.SetParent(colliders[0].transform);
        }
        
    }

    //public void VictimDeath()
    //{      
    //    if (victimPlayerController == null)
    //    {
    //        //Debug.Log(victimPlayerController.isDead);
    //        //if (victimPlayerController.isDead)
    //        //{
    //        //   Debug.Log("You killed someone");
    //        //}
    //        Debug.Log("You killed someone");
    //    }
    //    else
    //    {
    //        Debug.Log("You hit the environment");
    //    }
    //}
}