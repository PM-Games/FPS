using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;

    [SerializeField] Menu[] menus;

    FirebaseManager firebase;

    private void Awake()
    {
        Instance = this;
        firebase = GameObject.Find("FirebaseManager").GetComponent<FirebaseManager>();
    }

    public void OpenMenu(string menuName)
    {
        for (int i = 0; i < menus.Length; i++)
        {
            if (menus[i].menuName == menuName)
            {
                menus[i].Open();
            }
            else if (menus[i].open)
            {
                CloseMenu(menus[i]);
            }
        }
    }

    public void OpenMenu(Menu menu)
    {
        for (int i = 0; i < menus.Length; i++)
        {
            if (menus[i].open)
            {
                CloseMenu(menus[i]);
            }
        }
        menu.Open();
    }

    public void CloseMenu(Menu menu)
    {
        menu.Close();
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void SignOut()
    {
        firebase.auth.SignOut();
        firebase.menuCanvas.SetActive(false);
        firebase.accountCanvas.SetActive(true);
        firebase.mainCamera.transform.Find("PlayerViewer").gameObject.SetActive(false);
    }

    private void Update()
    {
        if (firebase.User.UserId == null)
        {
            firebase.menuCanvas.SetActive(true);
            firebase.accountCanvas.SetActive(false);
        }
    }
}
