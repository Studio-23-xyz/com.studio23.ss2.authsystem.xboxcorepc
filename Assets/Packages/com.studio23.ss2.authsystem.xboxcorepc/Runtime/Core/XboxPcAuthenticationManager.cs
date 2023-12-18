using System;
using System.Threading.Tasks;
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

        
        private void Login()
        {
            MSGdk.Helpers.InitAndSignIn();

            MSGdk.Helpers.UserDataLoaded.Task.ContinueWith(task => 
            {
                if (task.IsCompleted)
                {
                    OnAuthSuccess.Invoke();
                }
            });
           
           

        }
        
        public override UserData GetUserData()
        {
           
              return   MSGdk.Helpers.CurrentUserData;
              
        }
    }
}