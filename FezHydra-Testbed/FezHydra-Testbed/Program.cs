using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;

using FezHydra_Testbed.OperationManager;

namespace FezHydra_Testbed
{
    public partial class Program
    {
        private OperationsManager m_operationsManager;

        void ProgramStarted()
        {
            //Create operation manager.
            m_operationsManager = new OperationsManager();
            //Add bluetooth operation to the operation manager.
            Bluetooth.BluetoothOperation bto =
                new Bluetooth.BluetoothOperation("Hydra738", Bluetooth.BluetoothMode.CLIENT);
            m_operationsManager.AddOperation(bto, true);
            m_operationsManager.StartOperation(bto);

            //Add events for parsed bluetooth data
            bto.BtReceivedParsedData += bto_BtReceivedParsedData;
            bto.BtReceivedParsedDataArray += bto_BtReceivedParsedDataArray;
            bto.BtReceivedParsedString += bto_BtReceivedParsedString;
        }

        void bto_BtReceivedParsedDataArray( GTM.Velloso.Bluetooth sender, 
            uint dataId, Bluetooth.PacketValueType type, object data)
        {
            object[] dataArray = (object[])data;
            string datastring = "";
            for (int i = 0; i < dataArray.Length; i++)
            {
                datastring += dataArray[i].ToString();
            }
            Debug.Print("PARSED DATA Array:\n\tID: " + dataId + "\n\tType:" + type.ToString() + "\n>" + datastring);
            
        }

        void bto_BtReceivedParsedData(  GTM.Velloso.Bluetooth sender, 
            uint dataId, Bluetooth.PacketValueType type, object data)
        {
            Debug.Print("PARSED DATA:\n\tID: " + dataId + "\n\tType:" + type.ToString() + "\n>"+data.ToString());
        }

        void bto_BtReceivedParsedString( GTM.Velloso.Bluetooth sender, 
            uint dataId, string data)
        {

            Debug.Print("PARSED INVALID DATA:\n\tID: " + dataId + "\n>" + data);
        }
        

    }
}
