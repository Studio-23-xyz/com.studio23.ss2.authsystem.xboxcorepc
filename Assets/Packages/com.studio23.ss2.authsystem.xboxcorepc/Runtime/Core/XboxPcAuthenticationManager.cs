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
            MSGdk msGdk = FindObjectOfType<MSGdk>();
             if (msGdk != null)
             {
                  msGdk.InitAndSignIn();
             }
             else
             {
                Debug.LogError($"MS GDK Not Found!");
             }
        }
        public override UserData GetUserData()
        {
            MSGdk msGdk = FindObjectOfType<MSGdk>();
            if (msGdk != null)
            {
              return  msGdk.CurrentUserData;
            }  else
            {
                Debug.LogError($"MS GDK Not Found!");
                return null;
            }
            
           
        }
    }
}