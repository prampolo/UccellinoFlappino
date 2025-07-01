using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Threading;
using UnityEngine;

/**
 * SerialPortUtility 
 * Author: Mustafa Erdem Köşk <erdemkosk@gmail.com>
 * http://www.erdemkosk.com
 */

public delegate void SerialPortMessageEventHandler(byte[] sendData);
public delegate void SerialPortSendMessageReportHandler(byte[] sendData);
public class SerialCommunication
{
    public event SerialPortMessageEventHandler SerialPortMessageEvent;
    public event SerialPortSendMessageReportHandler SerialPortSendMessageReportEvent;
    private SerialPort serialPort;
    private Thread threadReceive;

    public SerialCommunication(SerialPort serialPort)
    {
        this.serialPort = serialPort;
    }
    public SerialCommunication(string portName, int boudrate)
    {
        serialPort = new SerialPort(portName, boudrate)
        {
            DtrEnable = true,
            RtsEnable = true
        };

    }

    public void OpenSerialPort()
    {
        serialPort.Open();
        UnityEngine.Debug.Log($"[SERIALE] Aperta {serialPort.PortName} a {serialPort.BaudRate} bps");
        UnityEngine.Debug.Log($"[SERIALE] Bytes già presenti: {serialPort.BytesToRead}");
        serialPort.ReadTimeout = 500;
        serialPort.WriteTimeout = 500;
        threadReceive = new Thread(ListenSerialPort);
        threadReceive.IsBackground = true;
        threadReceive.Start();
    }
    public bool IsSerialPortIsOpen()
    { 
        return serialPort.IsOpen;
    }
    public void CloseSerialPort()
    {
        serialPort.Close();
    }

    private void ListenSerialPort()
    {
        StringBuilder buffer = new StringBuilder();

        while (serialPort.IsOpen)
        {
            try
            {
                while (serialPort.BytesToRead > 0)
                {
                    int byteRead = serialPort.ReadByte();
                    char c = (char)byteRead;

                    if (c == '\n') // fine messaggio
                    {
                        string message = buffer.ToString().Trim();
                        buffer.Clear();

                        if (!string.IsNullOrEmpty(message))
                        {
                            UnityEngine.Debug.Log($"[SERIAL] Full line received: {message}");
                            byte[] dataBytes = System.Text.Encoding.ASCII.GetBytes(message);
                            SerialPortMessageEvent?.Invoke(dataBytes);
                        }
                    }
                    else
                    {
                        buffer.Append(c);
                    }
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError("[SERIAL] Error reading from port: " + ex.Message);
            }

            Thread.Sleep(10);
        }

    }

    public bool SendMessageFromSerialPort(byte[] byteArray)
    {
        if (serialPort != null && serialPort.IsOpen == true)
        {

            serialPort.Write(byteArray, 0, byteArray.Length);

            if (SerialPortSendMessageReportEvent != null && SerialPortSendMessageReportEvent.GetInvocationList().Length > 0) // If somebody is listening
            {
                SerialPortSendMessageReportEvent(byteArray);
            }
            return true;
        }
        else
        {
            return false;
        }


    }
}




