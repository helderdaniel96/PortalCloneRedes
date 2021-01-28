﻿using Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

public class TcpClientController : MonoBehaviour
{
    [HideInInspector]
    public Player Player;

    public Text PlayerNameInputText;
    public string IpAddress;
    public int Port;
    public GameObject PlayerPrefab;
    public GameObject SpawnPoint;
    public GameObject ConnectionUI;

    private Dictionary<Guid, GameObject> _players;

    private void Awake()
    {
        _players = new Dictionary<Guid, GameObject>();

        Player = new Player();
        Player.GameState = GameState.Disconnected;
        Player.TcpClient = new TcpClient();
    }
    void Update()
    {
        if (Player.TcpClient.Connected)
        {
            switch (Player.GameState)
            {
                case GameState.Connecting:
                    Debug.Log("Connecting");
                    Connecting();
                    break;
                case GameState.Connected:
                    Connected();
                    break;
                case GameState.Sync:
                    Sync();
                    break;
                case GameState.GameStarted:
                    GameStarted();
                    break;
            }
        }
    }

    private void GameStarted()
    {
        if (Player.DataAvailable())
        {
            Message m = Player.ReadMessage();
            if (m.MessageType == MessageType.NewPlayer)
            {
                GameObject newPlayer = Instantiate(PlayerPrefab, new Vector3(m.PlayerInfo.X, m.PlayerInfo.Y, m.PlayerInfo.Z), Quaternion.identity);
                newPlayer.GetComponent<PlayerUiController>().PlayerName.text = m.PlayerInfo.Name;

                //CAMERA//
                PlayerController temp = newPlayer.GetComponent<PlayerController>();
                temp.Playable = false;
                temp.camera.SetActive(false);
                
                _players.Add(m.PlayerInfo.Id, newPlayer);
                
            }
            else if (m.MessageType == MessageType.PlayerMovement)
            {
                if (m.PlayerInfo.Id != Player.Id && _players.ContainsKey(m.PlayerInfo.Id))
                {
                    //SYNC EACH PLAYER INFO//
                    _players[m.PlayerInfo.Id].transform.LookAt(new Vector3(m.PlayerInfo.lookX, _players[m.PlayerInfo.Id].transform.position.y, m.PlayerInfo.lookZ), Vector3.up);
                    _players[m.PlayerInfo.Id].transform.position = new Vector3(m.PlayerInfo.X, m.PlayerInfo.Y, m.PlayerInfo.Z);
                    _players[m.PlayerInfo.Id].GetComponent<PlayerController>().cube1.transform.position = new Vector3(m.PlayerInfo.C1x, m.PlayerInfo.C1y, m.PlayerInfo.C1z);
                    _players[m.PlayerInfo.Id].GetComponent<PlayerController>().cube2.transform.position = new Vector3(m.PlayerInfo.C2x, m.PlayerInfo.C2y, m.PlayerInfo.C2z);
                    _players[m.PlayerInfo.Id].GetComponent<PlayerController>().cube3.transform.position = new Vector3(m.PlayerInfo.C3x, m.PlayerInfo.C3y, m.PlayerInfo.C3z);
                    _players[m.PlayerInfo.Id].GetComponent<PlayerController>().leftPortal.transform.position = new Vector3(m.PlayerInfo.P1x, m.PlayerInfo.P1y, m.PlayerInfo.P1z);
                    _players[m.PlayerInfo.Id].GetComponent<PlayerController>().rightPortal.transform.position = new Vector3(m.PlayerInfo.P2x, m.PlayerInfo.P2y, m.PlayerInfo.P2z);
                    
                }
            }
        }
    }
    private void Sync()
    {
        Debug.Log("Sync");
        if (Player.DataAvailable())
        {
            Message message = Player.ReadMessage();

            if (message.MessageType == MessageType.NewPlayer)
            {
                GameObject gameObject = Instantiate(PlayerPrefab, new Vector3(message.PlayerInfo.X, message.PlayerInfo.Y, message.PlayerInfo.Z),Quaternion.identity);
                gameObject.GetComponent<PlayerUiController>().PlayerName.text = message.PlayerInfo.Name;

                PlayerController temp = gameObject.GetComponent<PlayerController>();
                temp.Playable = false;
                temp.camera.SetActive(false);

                _players.Add(message.PlayerInfo.Id, gameObject);
            }
            else if  (message.MessageType == MessageType.PlayerMovement)
            { 
                if (_players.ContainsKey(message.PlayerInfo.Id))
                {
                    _players[message.PlayerInfo.Id].transform.position = new Vector3(message.PlayerInfo.X, message.PlayerInfo.Y, message.PlayerInfo.Z);
                }
            }
            else if (message.MessageType == MessageType.FinishedSync)
            {
                ConnectionUI.SetActive(false);
                GameObject player = Instantiate(PlayerPrefab, SpawnPoint.transform.position, Quaternion.identity);
                player.GetComponent<PlayerController>().TcpClient = this;
                player.GetComponent<PlayerController>().Playable = true;
                player.GetComponent<PlayerController>().enabled = true;
                player.GetComponent<PlayerUiController>().PlayerName.text = Player.Name;

                _players.Add(Player.Id, player);
                Player.GameState = GameState.GameStarted;
            }
        }
    }
    private void Connected()
    {
        if (Player.DataAvailable())
        {
            Debug.Log("Connected");

            Message message = Player.ReadMessage();
            Debug.Log(message.Description);
            Player.GameState = GameState.Sync;
        }
    }

    private void Connecting()
    {
        if (Player.DataAvailable())
        {
            Player playerJson = Player.ReadPlayer();
            Player.Id = playerJson.Id;
            Player.Name = PlayerNameInputText.text;

            Player.SendPlayer(Player);
            Player.GameState = GameState.Connected;
        }
    }
    public void StartTcpClient()
    {
        Player.TcpClient.BeginConnect(IPAddress.Parse(IpAddress), Port,
            AcceptConnection, Player.TcpClient);
        Player.GameState = GameState.Connecting;
    }
    public void AcceptConnection(IAsyncResult result)
    {
        TcpClient client = (TcpClient)result.AsyncState;
        client.EndConnect(result);

        if (client.Connected)
        {
            Debug.Log("Connected");
        }
        else
        {
            Debug.Log("Connection to the server refused");
        }
    }
}
