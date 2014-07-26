using System;
using Microsoft.SPOT;

namespace FezHydra_Testbed.OperationManager
{
    /// <summary>
    /// Interface representing a single operation.
    /// </summary>
    public interface IsOperation
    {
        void InitializeOperation();
        void StartOperation();
        void StopOperation();
        void UpdateOperation();
        void UninitializeOperation();

        string GetOperationHandle();
    }
}
