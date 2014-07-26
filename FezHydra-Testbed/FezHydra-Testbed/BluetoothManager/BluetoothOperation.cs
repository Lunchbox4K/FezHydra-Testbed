#define BT_DEBUG
#define BT_ECHO_DATA_STRUCTURE

using System;
using Microsoft.SPOT;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using BT = Gadgeteer.Modules.Velloso;
using FezHydra_Testbed.OperationManager;

namespace FezHydra_Testbed.Bluetooth
{
    /* BT Packet Structure
     * -------------------------
     * {Header} + {Data}
     * {Header} = {HEADER_START_BYTE} + {TYPE} + {SEPARATOR} + Hex(ID) + {SEPARATOR} + Hex(Size)
     * {Data} = object + {SEPERATOR} + object...
     * {HEADER_START_BYTE} = 'SOH' = 1 = 0x001
     * {SEPARATOR} = 'ETX' = 3 = 0x003
     */

    /// <summary>
    /// Enumeration used for selecting the type of BluetoothOperation being used.
    /// </summary>
    public enum BluetoothMode
    {
        CLIENT,
        HOST
    }

    /* 
    * {TYPE} = enumeration Type with Hex
    *      + {TYPE:bool}   = 0x01
    *      + {TYPE:byte}   = 0x02 = {UInt8}
    *      + {TYPE:char}   = 0x03 = {Int16}
    *      + {TYPE:short}  = 0x04 = {Int16}
    *      + {Type:ushort} = 0x05 = {UInt16}
    *      + {TYPE:int}    = 0x06 = {Int32}
    *      + {TYPE:uint}   = 0x07      
    *      + {TYPE:long}   = 0x08 = {UIInt32}
    *      //+ {TYPE:float}  = 0x09 //NOt Supported
    *      + {TYPE:double} = 0x0A
    *      + {TYPE:string} = 0x0B
    *      + {TYPE:struct} = 0x0C
    */
    /// <summary>
    /// Enumeration used to represent the data type being received.
    /// </summary>
    public enum PacketValueType
    {
        UNKNOWN = 0x00,
        BOOL    = 0x01,
        BYTE    = 0x02,
        CHAR    = 0x03,
        SHORT   = 0x04,
        USHORT  = 0x05,
        INT     = 0x06,
        UINT    = 0x07,
        LONG    = 0x08,
        DOUBLE  = 0x0A,
        STRING  = 0x0B,
        STRUCT  = 0x0C,
        // 0x0C ++ -> Other Defined Objects, Returned as string
    }

    public class BluetoothOperation : IsTimer
    {
        public readonly char    BT_HEADER_START = '|';//(char)0x001;
        public readonly char[]  BT_SEPERATORS    = {',','\0','|'};//(char)0x003;
        public readonly int     BT_PAIR_DELAY   = 1000;
        public readonly byte    BT_SOCKET       = 4;
        public readonly string  BT_PIN          = "1234";
        public readonly byte    BT_NO_ID        = 0;
        public readonly byte    BT_HEADER_ORDER_ID  =  0;
        public readonly byte    BT_HEADER_ORDER_TYPE = 1;
        public readonly byte    BT_HEADER_ORDER_SIZE = 2;
        public readonly byte    BT_HEADER_ORDER_DATA = 3;

        //GTM.GHIElectronics.Bluetooth m_bt;  replaces with 3rd party driver
        private BT.Bluetooth        m_bt;
        private BluetoothMode       m_btMode;
        private Object              m_btClientHost;
        private Gadgeteer.Timer     m_timer;
        private string              m_deviceName;

        public BluetoothOperation(
            string deviceName, BluetoothMode mode)
        {
            m_deviceName = deviceName;
            m_btMode = mode;
            m_bt = new BT.Bluetooth(BT_SOCKET);
            m_btClientHost = null;
        }

