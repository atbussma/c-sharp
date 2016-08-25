﻿//Build Date: June 06, 2016
#region "Header"
#if (__MonoCS__)
#define TRACE
#endif
using System;
using System.IO;
using System.Text;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using PubnubApi.Interface;
using System.Threading.Tasks;
#endregion



namespace PubnubApi
{
    public abstract class PubnubCoreBase
    {
        #region "Class variables"
        private static bool _enableResumeOnReconnect = true;
        protected static bool overrideTcpKeepAlive = true;
        private LoggingMethod.Level _pubnubLogLevel = LoggingMethod.Level.Off;
        private static PubnubErrorFilter.Level _errorLevel = PubnubErrorFilter.Level.Info;
        protected static ConcurrentDictionary<Uri, Timer> channelLocalClientHeartbeatTimer = new ConcurrentDictionary<Uri, Timer>();
        private static ConcurrentDictionary<string, List<string>> _channelSubscribedAuthKeys = new ConcurrentDictionary<string, List<string>>();
        protected static System.Threading.Timer localClientHeartBeatTimer;
        protected static System.Threading.Timer presenceHeartbeatTimer = null;
        protected static bool pubnetSystemActive = true;
        protected Collection<Uri> pushRemoteImageDomainUri = new Collection<Uri>();
        #endregion

        private static IPubnubHttp pubnubHttp = null;

        private static PNConfiguration pubnubConfig = null;
        private static IJsonPluggableLibrary jsonLib = null;
        private static IPubnubUnitTest unitTest = null;

        private static int pubnubNetworkTcpCheckIntervalInSeconds = 15;

        protected static bool UuidChanged
        {
            get;
            set;
        }

        protected static string Uuid
        {
            get;
            set;
        }

        protected static long LastSubscribeTimetoken
        {
            get;
            set;
        }

        protected static int PubnubNetworkTcpCheckIntervalInSeconds
        {
            get
            {
                return pubnubNetworkTcpCheckIntervalInSeconds;
            }
            set
            {
                pubnubNetworkTcpCheckIntervalInSeconds = value;
            }
        }

        public static ConcurrentDictionary<PubnubChannelCallbackKey, object> ChannelCallbacks
        {
            get;
            set;
        } = new ConcurrentDictionary<PubnubChannelCallbackKey, object>();

        public static ConcurrentDictionary<PubnubChannelGroupCallbackKey, object> ChannelGroupCallbacks
        {
            get;
            set;
        } = new ConcurrentDictionary<PubnubChannelGroupCallbackKey, object>();

        protected static ConcurrentDictionary<string, long> MultiChannelSubscribe
        {
            get;
            set;
        } = new ConcurrentDictionary<string, long>();

        protected static ConcurrentDictionary<string, long> MultiChannelGroupSubscribe
        {
            get;
            set;
        } = new ConcurrentDictionary<string, long>();

        protected static ConcurrentDictionary<string, Timer> ChannelReconnectTimer
        {
            get;
            set;
        } = new ConcurrentDictionary<string, Timer>();

        protected static ConcurrentDictionary<string, Timer> ChannelGroupReconnectTimer
        {
            get;
            set;
        } = new ConcurrentDictionary<string, Timer>();

        protected ConcurrentDictionary<string, Type> ChannelSubscribeObjectType
        {
            get;
            set;
        } = new ConcurrentDictionary<string, Type>();

        protected ConcurrentDictionary<string, Type> ChannelGroupSubscribeObjectType
        {
            get;
            set;
        } = new ConcurrentDictionary<string, Type>();

        protected static ConcurrentDictionary<string, PubnubWebRequest> ChannelRequest
        {
            get;
            set;
        } = new ConcurrentDictionary<string, PubnubWebRequest>();

        protected static ConcurrentDictionary<string, bool> ChannelInternetStatus
        {
            get;
            set;
        } = new ConcurrentDictionary<string, bool>();

        protected static ConcurrentDictionary<string, bool> ChannelGroupInternetStatus
        {
            get;
            set;
        } = new ConcurrentDictionary<string, bool>();

        protected static ConcurrentDictionary<string, Dictionary<string, object>> ChannelLocalUserState
        {
            get;
            set;
        } = new ConcurrentDictionary<string, Dictionary<string, object>>();

        protected static ConcurrentDictionary<string, Dictionary<string, object>> ChannelUserState
        {
            get;
            set;
        } = new ConcurrentDictionary<string, Dictionary<string, object>>();

        protected static ConcurrentDictionary<string, Dictionary<string, object>> ChannelGroupLocalUserState
        {
            get;
            set;
        } = new ConcurrentDictionary<string, Dictionary<string, object>>();

        protected static ConcurrentDictionary<string, Dictionary<string, object>> ChannelGroupUserState
        {
            get;
            set;
        } = new ConcurrentDictionary<string, Dictionary<string, object>>();

        public PubnubCoreBase(PNConfiguration pubnubConfiguation)
        {
            if (pubnubConfiguation == null)
            {
                throw new ArgumentException("PNConfiguration missing");
            }

            InternalConstructor(pubnubConfiguation, new NewtonsoftJsonDotNet(), null);
        }

        public PubnubCoreBase(PNConfiguration pubnubConfiguation, IJsonPluggableLibrary jsonPluggableLibrary)
        {
            if (pubnubConfiguation == null)
            {
                throw new ArgumentException("PNConfiguration missing");
            }
            if (jsonPluggableLibrary == null)
            {
                InternalConstructor(pubnubConfiguation, new NewtonsoftJsonDotNet(), null);
            }
            else
            {
                InternalConstructor(pubnubConfiguation, jsonPluggableLibrary, null);
            }
        }

        public PubnubCoreBase(PNConfiguration pubnubConfiguation, IJsonPluggableLibrary jsonPluggableLibrary, IPubnubUnitTest pubnubUnitTest)
        {
            if (pubnubConfiguation == null)
            {
                throw new ArgumentException("PNConfiguration missing");
            }

            if (jsonPluggableLibrary == null)
            {
                InternalConstructor(pubnubConfiguation, new NewtonsoftJsonDotNet(), pubnubUnitTest);
            }
            else
            {
                InternalConstructor(pubnubConfiguation, jsonPluggableLibrary, pubnubUnitTest);
            }
        }

        private void InternalConstructor(PNConfiguration pubnubConfiguation, IJsonPluggableLibrary jsonPluggableLibrary, IPubnubUnitTest pubnubUnitTest)
        {
            pubnubConfig = pubnubConfiguation;
            jsonLib = jsonPluggableLibrary;
            unitTest = pubnubUnitTest;

            Uuid = pubnubConfig.Uuid;

            pubnubHttp = new PubnubHttp(pubnubConfiguation, jsonLib);

            if (pubnubConfig != null && pubnubConfig.PubnubLog != null)
            {
                LoggingMethod.PubnubLog = pubnubConfig.PubnubLog;
            }

#if (SILVERLIGHT || WINDOWS_PHONE)
            HttpWebRequest.RegisterPrefix("https://", WebRequestCreator.ClientHttp);
            HttpWebRequest.RegisterPrefix("http://", WebRequestCreator.ClientHttp);
#endif
        }


        #region "Constructors"

        public static bool IsNullOrWhiteSpace(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return true;
            }

            return string.IsNullOrEmpty(value.Trim());
        }
        #endregion

        #region "Internet connection and Reconnect Network"

        private bool InternetConnectionStatusWithUnitTestCheck(string channel, string channelGroup, Action<PubnubClientError> errorCallback, string[] rawChannels, string[] rawChannelGroups)
        {
            bool networkConnection;
            if (unitTest is IPubnubUnitTest && unitTest.EnableStubTest)
            {
                networkConnection = true;
            }
            else
            {
                networkConnection = InternetConnectionStatus(channel, channelGroup, errorCallback, rawChannels, rawChannelGroups);
                if (!networkConnection)
                {
                    string message = "Network connnect error - Internet connection is not available.";
                    new PNCallbackService(pubnubConfig, jsonLib).CallErrorCallback(PubnubErrorSeverity.Critical, PubnubMessageSource.Client,
                        channel, channelGroup, errorCallback, message,
                        PubnubErrorCode.NoInternet, null, null);
                }
            }

            return networkConnection;
        }

        protected virtual bool InternetConnectionStatus(string channel, string channelGroup, Action<PubnubClientError> errorCallback, string[] rawChannels, string[] rawChannelGroups)
        {
            bool networkConnection;
            networkConnection = ClientNetworkStatus.CheckInternetStatus(pubnetSystemActive, errorCallback, rawChannels, rawChannelGroups);
            return networkConnection;
        }

        protected static void ResetInternetCheckSettings(string[] channels, string[] channelGroups)
        {
            if (channels == null && channelGroups == null)
                return;

            string multiChannel = (channels != null) ? string.Join(",", channels) : "";
            string multiChannelGroup = (channelGroups != null) ? string.Join(",", channelGroups) : "";

            if (multiChannel != "")
            {
                if (ChannelInternetStatus.ContainsKey(multiChannel))
                {
                    ChannelInternetStatus.AddOrUpdate(multiChannel, true, (key, oldValue) => true);
                }
                else
                {
                    ChannelInternetStatus.GetOrAdd(multiChannel, true); //Set to true for internet connection
                }
            }

            if (multiChannelGroup != "")
            {
                if (ChannelGroupInternetStatus.ContainsKey(multiChannelGroup))
                {
                    ChannelGroupInternetStatus.AddOrUpdate(multiChannelGroup, true, (key, oldValue) => true);
                }
                else
                {
                    ChannelGroupInternetStatus.GetOrAdd(multiChannelGroup, true); //Set to true for internet connection
                }
            }
        }

        #endregion

        #region "Callbacks"

        protected static bool CheckInternetConnectionStatus(bool systemActive, Action<PubnubClientError> errorCallback, string[] channels, string[] channelGroups)
        {
            return ClientNetworkStatus.CheckInternetStatus(pubnetSystemActive, errorCallback, channels, channelGroups);
        }

