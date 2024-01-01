#if MICROSOFT_GAME_CORE
using XGamingRuntime;
#endif

using System;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
 
using Cysharp.Threading.Tasks;
using Studio23.SS2.AuthSystem.XboxCorePC.Core;
using Studio23.SS2.AuthSystem.XboxCorePC.Utility;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Studio23.SS2.Authsystem.XboxCorePC.Core
{
    public class ErrorEventArgs : System.EventArgs
    {
        public string ErrorCode { get; private set; }
        public string ErrorMessage { get; private set; }

        public ErrorEventArgs(string errorCode, string errorMessage)
        {
            this.ErrorCode = errorCode;
            this.ErrorMessage = errorMessage;
        }
    }
    public class GameSaveLoadedArgs : System.EventArgs
    {
        public byte[] Data { get; private set; }

        public GameSaveLoadedArgs(byte[] data)
        {
            this.Data = data;
        }
    }

#if MICROSOFT_GAME_CORE || UNITY_GAMECORE
    public delegate void ShowPurchaseUICallback(Int32 hresult, XStoreProduct storeProduct);
    public delegate void GetAssociatedProductsCallback(Int32 hresult, List<XStoreProduct> associatedProducts);
#endif

    public class MSGdk : MonoBehaviour
    {
        [Header("Changing the SCID here will also change the value in your MicrosoftGame.config")]
        [Tooltip("Service Configuration GUID in the form: 12345678-1234-1234-1234-123456789abc")]
        [Delayed]
        public string scid;

        
        
        private static MSGdk _xboxHelpers;
        private static bool _initialized;
        private static Dictionary<int, string> _hresultToFriendlyErrorLookup;

        private string _lastScid = string.Empty;

#if MICROSOFT_GAME_CORE || UNITY_GAMECORE
        private XStoreContext _storeContext = null;
        public XUserHandle UserHandle;
        public XblContextHandle XblContextHandle;
        private XGameSaveWrapper _gameSaveHelper;

        private List<XStoreProduct> _associatedProducts;
        GetAssociatedProductsCallback _queryAssociatedProductsCallback;
        private bool _pendingGetAssociatedProductsRequest;

        private const XStoreProductKind _addOnProducts = XStoreProductKind.Durable;
#endif

        private const int _100PercentAchievementProgress = 100;
        private   string _GameSaveContainerName =  "cloud";  //"x_game_save_default_container"; 
        private   string _GameSaveBlobName =  "cloud_blobBuffer";   //"x_game_save_default_blob";  
        
      
        
        private const int _MaxAssociatedProductsToRetrieve = 25;

      
      
        
       
         public static MSGdk Helpers
        {
            get
            {
                if (_xboxHelpers == null)
                {
                    MSGdk[] xboxHelperInstances = FindObjectsOfType<MSGdk>();
                    if (xboxHelperInstances.Length > 0)
                    {
                        _xboxHelpers = xboxHelperInstances[0];
                        _xboxHelpers.InitAndSignIn();
                    }
                    else
                    {
                        Debug.LogError("Error: Could not find Xbox prefab. Make sure you have added the Xbox prefab to your scene.");
                    }
                }

                return _xboxHelpers;
            }
        }

        public delegate void OnGameSaveLoadedHandler(object sender, GameSaveLoadedArgs e);
#pragma warning disable 0067 // Called when MICROSOFT_GAME_CORE is defined
        public event OnGameSaveLoadedHandler OnGameSaveLoaded;
#pragma warning restore 0067

        public delegate void OnErrorHandler(object sender, ErrorEventArgs e);
        public event OnErrorHandler OnError;

        private bool ValidateGuid(string guid)
        {
             
            var groups = guid.Split('-');
            if (groups.Length != 5) return false;

            if (!groups.Select(str => str.Length).SequenceEqual(new[] { 8, 4, 4, 4, 12 })) return false;

            if (!guid.All(c => "1234567890abcdef-".Contains(c))) return false;

            return true;
        }

        private void OnValidate()
        {
            if (scid == _lastScid) return;

            // Ensure guid formatted with only dashes
            if (scid.Length != 36 ||
                !ValidateGuid(scid))
            {
                Debug.LogError("Invalid SCID given");
                scid = _lastScid;
                return;
            }

            _lastScid = scid;

            var gameConfigDoc = XDocument.Load(MSGdkUtilities.GameConfigPath);
            try
            {
                var scidNode = (from node in gameConfigDoc.Descendants("ExtendedAttribute")
                                where node.Attribute("Name").Value == "Scid"
                                select node).First();

                scidNode.Attribute("Value").Value = scid;

                var xmlWriterSettings = new XmlWriterSettings()
                {
                    Indent = true,
                    NewLineOnAttributes = true
                };

                using (XmlWriter xmlWriter = XmlWriter.Create(MSGdkUtilities.GameConfigPath, xmlWriterSettings))
                {
                    gameConfigDoc.WriteTo(xmlWriter);
                }
            }
            catch
            {
                Debug.LogError("Malformed MicrosoftGame.Config. Try associating with the Micosoft Store again or re-import the plugin.");
            }
        }

        
        public void InitAndSignIn()
        {
            if (_initialized)
            {
                return;
            }
            
            _initialized = true;
            DontDestroyOnLoad(gameObject);
            OnPostSignInTaskFinished = new UniTaskCompletionSource<bool>();
            _hresultToFriendlyErrorLookup = new Dictionary<int, string>();
            InitializeHresultToFriendlyErrorLookup();

#if MICROSOFT_GAME_CORE || UNITY_GAMECORE
            if (!Succeeded(SDK.XGameRuntimeInitialize(), "Initialize gaming runtime"))
            {
#if UNITY_EDITOR
                Debug.LogError("You may need to update your config file for the editor. GDK -> PC -> Update Editor Game Config will copy your current game config to the Unity.exe location to enable GDK features when playing in-editor.");
#endif
                return;
            }

            // Check for store updates
            int hresult = SDK.XStoreCreateContext(out _storeContext);
            if (Succeeded(hresult, "Create store context"))
            {
                SDK.XStoreQueryGameAndDlcPackageUpdatesAsync(_storeContext, HandleQueryForUpdatesComplete);
            }

            _gameSaveHelper = new XGameSaveWrapper();
           
            SignIn();
#endif
        }

        private void InitializeHresultToFriendlyErrorLookup()
        {
            if (_hresultToFriendlyErrorLookup == null)
            {
                return;
            }

            _hresultToFriendlyErrorLookup.Add(-2143330041, "IAP_UNEXPECTED: Does the player you are signed in as have a license for the game? " +
                "You can get one by downloading your game from the store and purchasing it first. If you can't find your game in the store, " +
                "have you published it in Partner Center?");

            _hresultToFriendlyErrorLookup.Add(-1994108656, "E_GAMEUSER_NO_PACKAGE_IDENTITY: Are you trying to call GDK APIs from the Unity editor?" +
                " To call GDK APIs, you must use the GDK > Build and Run menu. You can debug your code by attaching the Unity debugger once your" +
                "game is launched.");

            _hresultToFriendlyErrorLookup.Add(-1994129152, "E_GAMERUNTIME_NOT_INITIALIZED: Are you trying to call GDK APIs from the Unity editor?" +
                " To call GDK APIs, you must use the GDK > Build and Run menu. You can debug your code by attaching the Unity debugger once your" +
                "game is launched.");

            _hresultToFriendlyErrorLookup.Add(-2015559675, "AM_E_XAST_UNEXPECTED: Have you added the Windows 10 PC platform on the Xbox Settings page " +
                "in Partner Center? Learn more: aka.ms/sandboxtroubleshootingguide");
        }

        public void SignIn()
        {
#if MICROSOFT_GAME_CORE || UNITY_GAMECORE
            SignInImpl();
#endif
             
        }

        public void Save(string key, byte[] data)
        {
         _GameSaveContainerName =  key;
         _GameSaveBlobName =  $"{key}_blobBuffer";
         
         Save( data);
        }

        public void Delete(string key)
        {
            _GameSaveContainerName =  key;
            _GameSaveBlobName =  $"{key}_blobBuffer";
            Delete();
            Debug.Log($"Delete successful " +
                      $"_GameSaveContainerName: {_GameSaveContainerName}" +
                      $"_GameSaveBlobName: {_GameSaveBlobName}" 
                      );
        }

        private void Delete()
        {
            _gameSaveHelper.Delete(
                _GameSaveContainerName,
                _GameSaveBlobName,
                GameSaveSaveCompleted);
        }
        private void Save(byte[] data)
        {
#if MICROSOFT_GAME_CORE || UNITY_GAMECORE
            _gameSaveHelper.Save(
                _GameSaveContainerName,
                _GameSaveBlobName,
                data,
                GameSaveSaveCompleted);
#endif
        }

        public void LoadSaveData(string key)
        {
            _GameSaveContainerName =  key;
            _GameSaveBlobName =  $"{key}_blobBuffer";
            LoadSaveData();
        }
        private void LoadSaveData()
        {
#if MICROSOFT_GAME_CORE || UNITY_GAMECORE
            _gameSaveHelper.Load(
                _GameSaveContainerName,
                _GameSaveBlobName,
                GameSaveLoadCompleted);
#endif
        }

        public void UnlockAchievement(string achievementId)
        {
#if MICROSOFT_GAME_CORE || UNITY_GAMECORE
            UnlockAchievementImpl(achievementId);
#endif
        }

#if MICROSOFT_GAME_CORE || UNITY_GAMECORE
        private void SignInImpl()
        {
            XUserAddOptions options = XUserAddOptions.AddDefaultUserAllowingUI;
            SDK.XUserAddAsync(options, AddUserComplete);
        }

        private void AddUserComplete(int hresult, XUserHandle userHandle)
        {
            if (!Succeeded(hresult, "Sign in."))
            {
                return;
            }

            Debug.Log($"Sing in success!");
            UserHandle = userHandle;
           
            CompletePostSignInInitialization();


          
        }

       
        private void CompletePostSignInInitialization()
        {
            
            Succeeded(SDK.XBL.XblInitialize(
                scid
                ), "Initialize Xbox Live");
            Succeeded(SDK.XBL.XblContextCreateHandle(
                    UserHandle,
                    out XblContextHandle
                ), "Create Xbox Live context");
            
            
           
            InitializeGameSaves();
        }
       
        public void InitializeGameSaves()
        {
            _gameSaveHelper.InitializeAsync(UserHandle, scid, XGameSaveInitializeCompleted);
        } 

        private void XGameSaveInitializeCompleted(int hresult)
        {
            if (!Succeeded(hresult, "Initialize game save provider"))
            {
                return;
            }

            FetchUserDataAsync();

        }

        private void GameSaveSaveCompleted(int hresult)
        {
            Succeeded(hresult, "Game save submit update complete");
        }

        private void GameSaveLoadCompleted(int hresult, byte[] savedData)
        {
             /*if (!Succeeded(hresult, "Loaded Blob"))
             {
                 return;
             }*/

             if (Helpers.OnGameSaveLoaded != null)
             {
                 Helpers.OnGameSaveLoaded(Helpers, new GameSaveLoadedArgs(savedData));
             }
        }

       
       public UniTaskCompletionSource<bool> OnPostSignInTaskFinished;
       public UserData CurrentUserData;
       
        [ContextMenu("Fetch User Data Async")]
        private async UniTask  FetchUserDataAsync()
        {
            CurrentUserData = new UserData();
            await GetUserData();
            await OnPostSignInTaskFinished.Task;
        }
        
        
        
        private async UniTask GetUserData()
        {
         
            if (!Succeeded(SDK.XUserGetId(UserHandle, out var xuid), "Get Xbox user ID"))
            {
                Debug.LogError("Failed to load XUserGetId from UserHandle");
                OnPostSignInTaskFinished.TrySetException(new Exception("Failed to load XUserGetId from UserHandle"));
                return; // Early return in case of error
            }

            if (!Succeeded(SDK.XUserGetGamertag(UserHandle, XUserGamertagComponent.UniqueModern, out var gamertag), "Get GamerTag."))
            {
                Debug.LogError("Failed to load XUserGetGamerTag from UserHandle");
                OnPostSignInTaskFinished.TrySetException(new Exception("Failed to load XUserGetGamerTag from UserHandle"));
                return; // Early return in case of error
            }

            CurrentUserData.UserID = xuid.ToString();
            CurrentUserData.UserName = gamertag;
            CurrentUserData.UserNickname = gamertag;

            // Continue with the rest of the method if no errors occurred
            SDK.XUserGetGamerPictureAsync(UserHandle, XUserGamerPictureSize.Small, CompletionRoutine);
        }
             
           
        private void CompletionRoutine(int hresult, byte[] buffer)
        {
            if (hresult == 0 && buffer != null)  
            {
                Texture2D texture = new Texture2D(2, 2);
                bool isLoaded = texture.LoadImage(buffer);
                if (isLoaded)
                {
                    CurrentUserData.UserAvatar = texture;
                }
                else
                {
                    Debug.LogError("Failed to load texture from buffer");
                }
            }
            else
            {
                Debug.LogError($"Error in CompletionRoutine: HRESULT = {hresult}");
            }
            Debug.Log($"Authentication, Login and set UserData Successful! {CurrentUserData.UserName}");
            OnPostSignInTaskFinished.TrySetResult(true);
        }

       
        
         #region MarufTest
         
        public void XblAchievementsGetAchievementsForTitleIdAsync()
        {
            ulong xuid;
            if (!Succeeded(SDK.XUserGetId(UserHandle, out xuid), "Get Xbox user ID"))
            {
                return;
            }
            uint titleId;
            SDK.XGameGetXboxTitleId(out titleId);
            SDK.XBL.XblAchievementsGetAchievementsForTitleIdAsync(XblContextHandle, xuid, titleId , XblAchievementType.All, false, XblAchievementOrderBy.DefaultOrder, 0, 20, XblAchievementsGetAchievementsForTitleIdAsync_CompletionRoutine);
            
        }

        private void XblAchievementsGetAchievementsForTitleIdAsync_CompletionRoutine(int hresult, XblAchievementsResultHandle result)
        {
            XblAchievement[] achievements;
            int resultCode = SDK.XBL. XblAchievementsResultGetAchievements(result, out achievements);
            if (resultCode == 0)
            {
                foreach (var achievement in achievements)
                {
                   // Debug.Log($"Achievement Id: {achievement.Id}; Name: {achievement.Name}; ProgressState: {achievement.ProgressState} ;");
                    if(achievement.Progression.Requirements.Length > 0)
                        foreach (XblAchievementRequirement requirement in achievement.Progression.Requirements)
                        {
                            Debug.Log($" Requirements {requirement.CurrentProgressValue} {requirement.TargetProgressValue}");
                        }
                    else Debug
                        .Log("Requirements length is 0");
                }
            }
            else
            {
                Debug.LogError("Error retrieving achievements");
            }
            
            SDK.XBL.XblAchievementsResultCloseHandle(result);
            
        }

        
        public void UnlockAchievementProgression(string achievementId, uint progression)
        {
            ulong xuid;
            if (!Succeeded(SDK.XUserGetId(UserHandle, out xuid), "Get Xbox user ID"))
            {
                return;
            }
            SDK.XBL.XblAchievementsUpdateAchievementAsync(
                XblContextHandle,
                xuid,
                achievementId,
                progression,
                UnlockAchievementComplete
            );
        }

        #endregion
        
        private void UnlockAchievementImpl(string achievementId)
        {
           
            ulong xuid;
            if (!Succeeded(SDK.XUserGetId(UserHandle, out xuid), "Get Xbox user ID"))
            {
                return;
            }
            SDK.XBL.XblAchievementsUpdateAchievementAsync(
                    XblContextHandle,
                    xuid,
                    achievementId,
                    _100PercentAchievementProgress,
                    UnlockAchievementComplete
                );
            
        }

        private void UnlockAchievementComplete(int hresult)
        {
            Succeeded(hresult, "Unlock achievement");
        }

        private void ProcessAssociatedProductsResults(Int32 hresult, XStoreQueryResult result)
        {
            if (Succeeded(hresult, "GetAssociatedProductsAsync callback"))
            {
                _associatedProducts.AddRange(result.PageItems);
                if (result.HasMorePages)
                {
                    SDK.XStoreQueryAssociatedProductsAsync(
                        _storeContext,
                        _addOnProducts,
                        _MaxAssociatedProductsToRetrieve,
                        ProcessAssociatedProductsResults
                        );
                }
                else
                {
                    if (_queryAssociatedProductsCallback != null)
                    {
                        _queryAssociatedProductsCallback(hresult, _associatedProducts);
                    }
                }
            }
            else
            {
                if (_queryAssociatedProductsCallback != null)
                {
                    _queryAssociatedProductsCallback(hresult, _associatedProducts);
                }
            }
        }

        public void GetAssociatedProductsAsync(GetAssociatedProductsCallback callback)
        {
            if (callback == null)
            {
                Debug.LogError("Callback cannot be null.");
            }

            _associatedProducts = new List<XStoreProduct>();
            _queryAssociatedProductsCallback = callback;
            Succeeded(SDK.XStoreCreateContext(out _storeContext), "Failed to create store context.");
            SDK.XStoreQueryAssociatedProductsAsync(
                _storeContext,
                _addOnProducts,
                _MaxAssociatedProductsToRetrieve,
                ProcessAssociatedProductsResults
                );
        }

        public void ShowPurchaseUIAsync(XStoreProduct storeProduct, ShowPurchaseUICallback callback)
        {
            SDK.XStoreShowPurchaseUIAsync(
                    _storeContext,
                    storeProduct.StoreId,
                    null,
                    null,
                    (Int32 hresult) =>
                    {
                        callback(hresult, storeProduct);
                    });
        }

        private void HandleQueryForUpdatesComplete(int hresult, XStorePackageUpdate[] packageUpdates)
        {
            List<string> _packageIdsToUpdate = new List<string>();
            if (hresult >= 0)
            {
                if (packageUpdates != null &&
                    packageUpdates.Length > 0)
                {
                    foreach (XStorePackageUpdate packageUpdate in packageUpdates)
                    {
                        _packageIdsToUpdate.Add(packageUpdate.PackageIdentifier);
                    }
                    // What do we do?
                    SDK.XStoreDownloadAndInstallPackageUpdatesAsync(
                        _storeContext,
                        _packageIdsToUpdate.ToArray(),
                        DownloadFinishedCallback);
                }
            }
            else
            {
                // No-op
            }
        }

        private void DownloadFinishedCallback(int hresult)
        {
            Succeeded(hresult, "DownloadAndInstallPackageUpdates callback");
        }
#endif

        // Update is called once per frame
        void Update()
        {
#if MICROSOFT_GAME_CORE || UNITY_GAMECORE
            SDK.XTaskQueueDispatch();
#endif
        }

        // Helper methods
        protected static bool Succeeded(int hresult, string operationFriendlyName)
        {
            bool succeeded = false;
            if (HR.SUCCEEDED(hresult))
            {
                succeeded = true;
            }
            else
            {
                string errorCode = hresult.ToString("X8");
                string errorMessage = string.Empty;
                if (_hresultToFriendlyErrorLookup.ContainsKey(hresult))
                {
                    errorMessage = _hresultToFriendlyErrorLookup[hresult];
                }
                else
                {
                    errorMessage = operationFriendlyName + " failed.";
                }
                string formattedErrorString = string.Format("{0} Error code: hr=0x{1}", errorMessage, errorCode);
                Debug.LogError(formattedErrorString);
                if (Helpers.OnError != null)
                {
                    Helpers.OnError(Helpers, new ErrorEventArgs(errorCode, errorMessage));
                }
            }

            return succeeded;
        }
    }
}