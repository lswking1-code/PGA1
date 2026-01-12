using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour, ISaveable
{
    public Transform playerTrans;
    public Vector3 firstPosition;
    public Vector3 menuPosition;

    [Header("EventListeners")]
    public SceneLoadEventSO loadEventSO;
    public VoidEventSO newGameEvent;
    public VoidEventSO backToMenuEvent;

    [Header("EventRaise")]
    public VoidEventSO afterSceneLoadedEvent;
    public FadeEventSO fadeEvent;
    public SceneLoadEventSO unloadedSceneEvent;
 

    [Header("Scene")]
    public GameSceneSO firstLoadScene;
    public GameSceneSO menuScene;
    private GameSceneSO currentLoadedScene;
    private GameSceneSO sceneToLoad;
    private Vector3 positionToGo;
    private bool fadeScreen;
    private bool isLoading;
    public float fadeDuration;

    private void Awake()
    {
        // Addressables.LoadSceneAsync(firstLoadScene.sceneReference, LoadSceneMode.Additive);
        // currentLoadedScene = firstLoadScene;
        // currentLoadedScene.sceneReference.LoadSceneAsync(LoadSceneMode.Additive);
    }

    private void Start()
    {
        // Check configuration
        Debug.Log("SceneLoader: Start() called");
        if (newGameEvent == null)
        {
            Debug.LogError("SceneLoader: newGameEvent is null in Start! Make sure it's assigned in the Inspector.");
        }
        else
        {
            Debug.Log($"SceneLoader: newGameEvent is assigned: {newGameEvent.name}");
            // Ensure re-subscription in Start (prevent WebGL platform ScriptableObject instance issues)
            newGameEvent.RemoveListener(NewGame); // Unsubscribe first (if already subscribed)
            newGameEvent.AddListener(NewGame); // Re-subscribe
            Debug.Log($"SceneLoader: Re-subscribed to newGameEvent in Start. Current subscribers: {(newGameEvent.OnEventRaised != null ? newGameEvent.OnEventRaised.GetInvocationList().Length : 0)}");
        }
        
        if (firstLoadScene == null)
        {
            Debug.LogError("SceneLoader: firstLoadScene is null! Make sure it's assigned in the Inspector.");
        }
        else
        {
            Debug.Log($"SceneLoader: firstLoadScene is assigned: {firstLoadScene.name}");
            if (firstLoadScene.sceneReference == null)
            {
                Debug.LogError($"SceneLoader: firstLoadScene.sceneReference is null!");
            }
            else if (!firstLoadScene.sceneReference.RuntimeKeyIsValid())
            {
                Debug.LogError($"SceneLoader: firstLoadScene.sceneReference is invalid! Make sure the scene is marked as Addressable.");
            }
            else
            {
                Debug.Log($"SceneLoader: firstLoadScene.sceneReference is valid");
            }
        }
        
        loadEventSO.RaiseLoadRequestEvent(menuScene, menuPosition, true);
        // NewGame();
    }

    private void OnEnable()
    {
        loadEventSO.LoadRequestEvent += OnLoadRequestEvent;
        
        if (newGameEvent != null)
        {
            newGameEvent.AddListener(NewGame);
            Debug.Log("SceneLoader: Subscribed to newGameEvent");
        }
        else
        {
            Debug.LogError("SceneLoader: newGameEvent is not assigned!");
        }
        
        if (backToMenuEvent != null)
        {
            backToMenuEvent.AddListener(OnBackToMenuEvent);
        }

        ISaveable saveable = this;
        saveable.RegisterSaveData();
    }

    private void OnDisable()
    {
        loadEventSO.LoadRequestEvent -= OnLoadRequestEvent;
        if (newGameEvent != null)
        {
            newGameEvent.RemoveListener(NewGame);
        }
        if (backToMenuEvent != null)
        {
            backToMenuEvent.RemoveListener(OnBackToMenuEvent);
        }

        ISaveable saveable = this;
        saveable.UnregisterSaveData();
    }

    private void OnBackToMenuEvent()
    {
        sceneToLoad = menuScene;
        loadEventSO.RaiseLoadRequestEvent(sceneToLoad, menuPosition, true);
    }

    private void NewGame()
    {
        Debug.Log("SceneLoader: NewGame() method called!");
        
        if (firstLoadScene == null)
        {
            Debug.LogError("SceneLoader: firstLoadScene is not assigned!");
            isLoading = false;
            return;
        }

        Debug.Log($"SceneLoader: firstLoadScene found: {firstLoadScene.name}");

        if (firstLoadScene.sceneReference == null)
        {
            Debug.LogError($"SceneLoader: firstLoadScene.sceneReference is null! Scene: {firstLoadScene.name}. Make sure the scene is assigned in GameSceneSO.");
            isLoading = false;
            return;
        }

        if (!firstLoadScene.sceneReference.RuntimeKeyIsValid())
        {
            Debug.LogError($"SceneLoader: firstLoadScene.sceneReference is invalid! Scene: {firstLoadScene.name}. Make sure the scene is marked as Addressable.");
            isLoading = false;
            return;
        }

        sceneToLoad = firstLoadScene;
        Debug.Log($"SceneLoader: NewGame called, loading scene: {firstLoadScene.name}");
        loadEventSO.RaiseLoadRequestEvent(sceneToLoad, firstPosition, true);
    }

    private void OnLoadRequestEvent(GameSceneSO locationToLoad, Vector3 posToGo, bool fadeScreen)
    {
        if (isLoading)
        {
            Debug.LogWarning("SceneLoader: Already loading a scene, ignoring request.");
            return;
        }

        if (locationToLoad == null)
        {
            Debug.LogError("SceneLoader: locationToLoad is null!");
            return;
        }

        if (locationToLoad.sceneReference == null || !locationToLoad.sceneReference.RuntimeKeyIsValid())
        {
            Debug.LogError($"SceneLoader: sceneReference is invalid for scene: {locationToLoad.name}. Make sure the scene is marked as Addressable.");
            return;
        }

        isLoading = true;
        sceneToLoad = locationToLoad;
        positionToGo = posToGo;
        this.fadeScreen = fadeScreen;
        Debug.Log($"SceneLoader: Starting to load scene: {locationToLoad.name}");
        StartCoroutine(UnLoadPreviousScene());
    }

    private IEnumerator UnLoadPreviousScene()
    {
        if (fadeScreen)
        {
            fadeEvent.FadeIn(fadeDuration);
        }

        yield return new WaitForSeconds(fadeDuration);

        // Broadcast event to adjust health bar display
        unloadedSceneEvent.RaiseLoadRequestEvent(sceneToLoad, positionToGo, true);

        // Check if there is a loaded scene that needs to be unloaded
        if (currentLoadedScene != null && currentLoadedScene.sceneReference != null)
        {
            yield return currentLoadedScene.sceneReference.UnLoadScene();
            Debug.Log("UnLoaded Previous Scene");
        }
        
        playerTrans.gameObject.SetActive(false);
        Debug.Log("Player is disabled");

        LoadNewScene();
    }

    private void LoadNewScene()
    {
        if (sceneToLoad == null)
        {
            Debug.LogError("SceneLoader: sceneToLoad is null in LoadNewScene!");
            isLoading = false;
            return;
        }

        if (sceneToLoad.sceneReference == null || !sceneToLoad.sceneReference.RuntimeKeyIsValid())
        {
            Debug.LogError($"SceneLoader: sceneReference is invalid for scene: {sceneToLoad.name}. Make sure the scene is marked as Addressable.");
            isLoading = false;
            return;
        }

        Debug.Log($"SceneLoader: Loading scene: {sceneToLoad.name}");
        var loadingOption = sceneToLoad.sceneReference.LoadSceneAsync(LoadSceneMode.Additive, true);
        loadingOption.Completed += OnLoadCompleted;
    }

    /// <summary>
    /// Called when scene loading is completed
    /// </summary>
    /// <param name="obj"></param>
    private void OnLoadCompleted(AsyncOperationHandle<SceneInstance> obj)
    {
        // Check loading status
        if (obj.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"SceneLoader: Failed to load scene. Status: {obj.Status}, Error: {obj.OperationException}");
            isLoading = false;
            
            // If loading failed, restore fade effect
            if (fadeScreen && fadeEvent != null)
            {
                fadeEvent.FadeOut(fadeDuration);
            }
            return;
        }

        Debug.Log($"SceneLoader: Scene loaded successfully: {sceneToLoad.name}");
        currentLoadedScene = sceneToLoad;

        if (playerTrans != null)
        {
            playerTrans.position = positionToGo;

            if (currentLoadedScene.sceneType == SceneType.Loaction)
            {
                playerTrans.gameObject.SetActive(true);
            }
            else if (currentLoadedScene.sceneType == SceneType.Menu)
            {
                playerTrans.gameObject.SetActive(false);
            }
        }

        if (fadeScreen && fadeEvent != null)
        {
            fadeEvent.FadeOut(fadeDuration);
        }

        isLoading = false;

        if (currentLoadedScene.sceneType == SceneType.Loaction && afterSceneLoadedEvent != null)
        {
            // Event fired after scene loading is completed
            afterSceneLoadedEvent.RaiseEvent();
        }
    }

    public DataDefination GetDataID()
    {
        return GetComponent<DataDefination>();
    }

    public void GetSaveData(Data data)
    {
        data.SaveGameScene(currentLoadedScene);
    }

    public void LoadSaveData(Data data)
    {
        var playerID = playerTrans.GetComponent<DataDefination>();
        if (data.characterPosDict.ContainsKey(playerID.ID))
        {
            positionToGo = data.characterPosDict[playerID.ID].ToVector3();
            sceneToLoad = data.GetSavedScene();

            OnLoadRequestEvent(sceneToLoad, positionToGo, true);
        }
    }
}
