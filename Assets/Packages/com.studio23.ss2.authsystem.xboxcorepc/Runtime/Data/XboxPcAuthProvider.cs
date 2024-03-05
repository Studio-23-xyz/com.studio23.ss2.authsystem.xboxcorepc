using Cysharp.Threading.Tasks;
using UnityEngine;
using Studio23.SS2.AuthSystem.Data;
using Studio23.SS2.Authsystem.XboxCorePC.Data;
using Studio23.SS2.AuthSystem.XboxCorePC.Core;


namespace Studio23.SS2.AuthSystem.XboxCorePC.Data
{
    
    public class XboxPcAuthProvider : AuthProviderBase
    {

        

        public override UniTask<int> Authenticate()
        {

            if(GamingRuntimeManager.Instance == null)
            {
                Debug.LogError("Gaming Runtime Manager not found. You can install from Studio-23>AuthSystem>DependencyInstaller>XBOXPC Runtime");
                return new UniTask<int>(-2);
            }

            UniTaskCompletionSource<int> _addUserTaskCompletionSource = new UniTaskCompletionSource<int>();
            if (GamingRuntimeManager.Instance.UserManager.UserDataList.Count == 0)
            {
                GamingRuntimeManager.Instance.UserManager.AddDefaultUserSilently((result) =>
                {
                    _addUserTaskCompletionSource.TrySetResult((int) result);
                });
            }
            else
            {
                GamingRuntimeManager.Instance.UserManager.AddUserWithUI((result) =>
                {
                    _addUserTaskCompletionSource.TrySetResult((int) result);
                });
            }

            return _addUserTaskCompletionSource.Task;
        }


        public override UniTask<UserData> GetUserData()
        {
            UserData userData = new UserData();
            UserManager.UserData currentUserData = GamingRuntimeManager.Instance.UserManager.m_CurrentUserData;

            userData.UserNickname = currentUserData.userGamertag;
            userData.UserID = currentUserData.userXUID.ToString();

            Texture2D avatarTexture = new Texture2D(2, 2);
            avatarTexture.LoadImage(currentUserData.imageBuffer);
            userData.UserAvatar = avatarTexture;


            return new UniTask<UserData>(userData);
        }
 

    }
}