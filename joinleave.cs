using System;

using ScrollsModLoader.Interfaces;
using UnityEngine;
using Mono.Cecil;
//using Mono.Cecil;
//using ScrollsModLoader.Interfaces;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Reflection;
using JsonFx.Json;
using System.Text.RegularExpressions;


namespace joinleave.mod
{
    public class joinleave : BaseMod, ICommListener
	{
        Dictionary<string, string[]> rooms=new Dictionary<string, string[]>();
        Dictionary<string, Boolean> roomstoggle = new Dictionary<string, Boolean>();
        Dictionary<string, string[]> newplayers = new Dictionary<string, string[]>();


		//initialize everything here, Game is loaded at this point
        public joinleave()
		{
            try
            {
                App.Communicator.addListener(this);
            }
            catch { }
            
		}



		public static string GetName ()
		{
            return "joinleave";
		}

		public static int GetVersion ()
		{
			return 1;
		}

        
        public void handleMessage(Message msg)
        {

            if (msg is RoomInfoMessage)
            {

                JsonReader jsonReader = new JsonReader();
                Dictionary<string, object> dictionary = (Dictionary<string, object>)jsonReader.Read(msg.getRawText());
                Dictionary<string, object>[] d = (Dictionary<string, object>[])dictionary["profiles"];
                string roomname=(string)dictionary["roomName"];
                int vorhanden = 0;
                string[] oldnames=new string[100]; 
                
                if (rooms.ContainsKey(roomname)) 
                {
                    oldnames = (string[])rooms[roomname].Clone(); ;
                    vorhanden = 1;
                } 
                else 
                { 
                    rooms.Add(roomname, new string[100]);
                    roomstoggle.Add(roomname, new Boolean());
                    roomstoggle[roomname] = true;
                }
                for (int i = 0; i < d.GetLength(0); i++)
                {
                    rooms[roomname][i] = d[i]["name"].ToString() ;
                }

                string[] leaves;
                string[] joins;
                if (vorhanden == 1)
                {
                    
                   leaves= rooms[roomname].Except(oldnames).ToArray(); 
                   joins= oldnames.Except(rooms[roomname]).ToArray();
                   string text = string.Join(", ", leaves);
                   if (leaves.Length > 0)
                   {
                       text = text + " has left the room";
                   }
                   string ltext = text;
                   RoomChatMessageMessage leftmessage = new RoomChatMessageMessage(roomname, "<color=#777460>" + ltext + "</color>");

                   text = string.Join(", ", joins);
                    if (joins.Length > 0)
                    {
                        text = text + " has joined the room";
                    }
                    RoomChatMessageMessage joinmessage = new RoomChatMessageMessage(roomname, "<color=#777460>" + text + "</color>");
                    Console.WriteLine(text);
                    if (roomstoggle[roomname])
                    {
                        if (!text.Equals(""))
                        {
                            App.ChatUI.handleMessage(joinmessage);
                            App.ArenaChat.ChatRooms.ChatMessage(joinmessage);
                           
                        }
                        if (!ltext.Equals(""))
                        {
                            App.ChatUI.handleMessage(leftmessage);
                            App.ArenaChat.ChatRooms.ChatMessage(leftmessage);
                        }
                    }
                }
                



            }
            
            return;
        }
        public void onReconnect()
        {
            return; // don't care
        }

       

		//only return MethodDefinitions you obtained through the scrollsTypes object
		//safety first! surround with try/catch and return an empty array in case it fails
		public static MethodDefinition[] GetHooks (TypeDefinitionCollection scrollsTypes, int version)
		{
            try
            {
                return new MethodDefinition[] {
                    //scrollsTypes["ChatUI"].Methods.GetMethod("Awake")[0],
                    scrollsTypes["Communicator"].Methods.GetMethod("sendRequest", new Type[]{typeof(Message)}),


             };
            }
            catch
            {
                return new MethodDefinition[] { };
            }
		}

        public override bool BeforeInvoke(InvocationInfo info, out object returnValue) {
            
            returnValue = null;


            if (info.targetMethod.Equals("sendRequest"))
            {
                if (info.arguments[0] is RoomChatMessageMessage)
                {
                    Boolean iscommand = false;
                    RoomChatMessageMessage msg = (RoomChatMessageMessage)info.arguments[0];
                   
                    string[] splitt = msg.text.Split(' ');
                    if (splitt[0] == "/jl" || splitt[0] == "\\jl") 
                    {
                        iscommand = true;
                        bool donesomething = false;
                        if (splitt.Length == 2) {
                            if (splitt[1] == "allon" || splitt[1] == "on")
                            {
                                donesomething = true;
                                var buffer = new List<string>(roomstoggle.Keys);
                                foreach (var key in buffer)
                                {
                                    roomstoggle[key] = true;
                                    
                                }
                                string text = "joinleave on";
                                RoomChatMessageMessage joinmessage = new RoomChatMessageMessage(msg.roomName, "<color=#777460>" + text + "</color>");
                                App.ChatUI.handleMessage(joinmessage);
                                App.ArenaChat.ChatRooms.ChatMessage(joinmessage);
                            }

                            if (splitt[1] == "alloff" || splitt[1] == "off")
                            {
                                donesomething = true;
                                var buffer = new List<string>(roomstoggle.Keys);
                                foreach (var key in buffer)
                                {
                                    roomstoggle[key] = false;

                                }
                                string text = "joinleave off";
                                RoomChatMessageMessage joinmessage = new RoomChatMessageMessage(msg.roomName, "<color=#777460>" + text + "</color>");
                                App.ChatUI.handleMessage(joinmessage);
                                App.ArenaChat.ChatRooms.ChatMessage(joinmessage);
                            }
                            if (splitt[1] == "tggl" || splitt[1] == "toggle") {
                                donesomething = true;
                                roomstoggle[msg.roomName] = !roomstoggle[msg.roomName] ;
                                string statuss = "on";
                                if (roomstoggle[msg.roomName] == false) { statuss = "off"; };
                                string text = "toggled " + msg.roomName + " " + statuss;
                                RoomChatMessageMessage joinmessage = new RoomChatMessageMessage(msg.roomName, "<color=#777460>" + text + "</color>");
                                App.ChatUI.handleMessage(joinmessage);
                                App.ArenaChat.ChatRooms.ChatMessage(joinmessage);
                            }

                            

                        };
                        if (donesomething == false)
                        {
                            string text = "/jl commands are: on/allon, off/alloff, tggl/toggle ";
                            RoomChatMessageMessage joinmessage = new RoomChatMessageMessage(msg.roomName, "<color=#777460>" + text + "</color>");
                            App.ChatUI.handleMessage(joinmessage);
                            App.ArenaChat.ChatRooms.ChatMessage(joinmessage);

                        }
                    }
                    
                    return iscommand;
                }
            }


            return false;
        
        }

        public override void AfterInvoke (InvocationInfo info, ref object returnValue)
        //public override bool BeforeInvoke(InvocationInfo info, out object returnValue)
        {

            /*if (info.target is ChatUI && info.targetMethod.Equals("Awake")) 
            {
                Console.WriteLine("dingst startet ####################################");
                this.chatRooms = (ChatRooms)typeof(ChatUI).GetField("chatRooms", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(info.target);
            }*/
            return;
        }

		/*public override void AfterInvoke (InvocationInfo info, ref object returnValue)
		{

           



			return;
		}*/



        
	}
}

