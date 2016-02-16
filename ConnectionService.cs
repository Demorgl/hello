using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StorageService;
using TypesLib;
using System.Threading;
using Castle.Core.Logging;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Net;
using System.IO;

namespace ConnectionService
{
    public class ConnectionService : IConnectionService
    {
        #region Атрибуты сервиса

        public ILogger Logger { get; set; }

        public ITerminal terminal { get; set; }
        public IStorageService store { get; set; }

        private EmercomModuleService remoteServer;

        Thread statusThread;
        Thread connectThread;
        Thread refreshThread;

        private TimeSpan statusThreadTimeout = new TimeSpan(0, 0, 0, 10);
        private TimeSpan refreshThreadTimeout = new TimeSpan(0, 0, 0, 30);

        private int threadLatch;
        private object lockObject = new Object();
        private void IncLatch()
        {
            lock (lockObject)
            {
                ++threadLatch;
                //Logger.DebugFormat("Latch incremented. count = {0}", threadLatch);
            }
        }
        private void DecLatch()
        {
            lock (lockObject)
            {
                --threadLatch;
                //Logger.DebugFormat("Latch decremented. count = {0}", threadLatch);
            }
        }
        private bool stopWorkThreads;
        private void StopWorkThreads(bool value)
        {
            lock (lockObject)
            {
                stopWorkThreads = value;
                /*if (value)
                    Logger.DebugFormat("Threads canceled");
                else
                    Logger.DebugFormat("Threads started");*/
            }
        }
        #endregion

        #region Конструктор
        public ConnectionService()
        {
            threadLatch = 0;
            stopWorkThreads = true;
        }

        //Запуск потоков сервиса
        public void restartService()
        {
            Logger.DebugFormat("ConnectionService restarted");

            //Запускаем поток для попыток установления соединения
            connectThread = new Thread(new ThreadStart(connectThreadExecutor));
            connectThread.Name = "connectThread";
            connectThread.IsBackground = true;
            connectThread.Start();
        }

        private void startAll()
        {
            while (threadLatch != 0)
                Thread.Sleep(1000);
            /*
            if (connect())
            {*/
                StopWorkThreads(false);
                //Запускаем поток для уведомления сервера
                if (statusThread == null || !statusThread.IsAlive)
                {
                    statusThread = new Thread(new ThreadStart(activeStatusThreadExecutor));
                    statusThread.Name = "statusThread";
                    statusThread.IsBackground = true;
                    statusThread.Start();
                }
                //Запускаем поток для обновления хранилища
                if (refreshThread == null || !refreshThread.IsAlive)
                {
                    refreshThread = new Thread(new ThreadStart(refreshThreadExecutor));
                    refreshThread.Name = "refreshThread";
                    refreshThread.IsBackground = true;
                    refreshThread.Start();
                }

                //Запускаем поток для попыток установления соединения
                connectThread = new Thread(new ThreadStart(connectThreadExecutor));
                connectThread.Name = "connectThread";
                connectThread.IsBackground = true;
                connectThread.Start();
            //}
        }
        #endregion

        #region потоки службы
        protected void activeStatusThreadExecutor()
        {
            int id = new Random().Next(65536);
            Logger.DebugFormat("ConnectionService activeStatusThreadExecutor[{0}] started", id);

            //Отправляем серверу текущий статус
            while (true)
            {
                if (stopWorkThreads)
                    break;

                IncLatch();
                if (!sendClientStatus())
                {
                    Logger.DebugFormat("activeStatusThreadExecutor[{0}] stop all work threads", id);
                    StopWorkThreads(true);
                }
                DecLatch();

                Thread.Sleep(statusThreadTimeout);
            }
            Logger.DebugFormat("ConnectionService activeStatusThreadExecutor[{0}] stoped", id);
        }

        protected void refreshThreadExecutor()
        {
            int id = new Random().Next(65536);
            Logger.DebugFormat("ConnectionService refreshThreadExecutor[{0}] started", id);

            //Обновляем данные в кеше
            while (true)
            {
                if (stopWorkThreads)
                    break;

                IncLatch();
                if (!requestPattern(terminal.Id))
                {
                    Logger.DebugFormat("refreshThreadExecutor[{0}] stop all work threads", id);
                    StopWorkThreads(true);
                }
                DecLatch();

                Thread.Sleep(refreshThreadTimeout);
            }
            Logger.DebugFormat("ConnectionService refreshThreadExecutor[{0}] stoped", id);
        }

