using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace RDACRMIcs
{
    class TCPServer : IDisposable
    {
        private object disposed_locker = new object();
        private volatile bool disposed = false;

        private String receive_password = "d$)Jww3KD9dTk@h+Jjre6$JZjQR8?0@Y9l#LFzjRFcWg#K8GijpWZxF9*h74rUj0";
        private Guid receive_security = Guid.Parse("8871C221-77F6-4ACC-80E5-D89C9BFBFC65");

        private String response_password = "S*P-dFF$$svOIowC-N-$9A1zATei(=)MATuv70kLI4396Cioi8-C2zbBozkMH!%v";
        private Guid response_security = Guid.Parse("CECE9755-6750-4DB1-BE3D-54478F4F3BEC");

        private TcpListener tcp_listener = null;
        private int buffer_size = 64*1024;
        
        private AutoResetEvent queue_event = new AutoResetEvent(false);
        private ConcurrentQueue<String> queue = new ConcurrentQueue<String>();
        private Thread producer = null;
        private ArrayList consumer = null;
        private TimeSpan join_timeout = TimeSpan.FromSeconds(3);

        public void start()
        {
            lock (disposed_locker)
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("Object is destroyed");
                }
            }

            Options options = Singleton<Options>.Instance;
            {
                int current_port = Int32.Parse(options.getValue("current_port"));
                tcp_listener = new TcpListener(IPAddress.Any, current_port);

                producer = new Thread(producer_);
                consumer = new ArrayList();

                int threads_count = Int32.Parse(options.getValue("threads_count"));
                for (int iter = 0; iter < threads_count; ++iter)
                {
                    consumer.Add( new Thread(consumer_) );
                }
            }

            {
                foreach (Thread consumer_item in consumer)
                {
                    consumer_item.Start();
                }

                tcp_listener.Start();
                producer.Start();
            }
        }

        public void stop()
        {
            lock (disposed_locker)
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("Object is destroyed");
                }
            }

            producer.Interrupt();
            if (!producer.Join(join_timeout))
            {
                producer.Abort();
            }

            tcp_listener.Stop();

            foreach (Thread consumer_item in consumer)
            {
                consumer_item.Interrupt();
                if (!consumer_item.Join(join_timeout))
                {
                    consumer_item.Abort();
                }
            }
        }

        private void producer_(){
            try
            {
                while (true)
                {
                    using (TcpClient tcp_client = tcp_listener.AcceptTcpClient())
                    {
                        using (var networkStream = tcp_client.GetStream())
                        {
                            byte[] buffer = new byte[buffer_size];
                            List<byte> data = new List<byte>();
                            int byteCount = 0;
                            while ((byteCount = networkStream.Read(buffer, 0, buffer.Length)) != 0)
                            {
                                data.AddRange(buffer.Take(byteCount));
                            }

                            {
                                String data_ = System.Text.Encoding.UTF8.GetString(data.ToArray());
                                queue.Enqueue(data_);
                                queue_event.Set();
                            }
                        }
                    }
                }
            }
            catch (ThreadInterruptedException)
            {

            }
            catch (ThreadAbortException)
            {

            }
            catch (Exception ex)
            {
                Log.error("TCPServer::producer_()", ex);
            }
        }

        private void consumer_()
        {
            try
            {
                while (true)
                {
                    String entry;
                    while (queue.TryDequeue(out entry))
                    {
                        processing(entry);
                    }
                    queue_event.WaitOne();
                }
            }
            catch (ThreadInterruptedException)
            {

            }
            catch (ThreadAbortException)
            {

            }
            catch (Exception ex)
            {
                Log.error("TCPServer::consumer_()", ex);
            }
        }

        private void processing(String receive_encript_xml)
        {
            try
            {
                String receive_decrypt_xml = Cryptography.DecryptAesManaged(receive_encript_xml, receive_password);
                ReceiveData receive_data = XML.Deserialize<ReceiveData>(receive_decrypt_xml);
                if (receive_security.Equals(receive_data.security))
                {
                    throw new InvalidDataException(String.Format("Invalid security tokken: {0}", receive_data.security));
                }

                ResponseData response_data = scriptExecute(receive_data.script);
                {
                    response_data.id = receive_data.id;
                    response_data.security = response_security;
                }

                String response_decrypt_xml = XML.Serialize<ResponseData>(response_data);
                String response_encrypt_xml = Cryptography.EncryptAesManaged(response_decrypt_xml, response_password);

                Options options = Singleton<Options>.Instance;
                String response_url = String.Format("http://{0}:{1}{2}",
                                      options.getValue("server_host"),
                                      options.getValue("server_port"),
                                      options.getValue("customer_path"));
                if (!sendData(response_url, response_encrypt_xml))
                {
                    throw new InvalidOperationException("Invalid send data");
                }
            }
            catch (Exception ex)
            {
                Log.error("TCPServer::processing()", ex);
            }
        }

        private ResponseData scriptExecute(String script)
        {
            ResponseData response_data = new ResponseData();
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    ScriptEngine engine = Python.CreateEngine();
                    ScriptRuntime runtime = engine.Runtime;
                    {
                        runtime.IO.SetOutput(ms, Encoding.UTF8);
                    }

                    ScriptSource source = engine.CreateScriptSourceFromString(script, SourceCodeKind.AutoDetect);
                    ScriptScope scope = runtime.CreateScope();
                    {
                        scope.SetVariable("result", "success");
                    }

                    CompiledCode compil = source.Compile();
                    object scope_result = compil.Execute(scope);
                    {
                        response_data.result = scope.GetVariable<String>("result");
                        response_data.scope = scope_result.ToString();

                        byte[] arr_byte = ms.ToArray();
                        response_data.console = Encoding.UTF8.GetString(arr_byte);
                    }
                }
            }
            catch (IronPython.Runtime.Exceptions.RuntimeException ex)
            {
                response_data.result = Log.getExceptionMessage(ex);
                Log.error("TCPServer::scriptExecute()", ex);
            }
            catch (Microsoft.Scripting.SyntaxErrorException ex)
            {
                response_data.result = Log.getExceptionMessage(ex);
                Log.error("TCPServer::scriptExecute()", ex);
            }

            return response_data;
        }

        private Boolean sendData(String url, String data)
        {
            try
            {
                HttpWebRequest request = null;
                {//Request
                    request = (HttpWebRequest)WebRequest.Create(url);

                    //CookieCollection cookies = new CookieCollection();
                    //request.CookieContainer = new CookieContainer();
                    //request.CookieContainer.Add(cookies); //recover cookies First request
                    request.Method = WebRequestMethods.Http.Post;

                    request.UserAgent = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2";
                    request.AllowWriteStreamBuffering = true;
                    request.ProtocolVersion = HttpVersion.Version11;
                    request.AllowAutoRedirect = false;
                    request.ContentType = "application/x-www-form-urlencoded";

                    byte[] bytes = Encoding.GetEncoding("windows-1251").GetBytes(data);
                    String data_ = Convert.ToBase64String(bytes);
                    byte[] bytes_ = Encoding.GetEncoding("windows-1251").GetBytes(data_);

                    request.Credentials = CredentialCache.DefaultCredentials;
                    request.ContentLength = bytes_.Length;

                    var newStream = request.GetRequestStream();
                    {
                        newStream.Write(bytes_, 0, bytes_.Length);
                        newStream.Close();
                    }
                }

                if (request != null)
                {//Responce
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    if (response != null)
                    {
                        StreamReader strreader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                        String responseToString = strreader.ReadToEnd();
                        if (responseToString.Equals("success") != true)
                        {
                            throw new Exception(String.Format("Responce: {0}", responseToString));
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.error("TCPServer::sendData()", ex);

                return false;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                lock (disposed_locker)
                {
                    if (!disposed)
                    {
                        if (disposing)
                        {
                            queue_event.Dispose();
                        }

                        disposed = true;
                    }
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~TCPServer()
        {
            Dispose(false);
        }
        
    }
}
