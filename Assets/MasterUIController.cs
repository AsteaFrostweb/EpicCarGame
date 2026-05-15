using System;
using System.Linq;
using UnityEditor;
using UnityEngine;


public class MasterUIController : MonoBehaviour
{
    [Serializable]
    public struct UIMenuObject 
    {
        public UserInterfaceMenu menu;
        public GameObject gameObject => menu.gameObject;

        public void SetMenuActive(bool value)
        {
            gameObject.SetActive(value);
        }
        public void HideMenu()
        {
            gameObject.SetActive(false);
        }   
        public void ShowMenu()
        {
            gameObject.SetActive(true);
        }
        public void ToggleMenu()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }
    }
    [SerializeField]
    public UIMenuObject DebugMenu;

    public UIMenuObject newMenu;

    private static UIMenuObject[] menuObjects;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        menuObjects = new UIMenuObject[] { DebugMenu, newMenu }; 
        foreach (var menu in menuObjects)
        {
            menu.SetMenuActive(menu.menu.IsVisible);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ToggleMenuByName(string name) 
    {
        menuObjects.First(o => o.menu.Name == name).ToggleMenu();
    }
}