        /// <summary>
        /// Initializes the Bluetooth and Client/Host settings, and
        /// binds the events and timer for start.
        /// </summary>
        public void InitializeOperation()
        {
#if BT_DEBUG
            Debug.Print(GetOperationHandle() + " : Initializing");
#endif
            //-- Bluetooth Setup
            m_bt.SetDeviceName(m_deviceName);
            m_bt.SetPinCode(BT_PIN);

            //-- Client/Host Setup
            if (m_btMode == BluetoothMode.CLIENT)
                m_btClientHost = new BT.Bluetooth.Client(m_bt);
            else
                m_btClientHost = new BT.Bluetooth.Host(m_bt);

            //-- Event Binds
            m_bt.BluetoothStateChanged += m_bt_BluetoothStateChanged;
            m_bt.DataReceived += m_bt_DataReceived;
            m_bt.DeviceInquired += m_bt_DeviceInquired;

            //-- Timer Bind
            m_timer = new Gadgeteer.Timer(BT_PAIR_DELAY, Gadgeteer.Timer.BehaviorType.RunOnce);
            m_timer.Tick += new Gadgeteer.Timer.TickEventHandler(m_timer_Tick);
        }

        /// <summary>
        /// Starts the timer.
        /// </summary>
        public void StartOperation()
        {
            m_timer.Start();
            
#if BT_DEBUG
            Debug.Print(GetOperationHandle() + " : Starting");
#endif
        }

        /// <summary>
        /// Timer used for starting the Bluetooth Module with a delay.
        /// </summary>
        /// <param name="timer"></param>
        void m_timer_Tick(Gadgeteer.Timer timer)
        {
#if BT_DEBUG
            Debug.Print(GetOperationHandle() + " : Entering Pairing mode");
#endif
            //You only need to enter pairing mode once with a device. After you pair for the first time, it will
            //automatically connect in the future.
            if (!m_bt.IsConnected)
                if (m_btMode == BluetoothMode.CLIENT)
                    ((BT.Bluetooth.Client)m_btClientHost).EnterPairingMode();

        }

        /// <summary>
        /// Event fired by BT.Bluetooth when the Bluetooth Module is Inquired.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="macAddress"></param>
        /// <param name="name"></param>
        void m_bt_DeviceInquired(   BT.Bluetooth sender,
                                    string macAddress, string name  )
        {
#if BT_DEBUG
            Debug.Print(GetOperationHandle() + " : Device Inquired");
#endif
        }

        //Packet parsed into individual data element.
        public delegate void BT_ReceivedParsedData(
            BT.Bluetooth sender, uint dataId, PacketValueType type, Object data);
        public event BT_ReceivedParsedData BtReceivedParsedData;
        //Packet parsed into data array.
        public delegate void BT_ReceivedParsedDataArray(
            BT.Bluetooth sender, uint dataId, PacketValueType type, Object data);
        public event BT_ReceivedParsedDataArray BtReceivedParsedDataArray;
        //Called when no header is found for the packet.
        public delegate void BT_ReceivedString(BT.Bluetooth sender, uint dataId, string data);
        public event BT_ReceivedString BtReceivedParsedString;

        /* BT Packet Structure
         * -------------------------
         * {Header} + {Data}
         * {Header} = {HEADER_START_BYTE} + {TYPE} + Hex(ID) + Hex(Size)
         * {Data} = object + {SEPERATOR} + object...
         * {HEADER_START_BYTE} = 'SOH' = 1 = 0x001
         * {SEPARATOR} = 'ETX' = 3 = 0x003
         */

