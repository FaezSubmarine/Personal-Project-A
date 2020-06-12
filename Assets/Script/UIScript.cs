using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class UIScript : MonoBehaviour
{
    [SerializeField]ScriptableFloat HP;
    [SerializeField]ScriptableFloat shadow;
    [SerializeField] GameObject HPDisplay;
    Image shadowDisplay;
    [SerializeField] Color shadowColor, lightColor;
    Transform healthParent;
    // Start is called before the first frame update

    //TODO: Make health on player and then associate it. Shadow too
    void Start()
    {
        healthParent = transform.Find("Health Parent");
        shadowDisplay = transform.Find("Shadow").GetComponent<Image>();
        changeHPCount();
        //for(int i = 0; i <= HP.point; ++i)
        //{
        //    GameObject go = Instantiate(HPDisplay, healthParent);
        //    go.transform.GetComponent<RectTransform>().anchoredPosition += new Vector2(i * 60, 0);
        //}
        changeShadowColor();
    }
    void changeHPCount()
    {
        int og = healthParent.childCount;
        if (HP.point < og)
        {
            for(int i = og - 1; i > HP.point; --i)
            {
                Destroy(healthParent.GetChild(i).gameObject);
            }
        }
        else
        {
            for(int i = og; i < (HP.point - og); ++i)
            {
                GameObject go = Instantiate(HPDisplay, healthParent);
                go.transform.GetComponent<RectTransform>().anchoredPosition += new Vector2(i * 60, 0);
            }
        }
    }
    void changeShadowColor()
    {
        shadowDisplay.color = Color.Lerp(shadowColor, lightColor, shadow.point / 2);
    }
    // Update is called once per frame
    void Update()
    {
        if (shadow.changed)
        {
            changeShadowColor();
            shadow.changed = false;
        }
        if (HP.changed)
        {
            changeHPCount();
            //TODO: change according to the amount of health
        }
    }
}