        protected static void OnPubnubLocalClientHeartBeatTimeoutCallback<T>(System.Object heartbeatState)
        {
            RequestState<T> currentState = heartbeatState as RequestState<T>;
            if (currentState != null)
            {
                string channel = (currentState.Channels != null) ? string.Join(",", currentState.Channels) : "";
                string channelGroup = (currentState.ChannelGroups != null) ? string.Join(",", currentState.ChannelGroups) : "";

                if ((ChannelInternetStatus.ContainsKey(channel) || ChannelGroupInternetStatus.ContainsKey(channelGroup))
                        && (currentState.ResponseType == ResponseType.Subscribe || currentState.ResponseType == ResponseType.Presence || currentState.ResponseType == ResponseType.PresenceHeartbeat)
                        && overrideTcpKeepAlive)
                {
                    bool networkConnection;
                    if (unitTest is IPubnubUnitTest && unitTest.EnableStubTest)
                    {
                        networkConnection = true;
                    }
                    else
                    {
                        networkConnection = CheckInternetConnectionStatus(pubnetSystemActive, currentState.ErrorCallback, currentState.Channels, currentState.ChannelGroups);
                    }
                    networkConnection = CheckInternetConnectionStatus(pubnetSystemActive, currentState.ErrorCallback, currentState.Channels, currentState.ChannelGroups);

                    ChannelInternetStatus[channel] = networkConnection;
                    ChannelGroupInternetStatus[channelGroup] = networkConnection;

                    LoggingMethod.WriteToLog(string.Format("DateTime: {0}, OnPubnubLocalClientHeartBeatTimeoutCallback - Internet connection = {1}", DateTime.Now.ToString(), networkConnection), LoggingMethod.LevelVerbose);
                    if (!networkConnection)
                    {
                        TerminatePendingWebRequest(currentState);
                    }
                }
            }
        }

