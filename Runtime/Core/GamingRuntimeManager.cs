using System;
using UnityEngine;
using System.Threading;
using Studio23.SS2.Authsystem.XboxCorePC.Data;
using XGamingRuntime;

namespace Studio23.SS2.AuthSystem.XboxCorePC.Core
{
    public class GamingRuntimeManager : MonoBehaviour
    {
        public string SCID;

        public UserManager UserManager
        {
            get { return m_UserManager; }
        }

        public static GamingRuntimeManager Instance { get { return m_Instance; } }

        private void Awake()
        {
            if (m_Instance != null && m_Instance != this)
            {
                Destroy(this);
                return;
            }
            else
            {
                m_Instance = this;
                DontDestroyOnLoad(this);

                // initialise the runtime
                Int32 hr = SDK.XGameRuntimeInitialize();
                if (hr == 0)
                {
                    // start the async task dispatch thread
                    m_StopExecution = false;
                    m_DispatchJob = new Thread(DispatchGXDKTaskQueue) { Name = "GXDK Task Queue Dispatch" };
                    m_DispatchJob.Start();

                    // also initialise Xbox Live services here

                hr = SDK.XBL.XblInitialize(SCID);
                if (hr == 0)
                {
                    // everything is OK so create our UserManager object
                    InitUserManager();
                }

                }

                if (hr != 0)
                {
                    // something went wrong
                    Debug.Log("Error initialising the gaming runtime, hresult: " + hr);
                }
            }
        }




        public void Update()
        {
            if (UserManager != null)
                UserManager.Update();
        }

        UserManager m_UserManager;
        Thread m_DispatchJob;
        bool m_StopExecution;
        static GamingRuntimeManager m_Instance;

        void DispatchGXDKTaskQueue()
        {
            // this will execute all GXDK async work
            while (m_StopExecution == false)
            {
                SDK.XTaskQueueDispatch(0);
                Thread.Sleep(32);
            }
        }

        void InitUserManager()
        {
            m_UserManager = new UserManager();
        }
    }
}


