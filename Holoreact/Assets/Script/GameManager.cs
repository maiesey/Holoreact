﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private List<GameObject> itemList;
    private Combination[] combinationList;
    private Collider[] collidedColliders;

    [SerializeField]
    private GameObject cameraForGameplay;

    [SerializeField]
    private GameObject handBookManager;

    private int combinationPerformed;
    private int selectedIndex;
    private int currentIndex;
    private int currentLvl;

    private bool paused;
    private bool selectedItem;

    // Start is called before the first frame update
    void Start()
    {
        currentIndex = 0;
        itemList = new List<GameObject>();
        selectedItem = false;
        selectedIndex = -1;
        combinationPerformed = 0;

        currentLvl = PlayerPrefs.GetInt("currentLevel");

        GetItemList();

        GetCombinationDataFromAPI();

        itemList[0].SetActive(true);

        paused = true;

        DeactiveAllItemAndResetPosition();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        CheckCollidedObject();
    }

    private void CheckCollidedObject()
    {
        Collider planeCollider = gameObject.GetComponent<Collider>();
        collidedColliders = Physics.OverlapBox(gameObject.transform.position, transform.localScale*10, Quaternion.identity, LayerMask.GetMask("ExperimentObject"));
        //Collider[] collidedColliders = Physics.OverlapBox(gameObject.transform.TransformPoint(planeCollider.bounds.center), gameObject.transform.TransformVector(planeCollider.bounds.size), gameObject.transform.rotation);
        int i = 0;
        while (i < collidedColliders.Length)
        {
            Debug.Log("Collided with : " + collidedColliders[i].name);
            i++;
        }
        
        if (i == 2)
        {
            FindCombinationResult(collidedColliders[0].gameObject, collidedColliders[1].gameObject);
        }
    }

    private int CalculateExperimentScore()
    {
        int result;
        return result = combinationPerformed / combinationList.Count() * 100;
    }

    private void ShowHandbook()
    {
        Debug.Log("selectedIndex at showHandbook: " + selectedIndex);
        cameraForGameplay.SetActive(false);
        itemList[currentIndex].SetActive(false);
        paused = true;
        handBookManager.GetComponent<HandBookManager>().UnPause();
    }

    public void UnPause()
    {
        paused = false;
        cameraForGameplay.SetActive(true);
        itemList[currentIndex].SetActive(true);

        if (selectedIndex != -1)
        {
            itemList[selectedIndex].SetActive(true);
        }
    }

    private void Combine()
    {
        if (FindCombinationResult(itemList[selectedIndex], itemList[currentIndex]))
        {
            //deactive to off all object and then active the combine result object
            DeactiveAllItemAndResetPosition();

            currentIndex = itemList.Count - 1;
            itemList[currentIndex].SetActive(true);
            combinationPerformed += 1;
        }
        else
        {
            //give marning and reduce score
            DeactiveAllItemAndResetPosition();

            itemList[currentIndex].SetActive(true);
        }
        selectedItem = false;
        selectedIndex = selectedIndex - 1;
    }

    private void DeactiveAllItemAndResetPosition()
    {
        foreach (GameObject data in itemList)
        {
            data.transform.position = new Vector3(0, 0, 0);

            data.SetActive(false);
        }
    }

    #region Get Data from API
    /// <summary>
    /// GetCombinationDataFromAPI()
    /// GetHandbookData()
    /// GetItemList()
    /// </summary>

    private void GetItemList()
    {
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(String.Format("https://api.backendless.com/09476775-387A-4C56-FFE4-B663DC24FC00/DED29ABA-8FAC-4985-86E0-FCCDA5A290B5/data/ItemList?pageSize=50&where=levelid%3D" + currentLvl));
        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        StreamReader reader = new StreamReader(response.GetResponseStream());
        string jsonResponse = reader.ReadToEnd();
        jsonResponse = JsonHelper.FixJSon(jsonResponse);

        Item[] items;

        items = JsonHelper.FromJson<Item>(jsonResponse);

        foreach (Item item in items)
        {
            GameObject instance = Instantiate(Resources.Load("Prefab/" + item.Name) as GameObject);
            itemList.Add(instance);
            instance.SetActive(false);
        }
    }

    private void GetCombinationDataFromAPI()
    {
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(String.Format("https://api.backendless.com/09476775-387A-4C56-FFE4-B663DC24FC00/DED29ABA-8FAC-4985-86E0-FCCDA5A290B5/data/CombinationList?pageSize=50&offset=0&where=levelid%3D" + currentLvl));
        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        StreamReader reader = new StreamReader(response.GetResponseStream());
        string jsonResponse = reader.ReadToEnd();
        jsonResponse = JsonHelper.FixJSon(jsonResponse);

        combinationList = JsonHelper.FromJson<Combination>(jsonResponse);
    }

    #endregion

    #region Find

    /// <summary>
    /// FindCombinationResult
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns>GameObject</returns>
    /// 

    /// <summary>
    /// FindHandBookContent
    /// </summary>
    /// <param name="page"></param>
    /// <returns>string</returns>

    private bool FindCombinationResult(GameObject a, GameObject b)
    {
        List<string> resultName =
           (
            from data in combinationList
            where (data.FirstItem == a.name.Replace("(Clone)", "") && data.SecondItem == b.name.Replace("(Clone)", "") || (data.FirstItem == b.name.Replace("(Clone)", "") && data.SecondItem == a.name.Replace("(Clone)", "")))
            select data.Result
           ).ToList();

        if (resultName != null && resultName.Count > 0)
        {
            bool exist = false;
            try
            {
                String.IsNullOrEmpty(itemList.Where(x => x.name.Replace("(Clone)", "") == resultName.FirstOrDefault()).FirstOrDefault().name);
                exist = true;
            }
            catch (Exception)
            {
                exist = false;
            }

            if (!exist)
            {
                string result = resultName.FirstOrDefault();
                GameObject instance = Instantiate(Resources.Load("Prefab/" + result) as GameObject);

                string animationName =
                    (
                        from anim in combinationList
                        where anim.Result == result
                        select anim.AnimationName
                    ).FirstOrDefault();

                instance.GetComponent<Animator>().Play(animationName);

                itemList.Add(instance);

                return true;
            }
        }
        return false;
    }

    #endregion

    #region Check Data by Debug.log

    /// <summary>
    /// CheckCombinationData()
    /// </summary>

    private void CheckCombinationData()
    {
        foreach (Combination data in combinationList)
        {
            //Debug.Log(data.LevelID);
            Debug.Log(data.FirstItem);
            Debug.Log(data.SecondItem);
            Debug.Log(data.Result);
        }
    }

    #endregion
}
