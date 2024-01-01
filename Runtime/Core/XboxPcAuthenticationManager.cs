using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Studio23.SS2.AuthSystem.Data;
using Studio23.SS2.Authsystem.XboxCorePC.Core;
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
            Login();
            
        }

        
        private async Task Login()
        {
            MSGdk.Helpers.InitAndSignIn();
            await MSGdk.Helpers.OnPostSignInTaskFinished.Task;
            await UniTask.Delay(TimeSpan.FromSeconds(1));
            OnAuthSuccess.Invoke();
        }
        
        public override UserData GetUserData()
        {
           
              return   MSGdk.Helpers.CurrentUserData;
              
        }
    }
}