        private static void ResponseToConnectCallback<T>(List<object> result, ResponseType type, string[] channels, string[] channelGroups, Action<ConnectOrDisconnectAck> connectCallback)
        {
            //Check callback exists and make sure previous timetoken = 0
            if (channels != null && channels.Length > 0 && connectCallback != null)
            {
                IEnumerable<string> newChannels = from channel in MultiChannelSubscribe
                                                  where channel.Value == 0
                                                  select channel.Key;
                foreach (string channel in newChannels)
                {
                    string jsonString = "";
                    List<object> connectResult = new List<object>();
                    switch (type)
                    {
                        case ResponseType.Subscribe:
                            jsonString = string.Format("[1, \"Connected\"]");

                            connectResult = jsonLib.DeserializeToListOfObject(jsonString);
                            connectResult.Add("");
                            connectResult.Add(channel);

                            PubnubChannelCallbackKey callbackKey = new PubnubChannelCallbackKey();
                            callbackKey.Channel = channel;
                            callbackKey.ResponseType = type;

                            if (ChannelCallbacks.Count > 0 && ChannelCallbacks.ContainsKey(callbackKey))
                            {
                                PubnubSubscribeChannelCallback<T> currentPubnubCallback = ChannelCallbacks[callbackKey] as PubnubSubscribeChannelCallback<T>;
                                if (currentPubnubCallback != null && currentPubnubCallback.ConnectCallback != null)
                                {
                                    Action<ConnectOrDisconnectAck> targetCallback = currentPubnubCallback.ConnectCallback;
                                    currentPubnubCallback.ConnectCallback = null;
                                    new PNCallbackService(pubnubConfig, jsonLib).GoToCallback<ConnectOrDisconnectAck>(connectResult, targetCallback, true, type);
                                }
                            }
                            break;
                        case ResponseType.Presence:
                            jsonString = string.Format("[1, \"Presence Connected\"]");
                            connectResult = jsonLib.DeserializeToListOfObject(jsonString);
                            connectResult.Add("");
                            connectResult.Add(channel.Replace("-pnpres", ""));

                            PubnubChannelCallbackKey presenceCallbackKey = new PubnubChannelCallbackKey();
                            presenceCallbackKey.Channel = channel;
                            presenceCallbackKey.ResponseType = type;

                            if (ChannelCallbacks.Count > 0 && ChannelCallbacks.ContainsKey(presenceCallbackKey))
                            {
                                PubnubPresenceChannelCallback currentPubnubCallback = ChannelCallbacks[presenceCallbackKey] as PubnubPresenceChannelCallback;
                                if (currentPubnubCallback != null && currentPubnubCallback.ConnectCallback != null)
                                {
                                    Action<ConnectOrDisconnectAck> targetCallback = currentPubnubCallback.ConnectCallback;
                                    currentPubnubCallback.ConnectCallback = null;
                                    new PNCallbackService(pubnubConfig, jsonLib).GoToCallback<ConnectOrDisconnectAck>(connectResult, targetCallback, true, type);
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            if (channelGroups != null && channelGroups.Length > 0 && connectCallback != null)
            {
                IEnumerable<string> newChannelGroups = from channelGroup in MultiChannelGroupSubscribe
                                                       where channelGroup.Value == 0
                                                       select channelGroup.Key;
                foreach (string channelGroup in newChannelGroups)
                {
                    string jsonString = "";
                    List<object> connectResult = new List<object>();
                    switch (type)
                    {
                        case ResponseType.Subscribe:
                            jsonString = string.Format("[1, \"Connected\"]");
                            connectResult = jsonLib.DeserializeToListOfObject(jsonString);
                            connectResult.Add(channelGroup);
                            connectResult.Add("");

                            PubnubChannelGroupCallbackKey callbackKey = new PubnubChannelGroupCallbackKey();
                            callbackKey.ChannelGroup = channelGroup;
                            callbackKey.ResponseType = type;

                            if (ChannelGroupCallbacks.Count > 0 && ChannelGroupCallbacks.ContainsKey(callbackKey))
                            {
                                PubnubSubscribeChannelGroupCallback<T> currentPubnubCallback = ChannelGroupCallbacks[callbackKey] as PubnubSubscribeChannelGroupCallback<T>;
                                if (currentPubnubCallback != null && currentPubnubCallback.ConnectCallback != null)
                                {
                                    Action<ConnectOrDisconnectAck> targetCallback = currentPubnubCallback.ConnectCallback;
                                    currentPubnubCallback.ConnectCallback = null;
                                    new PNCallbackService(pubnubConfig, jsonLib).GoToCallback<ConnectOrDisconnectAck>(connectResult, targetCallback, true, type);
                                }

                            }
                            break;
                        case ResponseType.Presence:
                            jsonString = string.Format("[1, \"Presence Connected\"]");
                            connectResult = jsonLib.DeserializeToListOfObject(jsonString);
                            connectResult.Add(channelGroup.Replace("-pnpres", ""));
                            connectResult.Add("");

                            PubnubChannelGroupCallbackKey presenceCallbackKey = new PubnubChannelGroupCallbackKey();
                            presenceCallbackKey.ChannelGroup = channelGroup;
                            presenceCallbackKey.ResponseType = type;

                            if (ChannelGroupCallbacks.Count > 0 && ChannelGroupCallbacks.ContainsKey(presenceCallbackKey))
                            {
                                PubnubPresenceChannelGroupCallback currentPubnubCallback = ChannelGroupCallbacks[presenceCallbackKey] as PubnubPresenceChannelGroupCallback;
                                if (currentPubnubCallback != null && currentPubnubCallback.ConnectCallback != null)
                                {
                                    Action<ConnectOrDisconnectAck> targetCallback = currentPubnubCallback.ConnectCallback;
                                    currentPubnubCallback.ConnectCallback = null;
                                    new PNCallbackService(pubnubConfig, jsonLib).GoToCallback<ConnectOrDisconnectAck>(connectResult, targetCallback, true, type);
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private static void ResponseToUserCallback<T>(List<object> result, ResponseType type, string[] channels, string[] channelGroups, Action<T> userCallback)
        {
            string[] messageChannels = null;
            string[] messageChannelGroups = null;
            string[] messageWildcardPresenceChannels = null;
            switch (type)
            {
                case ResponseType.Subscribe:
                case ResponseType.Presence:
                    var messages = (from item in result
                                    select item as object).ToArray();
                    if (messages != null && messages.Length > 0)
                    {
                        object[] messageList = messages[0] as object[];
                        if (messageList != null && messageList.Length > 0)
                        {
                            if (messages.Length == 4 || messages.Length == 6)
                            {
                                messageChannelGroups = messages[2].ToString().Split(',');
                                messageChannels = messages[3].ToString().Split(',');
                            }
                            else
                            {
                                messageChannels = messages[2].ToString().Split(',');
                                messageChannelGroups = null;
                            }
                            for (int messageIndex = 0; messageIndex < messageList.Length; messageIndex++)
                            {
                                string currentChannel = (messageChannels.Length == 1) ? (string)messageChannels[0] : (string)messageChannels[messageIndex];
                                string currentChannelGroup = "";
                                if (messageChannelGroups != null && messageChannelGroups.Length > 0)
                                {
                                    currentChannelGroup = (messageChannelGroups.Length == 1) ? (string)messageChannelGroups[0] : (string)messageChannelGroups[messageIndex];
                                }
                                List<object> itemMessage = new List<object>();
                                if (currentChannel.Contains(".*-pnpres"))
                                {
                                    itemMessage.Add(messageList[messageIndex]);
                                }
                                else if (currentChannel.Contains("-pnpres"))
                                {
                                    itemMessage.Add(messageList[messageIndex]);
                                }
                                else
                                {
                                    if (pubnubConfig.CiperKey.Length > 0) //decrypt the subscriber message if cipherkey is available
                                    {
                                        PubnubCrypto aes = new PubnubCrypto(pubnubConfig.CiperKey);
                                        string decryptMessage = aes.Decrypt(messageList[messageIndex].ToString());
                                        object decodeMessage = (decryptMessage == "**DECRYPT ERROR**") ? decryptMessage : jsonLib.DeserializeToObject(decryptMessage);

                                        itemMessage.Add(decodeMessage);
                                    }
                                    else
                                    {
                                        itemMessage.Add(messageList[messageIndex]);
                                    }
                                }
                                itemMessage.Add(messages[1].ToString());

                                //if (messageWildcardPresenceChannels != null)
                                //{
                                //    string wildPresenceChannel = (messageWildcardPresenceChannels.Length == 1) ? (string)messageWildcardPresenceChannels[0] : (string)messageWildcardPresenceChannels[messageIndex];
                                //    itemMessage.Add(wildPresenceChannel);
                                //}

                                if (currentChannel == currentChannelGroup)
                                {
                                    itemMessage.Add(currentChannel.Replace("-pnpres", ""));
                                }
                                else
                                {
                                    if (currentChannelGroup != "")
                                    {
                                        itemMessage.Add(currentChannelGroup.Replace("-pnpres", ""));
                                    }
                                    if (currentChannel != "")
                                    {
                                        itemMessage.Add(currentChannel.Replace("-pnpres", ""));
                                    }
                                }

                                PubnubChannelCallbackKey callbackKey = new PubnubChannelCallbackKey();

                                if (!string.IsNullOrEmpty(currentChannelGroup) && currentChannelGroup.Contains(".*"))
                                {
                                    callbackKey.Channel = currentChannelGroup;
                                    callbackKey.ResponseType = ResponseType.Subscribe;
                                }
                                else
                                {
                                    callbackKey.Channel = currentChannel;
                                    callbackKey.ResponseType = (currentChannel.LastIndexOf("-pnpres") == -1) ? ResponseType.Subscribe : ResponseType.Presence;
                                }

                                if (ChannelCallbacks.Count > 0 && ChannelCallbacks.ContainsKey(callbackKey))
                                {
                                    //TODO: PANDU REFACTOR REPEAT LOGIC
                                    if (callbackKey.ResponseType == ResponseType.Presence)
                                    {
                                        PubnubPresenceChannelCallback currentPubnubCallback = ChannelCallbacks[callbackKey] as PubnubPresenceChannelCallback;
                                        if (currentPubnubCallback != null)
                                        {
                                            if (currentPubnubCallback.PresenceRegularCallback != null)
                                            {
                                                new PNCallbackService(pubnubConfig, jsonLib).GoToCallback(itemMessage, currentPubnubCallback.PresenceRegularCallback, true, type);
                                            }
                                        }

                                    }
                                    else
                                    {
                                        PubnubSubscribeChannelCallback<T> currentPubnubCallback = ChannelCallbacks[callbackKey] as PubnubSubscribeChannelCallback<T>;
                                        //object pubnubSubscribeCallbackObject = channelCallbacks[callbackKey];
                                        //if (pubnubSubscribeCallbackObject is PubnubSubscribeChannelCallback<string>)
                                        //{
                                        //    currentPubnubCallback = pubnubSubscribeCallbackObject as PubnubSubscribeChannelCallback<string>;
                                        //}
                                        //else if (pubnubSubscribeCallbackObject is PubnubSubscribeChannelCallback<object>)
                                        //{
                                        //    currentPubnubCallback = pubnubSubscribeCallbackObject as PubnubSubscribeChannelCallback<object>;
                                        //}
                                        //else
                                        //{
                                        //    Type targetType = _channelSubscribeObjectType[currentChannel];

                                        //    if (_subscribeMessageType != null)
                                        //    {
                                        //        currentPubnubCallback = _subscribeMessageType.GetSubscribeMessageType(targetType, pubnubSubscribeCallbackObject, false);
                                        //    }
                                        //    else
                                        //    {
                                        //        currentPubnubCallback = null;
                                        //    }
                                        //}

                                        if (currentPubnubCallback != null)
                                        {
                                            if (itemMessage.Count >= 4 && currentChannel.Contains(".*") && currentChannel.Contains("-pnpres"))
                                            {
                                                if (currentPubnubCallback.WildcardPresenceCallback != null)
                                                {
                                                    new PNCallbackService(pubnubConfig, jsonLib).GoToCallback(itemMessage, currentPubnubCallback.WildcardPresenceCallback, true, type);
                                                }
                                            }
                                            else
                                            {
                                                if (currentPubnubCallback.SubscribeRegularCallback != null)
                                                {
                                                    new PNCallbackService(pubnubConfig, jsonLib).GoToCallback<Message<T>>(itemMessage, currentPubnubCallback.SubscribeRegularCallback, false, type);
                                                }
                                            }
                                        }
                                    }
                                }

                                PubnubChannelGroupCallbackKey callbackGroupKey = new PubnubChannelGroupCallbackKey();
                                callbackGroupKey.ChannelGroup = currentChannelGroup;
                                callbackGroupKey.ResponseType = (currentChannelGroup.LastIndexOf("-pnpres") == -1) ? ResponseType.Subscribe : ResponseType.Presence;

                                if (ChannelGroupCallbacks.Count > 0 && ChannelGroupCallbacks.ContainsKey(callbackGroupKey))
                                {
                                    if (callbackGroupKey.ResponseType == ResponseType.Presence)
                                    {
                                        PubnubPresenceChannelGroupCallback currentPubnubCallback = ChannelGroupCallbacks[callbackGroupKey] as PubnubPresenceChannelGroupCallback;
                                        if (currentPubnubCallback != null)
                                        {
                                            if (itemMessage.Count >= 4 && currentChannelGroup.Contains(".*") && currentChannel.Contains("-pnpres"))
                                            {
                                                //if (currentPubnubCallback.WildcardPresenceCallback != null)
                                                //{
                                                //    GoToCallback(itemMessage, currentPubnubCallback.WildcardPresenceCallback);
                                                //}
                                            }
                                            else
                                            {
                                                if (currentPubnubCallback.PresenceRegularCallback != null)
                                                {
                                                    new PNCallbackService(pubnubConfig, jsonLib).GoToCallback(itemMessage, currentPubnubCallback.PresenceRegularCallback, true, type);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        PubnubSubscribeChannelGroupCallback<T> currentPubnubCallback = ChannelGroupCallbacks[callbackGroupKey] as PubnubSubscribeChannelGroupCallback<T>;
                                        //dynamic currentPubnubCallback;
                                        //object pubnubSubscribeCallbackObject = channelCallbacks[callbackKey];
                                        //if (pubnubSubscribeCallbackObject is PubnubSubscribeChannelGroupCallback<string>)
                                        //{
                                        //    currentPubnubCallback = pubnubSubscribeCallbackObject as PubnubSubscribeChannelGroupCallback<string>;
                                        //}
                                        //else if (pubnubSubscribeCallbackObject is PubnubSubscribeChannelGroupCallback<object>)
                                        //{
                                        //    currentPubnubCallback = pubnubSubscribeCallbackObject as PubnubSubscribeChannelGroupCallback<object>;
                                        //}
                                        //else
                                        //{
                                        //    Type targetType = _channelGroupSubscribeObjectType[currentChannelGroup];

                                        //    if (_subscribeMessageType != null)
                                        //    {
                                        //        currentPubnubCallback = _subscribeMessageType.GetSubscribeMessageType(targetType, pubnubSubscribeCallbackObject, true);
                                        //    }
                                        //    else
                                        //    {
                                        //        currentPubnubCallback = null;
                                        //    }
                                        //}

                                        if (currentPubnubCallback != null)
                                        {
                                            if (itemMessage.Count >= 4 && currentChannelGroup.Contains(".*") && currentChannel.Contains("-pnpres"))
                                            {
                                                if (currentPubnubCallback.WildcardPresenceCallback != null)
                                                {
                                                    new PNCallbackService(pubnubConfig, jsonLib).GoToCallback(itemMessage, currentPubnubCallback.WildcardPresenceCallback, true, type);
                                                }
                                            }
                                            else
                                            {
                                                if (currentPubnubCallback.SubscribeRegularCallback != null)
                                                {
                                                    new PNCallbackService(pubnubConfig, jsonLib).GoToCallback(itemMessage, currentPubnubCallback.SubscribeRegularCallback, false, type);
                                                }
                                            }
                                        }
                                    }
                                }

                            }
                        }
                    }
                    break;
                case ResponseType.Time:
                    if (result != null && result.Count > 0)
                    {
                        new PNCallbackService(pubnubConfig, jsonLib).GoToCallback<T>(result, userCallback, true, type);
                    }
                    break;
                case ResponseType.Publish:
                    if (result != null && result.Count > 0)
                    {
                        new PNCallbackService(pubnubConfig, jsonLib).GoToCallback<T>(result, userCallback, true, type);
                    }
                    break;
                case ResponseType.DetailedHistory:
                    if (result != null && result.Count > 0)
                    {
                        new PNCallbackService(pubnubConfig, jsonLib).GoToCallback<T>(result, userCallback, true, type);
                    }
                    break;
                case ResponseType.Here_Now:
                    if (result != null && result.Count > 0)
                    {
                        new PNCallbackService(pubnubConfig, jsonLib).GoToCallback<T>(result, userCallback, true, type);
                    }
                    break;
                case ResponseType.GlobalHere_Now:
                    if (result != null && result.Count > 0)
                    {
                        new PNCallbackService(pubnubConfig, jsonLib).GoToCallback<T>(result, userCallback, true, type);
                    }
                    break;
                case ResponseType.Where_Now:
                    if (result != null && result.Count > 0)
                    {
                        new PNCallbackService(pubnubConfig, jsonLib).GoToCallback<T>(result, userCallback, true, type);
                    }
                    break;
                case ResponseType.GrantAccess:
                case ResponseType.AuditAccess:
                case ResponseType.RevokeAccess:
                case ResponseType.ChannelGroupGrantAccess:
                case ResponseType.ChannelGroupAuditAccess:
                case ResponseType.ChannelGroupRevokeAccess:
                case ResponseType.GetUserState:
                case ResponseType.SetUserState:
                    if (result != null && result.Count > 0)
                    {
                        new PNCallbackService(pubnubConfig, jsonLib).GoToCallback<T>(result, userCallback, true, type);
                    }
                    break;
                case ResponseType.PushRegister:
                case ResponseType.PushRemove:
                case ResponseType.PushGet:
                case ResponseType.PushUnregister:
                    if (result != null && result.Count > 0)
                    {
                        new PNCallbackService(pubnubConfig, jsonLib).GoToCallback<T>(result, userCallback, true, type);
                    }
                    break;
                case ResponseType.ChannelGroupAdd:
                case ResponseType.ChannelGroupRemove:
                case ResponseType.ChannelGroupGet:
                    if (result != null && result.Count > 0)
                    {
                        new PNCallbackService(pubnubConfig, jsonLib).GoToCallback<T>(result, userCallback, true, type);
                    }
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region "Simulate network fail and machine sleep"

        /// <summary>
        /// FOR TESTING ONLY - To Enable Simulation of Network Non-Availability
        /// </summary>
        public static void EnableSimulateNetworkFailForTestingOnly()
        {
            ClientNetworkStatus.SimulateNetworkFailForTesting = true;
            PubnubWebRequest.SimulateNetworkFailForTesting = true;
        }

        /// <summary>
        /// FOR TESTING ONLY - To Disable Simulation of Network Non-Availability
        /// </summary>
        public static void DisableSimulateNetworkFailForTestingOnly()
        {
            ClientNetworkStatus.SimulateNetworkFailForTesting = false;
            PubnubWebRequest.SimulateNetworkFailForTesting = false;
        }

        public static void EnableMachineSleepModeForTestingOnly()
        {
            pubnetSystemActive = false;
        }

        public static void DisableMachineSleepModeForTestingOnly()
        {
            pubnetSystemActive = true;
        }

        #endregion

        #region "Helpers"

        public static long TranslateDateTimeToSeconds(DateTime dotNetUTCDateTime)
        {
            TimeSpan timeSpan = dotNetUTCDateTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            long timeStamp = Convert.ToInt64(timeSpan.TotalSeconds);
            return timeStamp;
        }

        /// <summary>
        /// Convert the UTC/GMT DateTime to Unix Nano Seconds format
        /// </summary>
        /// <param name="dotNetUTCDateTime"></param>
        /// <returns></returns>
        public static long TranslateDateTimeToPubnubUnixNanoSeconds(DateTime dotNetUTCDateTime)
        {
            TimeSpan timeSpan = dotNetUTCDateTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            long timeStamp = Convert.ToInt64(timeSpan.TotalSeconds) * 10000000;
            return timeStamp;
        }

        /// <summary>
        /// Convert the Unix Nano Seconds format time to UTC/GMT DateTime
        /// </summary>
        /// <param name="unixNanoSecondTime"></param>
        /// <returns></returns>
        public static DateTime TranslatePubnubUnixNanoSecondsToDateTime(long unixNanoSecondTime)
        {
            try
            {
                double timeStamp = unixNanoSecondTime / 10000000;
                DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timeStamp);
                return dateTime;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        public static DateTime TranslatePubnubUnixNanoSecondsToDateTime(string unixNanoSecondTime)
        {
            long numericTime;
            bool tried = Int64.TryParse(unixNanoSecondTime, out numericTime);
            if (tried)
            {
                try
                {
                    double timeStamp = numericTime / 10000000;
                    DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timeStamp);
                    return dateTime;
                }
                catch
                {
                    return DateTime.MinValue;
                }
            }
            else
            {
                return DateTime.MinValue;
            }
        }

        protected bool IsPresenceChannel(string channel)
        {
            if (channel.LastIndexOf("-pnpres") > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected static string[] GetCurrentSubscriberChannels()
        {
            string[] channels = null;
            if (MultiChannelSubscribe != null && MultiChannelSubscribe.Keys.Count > 0)
            {
                channels = MultiChannelSubscribe.Keys.ToArray<string>();
            }

            return channels;
        }

        protected string[] GetCurrentSubscriberChannelGroups()
        {
            string[] channelGroups = null;
            if (MultiChannelGroupSubscribe != null && MultiChannelGroupSubscribe.Keys.Count > 0)
            {
                channelGroups = MultiChannelGroupSubscribe.Keys.ToArray<string>();
            }

            return channelGroups;
        }
        #endregion

        #region "Build, process and send request"

        internal protected static string UrlProcessRequest<T>(Uri requestUri, RequestState<T> pubnubRequestState, bool terminateCurrentSubRequest)
        {
            string channel = "";
            string channelGroup = "";

            if (terminateCurrentSubRequest)
            {
                TerminateCurrentSubscriberRequest();
            }

            if (pubnubRequestState != null)
            {
                if (pubnubRequestState.Channels != null)
                {
                    channel = (pubnubRequestState.Channels.Length > 0) ? string.Join(",", pubnubRequestState.Channels) : ",";
                }
                if (pubnubRequestState.ChannelGroups != null)
                {
                    channelGroup = string.Join(",", pubnubRequestState.ChannelGroups);
                }
            }

            try
            {
                if (!ChannelRequest.ContainsKey(channel) && (pubnubRequestState.ResponseType == ResponseType.Subscribe || pubnubRequestState.ResponseType == ResponseType.Presence))
                {
                    return "";
                }

                // Create Request
                PubnubWebRequestCreator requestCreator = new PubnubWebRequestCreator(unitTest);
                PubnubWebRequest request = (PubnubWebRequest)requestCreator.Create(requestUri);

                request = pubnubHttp.SetProxy<T>(request);
                request = pubnubHttp.SetTimeout<T>(pubnubRequestState, request);

                pubnubRequestState.Request = request;

                if (pubnubRequestState.ResponseType == ResponseType.Subscribe || pubnubRequestState.ResponseType == ResponseType.Presence)
                {
                    ChannelRequest.AddOrUpdate(channel, pubnubRequestState.Request, (key, oldState) => pubnubRequestState.Request);
                }

                if (overrideTcpKeepAlive) //overrideTcpKeepAlive must be true
                {
                    //Eventhough heart-beat is disabled, run one time to check internet connection by setting dueTime=0
                    if (localClientHeartBeatTimer != null)
                    {
                        try
                        {
                            localClientHeartBeatTimer.Dispose();
                        }
                        catch { }
                    }
                    localClientHeartBeatTimer = new System.Threading.Timer(
                        new TimerCallback(OnPubnubLocalClientHeartBeatTimeoutCallback<T>), pubnubRequestState, 0,
                        (-1 == pubnubNetworkTcpCheckIntervalInSeconds) ? Timeout.Infinite : pubnubNetworkTcpCheckIntervalInSeconds * 1000);
                    channelLocalClientHeartbeatTimer.AddOrUpdate(requestUri, localClientHeartBeatTimer, (key, oldState) => localClientHeartBeatTimer);
                }
                else
                {
                    request = pubnubHttp.SetServicePointSetTcpKeepAlive(request);
                }

                LoggingMethod.WriteToLog(string.Format("DateTime {0}, Request={1}", DateTime.Now.ToString(), requestUri.ToString()), LoggingMethod.LevelInfo);

                Task<string> jsonResponse = pubnubHttp.SendRequestAndGetJsonResponse(requestUri, pubnubRequestState, request);

                string jsonString = jsonResponse.Result;

                LoggingMethod.WriteToLog(string.Format("DateTime {0}, JSON= {1} for request={2}", DateTime.Now.ToString(), jsonString, requestUri), LoggingMethod.LevelInfo);
                return jsonString;
            }
            catch (Exception ex)
            {
                string exceptionMessage = "";
                Exception innerEx = null;
                WebException webEx = null;

                if (ex.InnerException != null)
                {
                    if (ex is WebException)
                    {
                        webEx = ex as WebException;
                        exceptionMessage = webEx.Message;
                    }
                    else
                    {
                        innerEx = ex.InnerException;
                        exceptionMessage = innerEx.Message;
                    }
                }
                else
                {
                    innerEx = ex;
                    exceptionMessage = innerEx.Message;
                }

                if (exceptionMessage.IndexOf("The request was aborted: The request was canceled") == -1
                && exceptionMessage.IndexOf("Machine suspend mode enabled. No request will be processed.") == -1)
                {
                    if (pubnubRequestState != null && pubnubRequestState.ErrorCallback != null)
                    {
                        string multiChannel = (pubnubRequestState.Channels != null) ? string.Join(",", pubnubRequestState.Channels) : "";
                        string multiChannelGroup = (pubnubRequestState.ChannelGroups != null) ? string.Join(",", pubnubRequestState.ChannelGroups) : "";

                        new PNCallbackService(pubnubConfig, jsonLib).CallErrorCallback(PubnubErrorSeverity.Critical, PubnubMessageSource.Client,
                            multiChannel, multiChannelGroup, pubnubRequestState.ErrorCallback, (webEx != null) ? webEx : innerEx, pubnubRequestState.Request, pubnubRequestState.Response);
                    }
                    LoggingMethod.WriteToLog(string.Format("DateTime {0} PubnubBaseCore UrlProcessRequest Exception={1}", DateTime.Now.ToString(), webEx.ToString()), LoggingMethod.LevelError);
                }

                return "";
            }
        }

        internal protected static List<object> ProcessJsonResponse<T>(RequestState<T> asyncRequestState, string jsonString)
        {
            List<object> result = new List<object>();

            string channel = "";
            string channelGroup = "";
            if (asyncRequestState != null)
            {
                if (asyncRequestState.Channels != null)
                {
                    channel = (asyncRequestState.Channels.Length > 0) ? string.Join(",", asyncRequestState.Channels) : ",";
                }
                if (asyncRequestState.ChannelGroups != null)
                {
                    channelGroup = string.Join(",", asyncRequestState.ChannelGroups);
                }
            }

            bool errorCallbackRaised = false;
            if (jsonLib.IsDictionaryCompatible(jsonString))
            {
                Dictionary<string, object> deserializeStatus = jsonLib.DeserializeToDictionaryOfObject(jsonString);
                int statusCode = 0; //default. assuming all is ok 
                if (deserializeStatus.ContainsKey("status") && deserializeStatus.ContainsKey("message"))
                {
                    Int32.TryParse(deserializeStatus["status"].ToString(), out statusCode);
                    string statusMessage = deserializeStatus["message"].ToString();

                    if (statusCode != 200)
                    {
                        PubnubErrorCode pubnubErrorType = PubnubErrorCodeHelper.GetErrorType(statusCode, statusMessage);
                        int pubnubStatusCode = (int)pubnubErrorType;
                        string errorDescription = PubnubErrorCodeDescription.GetStatusCodeDescription(pubnubErrorType);

                        PubnubClientError error = new PubnubClientError(pubnubStatusCode, PubnubErrorSeverity.Critical, statusMessage, PubnubMessageSource.Server, asyncRequestState.Request, asyncRequestState.Response, errorDescription, channel, channelGroup);
                        errorCallbackRaised = true;
                        new PNCallbackService(pubnubConfig, jsonLib).GoToCallback(error, asyncRequestState.ErrorCallback);
                    }
                }
            }
            if (!errorCallbackRaised)
            {
                result = WrapResultBasedOnResponseType<T>(asyncRequestState.ResponseType, jsonString, asyncRequestState.Channels, asyncRequestState.ChannelGroups, asyncRequestState.Reconnect, asyncRequestState.Timetoken, asyncRequestState.Request, asyncRequestState.ErrorCallback);
            }

            return result;
        }

        protected static List<object> WrapResultBasedOnResponseType<T>(ResponseType type, string jsonString, string[] channels, string[] channelGroups, bool reconnect, long lastTimetoken, PubnubWebRequest request, Action<PubnubClientError> errorCallback)
        {
            List<object> result = new List<object>();

            try
            {
                string multiChannel = (channels != null) ? string.Join(",", channels) : "";
                string multiChannelGroup = (channelGroups != null) ? string.Join(",", channelGroups) : "";

                if (!string.IsNullOrEmpty(jsonString))
                {
                    if (!string.IsNullOrEmpty(jsonString))
                    {
                        object deserializedResult = jsonLib.DeserializeToObject(jsonString);
                        List<object> result1 = ((IEnumerable)deserializedResult).Cast<object>().ToList();

                        if (result1 != null && result1.Count > 0)
                        {
                            result = result1;
                        }

                        switch (type)
                        {
                            case ResponseType.Subscribe:
                            case ResponseType.Presence:
                                if (result.Count == 3 && result[0] is object[] && (result[0] as object[]).Length == 0 && result[2].ToString() == "")
                                {
                                    result.RemoveAt(2);
                                }
                                if (result.Count == 4 && result[0] is object[] && (result[0] as object[]).Length == 0 && result[2].ToString() == "" && result[3].ToString() == "")
                                {
                                    result.RemoveRange(2, 2);
                                }
                                result.Add(multiChannelGroup);
                                result.Add(multiChannel);

                                long receivedTimetoken = (result.Count > 1 && result[1].ToString() != "") ? Convert.ToInt64(result[1].ToString()) : 0;

                                long minimumTimetoken1 = (MultiChannelSubscribe.Count > 0) ? MultiChannelSubscribe.Min(token => token.Value) : 0;
                                long minimumTimetoken2 = (MultiChannelGroupSubscribe.Count > 0) ? MultiChannelGroupSubscribe.Min(token => token.Value) : 0;
                                long minimumTimetoken = Math.Max(minimumTimetoken1, minimumTimetoken2);

                                long maximumTimetoken1 = (MultiChannelSubscribe.Count > 0) ? MultiChannelSubscribe.Max(token => token.Value) : 0;
                                long maximumTimetoken2 = (MultiChannelGroupSubscribe.Count > 0) ? MultiChannelGroupSubscribe.Max(token => token.Value) : 0;
                                long maximumTimetoken = Math.Max(maximumTimetoken1, maximumTimetoken2);

                                if (minimumTimetoken == 0 || lastTimetoken == 0)
                                {
                                    if (maximumTimetoken == 0)
                                    {
                                        LastSubscribeTimetoken = receivedTimetoken;
                                    }
                                    else
                                    {
                                        if (!_enableResumeOnReconnect)
                                        {
                                            LastSubscribeTimetoken = receivedTimetoken;
                                        }
                                        else
                                        {
                                            //do nothing. keep last subscribe token
                                        }
                                    }
                                }
                                else
                                {
                                    if (reconnect)
                                    {
                                        if (_enableResumeOnReconnect)
                                        {
                                            //do nothing. keep last subscribe token
                                        }
                                        else
                                        {
                                            LastSubscribeTimetoken = receivedTimetoken;
                                        }
                                    }
                                    else
                                    {
                                        LastSubscribeTimetoken = receivedTimetoken;
                                    }
                                }
                                break;
                            case ResponseType.Leave:
                                result.Add(multiChannel);
                                break;
                            case ResponseType.Time:
                                break;
                            case ResponseType.Publish:
                                #region "Publish"
                                result.Add(multiChannel);
                                if (pubnubConfig.AddPayloadToPublishResponse && request != null & request.RequestUri != null)
                                {
                                    Uri webUri = request.RequestUri;
                                    string absolutePath = webUri.AbsolutePath.ToString();
                                    int posLastSlash = absolutePath.LastIndexOf("/");
                                    if (posLastSlash > 1)
                                    {
                                        bool stringType = false;
                                        string publishPayload = absolutePath.Substring(posLastSlash + 1);
                                        int posOfStartDQ = publishPayload.IndexOf("%22");
                                        int posOfEndDQ = publishPayload.LastIndexOf("%22");
                                        if (posOfStartDQ == 0 && posOfEndDQ + 3 == publishPayload.Length)
                                        {
                                            publishPayload = publishPayload.Remove(posOfEndDQ).Remove(posOfStartDQ, 3);
                                            stringType = true;
                                        }
                                        string publishMsg = System.Uri.UnescapeDataString(publishPayload);

                                        double doubleData;
                                        int intData;
                                        if (!stringType && int.TryParse(publishMsg, out intData)) //capture numeric data
                                        {
                                            result.Add(intData);
                                        }
                                        else if (!stringType && double.TryParse(publishMsg, out doubleData)) //capture numeric data
                                        {
                                            result.Add(doubleData);
                                        }
                                        else
                                        {
                                            result.Add(publishMsg);
                                        }
                                    }
                                }
                                #endregion
                                break;
                            case ResponseType.DetailedHistory:
                                result = SecureMessage.Instance(pubnubConfig, jsonLib).DecodeDecryptLoop(result, channels, channelGroups, errorCallback);
                                result.Add(multiChannel);
                                break;
                            case ResponseType.Here_Now:
                                Dictionary<string, object> dictionary = jsonLib.DeserializeToDictionaryOfObject(jsonString);
                                result = new List<object>();
                                result.Add(dictionary);
                                result.Add(multiChannel);
                                break;
                            case ResponseType.GlobalHere_Now:
                                Dictionary<string, object> globalHereNowDictionary = jsonLib.DeserializeToDictionaryOfObject(jsonString);
                                result = new List<object>();
                                result.Add(globalHereNowDictionary);
                                break;
                            case ResponseType.Where_Now:
                                Dictionary<string, object> whereNowDictionary = jsonLib.DeserializeToDictionaryOfObject(jsonString);
                                result = new List<object>();
                                result.Add(whereNowDictionary);
                                result.Add(multiChannel);
                                break;
                            case ResponseType.GrantAccess:
                            case ResponseType.AuditAccess:
                            case ResponseType.RevokeAccess:
                                Dictionary<string, object> grantDictionary = jsonLib.DeserializeToDictionaryOfObject(jsonString);
                                result = new List<object>();
                                result.Add(grantDictionary);
                                result.Add(multiChannel);
                                break;
                            case ResponseType.ChannelGroupGrantAccess:
                            case ResponseType.ChannelGroupAuditAccess:
                            case ResponseType.ChannelGroupRevokeAccess:
                                Dictionary<string, object> channelGroupPAMDictionary = jsonLib.DeserializeToDictionaryOfObject(jsonString);
                                result = new List<object>();
                                result.Add(channelGroupPAMDictionary);
                                result.Add(multiChannelGroup);
                                break;
                            case ResponseType.GetUserState:
                            case ResponseType.SetUserState:
                                Dictionary<string, object> userStateDictionary = jsonLib.DeserializeToDictionaryOfObject(jsonString);
                                result = new List<object>();
                                result.Add(userStateDictionary);
                                result.Add(multiChannelGroup);
                                result.Add(multiChannel);
                                break;
                            case ResponseType.PushRegister:
                            case ResponseType.PushRemove:
                            case ResponseType.PushGet:
                            case ResponseType.PushUnregister:
                                result.Add(multiChannel);
                                break;
                            case ResponseType.ChannelGroupAdd:
                            case ResponseType.ChannelGroupRemove:
                            case ResponseType.ChannelGroupGet:
                                Dictionary<string, object> channelGroupDictionary = jsonLib.DeserializeToDictionaryOfObject(jsonString);
                                result = new List<object>();
                                result.Add(channelGroupDictionary);
                                if (multiChannelGroup != "")
                                {
                                    result.Add(multiChannelGroup);
                                }
                                if (multiChannel != "")
                                {
                                    result.Add(multiChannel);
                                }
                                break;
                            default:
                                break;
                        }
                        //switch stmt end
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return result;
        }

        protected static void ProcessResponseCallbacks<T>(List<object> result, RequestState<T> asyncRequestState)
        {
            bool callbackAvailable = false;
            if (result != null && result.Count >= 1)
            {
                if (asyncRequestState.SubscribeRegularCallback != null || asyncRequestState.PresenceRegularCallback != null || asyncRequestState.NonSubscribeRegularCallback != null)
                {
                    callbackAvailable = true;
                }
                else
                {
                    if (asyncRequestState.ResponseType == ResponseType.Subscribe || asyncRequestState.ResponseType == ResponseType.Presence)
                    {
                        if (asyncRequestState.Channels != null && asyncRequestState.Channels.Length > 0)
                        {
                            List<string> channelList = asyncRequestState.Channels.ToList();
                            foreach (string ch in channelList)
                            {
                                PubnubChannelCallbackKey callbackKey = new PubnubChannelCallbackKey();
                                callbackKey.Channel = ch;
                                callbackKey.ResponseType = asyncRequestState.ResponseType;

                                if (ChannelCallbacks.Count > 0 && ChannelCallbacks.ContainsKey(callbackKey))
                                {
                                    callbackAvailable = true;
                                    break;
                                }
                            }
                        }
                        if (!callbackAvailable && asyncRequestState.ChannelGroups != null && asyncRequestState.ChannelGroups.Length > 0)
                        {
                            List<string> channelGroupList = asyncRequestState.ChannelGroups.ToList();
                            foreach (string cg in channelGroupList)
                            {
                                PubnubChannelGroupCallbackKey callbackKey = new PubnubChannelGroupCallbackKey();
                                callbackKey.ChannelGroup = cg;
                                callbackKey.ResponseType = asyncRequestState.ResponseType;

                                if (ChannelGroupCallbacks.Count > 0 && ChannelGroupCallbacks.ContainsKey(callbackKey))
                                {
                                    callbackAvailable = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            if (callbackAvailable)
            {
                ResponseToConnectCallback<T>(result, asyncRequestState.ResponseType, asyncRequestState.Channels, asyncRequestState.ChannelGroups, asyncRequestState.ConnectCallback);
                ResponseToUserCallback<T>(result, asyncRequestState.ResponseType, asyncRequestState.Channels, asyncRequestState.ChannelGroups, asyncRequestState.NonSubscribeRegularCallback);
            }
        }

        #endregion

        protected static string BuildJsonUserState(string channel, string channelGroup, bool local)
        {
            Dictionary<string, object> channelUserStateDictionary = null;
            Dictionary<string, object> channelGroupUserStateDictionary = null;

            if (!string.IsNullOrEmpty(channel) && !string.IsNullOrEmpty(channelGroup))
            {
                throw new ArgumentException("BuildJsonUserState takes either channel or channelGroup at one time. Send one at a time by passing empty value for other.");
            }

            if (local)
            {
                if (!string.IsNullOrEmpty(channel) && ChannelLocalUserState.ContainsKey(channel))
                {
                    channelUserStateDictionary = ChannelLocalUserState[channel];
                }
                if (!string.IsNullOrEmpty(channelGroup) && ChannelGroupLocalUserState.ContainsKey(channelGroup))
                {
                    channelGroupUserStateDictionary = ChannelGroupLocalUserState[channelGroup];
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(channel) && ChannelUserState.ContainsKey(channel))
                {
                    channelUserStateDictionary = ChannelUserState[channel];
                }
                if (!string.IsNullOrEmpty(channelGroup) && ChannelGroupUserState.ContainsKey(channelGroup))
                {
                    channelGroupUserStateDictionary = ChannelGroupUserState[channelGroup];
                }
            }

            StringBuilder jsonStateBuilder = new StringBuilder();

            if (channelUserStateDictionary != null)
            {
                string[] channelUserStateKeys = channelUserStateDictionary.Keys.ToArray<string>();

                for (int keyIndex = 0; keyIndex < channelUserStateKeys.Length; keyIndex++)
                {
                    string channelUserStateKey = channelUserStateKeys[keyIndex];
                    object channelUserStateValue = channelUserStateDictionary[channelUserStateKey];
                    if (channelUserStateValue == null)
                    {
                        jsonStateBuilder.AppendFormat("\"{0}\":{1}", channelUserStateKey, string.Format("\"{0}\"", "null"));
                    }
                    else
                    {
                        jsonStateBuilder.AppendFormat("\"{0}\":{1}", channelUserStateKey, (channelUserStateValue.GetType().ToString() == "System.String") ? string.Format("\"{0}\"", channelUserStateValue) : channelUserStateValue);
                    }
                    if (keyIndex < channelUserStateKeys.Length - 1)
                    {
                        jsonStateBuilder.Append(",");
                    }
                }
            }
            if (channelGroupUserStateDictionary != null)
            {
                string[] channelGroupUserStateKeys = channelGroupUserStateDictionary.Keys.ToArray<string>();

                for (int keyIndex = 0; keyIndex < channelGroupUserStateKeys.Length; keyIndex++)
                {
                    string channelGroupUserStateKey = channelGroupUserStateKeys[keyIndex];
                    object channelGroupUserStateValue = channelGroupUserStateDictionary[channelGroupUserStateKey];
                    if (channelGroupUserStateValue == null)
                    {
                        jsonStateBuilder.AppendFormat("\"{0}\":{1}", channelGroupUserStateKey, string.Format("\"{0}\"", "null"));
                    }
                    else
                    {
                        jsonStateBuilder.AppendFormat("\"{0}\":{1}", channelGroupUserStateKey, (channelGroupUserStateValue.GetType().ToString() == "System.String") ? string.Format("\"{0}\"", channelGroupUserStateValue) : channelGroupUserStateValue);
                    }
                    if (keyIndex < channelGroupUserStateKeys.Length - 1)
                    {
                        jsonStateBuilder.Append(",");
                    }
                }
            }

            return jsonStateBuilder.ToString();
        }

        protected static string BuildJsonUserState(string[] channels, string[] channelGroups, bool local)
        {
            string retJsonUserState = "";

            StringBuilder jsonStateBuilder = new StringBuilder();

            if (channels != null && channels.Length > 0)
            {
                for (int index = 0; index < channels.Length; index++)
                {
                    string currentJsonState = BuildJsonUserState(channels[index].ToString(), "", local);
                    if (!string.IsNullOrEmpty(currentJsonState))
                    {
                        currentJsonState = string.Format("\"{0}\":{{{1}}}", channels[index].ToString(), currentJsonState);
                        if (jsonStateBuilder.Length > 0)
                        {
                            jsonStateBuilder.Append(",");
                        }
                        jsonStateBuilder.Append(currentJsonState);
                    }
                }
            }

            if (channelGroups != null && channelGroups.Length > 0)
            {
                for (int index = 0; index < channelGroups.Length; index++)
                {
                    string currentJsonState = BuildJsonUserState("", channelGroups[index].ToString(), local);
                    if (!string.IsNullOrEmpty(currentJsonState))
                    {
                        currentJsonState = string.Format("\"{0}\":{{{1}}}", channelGroups[index].ToString(), currentJsonState);
                        if (jsonStateBuilder.Length > 0)
                        {
                            jsonStateBuilder.Append(",");
                        }
                        jsonStateBuilder.Append(currentJsonState);
                    }
                }
            }

            if (jsonStateBuilder.Length > 0)
            {
                retJsonUserState = string.Format("{{{0}}}", jsonStateBuilder.ToString());
            }

            return retJsonUserState;
        }

        #region "Terminate requests and Timers"

        protected static void TerminatePendingWebRequest()
        {
            TerminatePendingWebRequest<object>(null);
        }

        protected static void TerminatePendingWebRequest<T>(RequestState<T> state)
        {
            if (state != null && state.Request != null)
            {
                if (state.Channels != null && state.Channels.Length > 0 && state.Channels[0] != null)
                {
                    string activeChannel = state.Channels[0].ToString(); //Assuming one channel exist, else will refactor later
                    PubnubChannelCallbackKey callbackKey = new PubnubChannelCallbackKey();
                    callbackKey.Channel = (state.ResponseType == ResponseType.Subscribe) ? activeChannel.Replace("-pnpres", "") : activeChannel;
                    callbackKey.ResponseType = state.ResponseType;

                    if (ChannelCallbacks.Count > 0 && ChannelCallbacks.ContainsKey(callbackKey))
                    {
                        object callbackObject;
                        bool channelAvailable = ChannelCallbacks.TryGetValue(callbackKey, out callbackObject);
                        if (channelAvailable)
                        {
                            if (state.ResponseType == ResponseType.Presence)
                            {
                                PubnubPresenceChannelCallback currentPubnubCallback = callbackObject as PubnubPresenceChannelCallback;
                                if (currentPubnubCallback != null && currentPubnubCallback.ErrorCallback != null)
                                {
                                    state.Request.Abort(currentPubnubCallback.ErrorCallback, _errorLevel);
                                }
                            }
                            else
                            {
                                PubnubSubscribeChannelCallback<T> currentPubnubCallback = callbackObject as PubnubSubscribeChannelCallback<T>;
                                if (currentPubnubCallback != null && currentPubnubCallback.ErrorCallback != null)
                                {
                                    state.Request.Abort(currentPubnubCallback.ErrorCallback, _errorLevel);
                                }
                            }
                        }
                    }
                }
                if (state.ChannelGroups != null && state.ChannelGroups.Length > 0 && state.ChannelGroups[0] != null)
                {
                    string activeChannelGroup = state.ChannelGroups[0].ToString(); //Assuming one channel exist, else will refactor later
                    PubnubChannelGroupCallbackKey callbackKey = new PubnubChannelGroupCallbackKey();
                    callbackKey.ChannelGroup = (state.ResponseType == ResponseType.Subscribe) ? activeChannelGroup.Replace("-pnpres", "") : activeChannelGroup;
                    callbackKey.ResponseType = state.ResponseType;

                    if (ChannelGroupCallbacks.Count > 0 && ChannelGroupCallbacks.ContainsKey(callbackKey))
                    {
                        object callbackObject;
                        bool channelAvailable = ChannelGroupCallbacks.TryGetValue(callbackKey, out callbackObject);
                        if (channelAvailable)
                        {
                            if (state.ResponseType == ResponseType.Presence)
                            {
                                PubnubPresenceChannelGroupCallback currentPubnubCallback = callbackObject as PubnubPresenceChannelGroupCallback;
                                if (currentPubnubCallback != null && currentPubnubCallback.ErrorCallback != null)
                                {
                                    state.Request.Abort(currentPubnubCallback.ErrorCallback, _errorLevel);
                                }
                            }
                            else
                            {
                                PubnubSubscribeChannelGroupCallback<T> currentPubnubCallback = callbackObject as PubnubSubscribeChannelGroupCallback<T>;
                                if (currentPubnubCallback != null && currentPubnubCallback.ErrorCallback != null)
                                {
                                    state.Request.Abort(currentPubnubCallback.ErrorCallback, _errorLevel);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                ICollection<string> keyCollection = ChannelRequest.Keys;
                foreach (string key in keyCollection)
                {
                    PubnubWebRequest currentRequest = ChannelRequest[key];
                    if (currentRequest != null)
                    {
                        TerminatePendingWebRequest(currentRequest, null);
                    }
                }
            }
        }

        protected static void TerminatePendingWebRequest(PubnubWebRequest request, Action<PubnubClientError> errorCallback)
        {
            if (request != null)
            {
                request.Abort(errorCallback, _errorLevel);
            }
        }

        private void RemoveChannelDictionary()
        {
            RemoveChannelDictionary<object>(null);
        }

        private void RemoveChannelDictionary<T>(RequestState<T> state)
        {
            if (state != null && state.Request != null)
            {
                string channel = (state.Channels != null) ? string.Join(",", state.Channels) : ",";

                if (ChannelRequest.ContainsKey(channel))
                {
                    PubnubWebRequest removedRequest;
                    bool removeKey = ChannelRequest.TryRemove(channel, out removedRequest);
                    if (removeKey)
                    {
                        LoggingMethod.WriteToLog(string.Format("DateTime {0} Remove web request from dictionary in RemoveChannelDictionary for channel= {1}", DateTime.Now.ToString(), channel), LoggingMethod.LevelInfo);
                    }
                    else
                    {
                        LoggingMethod.WriteToLog(string.Format("DateTime {0} Unable to remove web request from dictionary in RemoveChannelDictionary for channel= {1}", DateTime.Now.ToString(), channel), LoggingMethod.LevelError);
                    }
                }
            }
            else
            {
                ICollection<string> keyCollection = ChannelRequest.Keys;
                if (keyCollection != null && keyCollection.Count > 0)
                {
                    List<string> keysList = keyCollection.ToList();
                    foreach (string key in keysList)
                    {
                        PubnubWebRequest currentRequest = ChannelRequest[key];
                        if (currentRequest != null)
                        {
                            bool removeKey = ChannelRequest.TryRemove(key, out currentRequest);
                            if (removeKey)
                            {
                                LoggingMethod.WriteToLog(string.Format("DateTime {0} Remove web request from dictionary in RemoveChannelDictionary for channel= {1}", DateTime.Now.ToString(), key), LoggingMethod.LevelInfo);
                            }
                            else
                            {
                                LoggingMethod.WriteToLog(string.Format("DateTime {0} Unable to remove web request from dictionary in RemoveChannelDictionary for channel= {1}", DateTime.Now.ToString(), key), LoggingMethod.LevelError);
                            }
                        }
                    }
                }
            }
        }

        protected void RemoveChannelCallback<T>(string channel, ResponseType type)
        {
            string[] arrChannels = channel.Split(',');
            if (arrChannels != null && arrChannels.Length > 0)
            {
                foreach (string arrChannel in arrChannels)
                {
                    PubnubChannelCallbackKey callbackKey = new PubnubChannelCallbackKey();
                    callbackKey.Channel = arrChannel;
                    switch (type)
                    {
                        case ResponseType.Unsubscribe:
                            callbackKey.ResponseType = ResponseType.Subscribe;
                            break;
                        case ResponseType.PresenceUnsubscribe:
                            callbackKey.ResponseType = ResponseType.Presence;
                            break;
                        default:
                            callbackKey.ResponseType = ResponseType.Time; //overriding the default
                            break;
                    }

                    if (ChannelCallbacks.Count > 0 && ChannelCallbacks.ContainsKey(callbackKey))
                    {
                        if (type == ResponseType.Presence)
                        {
                            PubnubPresenceChannelCallback currentPubnubCallback = ChannelCallbacks[callbackKey] as PubnubPresenceChannelCallback;
                            if (currentPubnubCallback != null)
                            {
                                currentPubnubCallback.PresenceRegularCallback = null;
                                currentPubnubCallback.ConnectCallback = null;
                            }
                        }
                        else
                        {
                            PubnubSubscribeChannelCallback<T> currentPubnubCallback = ChannelCallbacks[callbackKey] as PubnubSubscribeChannelCallback<T>;
                            if (currentPubnubCallback != null)
                            {
                                currentPubnubCallback.SubscribeRegularCallback = null;
                                currentPubnubCallback.ConnectCallback = null;
                            }
                        }
                    }

                }
            }

        }

        protected void RemoveChannelCallback()
        {
            ICollection<PubnubChannelCallbackKey> channelCollection = ChannelCallbacks.Keys;
            if (channelCollection != null && channelCollection.Count > 0)
            {
                List<PubnubChannelCallbackKey> channelList = channelCollection.ToList();
                foreach (PubnubChannelCallbackKey keyChannel in channelList)
                {
                    if (ChannelCallbacks.ContainsKey(keyChannel))
                    {
                        object tempChannelCallback;
                        bool removeKey = ChannelCallbacks.TryRemove(keyChannel, out tempChannelCallback);
                        if (removeKey)
                        {
                            LoggingMethod.WriteToLog(string.Format("DateTime {0} RemoveChannelCallback from dictionary in RemoveChannelCallback for channel= {1}", DateTime.Now.ToString(), removeKey), LoggingMethod.LevelInfo);
                        }
                        else
                        {
                            LoggingMethod.WriteToLog(string.Format("DateTime {0} Unable to RemoveChannelCallback from dictionary in RemoveChannelCallback for channel= {1}", DateTime.Now.ToString(), removeKey), LoggingMethod.LevelError);
                        }
                    }
                }
            }
        }

        protected void RemoveChannelGroupCallback<T>(string channelGroup, ResponseType type)
        {
            string[] arrChannelGroups = channelGroup.Split(',');
            if (arrChannelGroups != null && arrChannelGroups.Length > 0)
            {
                foreach (string arrChannelGroup in arrChannelGroups)
                {
                    PubnubChannelGroupCallbackKey callbackKey = new PubnubChannelGroupCallbackKey();
                    callbackKey.ChannelGroup = arrChannelGroup;
                    switch (type)
                    {
                        case ResponseType.Unsubscribe:
                            callbackKey.ResponseType = ResponseType.Subscribe;
                            break;
                        case ResponseType.PresenceUnsubscribe:
                            callbackKey.ResponseType = ResponseType.Presence;
                            break;
                        default:
                            callbackKey.ResponseType = ResponseType.Time; //overriding the default
                            break;
                    }

                    if (ChannelGroupCallbacks.Count > 0 && ChannelGroupCallbacks.ContainsKey(callbackKey))
                    {
                        if (type == ResponseType.Presence)
                        {
                            PubnubPresenceChannelGroupCallback currentPubnubCallback = ChannelGroupCallbacks[callbackKey] as PubnubPresenceChannelGroupCallback;
                            if (currentPubnubCallback != null)
                            {
                                currentPubnubCallback.PresenceRegularCallback = null;
                                currentPubnubCallback.ConnectCallback = null;
                            }
                        }
                        else
                        {
                            PubnubSubscribeChannelGroupCallback<T> currentPubnubCallback = ChannelGroupCallbacks[callbackKey] as PubnubSubscribeChannelGroupCallback<T>;
                            if (currentPubnubCallback != null)
                            {
                                currentPubnubCallback.SubscribeRegularCallback = null;
                                currentPubnubCallback.ConnectCallback = null;
                            }
                        }
                    }

                }
            }

        }

        protected void RemoveChannelGroupCallback()
        {
            ICollection<PubnubChannelGroupCallbackKey> channelGroupCollection = ChannelGroupCallbacks.Keys;
            if (channelGroupCollection != null && channelGroupCollection.Count > 0)
            {
                List<PubnubChannelGroupCallbackKey> channelGroupCallbackList = channelGroupCollection.ToList();
                foreach (PubnubChannelGroupCallbackKey keyChannelGroup in channelGroupCallbackList)
                {
                    if (ChannelGroupCallbacks.ContainsKey(keyChannelGroup))
                    {
                        object tempChannelGroupCallback;
                        bool removeKey = ChannelGroupCallbacks.TryRemove(keyChannelGroup, out tempChannelGroupCallback);
                        if (removeKey)
                        {
                            LoggingMethod.WriteToLog(string.Format("DateTime {0} RemoveChannelGroupCallback from dictionary in RemoveChannelGroupCallback for channelgroup= {1}", DateTime.Now.ToString(), keyChannelGroup), LoggingMethod.LevelInfo);
                        }
                        else
                        {
                            LoggingMethod.WriteToLog(string.Format("DateTime {0} Unable to RemoveChannelGroupCallback from dictionary in RemoveChannelGroupCallback for channelgroup= {1}", DateTime.Now.ToString(), keyChannelGroup), LoggingMethod.LevelError);
                        }
                    }
                }
            }
        }

        private void RemoveUserState()
        {
            ICollection<string> channelLocalUserStateCollection = ChannelLocalUserState.Keys;
            ICollection<string> channelUserStateCollection = ChannelUserState.Keys;

            ICollection<string> channelGroupLocalUserStateCollection = ChannelGroupLocalUserState.Keys;
            ICollection<string> channelGroupUserStateCollection = ChannelGroupUserState.Keys;

            if (channelLocalUserStateCollection != null && channelLocalUserStateCollection.Count > 0)
            {
                List<string> channelLocalStateList = channelLocalUserStateCollection.ToList();
                foreach (string key in channelLocalStateList)
                {
                    if (ChannelLocalUserState.ContainsKey(key))
                    {
                        Dictionary<string, object> tempUserState;
                        bool removeKey = ChannelLocalUserState.TryRemove(key, out tempUserState);
                        if (removeKey)
                        {
                            LoggingMethod.WriteToLog(string.Format("DateTime {0} RemoveUserState from local user state dictionary for channel= {1}", DateTime.Now.ToString(), key), LoggingMethod.LevelInfo);
                        }
                        else
                        {
                            LoggingMethod.WriteToLog(string.Format("DateTime {0} Unable to RemoveUserState from local user state dictionary for channel= {1}", DateTime.Now.ToString(), key), LoggingMethod.LevelError);
                        }
                    }
                }
            }

            if (channelUserStateCollection != null && channelUserStateCollection.Count > 0)
            {
                List<string> channelStateList = channelUserStateCollection.ToList();
                foreach (string key in channelStateList)
                {
                    if (ChannelUserState.ContainsKey(key))
                    {
                        Dictionary<string, object> tempUserState;
                        bool removeKey = ChannelUserState.TryRemove(key, out tempUserState);
                        if (removeKey)
                        {
                            LoggingMethod.WriteToLog(string.Format("DateTime {0} RemoveUserState from user state dictionary for channel= {1}", DateTime.Now.ToString(), key), LoggingMethod.LevelInfo);
                        }
                        else
                        {
                            LoggingMethod.WriteToLog(string.Format("DateTime {0} Unable to RemoveUserState from user state dictionary for channel= {1}", DateTime.Now.ToString(), key), LoggingMethod.LevelError);
                        }
                    }
                }
            }

            if (channelGroupLocalUserStateCollection != null && channelGroupLocalUserStateCollection.Count > 0)
            {
                List<string> channelGroupLocalStateList = channelGroupLocalUserStateCollection.ToList();
                foreach (string key in channelGroupLocalStateList)
                {
                    if (ChannelGroupLocalUserState.ContainsKey(key))
                    {
                        Dictionary<string, object> tempUserState;
                        bool removeKey = ChannelGroupLocalUserState.TryRemove(key, out tempUserState);
                        if (removeKey)
                        {
                            LoggingMethod.WriteToLog(string.Format("DateTime {0} RemoveUserState from local user state dictionary for channelgroup= {1}", DateTime.Now.ToString(), key), LoggingMethod.LevelInfo);
                        }
                        else
                        {
                            LoggingMethod.WriteToLog(string.Format("DateTime {0} Unable to RemoveUserState from local user state dictionary for channelgroup= {1}", DateTime.Now.ToString(), key), LoggingMethod.LevelError);
                        }
                    }
                }
            }


            if (channelGroupUserStateCollection != null && channelGroupUserStateCollection.Count > 0)
            {
                List<string> channelGroupStateList = channelGroupUserStateCollection.ToList();

                foreach (string key in channelGroupStateList)
                {
                    if (ChannelGroupUserState.ContainsKey(key))
                    {
                        Dictionary<string, object> tempUserState;
                        bool removeKey = ChannelGroupUserState.TryRemove(key, out tempUserState);
                        if (removeKey)
                        {
                            LoggingMethod.WriteToLog(string.Format("DateTime {0} RemoveUserState from user state dictionary for channelgroup= {1}", DateTime.Now.ToString(), key), LoggingMethod.LevelInfo);
                        }
                        else
                        {
                            LoggingMethod.WriteToLog(string.Format("DateTime {0} Unable to RemoveUserState from user state dictionary for channelgroup= {1}", DateTime.Now.ToString(), key), LoggingMethod.LevelError);
                        }
                    }
                }
            }

        }

        protected void TerminatePresenceHeartbeatTimer()
        {
            if (presenceHeartbeatTimer != null)
            {
                presenceHeartbeatTimer.Dispose();
                presenceHeartbeatTimer = null;
            }

        }

        protected virtual void TerminateLocalClientHeartbeatTimer()
        {
            TerminateLocalClientHeartbeatTimer(null);
        }

        protected virtual void TerminateLocalClientHeartbeatTimer(Uri requestUri)
        {
            if (requestUri != null)
            {
                if (channelLocalClientHeartbeatTimer.ContainsKey(requestUri))
                {
                    Timer requestHeatbeatTimer = null;
                    if (channelLocalClientHeartbeatTimer.TryGetValue(requestUri, out requestHeatbeatTimer) && requestHeatbeatTimer != null)
                    {
                        try
                        {
                            requestHeatbeatTimer.Change(
                                (-1 == pubnubNetworkTcpCheckIntervalInSeconds) ? -1 : pubnubNetworkTcpCheckIntervalInSeconds * 1000,
                                (-1 == pubnubNetworkTcpCheckIntervalInSeconds) ? -1 : pubnubNetworkTcpCheckIntervalInSeconds * 1000);
                            requestHeatbeatTimer.Dispose();
                        }
                        catch (ObjectDisposedException ex)
                        {
                            //Known exception to be ignored
                        }

                        Timer removedTimer = null;
                        bool removed = channelLocalClientHeartbeatTimer.TryRemove(requestUri, out removedTimer);
                        if (removed)
                        {
                            LoggingMethod.WriteToLog(string.Format("DateTime {0} Remove local client heartbeat reference from collection for {1}", DateTime.Now.ToString(), requestUri.ToString()), LoggingMethod.LevelInfo);
                        }
                        else
                        {
                            LoggingMethod.WriteToLog(string.Format("DateTime {0} Unable to remove local client heartbeat reference from collection for {1}", DateTime.Now.ToString(), requestUri.ToString()), LoggingMethod.LevelInfo);
                        }
                    }
                }
            }
            else
            {
                ConcurrentDictionary<Uri, Timer> timerCollection = channelLocalClientHeartbeatTimer;
                ICollection<Uri> keyCollection = timerCollection.Keys;
                if (keyCollection != null && keyCollection.Count > 0)
                {
                    List<Uri> keyList = keyCollection.ToList();
                    foreach (Uri key in keyList)
                    {
                        if (channelLocalClientHeartbeatTimer.ContainsKey(key))
                        {
                            Timer currentTimer = null;
                            if (channelLocalClientHeartbeatTimer.TryGetValue(key, out currentTimer) && currentTimer != null)
                            {
                                currentTimer.Dispose();
                                Timer removedTimer = null;
                                bool removed = channelLocalClientHeartbeatTimer.TryRemove(key, out removedTimer);
                                if (!removed)
                                {
                                    LoggingMethod.WriteToLog(string.Format("DateTime {0} TerminateLocalClientHeartbeatTimer(null) - Unable to remove local client heartbeat reference from collection for {1}", DateTime.Now.ToString(), key.ToString()), LoggingMethod.LevelInfo);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void TerminateReconnectTimer()
        {
            ConcurrentDictionary<string, Timer> channelReconnectCollection = ChannelReconnectTimer;
            ICollection<string> keyCollection = channelReconnectCollection.Keys;
            if (keyCollection != null && keyCollection.Count > 0)
            {
                List<string> keyList = keyCollection.ToList();
                foreach (string key in keyList)
                {
                    if (ChannelReconnectTimer.ContainsKey(key))
                    {
                        Timer currentTimer = ChannelReconnectTimer[key];
                        currentTimer.Dispose();
                        Timer removedTimer = null;
                        bool removed = ChannelReconnectTimer.TryRemove(key, out removedTimer);
                        if (!removed)
                        {
                            LoggingMethod.WriteToLog(string.Format("DateTime {0} TerminateReconnectTimer(null) - Unable to remove channel reconnect timer reference from collection for {1}", DateTime.Now.ToString(), key.ToString()), LoggingMethod.LevelInfo);
                        }
                    }
                }
            }


            ConcurrentDictionary<string, Timer> channelGroupReconnectCollection = ChannelGroupReconnectTimer;
            ICollection<string> groupKeyCollection = channelGroupReconnectCollection.Keys;
            if (groupKeyCollection != null && groupKeyCollection.Count > 0)
            {
                List<string> groupKeyList = groupKeyCollection.ToList();
                foreach (string groupKey in groupKeyList)
                {
                    if (ChannelGroupReconnectTimer.ContainsKey(groupKey))
                    {
                        Timer currentTimer = ChannelGroupReconnectTimer[groupKey];
                        currentTimer.Dispose();
                        Timer removedTimer = null;
                        bool removed = ChannelGroupReconnectTimer.TryRemove(groupKey, out removedTimer);
                        if (!removed)
                        {
                            LoggingMethod.WriteToLog(string.Format("DateTime {0} TerminateReconnectTimer(null) - Unable to remove channelgroup reconnect timer reference from collection for {1}", DateTime.Now.ToString(), groupKey.ToString()), LoggingMethod.LevelInfo);
                        }
                    }
                }
            }
        }

        protected bool DeleteLocalChannelUserState(string channel)
        {
            bool userStateDeleted = false;

            if (ChannelLocalUserState.ContainsKey(channel))
            {
                Dictionary<string, object> returnedUserState = null;
                userStateDeleted = ChannelLocalUserState.TryRemove(channel, out returnedUserState);
            }

            return userStateDeleted;
        }

        protected bool DeleteLocalChannelGroupUserState(string channelGroup)
        {
            bool userStateDeleted = false;

            if (ChannelGroupLocalUserState.ContainsKey(channelGroup))
            {
                Dictionary<string, object> returnedUserState = null;
                userStateDeleted = ChannelGroupLocalUserState.TryRemove(channelGroup, out returnedUserState);
            }

            return userStateDeleted;
        }

        public void EndPendingRequests()
        {
            RemoveChannelDictionary();
            TerminatePendingWebRequest();
            TerminateLocalClientHeartbeatTimer();
            TerminateReconnectTimer();
            RemoveChannelCallback();
            RemoveChannelGroupCallback();
            RemoveUserState();
            TerminatePresenceHeartbeatTimer();
        }

        public static void TerminateCurrentSubscriberRequest()
        {
            string[] channels = GetCurrentSubscriberChannels();
            if (channels != null)
            {
                string multiChannel = (channels.Length > 0) ? string.Join(",", channels) : ",";
                PubnubWebRequest request = (ChannelRequest.ContainsKey(multiChannel)) ? ChannelRequest[multiChannel] : null;
                if (request != null)
                {
                    request.Abort(null, _errorLevel);
                    LoggingMethod.WriteToLog(string.Format("DateTime {0} TerminateCurrentSubsciberRequest {1}", DateTime.Now.ToString(), request.RequestUri.ToString()), LoggingMethod.LevelInfo);
                }
            }
        }

        #endregion

    }
}