        /// <summary>
        /// Event fired by BT.Bluetooth when data is received from the Bluetooth Module.
        /// </summary>
        /// <param name="sender">Bluetooth module object receiving the sent data.</param>
        /// <param name="data">Data sent.</param>
        void m_bt_DataReceived( BT.Bluetooth sender, string data)
        {
            if (data[0] == BT_HEADER_START)
            {
#if BT_DEBUG
                Debug.Print(GetOperationHandle() + " : Received Data Packet! ");
#endif
                PacketValueType type;
                int length;

                // 0  - ID
                // 1  - Value Type
                // 2  - Length
                // 3  - Data [0]
                // 4+ - Data [1]+
                string[] seperatedData = data.Substring(1, data.Length - 1).Split(BT_SEPERATORS);        
                //Check array parsed
                if (seperatedData.Length <= BT_HEADER_ORDER_DATA)
                    throw new InvalidCastException("Invalid Header Length or Format.");
                uint id = BT_NO_ID;
                try
                {
                    //read id
                    id = UInt32.Parse(seperatedData[BT_HEADER_ORDER_ID]);
                    //read type
                    type = (PacketValueType)
                        Convert.ToInt32(seperatedData[BT_HEADER_ORDER_TYPE], 16);
                    //read length
                    length = Convert.ToInt32(seperatedData[BT_HEADER_ORDER_SIZE]);
                    switch (type)
                    {
                        case PacketValueType.BOOL:
#if BT_DEBUG
                            Debug.Print(GetOperationHandle() + " : Parsing Bool! ");
#endif
                            //Array for parsing data into
                            object[] boolArray = new object[length];
                            //Parse data
                            for (int i = 0; i < length; i++)
                            {
                                boolArray[i] =
                                    (   seperatedData[i + BT_HEADER_ORDER_DATA] == 
                                        Boolean.TrueString);
                            }
                            //Send event with parsed data.
                            if (boolArray.Length == 1)
                            {
                                BtReceivedParsedData(sender, id, PacketValueType.BOOL, boolArray[0]);
#if BT_ECHO_DATA_STRUCTURE
                                ((BT.Bluetooth.Client)m_btClientHost).Send("\n" + id + " -> " + boolArray[0].ToString());
#endif
                            }
                            else
                            {
                                BtReceivedParsedDataArray(sender, id, PacketValueType.BOOL, boolArray);
#if BT_ECHO_DATA_STRUCTURE
                                string message = "";
                                foreach (bool value in boolArray)
                                    message += value + " ";
                                ((BT.Bluetooth.Client)m_btClientHost).Send("\n" + id + " -> " + message);
#endif
                            }
                            break;
                        //--------------------------------------
                        case PacketValueType.BYTE:
#if BT_DEBUG
                            Debug.Print(GetOperationHandle() + " : Parsing Byte! ");
#endif
                            object[] byteArray = new object[length];
                            for (int i = 0; i < length; i++)
                            {
                                byteArray[i] = Byte.Parse(
                                    seperatedData[i + BT_HEADER_ORDER_DATA]);
                            }
                            //Send event with parsed data.
                            if (byteArray.Length == 1)
                            {
                                BtReceivedParsedData(sender, id, PacketValueType.BYTE, byteArray[0]);
#if BT_ECHO_DATA_STRUCTURE
                                ((BT.Bluetooth.Client)m_btClientHost).Send("\n" + id + " -> " + byteArray[0].ToString());
#endif
                            }
                            else
                            {
                                BtReceivedParsedDataArray(sender, id, PacketValueType.BYTE, byteArray);
#if BT_ECHO_DATA_STRUCTURE
                                string message = "";
                                foreach (byte value in byteArray)
                                    message += value + " ";
                                ((BT.Bluetooth.Client)m_btClientHost).Send("\n" + id + " -> " + message);
#endif
                            }
                            break;
                        //--------------------------------------
                        case PacketValueType.CHAR:
#if BT_DEBUG
                            Debug.Print(GetOperationHandle() + " : Parsing Char! ");
#endif
                            object[] charArray = new object[length];
                            for (int i = 0; i < length; i++)
                            {
                                charArray[i] = seperatedData[i + BT_HEADER_ORDER_DATA][0];
                            }
                            //Send event with parsed data.
                            if (charArray.Length == 1)
                            {
                                BtReceivedParsedData(sender, id, PacketValueType.CHAR, charArray[0]);
#if BT_ECHO_DATA_STRUCTURE
                                ((BT.Bluetooth.Client)m_btClientHost).Send("\n" + id + " -> " + charArray[0].ToString());
#endif
                            }
                            else
                            {
                                BtReceivedParsedDataArray(sender, id, PacketValueType.CHAR, charArray);
#if BT_ECHO_DATA_STRUCTURE
                                string message = "";
                                foreach (char value in charArray)
                                    message += value + " ";
                                ((BT.Bluetooth.Client)m_btClientHost).Send("\n" + id + " -> " + message);
#endif
                            }
                            break;
                        //--------------------------------------
                        case PacketValueType.DOUBLE:
#if BT_DEBUG
                            Debug.Print(GetOperationHandle() + " : Parsing Double! ");
#endif
                            object[] doubleArray = new object[length];
                            for (int i = 0; i < length; i++)
                            {
                                doubleArray[i] = Double.Parse(
                                    seperatedData[i + BT_HEADER_ORDER_DATA]);
                            }
                            //Send event with parsed data.
                            if (doubleArray.Length == 1)
                            {
                                BtReceivedParsedData(sender, id, PacketValueType.DOUBLE, doubleArray[0]);
#if BT_ECHO_DATA_STRUCTURE
                                ((BT.Bluetooth.Client)m_btClientHost).Send("\n" + id + " -> " + doubleArray[0].ToString());
#endif
                                
                            }
                            else
                            {
                                BtReceivedParsedDataArray(sender, id, PacketValueType.DOUBLE, doubleArray);
#if BT_ECHO_DATA_STRUCTURE
                                string message = "";
                                foreach (double value in doubleArray)
                                    message += value + " ";
                                ((BT.Bluetooth.Client)m_btClientHost).Send("\n" + id + " -> " + message);
#endif
                            }
                            break;
                        //--------------------------------------
                        case PacketValueType.INT:
#if BT_DEBUG
                            Debug.Print(GetOperationHandle() + " : Parsing Int! ");
#endif
                            object[] intArray = new object[length];
                            for (int i = 0; i < length; i++)
                            {
                                intArray[i] = Int32.Parse(
                                    seperatedData[i + BT_HEADER_ORDER_DATA]);
                            }
                            //Send event with parsed data.
                            if (intArray.Length == 1)
                            {
                                BtReceivedParsedData(sender, id, PacketValueType.INT, intArray[0]);
#if BT_ECHO_DATA_STRUCTURE
                                ((BT.Bluetooth.Client)m_btClientHost).Send("    \n" + id + " -> " + intArray[0].ToString());
#endif
                            }
                            else
                            {
                                BtReceivedParsedDataArray(sender, id, PacketValueType.INT, intArray);
#if BT_ECHO_DATA_STRUCTURE
                                string message = "";
                                foreach (int value in intArray)
                                    message += value + " ";
                                ((BT.Bluetooth.Client)m_btClientHost).Send("    \n" + id + " -> " + message);
#endif
                            }
                            break;
                        //--------------------------------------
                        case PacketValueType.UINT:
#if BT_DEBUG
                            Debug.Print(GetOperationHandle() + " : Parsing Unsigned Int! ");
#endif
                            object[] uintArray = new object[length];
                            for (int i = 0; i < length; i++)
                            {
                                uintArray[i] = UInt32.Parse(
                                    seperatedData[i + BT_HEADER_ORDER_DATA]);
                            }
                            //Send event with parsed data.
                            if (uintArray.Length == 1)
                            {
                                BtReceivedParsedData(sender, id, PacketValueType.UINT, uintArray[0]);
#if BT_ECHO_DATA_STRUCTURE
                                string  message = "";
                                foreach (uint value in uintArray)
                                    message += value + " ";
                                ((BT.Bluetooth.Client)m_btClientHost).Send("\n" + id + " -> " + message);
#endif
                            }
                            else
                            {
                                BtReceivedParsedDataArray(sender, id, PacketValueType.UINT, uintArray);
#if BT_ECHO_DATA_STRUCTURE
                                ((BT.Bluetooth.Client)m_btClientHost).Send("\n" + id + " -> " + uintArray.ToString());
#endif
                            }
                            break;
                        //--------------------------------------
                        case PacketValueType.LONG:
#if BT_DEBUG
                            Debug.Print(GetOperationHandle() + " : Parsing Long! ");
#endif
                            object[] longArray = new object[length];
                            for (int i = 0; i < length; i++)
                            {
                                longArray[i] = Int64.Parse(
                                    seperatedData[i + BT_HEADER_ORDER_DATA]);
                            }
                            //Send event with parsed data.
                            if (longArray.Length == 1)
                            {
                                BtReceivedParsedData(sender, id, PacketValueType.LONG, longArray[0]);
#if BT_ECHO_DATA_STRUCTURE
                                ((BT.Bluetooth.Client)m_btClientHost).Send("\n" + id + " -> " + longArray[0].ToString());
#endif
                            }
                            else
                            {
                                BtReceivedParsedDataArray(sender, id, PacketValueType.LONG, longArray);
#if BT_ECHO_DATA_STRUCTURE
                                string message = "";
                                foreach (long value in longArray)
                                    message += value + " ";
                                ((BT.Bluetooth.Client)m_btClientHost).Send("\n" + id + " -> " + message);
#endif
                            }
                            break;
                        //--------------------------------------
                        case PacketValueType.SHORT:
#if BT_DEBUG
                            Debug.Print(GetOperationHandle() + " : Parsing Short! ");
#endif
                            object[] shortArray = new object[length];
                            for (int i = 0; i < length; i++)
                            {
                                shortArray[i] = Int16.Parse(
                                    seperatedData[i + BT_HEADER_ORDER_DATA]);
                            }
                            //Send event with parsed data.
                            if (shortArray.Length == 1)
                            {
                                BtReceivedParsedData(sender, id, PacketValueType.SHORT, shortArray[0]);
#if BT_ECHO_DATA_STRUCTURE
                                ((BT.Bluetooth.Client)m_btClientHost).Send("\n" + id + " -> " + shortArray[0].ToString());
#endif
                            }
                            else
                            {
                                BtReceivedParsedDataArray(sender, id, PacketValueType.SHORT, shortArray);
#if BT_ECHO_DATA_STRUCTURE
                                string message = "";
                                foreach (short value in shortArray)
                                    message += value + " ";
                                ((BT.Bluetooth.Client)m_btClientHost).Send("\n" + id + " -> " + message);
#endif
                            }
                            break;
                        //--------------------------------------
                        case PacketValueType.USHORT:
#if BT_DEBUG
                            Debug.Print(GetOperationHandle() + " : Parsing Unsigned Short! ");
#endif
                            object[] ushortArray = new object[length];
                            for (int i = 0; i < length; i++)
                            {
                                ushortArray[i] = UInt16.Parse(
                                    seperatedData[i + BT_HEADER_ORDER_DATA]);
                            }
                            //Send event with parsed data.
                            if (ushortArray.Length == 1)
                            {
                                BtReceivedParsedData(sender, id, PacketValueType.USHORT, ushortArray[0]);
#if BT_ECHO_DATA_STRUCTURE
                                ((BT.Bluetooth.Client)m_btClientHost).Send("\n" + id + " -> " + ushortArray[0].ToString());
#endif
                            }
                            else
                            {
                                BtReceivedParsedDataArray(sender, id, PacketValueType.USHORT, ushortArray);
#if BT_ECHO_DATA_STRUCTURE
                                string message = "";
                                foreach (ushort value in ushortArray)
                                    message += value + " ";
                                ((BT.Bluetooth.Client)m_btClientHost).Send("\n" + id + " -> " + message);
#endif
                            }
                            break;
                        //--------------------------------------
                        case PacketValueType.STRING:
#if BT_DEBUG
                            Debug.Print(GetOperationHandle() + " : Parsing String! ");
#endif
                            string[] stringArray = new string[length];
                            for (int i = 0; i < length; i++)
                            {
                                stringArray[i] = 
                                    seperatedData[i + BT_HEADER_ORDER_DATA];
                            }
                            //Send event with parsed data.
                            if (stringArray.Length == 1)
                            {
                                BtReceivedParsedData(sender, id, PacketValueType.STRING, stringArray[0]);
#if BT_ECHO_DATA_STRUCTURE
                                ((BT.Bluetooth.Client)m_btClientHost).Send(id + stringArray[0]);
#endif
                            }
                            else
                            {
                                BtReceivedParsedDataArray(sender, id, PacketValueType.STRING, stringArray);
#if BT_ECHO_DATA_STRUCTURE
                                string datas = "";
                                for (int i = 0; i < stringArray.Length; i++)
                                {
                                    datas += "\n\t" + stringArray[i];
                                }
                                ((BT.Bluetooth.Client)m_btClientHost).Send(id + datas);
#endif
                            }
                            break;
                        //--------------------------------------
                        case PacketValueType.STRUCT:
#if BT_DEBUG
                            Debug.Print(GetOperationHandle() + " : Parsing Struct! ");
#endif
                            throw new InvalidCastException("Structures not yet supported!");
                        //--------------------------------------
                        case PacketValueType.UNKNOWN:
                            break;
                    }
                    //Read data
                    
                }
                catch (InvalidCastException e)
                {
                    Debug.Print(GetOperationHandle() + ": " + e.Message);
                }
                catch (Exception e)
                {
                    Debug.Print(GetOperationHandle() + ": " + e.Message);
                }   
            }
                //Send as string event.
            else
            {
#if BT_DEBUG
                Debug.Print(GetOperationHandle() + 
                    " : Received Invalid Header Sending as String! ");
#endif
#if BT_ECHO_DATA_STRUCTURE
               // ((BT.Bluetooth.Client)m_btClientHost).Send("\n" + "Invalid Data: " + data);
#endif
                BtReceivedParsedString(sender, BT_NO_ID, data);
            }
        }

