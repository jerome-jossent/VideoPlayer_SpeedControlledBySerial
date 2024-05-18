using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using System;
using TMPro;
using System.Linq;
using System.Threading;
using UnityEngine.Events;

public class Arduino : MonoBehaviour
{
    public object _messagesLock = new object();
    List<Message> _messages = new List<Message>();
    int _messagesMax = 300;
    public int _messages_Count { get { return _messages.Count; } }

    #region "Parametres"
    private SerialPort _mySerialPort;
    private string _port;
    private string _baud;

    private void OnDestroy()
    {
        PortCom_OFF();
    }

    internal void Fill_Dropdown(TMP_Dropdown dd, List<string> list)
    {
        if (dd == null) return;
        dd.ClearOptions();
        dd.AddOptions(list);
    }

    public SerialPort MySerialPort
    {
        get { return _mySerialPort; }
        set { _mySerialPort = value; }
    }
    public string Port
    {
        get { return _port; }
        set
        {
            if (value.ToLower().Substring(0, 2).Contains("com"))
                _port = value;
            else
                _port = "COM" + System.Text.RegularExpressions.Regex.Match(value, @"\d+").Value;
        }
    }
    public string Baud
    {
        get { return _baud; }
        set { _baud = value; }
    }

    public bool IsOpen
    {
        get
        {
            if (MySerialPort == null)
                return false;
            return MySerialPort.IsOpen;
        }
    }

    public static List<string> COM_InfoHardware { get => PortComs(); }
    #endregion

    public static List<string> PortComs()
    {
        return SerialPort.GetPortNames().ToList();
    }

    public bool PortCom_ON()
    {
        try
        {
            //utilisation des param�tres
            MySerialPort = new SerialPort(Port, int.Parse(Baud));
            MySerialPort.ReadTimeout = 1;

            //attribution en interne des autres param�tres n�cessaires
            MySerialPort.Parity = Parity.None;
            MySerialPort.StopBits = StopBits.One;
            MySerialPort.DataBits = 8;
            MySerialPort.Handshake = Handshake.None;
            MySerialPort.Open();
            Debug.Log("Connexion au port " + Port + " � " + Baud + "bps.");

            SendMessage(""); //purge (premier message foir�, donc je le foire imm�diatement)

            NewMessage("Connexion : ok");
            return true;
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
            NewMessage("Connexion : " + ex.Message);
            return false;
        }
    }

    public bool PortCom_OFF()
    {
        if (MySerialPort == null) return true;
        try
        {
            MySerialPort.Close();
            Debug.Log("D�connexion du port " + Port);
            return true;
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
            return false;
        }
    }

    void Update()
    {
        ReadLine();
    }

    void ReadLine()
    {
        if (MySerialPort != null && MySerialPort.IsOpen)
        {
            string strData = null;
            try
            {
                strData = MySerialPort.ReadLine(); // blocking call.
                NewMessage(strData);
            }
            catch (Exception ex)
            {
                switch (ex.Message)
                {
                    case "The operation has timed out.":
                        break;

                    default:
                        Debug.Log(ex.Message);
                        break;
                }
            }
        }
    }

    void NewMessage(string strData)
    {
        Message m = new Message(strData);
        lock (_messagesLock)
        {
            _messages.Add(m);
            while (_messages.Count > _messagesMax)
                _messages.RemoveAt(0);
        }
    }

    public Message _messagePop()
    {
        Message m = null;
        lock (_messagesLock)
        {
            m = _messages[0];
            _messages.Remove(m);
        }
        return m;
    }

    public new bool SendMessage(string texte)
    {
        try
        {
            if (MySerialPort ==null || !MySerialPort.IsOpen)
                return false;

            MySerialPort?.WriteLine(texte); //write LINE, pas juste write !
            //Debug.Log("SendMessage : " + texte);
            return true;
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
            return false;
        }
    }
}