        protected void connectThreadExecutor()
        {
            int id = new Random().Next(65536);
            Logger.DebugFormat("ConnectionService connectThreadExecutor[{0}] started", id);

            while (!stopWorkThreads)
            {
                Thread.Sleep(5000);
            }

            //Пытаемся соединиться с сервером
            while (!connect())
            {
                //TO-DO переместить в параметр
                Thread.Sleep(5000);
            }

            //Соедниться удалось - перезапускаем
            startAll();
            Logger.DebugFormat("ConnectionService connectThreadExecutor[{0}] stoped", id);
        }

        #endregion

        #region Функции
        //Соединение с сервером
        public bool connect()
        {
            Logger.DebugFormat("Connect to {0}", terminal.ServiceUrl);
            try
            {
                if (remoteServer != null)
                    remoteServer.Dispose();

                Logger.DebugFormat("create remote service wrapper instance");
                remoteServer = new EmercomModuleService();
                remoteServer.Url = terminal.ServiceUrl;

                Logger.DebugFormat("try to register terminal");
                //System.Net.ServicePointManager.Expect100Continue = false;

                //Регистрация терминала
                String result = remoteServer.registerClient(terminal.Name,
                                                            terminal.OS,
                                                            terminal.ResolutionWidth,
                                                            terminal.ResolutionHeight,
                                                            terminal.Size,
                                                            terminal.IP,
                                                            terminal.MAC,
                                                            terminal.ClientVersion,
                                                            terminal.Address,
                                                            String.Format("rtsp://{0}:{1}", terminal.IP, terminal.WebcamPath)
                                                            );

                Logger.DebugFormat("register result is {0}", result);

                //Устанавливаем ID терминала
                terminal.Id = result;

                return true;
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("failed to connect [{0}]", ex.Message);
                return false;
            }
        }

