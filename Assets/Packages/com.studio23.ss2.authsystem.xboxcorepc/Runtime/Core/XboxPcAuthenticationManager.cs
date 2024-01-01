using Studio23.SS2.AuthSystem.Data;
using Studio23.SS2.Authsystem.XboxCorePC.Data;
using UnityEngine;
using XGamingRuntime;


namespace Studio23.SS2.AuthSystem.XboxCorePC.Core
{
    [CreateAssetMenu(fileName = "AuthProvider", menuName = "Studio-23/Authentication System/Provider/XboxCorePc",
        order = 1)]
    public class XboxPcAuthenticationManager : ProviderBase
    {
        public override void Authenticate()
        {
            GamingRuntimeManager.Instance.UserManager.UsersChanged += UserManager_UsersChanged;

            Login();
        }


        public override UserData GetUserData()
        {
            UserData userData = new UserData();
            UserManager.UserData currentUserData = GamingRuntimeManager.Instance.UserManager.m_CurrentUserData;

            userData.UserNickname = currentUserData.userGamertag;
            userData.UserID = currentUserData.userXUID.ToString();

            Texture2D avatarTexture = new Texture2D(2, 2);
            avatarTexture.LoadImage(currentUserData.imageBuffer);
            userData.UserAvatar = avatarTexture;


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
                        OnAuthSuccess?.Invoke();
                        Debug.Log("User Succesfully Added");
                        break;
                    }
                case UserManager.UserOpResult.NoDefaultUser:
                    {
                        GamingRuntimeManager.Instance.UserManager.AddUserWithUI(AddUserCompleted);
                        break;
                    }
                case UserManager.UserOpResult.UnknownError:
                    {
                        OnAuthFailed?.Invoke();
                        Debug.Log("Error adding user. Unknown error");
                        break;
                    }
                default:
                    break;
            }
        }


    }
}