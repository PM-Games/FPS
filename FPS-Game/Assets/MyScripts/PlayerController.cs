using System.Collections;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Realtime;
using TMPro;
using Cinemachine;
using System.Linq;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviourPunCallbacks, IDamageable
{
    [SerializeField] Image healthBarImage; // health bar image from Tutorial
    [SerializeField] GameObject ui; // ui from Tutorial
    
    [SerializeField] GameObject cameraHolder;
    public AudioClip killSFX;

    public float mouseSensitivity, sprintSpeed, walkSpeed, jumpForce, smoothTime;

    [SerializeField] Item[] items;

    public int itemIndex;
    int previousItemIndex = -1;

    float verticalLookRotation; 
    public bool grounded; 
    Vector3 smoothMoveVelocity; 
    Vector3 moveAmount; 

    Rigidbody rb; 

    public PhotonView PV; 

    const float maxHealth = 100f; 
    public float currentHealth = maxHealth; 

    PlayerManager playerManager; // player manager

    [SerializeField] GameObject playerVisuals; // this is the colored visual part of the "bean" player
    [HideInInspector] public int playerHealth; 

    public GameObject redDeathParticleSystem; 

    public bool isDead = false; 

    [SerializeField] GameObject overheadUsernameText; 
    [SerializeField] GameObject itemHolder; 
    [SerializeField] GameObject healthBar; // health bar from tutorial

    [SerializeField] Camera normalCam;   

    bool hasDeathPanelActivated = false;

    [SerializeField] GameObject gunClippingCam; // a camera that only renders the weapons to prevent them clipping into the walls

    public bool canSwitchWeapons = true;

    [HideInInspector] public GameObject scoreBoard; // scoreboard that I assign later

    public GameObject[] canvas; // canvases I want to destroy when a player dies

    bool canRegenerateHealth = false;

    [SerializeField] GameObject playerHitParticleEffect; 

    public Shader glowShader; // a glow shader that i use for my player visuals

    bool micIsOn = true; 

    GameObject micToggleText; // a UI text that shows if the mic is on or off to the player ingame

    public BuildingSystem buildingSystem; 

    GameObject mapViewerCamera; // a camera that overlooks the map

    public AudioClip walkSound;
    public AudioClip runSound;
    public float footstepDelay;
    public AudioSource footstepAudioSource;
    public bool hasDiedFromFallDamage = false;

    public GameObject inventory;
    public GameObject inventoryNumbers;
    public bool inventoryEnabled = false;

    public ParticleSystem dustTrailParticleSystem;

    public FirebaseManager firebase;

    public Shader toonShader;

    public int itemGlobal;

    public GameObject damageNumber;

    public bool needToClearFog = false;

    public bool isMoving = false;

    public GameObject levelUpAnimation;

    public GameObject xpAnimation;

    public GameObject lilBean;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        PV = GetComponent<PhotonView>();

        playerManager = PhotonView.Find((int)PV.InstantiationData[0]).GetComponent<PlayerManager>(); // HONESTLY I DONT KNOW HOW THIS WORKS BUT THIS IS FROM THE TUTORIAL
        Cursor.lockState = CursorLockMode.Locked;
    }
    private void Start()
    {

        if (PV.IsMine) // if PV is mine
        {
            EquipItem(0); // equip with int of 0
            scoreBoard = GameObject.Find("ScoreBoard");
            micToggleText = GameObject.Find("MicToggleText");
            mapViewerCamera = GameObject.Find("RoomViewerCamera");
            firebase = GameObject.Find("FirebaseManager").GetComponent<FirebaseManager>();
            //normalCam.gameObject.tag = "Untagged";
            needToClearFog = true;
        }
        else // if PV isn't mine
        {
            Destroy(GetComponentInChildren<Camera>().gameObject); // destroy camera's children
            Destroy(rb); // destroy rigidbody
            Destroy(ui); // destroy ui
            for (int i = 0; i < canvas.Count(); i++)
            {
                Destroy(canvas[i]); // destroy all the canvases that i want destroyed
            }
            //Destroy(buildingSystem.handHeldBlock); // destroy hand held block gameobject from the buildingsystem class
            //Destroy(buildingSystem.blockCrosshair); // destroy block crosshair gameobject from the buildingsystem class            
        }

        if (SceneManager.GetActiveScene().name == "Sky-Beans") // if the active scene is the low gravity scene
        {
            Physics.gravity = new Vector3(0, -2, 0); // set gravity lower
            jumpForce = 500; // increase jump force
        } else
        {
            Physics.gravity = new Vector3(0, -9.81f, 0);
            jumpForce = 300f;
        }
    }

    private void Update()
    {
        if (PhotonNetwork.IsConnectedAndReady == false) // if player isn't connected and ready
            return;

        if (!PV.IsMine) // if PV isn't mine
            return;

        normalCam.enabled = true;

        // assign following variables to objects found in each scene


        // if you click escape and the death panel hasn't been instantiated, then unlock cursor
        if (Input.GetKeyDown(KeyCode.Escape) && Cursor.lockState == CursorLockMode.Locked && playerManager.hasDeathPanelActivated == false)
        {
            Cursor.lockState = CursorLockMode.None;
        }
        // if you click escape and the death panel hasn't been instantiated, if the scoreboard isn't active, and if the leave confirmation is disactive, then unlock cursor
        if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None && playerManager.hasDeathPanelActivated == false && scoreBoard.GetComponent<ScoreBoard>().isOpen == false && scoreBoard.GetComponent<ScoreBoard>().leaveConfirmation.alpha != 1)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }        

        string _deviceName = Microphone.devices[0]; // find the first device and log the name

        // if you press M
        if (Input.GetKeyDown(KeyCode.M)) 
        {
            micIsOn = !micIsOn; // is mic on changes
            //if (micIsOn)
            //{
            //    VoiceChatManager.Instance.OnJoinedRoom();
            //}
            //else
            //{
            //    VoiceChatManager.Instance.OnLeftRoom();
            //}

            GetComponent<MicrophoneToggle>().ToggleMicrophone();
        }
        if (micIsOn) micToggleText.GetComponent<TMP_Text>().text = "Click 'M' to toggle mic on"; // if the mic is on, set the mic UI text to "on"
        else micToggleText.GetComponent<TMP_Text>().text = "Click 'M' to toggle mic off"; // if the mic is on, set the mic UI text to "off"

        Debug.Log(micIsOn);

        scoreBoard.GetComponent<ScoreBoard>().OpenLeaveConfirmation(); // call OpenLeaveConfirmation function from ScoreBoard class

        // if the leave confirmation is visible to the player
        if (scoreBoard.GetComponent<ScoreBoard>().leaveConfirmation.alpha == 1)
        {
            StartCoroutine(nameof(Leave)); // start Leave coroutine
        }        

        if (isDead == true) // if dead, return
            return;

        playerManager.transform.position = this.gameObject.transform.position;

        if (needToClearFog == true)
        {
            StartCoroutine(nameof(ClearFog));            
        }

        Look(); // look

        Move(); // move

        Jump(); // jump

        SetPlayerHealthShader(); // set player health shader

        //healthBarImage.fillAmount = currentHealth / maxHealth; // set the fill amount of the health bar

        // if the current health is <= 100 and >= 50
        if (currentHealth <= 100 && currentHealth >= 50)
        {
            playerHealth = 3; // set the player health to 3
        }
        // if the current health is <= 49 and >= 30
        if (currentHealth <= 49 && currentHealth >= 30)
        {
            playerHealth = 2; // set the player health to 2
        }
        // if the current health is <= 29 and >= 0
        if (currentHealth <= 29 && currentHealth >= 0)
        {
            playerHealth = 1; // set the player health to 1
        }

        //if PV is mine and I'm connected to the servers
        if (PV.IsMine && PhotonNetwork.IsConnectedAndReady)
        {
            Hashtable hash = new(); // new hash
            if (hash.ContainsKey("healthColor"))
            {
                hash.Remove("healthColor"); // make sure to not remove a custom property when changing it
            }
            hash.Add("healthColor", playerHealth); // add "healthColor" to hash
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash); //set custom properties

            PV.RPC(nameof(SetGlowIntensitity), RpcTarget.All); // set glow shader intensity to all players using RPC
        }

        // all this is from the tutorial, and I don't wanna explain it v
        if (canSwitchWeapons)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (Input.GetKeyDown((i + 1).ToString()))
                {
                    EquipItem(i);
                    break;
                }
            }
            if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
            {
                if (itemIndex >= items.Length - 1)
                {
                    EquipItem(0);
                }
                else
                {
                    EquipItem(itemIndex + 1);
                }
            }
            if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
            {
                if (itemIndex <= 0)
                {
                    EquipItem(items.Length - 1);
                }
                else
                {
                    EquipItem(itemIndex - 1);
                }
            }
        }

        items[itemIndex].Use();
        // all of this is from the tutorial, and I don't wanna explain it ^

        // if the transform.y is < -10 and the PV is mine
        if (transform.position.y < -10f && PV.IsMine)
        {
            hasDiedFromFallDamage = true;
            Die(); // die            
        }

        PV.RPC(nameof(ProcessFootstepSFX), RpcTarget.All);

        if (Input.GetKeyDown(KeyCode.I))
        {
            inventoryEnabled = !inventoryEnabled; // is mic on changes
            if (inventoryEnabled)
            {
                inventory.SetActive(true);
                inventoryNumbers.SetActive(true);
                inventoryEnabled = true;
                scoreBoard.GetComponent<ScoreBoard>().blur.SetActive(true);
            }
            else
            {
                inventory.SetActive(false);
                inventoryNumbers.SetActive(false);
                inventoryEnabled = false;
                scoreBoard.GetComponent<ScoreBoard>().blur.SetActive(false);
            }
        }
    }

   [PunRPC]
    void ProcessFootstepSFX()
    {
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
        {
            if (grounded)
            {
                if (FirebaseManager.Singleton.alwaysSprint == false)
                {
                    if (Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift))
                    {
                        StartCoroutine(nameof(RunSFX));
                    }
                    else if (!Input.GetKey(KeyCode.RightShift) && !Input.GetKey(KeyCode.LeftShift))
                    {
                        StartCoroutine(nameof(WalkSFX));
                    }
                }
                else
                {
                    StartCoroutine(nameof(RunSFX)); Debug.Log("running is always on");
                }               
            }
            if (!PV.IsMine)
                return;
            PV.RPC(nameof(RPC_DisplayDustTrail), RpcTarget.All);
        }
        else
        {
            StartCoroutine(nameof(StopWalkSFX));
            if (!PV.IsMine)
                return;
            PV.RPC(nameof(RPC_StopDustTrail), RpcTarget.All);
        }
    }

    [PunRPC]
    void RPC_DisplayDustTrail()
    {
        dustTrailParticleSystem.GetComponent<ParticleSystem>().Emit(1);
    }

    [PunRPC]
    void RPC_StopDustTrail()
    {
        dustTrailParticleSystem.GetComponent<ParticleSystem>().Emit(0);
    }

    IEnumerator ClearFog()
    {
        float elapsedTime = 0f;
        elapsedTime += Time.timeSinceLevelLoad;
        float percentComplete = elapsedTime / 1;
        float lerpTime = Mathf.Lerp(0.2f, 0f, percentComplete);

        RenderSettings.fogDensity = lerpTime;

        yield return new WaitUntil(predicate: () => lerpTime == 0);

        needToClearFog = false;
    }

    IEnumerator WalkSFX()
    {
        yield return new WaitForSeconds(0.05f);
        footstepAudioSource.enabled = true;
        if (footstepAudioSource.isPlaying == false)
        {
            footstepAudioSource.PlayOneShot(walkSound, 0.25f);
        }
    }
    IEnumerator RunSFX()
    {
        yield return new WaitForSeconds(0.05f);
        footstepAudioSource.enabled = true;
        if (footstepAudioSource.isPlaying == false)
        {
            footstepAudioSource.PlayOneShot(runSound, 0.25f);
        }
    }

    IEnumerator StopWalkSFX()
    {
        yield return new WaitForSeconds(0.1f);
        footstepAudioSource.enabled = false;        
    }

    // this function basically gets both player visual gameobjects and sets the material Glow Intensity variable in the shader to 2.5f
    [PunRPC]
    void SetGlowIntensitity()
    {
        playerVisuals.transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.SetFloat("_FresnelGlowIntensity", 2.5f);
        lilBean.GetComponent<MeshRenderer>().material.SetFloat("_FresnelGlowIntensity", 2.5f);
        //playerVisuals.transform.GetChild(1).gameObject.GetComponent<MeshRenderer>().material.SetFloat("_FresnelGlowIntensity", 2.5f);
    }

    // look function from the tutorial
    void Look()
    {
        transform.Rotate(Input.GetAxisRaw("Mouse X") * mouseSensitivity * Vector3.up);

        verticalLookRotation += Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);

        cameraHolder.transform.localEulerAngles = Vector3.left * verticalLookRotation;
    }

    // move function from the tutorial
    void Move()
    {
        Vector3 moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

        if (FirebaseManager.Singleton.alwaysSprint == false)
            moveAmount = Vector3.SmoothDamp(moveAmount, moveDir * (Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed), ref smoothMoveVelocity, smoothTime);
        else
            moveAmount = Vector3.SmoothDamp(moveAmount, moveDir * sprintSpeed, ref smoothMoveVelocity, smoothTime);
    }

    // jump function from the tutorial
    void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && grounded)
        {
            rb.AddForce(transform.up * jumpForce);            
        }
        
    }

    // a leave ienumerator
    IEnumerator Leave()
    {
        if (PV.IsMine) // if PV is mine and is active
        {
            yield return new WaitForSeconds(0.01f); // wait for 0.01 seconds

            // if esc is pressed
            if (Input.GetKeyDown(KeyCode.Escape)) 
            {
                StartCoroutine(DisconnectAndLoad()); // start Disconnect And Load coroutine
            }
        }
    }

    // Disconnect And Load ieunmerator
    IEnumerator DisconnectAndLoad()
    {
        if (PhotonNetwork.IsConnectedAndReady && !isDead) // if connected and ready
        {
            PhotonNetwork.LeaveRoom(); // leave room
            Destroy(RoomManager.Instance.gameObject); // destroy room manager.instance

            normalCam.gameObject.SetActive(false); // disable the normal cam
            gunClippingCam.SetActive(false); // disable the gun clipping cam
            mapViewerCamera.SetActive(true); // enable the map viewer camera

            while (PhotonNetwork.InRoom) // while in a room, yield
                yield return null;

            SceneManager.LoadScene("Menu"); // load scene 0 (main menu)
        }
    }

    // EquipItem function from tutorial (I DON'T KNOW HOW THIS WORKS SO I'M NOT GOING TO EXPLAIN IT
    void EquipItem(int _index)
    {
        if (_index == previousItemIndex)
            return;

        itemIndex = _index;

        items[itemIndex].itemGameObject.SetActive(true);

        if (previousItemIndex != -1)
        {
            items[previousItemIndex].itemGameObject.SetActive(false);
        }

        previousItemIndex = itemIndex;

        if (PV.IsMine)
        {
            Hashtable hash = new();
            if (hash.ContainsKey("itemIndex"))
            {
                hash.Remove("itemIndex");
            }
            hash.Add("itemIndex", itemIndex);
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        }
    }

    // on player properties update
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
       // if the changed props is "itemIndex"
       if (changedProps.ContainsKey("itemIndex") && !PV.IsMine && targetPlayer == PV.Owner)
       {
            EquipItem((int)changedProps["itemIndex"]); //EquipItem with a parameter of changed props["itemIndex"]
       }

       // if the changed props is "beanColor"
       if (changedProps.ContainsKey("beanColor") && !PV.IsMine && targetPlayer == PV.Owner)
       {
            Material healthyMat = new(glowShader); // make a new material healthyMat and set the shader of that as glowShader

            // convert a hexadecimal string to a color value
            if (ColorUtility.TryParseHtmlString("#" + changedProps["beanColor"], out Color healthyColor))
            {
                healthyMat.SetColor("_MaterialColor", healthyColor); // set the "_MaterialColor" value in healthyMat to the newly made healthyColor
                playerVisuals.transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material = healthyMat; // get the mesh renderer of the player visual and set the material to healthyMat
                lilBean.GetComponent<MeshRenderer>().material = healthyMat;
                //playerVisuals.transform.GetChild(1).gameObject.GetComponent<MeshRenderer>().material = healthyMat; // get the mesh renderer of the player visual and set the material to healthyMat
            }
        }

       // if the changed props is "healthColor"
       if (changedProps.ContainsKey("healthColor") && !PV.IsMine && targetPlayer == PV.Owner)
       {
            // if the changed props ["healthColor"] == 3
            if ((int)changedProps["healthColor"] == 3)
            {
                playerVisuals.transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.SetColor("_FresnelColor", Color.green); // set the "_FresnelColor" variable in the player visual's material to green
                lilBean.GetComponent<MeshRenderer>().material.SetColor("_FresnelColor", Color.green);
                //playerVisuals.transform.GetChild(1).gameObject.GetComponent<MeshRenderer>().material.SetColor("_FresnelColor", Color.green); // set the "_FresnelColor" variable in the player visual's material to green
            }

            // if the changed props ["healthColor"] == 2
            else if ((int)changedProps["healthColor"] == 2)
            {
                playerVisuals.transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.SetColor("_FresnelColor", Color.yellow); // set the "_FresnelColor" variable in the player visual's material to yellow
                lilBean.GetComponent<MeshRenderer>().material.SetColor("_FresnelColor", Color.yellow);

                //playerVisuals.transform.GetChild(1).gameObject.GetComponent<MeshRenderer>().material.SetColor("_FresnelColor", Color.yellow); // set the "_FresnelColor" variable in the player visual's material to yellow
            }

            // if the changed props ["healthColor"] == 1
            else if ((int)changedProps["healthColor"] == 1)
            {
                playerVisuals.transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.SetColor("_FresnelColor", Color.red); // set the "_FresnelColor" variable in the player visual's material to red
                lilBean.GetComponent<MeshRenderer>().material.SetColor("_FresnelColor", Color.red);

                //playerVisuals.transform.GetChild(1).gameObject.GetComponent<MeshRenderer>().material.SetColor("_FresnelColor", Color.red); // set the "_FresnelColor" variable in the player visual's material to red
            }
        }
    }

    // set grounded state from tutorial
    public void SetGroundedState(bool _grounded)
    {
        grounded = _grounded;
    }

    // IDK what Rugbug did with this, but this is from the tutorial
    private void FixedUpdate()
    {
        if (!PV.IsMine)
            return;
        rb.MovePosition(rb.position + transform.TransformDirection(moveAmount) * Time.fixedDeltaTime);
    }

    //TakeDamage with parameter damage from tutorial
    public void TakeDamage(float damage)
    {
        PV.RPC(nameof(RPC_DisplayDamageText), RpcTarget.All);
        PV.RPC(nameof(RPC_TakeDamage), PV.Owner, damage); // call RPC Take Damage to me with a parameter of damage
        PV.RPC(nameof(RPC_DisplayDamage), RpcTarget.All); // call RPC DisplayDamage to all players        
    }

    // DisplayDamage function
    [PunRPC]
    public void RPC_DisplayDamage()
    {
        GameObject playerHitEffect = Instantiate(playerHitParticleEffect, this.gameObject.transform.position, Quaternion.identity); // instantiate a player hit particle effect when you take damage
        playerHitEffect.GetComponent<ParticleSystem>().Emit(15); // emit 15 particles
        Destroy(playerHitEffect, 2f); // destroy this in 2 secs
    }

    [PunRPC]
    void RPC_DisplayDamageText(PhotonMessageInfo info)
    {
        if (isDead)
            return;

        PlayerManager.Find(info.Sender).GetBulletDamageInfo(PV.Owner);
        float damageInfo = itemGlobal;
        float damageAmount;

        GameObject damagePrefab = Instantiate(damageNumber, transform, true);
        damagePrefab.transform.position = new Vector3(transform.position.x, transform.position.y + 1.5f, transform.position.z);
        damagePrefab.transform.position += new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(-0.05f, 0.05f), 0);
        damagePrefab.GetComponent<TextMeshPro>().color = Color.red;
        Destroy(damagePrefab, 2f);
        Debug.Log("This is the itemGlobal: " + itemGlobal);

        if (damageInfo == 0)
        {
            damageAmount = 10f;
            damagePrefab.GetComponent<TextMeshPro>().text = damageAmount.ToString();
            Debug.Log("Gun");
        }
        if (damageInfo == 1)
        {
            damageAmount = 10f;
            damagePrefab.GetComponent<TextMeshPro>().text = damageAmount.ToString();
            Debug.Log("Gun");

        }
        if (damageInfo == 2)
        {
            damageAmount = 14f;
            damagePrefab.GetComponent<TextMeshPro>().text = damageAmount.ToString();
            Debug.Log("Gun");

        }
        if (damageInfo == 3)
        {
            damageAmount = 100f;
            damagePrefab.GetComponent<TextMeshPro>().text = damageAmount.ToString();
            Debug.Log("Gun");

        }
        if (damageInfo == 4)
        {
            damageAmount = 40f;
            damagePrefab.GetComponent<TextMeshPro>().text = damageAmount.ToString();
            Debug.Log("Gun");
        }
    }

    // TakeDamage function with parameters damage and info
    [PunRPC]
    void RPC_TakeDamage(float damage, PhotonMessageInfo info)
    {
        currentHealth -= damage; // subtract damage from currentHealth

        CancelInvoke(nameof(StartRegen)); // first, cancel the StartRegen function invoke
        Invoke(nameof(StartRegen), 5f); // then, start invoking StartRegen in 5 secs
        canRegenerateHealth = false; // set canRegenerateHealth to false

        
        // if current health <= 0
        if (currentHealth <= 0)
        {
            if (isDead || !PV.IsMine) // if isDead OR if PV isn't mine, return 
                return;

            if (PlayerManager.Find(info.Sender).transform.gameObject == null)
                return;


            playerManager.killer = info.Sender;

            PlayerManager.Find(info.Sender).GetKill(); // find info.Sender's playermanager and call GetKill

            PV.RPC(nameof(RPC_PlayKillDingSFX), info.Sender); // play ding sfx to info.sender

            Die(); // die
        }
    } 

    // function StartRegen
    void StartRegen()
    {
        canRegenerateHealth = true; // make can regenerate health to true
        StartCoroutine(nameof(Regen)); // start coroutine of Regen
    }
    
    // function PlayKillDingSFX
    [PunRPC]
    public void RPC_PlayKillDingSFX()
    {
        Debug.Log("Play ding sfx symbolizing a kill");

        // get this gameobject and check if it's not playing
        if (this.gameObject.GetComponent<AudioSource>().isPlaying == false)
        {
            this.gameObject.GetComponent<AudioSource>().PlayOneShot(killSFX); // play kill sfx 
        }
        else // get this gameobject and check if it is playing
        {
            this.gameObject.GetComponent<AudioSource>().Stop(); // stop playing
            this.gameObject.GetComponent<AudioSource>().PlayOneShot(killSFX); // play kill sfx
        }
    }

    // Regen ienumerator
    IEnumerator Regen()
    {
        // while current health < 100 and can regenerate health
        while (currentHealth < 100 && canRegenerateHealth)
        {            
            yield return new WaitForSeconds(0.1f); // wait for .1 seconds
            currentHealth += 0.5f; // increase current health by 0.5f
        }
    }

    // set player health shader
    public void SetPlayerHealthShader()
    {
        // if the PV is mine
        if (PV.IsMine)
        {
            Hashtable hash = new(); // new hash
            if (hash.ContainsKey("beanColor"))
            {
                hash.Remove("beanColor"); // make sure to not remove a custom property when changing it
            }
            //hash.Add("beanColor", PlayerPrefs.GetString("BeanPlayerColor")); // add "beanColor" with a value of PlayerPrefs.GetString("BeanPlayerColor")
            //PhotonNetwork.LocalPlayer.SetCustomProperties(hash); // set custom properties

            hash.Add("beanColor", firebase.playerColorValue); // add "beanColor" with a value of PlayerPrefs.GetString("BeanPlayerColor")
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash); // set custom properties

            if (isDead)
                return;

            if (playerHealth == 3) // if player health is 3
                SetHealthyNewMaterial(); // set HEALTHY new material function

            if (playerHealth == 2) // if player health is 2
                SetNormalNewMaterial(); // set NORMAL new material function

            if (playerHealth == 1) // if player health is 1
                SetHurtNewMaterial(); // set HURT new material function
        }                
    }

    // function set player health material
    void SetHealthyNewMaterial()
    {
        Material healthyMat = new(glowShader); // new material with glowshader
        
        // get player hexadecimal that was saved in player prefs and convert that into a color
        if (ColorUtility.TryParseHtmlString("#" + firebase.playerColorValue, out Color healthyColor))
        {
            healthyMat.SetColor("_MaterialColor", healthyColor); // healthymat's "_MaterialColor" should be set to the converted healthyColor
            healthyMat.SetColor("_FresnelColor", Color.green); // set "_FresnelColor" color to green
            playerVisuals.transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material = healthyMat; // get player visual gameobject and set the material to healthyMat
            lilBean.GetComponent<MeshRenderer>().material = healthyMat; // get player visual gameobject and set the material to healthyMat

            //playerVisuals.transform.GetChild(1).gameObject.GetComponent<MeshRenderer>().material = healthyMat; // get player visual gameobject and set the material to healthyMat
        }

    }

    // function set player normal material
    void SetNormalNewMaterial()
    {
        Material normalMat = new(glowShader); // new material with glowshader

        // get player hexadecimal that was saved in player prefs and convert that into a color
        if (ColorUtility.TryParseHtmlString("#" + firebase.playerColorValue, out Color normalColor))
        {
            normalMat.SetColor("_MaterialColor", normalColor); // normalmat's "_MaterialColor" should be set to the converted normalColor
            normalMat.SetColor("_FresnelColor", Color.yellow); // set "_FresnelColor" color to yellow
            playerVisuals.transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material = normalMat; // get player visual gameobject and set the material to normalMat
            lilBean.GetComponent<MeshRenderer>().material = normalMat; // get player visual gameobject and set the material to healthyMat

            //playerVisuals.transform.GetChild(1).gameObject.GetComponent<MeshRenderer>().material = normalMat; // get player visual gameobject and set the material to normalMat
        }
    }

    // function set player hurt material
    void SetHurtNewMaterial()
    {
        Material hurtMat = new(glowShader); // new material with glowshader

        // get player hexadecimal that was saved in player prefs and convert that into a color
        if (ColorUtility.TryParseHtmlString("#" + firebase.playerColorValue, out Color hurtColor))
        {
            hurtMat.SetColor("_MaterialColor", hurtColor); // normalmat's "_MaterialColor" should be set to the converted hurtColor
            hurtMat.SetColor("_FresnelColor", Color.red); // set "_FresnelColor" color to red
            playerVisuals.transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material = hurtMat; // get player visual gameobject and set the material to hurtMat
            lilBean.GetComponent<MeshRenderer>().material = hurtMat; // get player visual gameobject and set the material to hurtMat
            //playerVisuals.transform.GetChild(1).gameObject.GetComponent<MeshRenderer>().material = hurtMat; // get player visual gameobject and set the material to hurtMat
        }
    }

    // die function
    void Die()
    {
        if (!PV.IsMine) // if PV isn't mine, return
            return;

        PV.RPC(nameof(RPC_DisplayDeath), RpcTarget.All); // call RPC DisplayDeath to all players
        //buildingSystem.enabled = false; // disable building system class
        GetComponent<CapsuleCollider>().enabled = false; // disable capsule collider
        playerManager.Die();

        for (int i = 0; i < canvas.Count(); i++)
        {
            Destroy(canvas[i]); // destroy all canvases specified
        }

        isDead = true; // set isdead to true
        GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePosition; // freeze position of player
    }

    // respawn function
    void Respawn()
    {
        if (!PV.IsMine) // if PV isn't mine, return
            return;

        playerManager.Die(); // call die function from my playerManager
    }

    // function DisplayDeath
    [PunRPC] 
    void RPC_DisplayDeath()
    {
        GameObject particleSystem = PhotonNetwork.Instantiate(nameof(redDeathParticleSystem), this.gameObject.transform.position, Quaternion.identity, 0); // instantiate a death particle effect
        particleSystem.GetComponent<ParticleSystem>().Emit(30); // emit 30 particles
        Destroy(particleSystem, 5f); // destroy particle system in 5 seconds

        Destroy(playerVisuals); // destroy player visuals
        Destroy(itemHolder); // destroy item holder
        Destroy(overheadUsernameText); // destroy overhead username text
        Destroy(healthBar); // destroy health bar
        GetComponent<CapsuleCollider>().enabled = false; // disable capsule collider
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log(newPlayer);
        normalCam.enabled = false;
        normalCam.enabled = true;
    }
}