        //Уведомление о доступности терминала
        public bool sendClientStatus()
        {
            try
            {
                Logger.DebugFormat("Send terminal status");

                string value = remoteServer.sendClientStatus(terminal.Id, terminal.IP, terminal.MAC);

                if (value != null)
                {
                    if (int.Parse(value) == 1)
                        SendTerminalLog();
                    else if (int.Parse(value) == 2)
                        DeleteTerminalLog();
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("failed to send status [{0}]", ex.Message);
                return false;
            }
        }

        //Запрос шаблона
        public bool requestPattern(string terminalID)
        {
            try
            {
                PatternLite result = new PatternLite();
                Logger.DebugFormat("Request pattern for terminal with id {0}", terminalID);

                var pattern = remoteServer.getPatternByTerminalId(terminalID);

                if (pattern != null)
                {
                    result = s2c(pattern);

                    PatternLite existPattern = store.ReadPattern(result.Name);
                    if (existPattern == null || !existPattern.XML.ToString().Equals(result.XML.ToString()))
                    {
                        Logger.DebugFormat("Store pattern with id {0}", result.Name);
                        store.WritePattern(result, result.Name);
                    }

                    Logger.DebugFormat("Request contents for pattern {0}", result.Name);

                    String[] contents = remoteServer.getContentIdsByPatternId(result.Name);

                    foreach (var contentID in contents)
                    {
                        DateTime contentDate = remoteServer.getContentDateById(contentID);
                        
                        ContentLite existContent = store.ReadContentLite(contentID);

                        if (existContent == null || contentDate.ToUniversalTime().CompareTo(existContent.Date.ToUniversalTime()) != 0)
                        {
                            Logger.DebugFormat("Request content with id {0}", contentID);
                            //Если контент не был докачан, не помещаем его в базу
                            if (requestContentBinaryData(contentID))
                                requestContent(contentID);
                            else
                                requestContent(contentID, true); 
                        }
                    }
                    terminal.PatternName = result.Name;
                    Logger.DebugFormat("Requested contents for pattern {0} loaded", result.Name);
                }
                else
                {
                    Logger.DebugFormat("Pattern for terminal {0} not set by Administrator", terminal.Id);
                    return true;
                }
            }
            catch (Exception E)
            {
                Logger.ErrorFormat("Error in requestPattern [{0}]", E.Message);
                return false;
            }
            return true;
        }

        //Запрос контента
        public Content requestContent(string contentID)
        {
            Content result = new Content();

            VContentSDO content = remoteServer.getContentById(contentID);
            if (content != null)
            {
                result = s2c(content);

                Logger.DebugFormat("Store content with id {0}", content.Rn);
                store.WriteContent(result, result.ID);
            }

            return result;
        }

        //ЗАГЛУШКА, когда контент который хранится на HDD не докачивается, то контент в базу дописывается произвольной датой, чтобы показать что в след раз его надо будет пробовать еще раз скачать.
        private Content requestContent(string contentID, bool hasError)
        {
            Content result = new Content();

            VContentSDO content = remoteServer.getContentById(contentID);
            if (content != null)
            {
                result = s2c(content);
                result.Date = DateTime.Now;

                Logger.DebugFormat("Store content with random date id {0}", content.Rn);
                store.WriteContent(result, result.ID);
            }

            return result;
        }

        private bool requestContentBinaryData(string contentID)
        {
            bool result = true;
            int n = 3;
            int pos = terminal.ServiceUrl.TakeWhile(c => ((n -= (c == '/' ? 1 : 0))) > 0).Count();
            
            String reqAddr = String.Empty;
            if (terminal.CurrentApplication == ApplicationsEnum.SIN_PBJN)
                reqAddr = terminal.ServiceUrl.Substring(0, pos) + "/emercom/getbigdata?content_data_id=";
            else if (terminal.CurrentApplication == ApplicationsEnum.AIS_TOKGO)
                reqAddr = terminal.ServiceUrl.Substring(0, pos) + "/emercom_tutorial/getbigdata?content_data_id=";

            VContentDataSDO[] binContents = remoteServer.getBinaryDataRowsByContentId(contentID);
            if (binContents != null)
            {
                foreach (var binContent in binContents)
                {
                    WebClient webClient = new WebClient();
                    try
                    {
                        webClient.DownloadFile(reqAddr + binContent.Rn, Path.Combine(createContentBinaryDataPath(binContent.ContentRn.ToString()), binContent.Filename));
                    }
                    catch (Exception ex)
                    {
                        Logger.ErrorFormat("Error in store file {0} content {1}\nError mesage {2}", binContent.ContentRn, binContent.Filename, ex.InnerException);
                        result = false;
                    }
                }
            }

            return result;
        }

        private String createContentBinaryDataPath(String binaryID)
        { 
            DirectoryInfo dir = new DirectoryInfo(Path.Combine(terminal.BinaryDataPath, binaryID));

            if (!dir.Exists)
                dir.Create();

            return dir.ToString();
        }

        //Отправка статистики
        public void sendStatistic(/*string ID, object statistic*/)
        {
            Logger.DebugFormat("Send statistic");
            //remoteServer.sendClientStatistics(terminal.Id, "");
        }

        //Отправка чего?
        public void SendFeedback(string ID, string date, string text, string data, string callbackName, string callbackGender, string callbackPhone, string callbackEmail)
        {
            Logger.DebugFormat("Send feedback");
            remoteServer.sendClientFeedback(ID, date, text, data, callbackName, callbackGender, callbackPhone, callbackEmail);
        }

        public void SendWarning(string message)
        {
            //TO-DO
            //Реализовать отправку сообщений на сервер
        }
        #endregion

        #region Вспомогательные конверторы

        //Структура сервера в структуру клиента
        protected PatternLite s2c(VPatternSDO s)
        {
            PatternLite d = new PatternLite();

            try
            {
                d.XML = System.Text.Encoding.UTF8.GetString(s.PatternXml);
                d.Name = s.Rn.ToString();
            }
            catch (Exception E)
            {
                Logger.ErrorFormat("Error in s2c \n{0}", E.Message);
            }
            return d;
        }
        
        protected Content s2c(VContentSDO s)
        {
            Content d = new Content();

            try
            {
                d.Data = s.ValueClobStr;

                d.Date = s.DateAdded;
                d.ID = s.Rn.ToString();
                d.Name = s.Name;
                d.Note = s.Note;
                d.Type = s.TypeName;
            }
            catch (Exception E)
            {
                Logger.ErrorFormat("Error in s2c \n{0}", E.Message);
            }

            return d;
        }

        #endregion

        public void StopService()
        {
            Logger.DebugFormat("ConnectionService shutingdown");

            StopWorkThreads(true);

            if (connectThread != null)
                connectThread.Abort();

            if (statusThread != null)
                statusThread.Join();
            if (refreshThread != null)
                refreshThread.Join();
        }


        public int SendTerminalLog()
        {
            //TO-DO
            //Реализовать отправку сообщений на сервер
            LogList log = store.ReadLog();

            int result = 0;

            //result = remoteServer.sendClientLog(tmp);

            result = remoteServer.sendClientLogXML(log.GetLogAsXML(terminal.Id));

            if (result == 1)
                store.SetLogListSended(log);

            return result;
        }

        public void DeleteTerminalLog()
        {
            store.DeleteLog();
        }
    }
}
