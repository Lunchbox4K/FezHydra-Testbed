using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;

namespace FezHydra_Testbed.OperationManager
{
    /// <summary>
    /// Manages timers and threads.
    /// </summary>
    class OperationsManager : IsThread
    {
        //--------------------------------------------------
        #region [# Static Defines #]

        public readonly byte MAX_OPERATIONS = 128;
        public readonly string OPERATION_MANAGER_HANDLE = "OperationManager";

        #endregion
        //--------------------------------------------------
        #region [# Private Members #]

        /// <summary>
        /// Hash table of operations
        /// </summary>
        private Hashtable m_operations;

        private Thread m_operationManagerThread;
        private bool m_threadRunning;
        private bool m_threadStopRequested;

        #endregion
        //--------------------------------------------------
        #region [# Constructor and Initialization #]

        public OperationsManager(bool autoInitialize=false) 
        {
            //Initialize the hash table
            m_operations = new Hashtable();

        }

        #endregion
        //--------------------------------------------------
        #region [# Operation Management #]

        public void AddOperation(IsOperation operation, bool initialize=false) {
            m_operations.Add(operation.GetOperationHandle(), operation);
            if (initialize)
            {
                operation.InitializeOperation();
            }
        }
        public void RemoveOperation(IsOperation operation) { 
            m_operations.Remove( operation.GetHashCode() ); 
        }
        public void StartOperation(IsOperation operation) {
            try
            {
                Debug.Print("Starting Operation!");
                ((IsOperation)m_operations[operation.GetOperationHandle()]).StartOperation();

            }
            catch (Exception e)
            {

            }
        }
        public void StopOperation(IsOperation operation) {
            try
            {
                ((IsOperation)m_operations[operation.GetHashCode()]).StopOperation();
                
            }catch (Exception e)
            {

            }
        }

        #endregion
        //--------------------------------------------------
        #region [# Thread Operation Methods #]


        /// <summary>
        /// Operation thread process function.
        /// </summary>
        private void OperationManagerThread()
        {
            while (m_threadRunning)
            {

                foreach (IsOperation operation in m_operations)
                {
                    //Update the operation.
                    operation.UpdateOperation();
                }

                if (m_threadStopRequested)
                {
                    //Uninitialize all the operations and cleanup.
                    UninitializeOperation();
                    m_threadRunning = false;
                }
            }
        }

        /// <summary>
        /// Initializes the operation managers thread.
        /// </summary>
        public void InitializeOperation()
        {
            if (m_operationManagerThread == null)
            {
                //Creates the thread and starts it.
                ThreadStart ts = new ThreadStart(OperationManagerThread);
                m_operationManagerThread = new Thread(ts);
            }
            else
            {
                throw new Exception(
                    OPERATION_MANAGER_HANDLE + " thread already exists!");
            }
        }

        /// <summary>
        /// Starts the operation managers thread.
        /// </summary>
        void IsOperation.StartOperation()
        {
            if (m_operationManagerThread == null)
                throw new Exception(
                   OPERATION_MANAGER_HANDLE + " does not exists, can't start!");

            if (!m_threadRunning)
            {
                m_operationManagerThread.Start();
            }
            else
            {
                throw new Exception(
                    OPERATION_MANAGER_HANDLE + " thread already running!");
            }
        }

        /// <summary>
        /// Requests a stop of the operation managers thread.
        /// </summary>
        void IsOperation.StopOperation()
        {
            if (m_operationManagerThread == null)
                throw new Exception(
                   OPERATION_MANAGER_HANDLE + " does not exists, can't stop!");

            //!TODO
             // Uninitialize and shutdown thread

            m_threadStopRequested = true;
        }

        /// <summary>
        /// External call from another thread to update the operation.
        /// </summary>
        public void UpdateOperation()
        {
            throw new Exception(OPERATION_MANAGER_HANDLE + 
                "Currently has no external update operations.");
        }

        /// <summary>
        /// Operation manager's thread uninitialization. (Called by the update thread)
        /// </summary>
        public void UninitializeOperation()
        {
            if (Thread.CurrentThread.ManagedThreadId == m_operationManagerThread.ManagedThreadId)
                throw new Exception(
                  OPERATION_MANAGER_HANDLE + " can't call the thread uninitization operation from an outside thread!");

           //!TODO
            //Unload and remove all the operations
        }

        public string GetOperationHandle()
        {
            return OPERATION_MANAGER_HANDLE;
        }

        #endregion
        //--------------------------------------------------

    } //End of Class

}
