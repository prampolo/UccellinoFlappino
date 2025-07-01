using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.SerialPortUtility.Scripts;

public class SerialTest : MonoBehaviour
{
    SerialCommunicationFacade serialFacade;

    // Start is called before the first frame update
    void Start()
    {
        serialFacade = new SerialCommunicationFacade();
        serialFacade.Connect(9600, "COM1"); // <=== QUI imposti la porta VIRTUALE da cui Unity riceve
        Debug.Log("Tentativo di connessione a COM1...");

    }

    void OnApplicationQuit()
    {
        serialFacade.Disconnect();
    }
}
