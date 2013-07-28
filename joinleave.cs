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
        List<string> nomssg = new List<string>();

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
            if (msg is RoomEnterMessage)
            {
                RoomEnterMessage rms = (RoomEnterMessage)msg;
                this.nomssg.Add(rms.roomName);

            }

            if (msg is RoomInfoMessage)
            {
                Console.WriteLine("joinleave bearbeitet: " + msg.getRawText());
                //"removed";
                //"updated";
                JsonReader jsonReader = new JsonReader();
                Dictionary<string, object> dictionary = (Dictionary<string, object>)jsonReader.Read(msg.getRawText());
                string roomname = (string)dictionary["roomName"];

                

                if (msg.getRawText().Contains("removed"))
                {
                    Dictionary<string, object>[] d = (Dictionary<string, object>[])dictionary["removed"];

                    for (int i = 0; i < d.Length; i++)
                    {
                        string ltext = d[i]["name"] + " has left the room";
                        
                        RoomChatMessageMessage leftmessage = new RoomChatMessageMessage(roomname, "<color=#777460>" + ltext + "</color>");
                        App.ChatUI.handleMessage(leftmessage);
                        App.ArenaChat.ChatRooms.ChatMessage(leftmessage);
                    }

                }

                if (msg.getRawText().Contains("updated") && this.nomssg.Contains(roomname))
                {
                    nomssg.RemoveAll(item => item == roomname);
                }
                else
                {
                    if (msg.getRawText().Contains("updated"))
                    {
                        Dictionary<string, object>[] d = (Dictionary<string, object>[])dictionary["updated"];

                        for (int i = 0; i < d.Length; i++)
                        {
                            string ltext = d[i]["name"] + " has joined the room";
                            RoomChatMessageMessage leftmessage = new RoomChatMessageMessage(roomname, "<color=#777460>" + ltext + "</color>");
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

        public override bool WantsToReplace(InvocationInfo info)
        {
             if (info.targetMethod.Equals("sendRequest"))
            {

                if (info.arguments[0] is RoomChatMessageMessage)
                {
                    RoomChatMessageMessage msg = (RoomChatMessageMessage)info.arguments[0];

                    string[] splitt = msg.text.Split(' ');
                    if (splitt[0] == "/jl" || splitt[0] == "\\jl")
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public override void ReplaceMethod(InvocationInfo info, out object returnValue)
        {
            
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
                    
                    return;
                }
            }


            return;
        
        }

        public override void BeforeInvoke(InvocationInfo info)
        {

            return;

        }

        public override void AfterInvoke (InvocationInfo info, ref object returnValue)
        //public override bool BeforeInvoke(InvocationInfo info, out object returnValue)
        {

            return;
        }
 
	}
}

