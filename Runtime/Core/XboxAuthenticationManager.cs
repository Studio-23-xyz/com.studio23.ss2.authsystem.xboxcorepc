using Studio23.SS2.Authsystem.XboxCorePC.Data;
using Studio23.SS2.AuthSystem.Data;
using UnityEngine;

using XGamingRuntime;


namespace Studio23.SS2.AuthSystem.XboxCorePC.Core
{

    [CreateAssetMenu(fileName = "AuthProvider", menuName = "Studio-23/Authentication System/Provider/XboxCore", order = 1)]
    public class XboxAuthenticationManager : ProviderBase
    {

        private bool m_UsersChanged;

        public override void Authenticate()
        {
            GamingRuntimeManager.Instance.UserManager.UsersChanged += UserManager_UsersChanged;
#if UNITY_GAMECORE
        UnityEngine.WindowsGames.WindowsGamesPLM.OnSuspendingEvent += GameCorePLM_OnSuspendingEvent;
#endif
            Login();
        }


        public override UserData GetUserData()
        {
            UserData userData = new UserData();
            UserManager.UserData currentUserData = GamingRuntimeManager.Instance.UserManager.UserDataList[0];

            userData.UserNickname = currentUserData.userGamertag;
            userData.UserID = currentUserData.userXUID.ToString();
            //TODO Add feature later
            //userData.UserAvatar.LoadImage(currentUserData.imageBuffer); 

            return userData;
        }


        //User Login
        public void Login()
        {
            // We attempt to add the first user as the default one, the others need to be explicitly selected
            if (GamingRuntimeManager.Instance.UserManager.UserDataList.Count == 0)
                GamingRuntimeManager.Instance.UserManager.AddDefaultUserSilently(AddUserCompleted);
            else
                GamingRuntimeManager.Instance.UserManager.AddUserWithUI(AddUserCompleted);

        }




        private void GameCorePLM_OnSuspendingEvent()
        {
#if UNITY_GAMECORE
        UnityEngine.WindowsGames.WindowsGamesPLM.AmReadyToSuspendNow();
#endif //UNITY_GAMECORE
        }

        private void UserManager_UsersChanged(object sender, XUserChangeEvent e)
        {
            Debug.Log("User Logged IN Changed");
        }


        private void AddUserCompleted(UserManager.UserOpResult result)
        {
            switch (result)
            {
                case UserManager.UserOpResult.Success:
                    {
                        m_UsersChanged = true;
                        break;
                    }
                case UserManager.UserOpResult.NoDefaultUser:
                    {
                        GamingRuntimeManager.Instance.UserManager.AddUserWithUI(AddUserCompleted);
                        break;
                    }
                case UserManager.UserOpResult.UnknownError:
                    {
                        Debug.Log("Error adding user.");
                        break;
                    }
                default:
                    break;
            }
        }




    }
}

