
//Must be placed on root of ui menu
using System;
using UnityEngine;

[Serializable]
public abstract class UserInterfaceMenu : MonoBehaviour
{
    public string Name;

    public bool IsVisible = true;
    public GameObject[] childElements { get; private set; }
    protected void Inititalize()
    {
        childElements = new GameObject[gameObject.transform.childCount];
        for (int i = 0; i < childElements.Length; i++)
        {
            childElements[i] = gameObject.transform.GetChild(i).gameObject;
        }
        foreach (GameObject child in childElements) { 
            child.SetActive(IsVisible);
        }
        Debug.Log($"Initialized UI menu: {Name} with {childElements.Length} child elements.");
    }

    public virtual void HideElements()
    {
        foreach (GameObject child in childElements)
        {
            child.SetActive(false);
        }
    }
    public virtual void ShowElements()
    {
        foreach (GameObject child in childElements)
        {
            child.SetActive(true);
        }
    }
    public virtual void ToggleElements()
    {
        foreach (GameObject child in childElements)
        {
            child.SetActive(!IsVisible);
        }
    }
}