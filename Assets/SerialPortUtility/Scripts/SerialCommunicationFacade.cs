using Assets.SerialPortUtility.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.SerialPortUtility.Scripts
{
   public class SerialCommunicationFacade : ISerialCommunication
    {
        public event Action<byte[]> OnSerialMessageReceived;


        SerialCommunication serialCom;
        public void Connect(int baudrate, string portName)
        {
            serialCom = new SerialCommunication(portName, baudrate);
            serialCom.SerialPortMessageEvent += SerialCom_SerialPortMessageEvent;
            serialCom.SerialPortSendMessageReportEvent += SerialCom_SerialPortSendMessageReportEvent;
            try
            {
                serialCom.OpenSerialPort();
                Debug.Log($"[SERIALE] Porta {portName} aperta.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SERIALE] Errore apertura porta {portName}: {ex.Message}");
            }

        }
        public void Disconnect()
        {
            serialCom.CloseSerialPort();
            Debug.Log("Serial Disconnected");
        }
        public void SendMessage(byte[] byteArray)
        {
            if (serialCom.IsSerialPortIsOpen() == true)
            {
                serialCom.SendMessageFromSerialPort(byteArray);
                Debug.Log("Message Sended");
            }
            else
            {
                Debug.Log("Message Send Failed!");
            }
        }
        private void SerialCom_SerialPortSendMessageReportEvent(byte[] sendData)
        {
            string text = System.Text.Encoding.ASCII.GetString(sendData, 0, sendData.Length);
            Debug.Log("Message Send From Serial.. Message =>  " + text.ToString());
        }

        private void SerialCom_SerialPortMessageEvent(byte[] sendData)
        {
            string text = System.Text.Encoding.ASCII.GetString(sendData, 0, sendData.Length);
            Debug.Log("Message Coming From Serial.. Message =>  " + text);

            // Propagazione esterna
            OnSerialMessageReceived?.Invoke(sendData);
        }



    }
}