        /// <summary>
        /// Event fired by BT.Bluetooth when the Bluetooth Modules State changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="btState"></param>
        void m_bt_BluetoothStateChanged(    BT.Bluetooth sender,
                                            BT.Bluetooth.BluetoothState btState )
        {
#if BT_DEBUG
            Debug.Print(GetOperationHandle() + " : State Changes");
#endif
        }

        /// <summary>
        /// Sends an Operation stop Request.
        /// </summary>
        /// <remarks>Note: Currently Does Nothing!</remarks>
        public void StopOperation()
        {
            
#if BT_DEBUG
            Debug.Print(GetOperationHandle() + 
                " : Stopping balled but not used!");
#endif
        }

        /// <summary>
        /// Updates the Operation.
        /// </summary>
        /// <remarks>Note: Currently Does Nothing!</remarks>
        public void UpdateOperation()
        {
            
#if BT_DEBUG
            Debug.Print(GetOperationHandle() 
                + " : Updating called but not used!");
#endif
        }

        /// <summary>
        /// UnInitializes the operation.
        /// </summary>
        /// <remarks>Note: Currently Does Nothing!</remarks>
        public void UninitializeOperation()
        {
            
#if BT_DEBUG
            Debug.Print(GetOperationHandle() 
                + " : Uninitializing called but not used!");
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetOperationHandle()
        {
            return m_deviceName;
        }
    }
}
