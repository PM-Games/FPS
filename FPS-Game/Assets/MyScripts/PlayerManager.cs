using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.IO;
using Photon.Realtime;
using System.Linq;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using TMPro;
using UnityEngine.UI;
using Cinemachine;

public class PlayerManager : MonoBehaviour
{
    PhotonView PV;

    public GameObject controller;

    int kills;
    int deaths;

    [SerializeField] GameObject killTextNotification; // a kill (player killed player) that instantiates whenever a victim dies
    GameObject killTextNotificationHolder; // kill text notification holder empty gameobject
    [SerializeField] GameObject deathPanel; // death panel with respawn button

    GameObject killTextNotificationGameObject; // a gameobject that I assign later in the script

    public bool hasDeathPanelActivated = false;

    GameObject scoreBoardCanvas;

    GameObject deathPanelGameObject;
    public GameObject musicHolder;

    public GameObject cinemachineCam;
    public GameObject virtualCam;

    GameObject cinemachineCamInstantiation;
    GameObject virtualCamInstantiation;

    public Player killer;

    int itemIndex;

    public int currentExperience;

    public int currentLVL;

    public GameObject threetwooneanim;
    public Transform canvas;
    public AudioClip xp, levelup;

    private void Awake()
    {
        PV = GetComponent<PhotonView>();
        StartCoroutine(FirebaseManager.Singleton.LoadExperience());
    }

    void Start()
    {        
        if (PV.IsMine)
        {
            if (PhotonNetwork.InRoom)
            {
                GameObject anim = Instantiate(threetwooneanim, canvas);
                Destroy(anim, 3);
                Invoke(nameof(CreateController), 3f);
            }
            killTextNotificationHolder = GameObject.Find("KillTextNotificationHolder");
            scoreBoardCanvas = GameObject.Find("ScoreBoard");
            if (FirebaseManager.Singleton.isMusicOn)
            {
                GameObject musicHolderGO = Instantiate(musicHolder);
                musicHolderGO.GetComponent<PersonalMusicManager>().PV = PV;
            }            
        }
    }

    private void Update()
    {
        if (!PV.IsMine)
            return;

        if (controller == null)
            return;

        itemIndex = controller.GetComponent<PlayerController>().itemIndex;               
    }
    void CreateController()
    {        
        Transform spawnpoint = SpawnManager.Instance.GetSpawnPoint();
        controller = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerController"), spawnpoint.position, spawnpoint.rotation, 0, new object[] { PV.ViewID });
    }

    void Respawn()
    {
        Destroy(deathPanelGameObject);
        Destroy(virtualCamInstantiation);
        Destroy(cinemachineCamInstantiation);
        GameObject anim = Instantiate(threetwooneanim, canvas);
        Destroy(anim, 3);
        Invoke(nameof(CreateController), 3f);
        hasDeathPanelActivated = false;
    }

    public void Die()
    {
        if (!PV.IsMine)
            return;

        killTextNotificationGameObject = Instantiate(killTextNotification, killTextNotificationHolder.transform); // instantiate a new kill text notif
         
        Destroy(killTextNotificationGameObject, 5); // destroy that in 5 secs

        if (controller.GetComponent<PlayerController>().hasDiedFromFallDamage == false)
        {
            EnableCinemachineKillerTracker();
            killTextNotificationGameObject.GetComponent<TMP_Text>().text = "You were killed by: " + Find(killer).GetComponent<PhotonView>().Owner.NickName; // set the text of that to you were killed by the player
        }
        else
        {
            //GameObject fallCamera = Instantiate(fallDamageCamera, (new Vector3(controller.transform.position.x, controller.transform.position.y + 0.5f, controller.transform.position.z)), controller.transform.rotation);
            transform.GetChild(0).gameObject.SetActive(true);
            transform.GetChild(0).transform.SetPositionAndRotation(new Vector3(controller.transform.position.x, controller.transform.position.y + 0.5f, controller.transform.position.z), controller.transform.rotation);
            killTextNotificationGameObject.GetComponent<TMP_Text>().text = "You were killed by: The Void"; // set the text of that to you were killed by the void
        }

        StartCoroutine(OpenDeathPanel());

        PhotonNetwork.Destroy(controller);

        deaths++;

        Hashtable hash = new();
        hash.Add("deaths", deaths);
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
    }

    public IEnumerator OpenDeathPanel() 
    {
        yield return new WaitUntil(predicate: () => scoreBoardCanvas.GetComponent<ScoreBoard>().isConfirmationOpen == false);


        deathPanelGameObject = Instantiate(deathPanel, scoreBoardCanvas.transform);


        hasDeathPanelActivated = true;

        deathPanelGameObject.transform.Find("Replay").GetComponent<Button>().onClick.AddListener(Respawn); // of the deathPanelGameObject, find the button called "Replay" and add listener with the function called "Respawn"
        Cursor.lockState = CursorLockMode.None; // unlock the cursor
    }

    void EnableCinemachineKillerTracker()
    {
        cinemachineCamInstantiation = Instantiate(cinemachineCam, this.transform.position, Quaternion.identity);
        GameObject virtualCamera = Instantiate(virtualCam, this.transform.position, Quaternion.identity);

        if (Find(killer).transform.gameObject != null)
        {
            virtualCamera.GetComponent<CinemachineVirtualCamera>().LookAt = Find(killer).transform;
        }

        virtualCamInstantiation = virtualCamera;
    }

    public void GetKill()
    {
        PV.RPC(nameof(RPC_GetKill), PV.Owner);        
    }


    [PunRPC]
    void RPC_GetKill()
    {
        kills++;

        GameObject levelUpEmpty = controller.GetComponent<PlayerController>().levelUpAnimation;

        controller.GetComponent<PlayerController>().xpAnimation.SetActive(true);
        controller.GetComponent<PlayerController>().xpAnimation.GetComponent<Animator>().Play("XPAnimation");
        StartCoroutine(nameof(DisableXPAnimation));

        LevelUpManager.Singleton.AddExperiencePoints();
        LevelUpManager.Singleton.CheckLevelUp(int.Parse(PhotonNetwork.LocalPlayer.CustomProperties["playerLevel"].ToString()), levelUpEmpty, PV);

        GetComponent<AudioSource>().PlayOneShot(xp);
        GetComponent<AudioSource>().volume = 0.5f;
        Invoke(nameof(PlayXPAudio), 2f);

        Hashtable hash = new();
        hash.Add("kills", kills);
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
    }

    void PlayXPAudio() 
    {
        GetComponent<AudioSource>().volume = 1;
    }

    IEnumerator DisableXPAnimation()
    {
        yield return new WaitForSeconds(4f);

        controller.GetComponent<PlayerController>().xpAnimation.SetActive(false);
    }


    public static PlayerManager Find(Player player)
    {
        return FindObjectsOfType<PlayerManager>().SingleOrDefault(x => x.PV.Owner == player);
    }

    [PunRPC]
    public void GetBulletDamageInfo(Player player)
    {
        //return controller.gameObject.GetComponent<PlayerController>().itemIndex;
        PV.RPC(nameof(ReturnBulletDamageInfo), player, GetControllerItemIndex(), player);
    }

    [PunRPC]
    public void ReturnBulletDamageInfo(int itemIndex, Player player)
    {
        if (PV.IsMine)
        {
            //controller.GetComponent<PlayerController>().itemGlobal = itemIndex;
            Find(player).controller.GetComponent<PlayerController>().itemGlobal = itemIndex;
            Debug.Log(player + "The Player!");
        }        
    }

    public int GetControllerItemIndex()
    {

        return itemIndex;
    }
}
